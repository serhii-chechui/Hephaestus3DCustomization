# Hephaestus 3D Customization

Hephaestus 3D Customization is a part of the Hephaestus framework. It dresses 3D
characters in Unity: every outfit item is a separate prefab loaded on demand, and
skinned clothes are rebound to the character's skeleton so they animate with the body.

## Features

- One item per slot; equipping an item replaces the one in its slot.
- **Skinned items** (shirts, pants, shoes, hair) are rebound to the character's bones by
  name. The meshes keep the bone weights they were exported with, so they deform exactly
  like the body.
- **Socket items** (hats, watches, glasses) are parented to a socket on the character;
  a slot can have several sockets (left and right earring).
- **Material items** (skin tone, tattoos, make-up) change materials of body parts.
- **Custom items** bring their own attach strategy asset.
- **Body parts under clothes are hidden**, so the skin never pokes through when the body
  bends.
- Any asset source: Addressables, asset bundles, Resources or procedural content, behind
  the `IOutfitAssetProvider` interface.
- Asynchronous and race-safe: a newer request for a slot supersedes a pending one, and
  every loaded prefab is released exactly once.
- Loadouts: save what a character wears as item ids and restore it later.
- A default outfit that slots fall back to when their item is taken off.
- Items made on other rigs: bone aliases, case-insensitive and namespace-free bone matching.
- Replaceable attach strategies per attach mode.
- Optional mesh combining for crowds: a dressed character becomes one skinned mesh with
  atlased materials.
- No dependencies.

## How skinned items work

Model the clothes on the character's skeleton in your DCC tool (Maya, Blender) and export
each item with **its own copy of that skeleton**: same bone names, same rest pose. When
the item is equipped:

1. its prefab is instantiated under the character;
2. every `SkinnedMeshRenderer` of the item gets the character's bones with the same names
   (`bones` and `rootBone`); the mesh, its bone weights and bind poses stay untouched;
3. the item's own copy of the skeleton is destroyed, and so are `Animator` and `Animation`
   components on the item, which have nothing left to drive (set
   `SkinnedOutfitAttachStrategy.RemoveAnimators` to `false` to keep them).

The character's `Animator` now moves the item's vertices through the same bones as the
body. This works with Generic and Humanoid characters alike: a Humanoid avatar still drives
the character's own bone transforms, which the items are bound to. A bone the character doesn't have is bound to its closest ancestor that the
character has, with one warning per item that lists every such bone.

Import skinned items with **Rig > Animation Type** set to `Generic` (no avatar is needed).
With `None`, Unity imports them as static meshes and the item can't be rebound. **Avatar
Definition** `No Avatar` keeps Unity from adding an `Animator` to the item's root; items
imported with an avatar work too, since the strategy removes the `Animator`.

## Hiding the body under clothes

Tight clothes follow the body, but the skin under them still pokes through at bending
joints. Split the body mesh into parts that clothes cover (e.g. torso and legs, arms,
feet), one renderer each, and:

- create an **Outfit Body Part** asset per part;
- add `OutfitBodyPartRenderer` to each part's renderer and set its body part;
- list the parts every item covers in its **Hidden Body Parts**.

While at least one worn item hides a part, its renderers are turned off. A part should
only contain skin that every item hiding it covers completely; skin that some of those
items leave visible belongs in another part.

## Installation

The package is distributed via UPM from the WTFGames registry. Add the registry and the
package to your `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "WTFGames",
      "url": "https://upm.wtfgames.com.ua/",
      "scopes": ["com.wtfgames.hephaestus"]
    }
  ],
  "dependencies": {
    "com.wtfgames.hephaestus.3d.customization": "1.0.0"
  }
}
```

## Setup

### 1. Data

Create the assets via **Create > HephaestusMobile > 3D > Customization**:

- **Outfit Slot**: a place that holds one item (Head, Torso, Legs, Feet, ...).
- **Outfit Body Part**: a part of the body that clothes can cover.
- **Outfit Item**: the item's id (defaults to the asset name), slot, attach mode
  (`Skinned`, `Socket`, `Material` or `Custom`), asset key, which your provider resolves to
  the prefab (for Addressables: the address), and the body parts it hides. Socket items can
  name a socket id, custom items their strategy asset.
- **Outfit Item Catalog**: the items a saved loadout can refer to.
- **Outfit Preset**: a set of items equipped together, e.g. a default outfit. When several
  items share a slot, the last one wins.

### 2. Character

On the character's root:

- `OutfitSkeleton`: set **Root Bone** to the top bone of the skeleton.
- `OutfitWearer`: finds the `OutfitSkeleton` on the same object or its children. Its
  **Default Outfit** is optional (see below).
- For socket items, add an empty child under the bone the item should follow (e.g. a
  `HatSocket` under the head) with an `OutfitSocket` for the item's slot. The item's
  prefab keeps its local offset from the socket.
- Optionally, `OutfitBodyPartRenderer` on the body part renderers (see above).

### 3. Asset provider

Implement `IOutfitAssetProvider` over your loader, or import the **Addressables Provider**
sample. Give it to the wearer before equipping:

```csharp
wearer.Construct(provider);
```

With Zenject, call `Construct` from an `[Inject]` method; the Addressables sample has an
installer and an `OutfitWearerInjector` component for that.

## Usage

```csharp
public class Wardrobe
{
    private readonly IOutfitWearer _wearer;

    public Wardrobe(IOutfitWearer wearer) => _wearer = wearer;

    public async Task TryOn(OutfitItem item)
    {
        var equipped = await _wearer.EquipAsync(item);
        // false: a newer request for the slot replaced this one, or the load failed.
    }

    public void TakeOff(OutfitSlot slot) => _wearer.Unequip(slot);
}
```

- `EquipAsync(OutfitPreset)` equips a whole look.
- `Equipped` / `Unequipped` events report changes, including an item replaced by another.
- `Unequip` and `UnequipAll` also cancel pending requests.
- Requesting an item that is already loading into its slot returns that pending request
  instead of loading it again.
- A load that fails while its request is current faults the returned task; the failure of a
  request that a newer one superseded is ignored.
- Destroying the character releases every loaded prefab. Unity doesn't call `OnDestroy` on
  objects that were never active, so call `wearer.Dispose()` before destroying such a
  character; it takes every item off, releases the prefabs and cancels pending loads.

- `IsLoading(slot)` tells whether an item is on its way into a slot, e.g. to show a spinner;
  `EquipFailed` reports items a current request couldn't put on (load error, missing prefab,
  attach failure). Both are on `IOutfitLoadingStatus`, which `OutfitWearer` implements.

### Saving and restoring outfits

`GetLoadout()` returns the ids of the worn items; `OutfitLoadout` serializes with
`JsonUtility`. Both calls below are on `IOutfitLoadoutWearer`, which extends `IOutfitWearer`
and which `OutfitWearer` implements. `ApplyLoadoutAsync` makes the character wear exactly that loadout, resolving
ids through an `IOutfitItemCatalog`, e.g. an **Outfit Item Catalog** asset:

```csharp
PlayerPrefs.SetString("outfit", JsonUtility.ToJson(wearer.GetLoadout()));

var loadout = JsonUtility.FromJson<OutfitLoadout>(PlayerPrefs.GetString("outfit"));
await wearer.ApplyLoadoutAsync(loadout, catalog);
```

Give items explicit ids when their asset names may change; ids the catalog doesn't know
are skipped with a warning.

### Default outfit

Items of the wearer's **Default Outfit** are what slots fall back to (underwear, bare
feet): `Unequip(slot)` puts the slot's default item back on, `UnequipAll()` returns to the
default outfit, and `EquipDefaultOutfitAsync()` fills the empty slots, e.g. after spawning.
Unequipping a slot that already wears its default item leaves the slot empty.

### Items from other rigs

Skinned items bind to bones with the same names. When items come from a rig with other
names, set on `OutfitSkeleton`:

- **Bone Aliases**: item bone name → character bone name (`Head` → `head`);
- **Ignore Case**: `Spine` matches `spine`;
- **Ignore Namespaces**: `mixamorig:Hips` matches `Hips`.

Exact names always win; call `Rebuild()` after changing these at runtime.

### Several sockets per slot

Give sockets of one slot different **Socket Id**s (e.g. `Left` and `Right` under the ears)
and set the same id on the socket items. Items and sockets without an id use the slot's
default socket.

### Material items

A `Material` item doesn't add geometry: its prefab holds an `OutfitMaterialSet` listing
materials per body part, which replace the materials of those body parts' renderers while
the item is worn. Give material items their own slot (e.g. `SkinTone`), so two of them never
change the same body part at once.

### Custom attach logic

An item with the `Custom` attach mode is put on by its own strategy asset: derive from
`OutfitAttachStrategyAsset` and assign the asset to the item. One asset serves every
character, so keep per-character state on the instance it returns.

To change how a built-in mode works for a character, replace its strategy, e.g. to pool
instances:

```csharp
wearer.SetStrategy(new MySkinnedStrategy());
```

### Many characters on screen

Every worn item is a separate `SkinnedMeshRenderer`: it is skinned on its own and drawn at
least once per material. For crowds, add `OutfitMeshCombiner` next to the `OutfitWearer`.
After the worn items change (and nothing is loading any more), it bakes the character's
visible skinned renderers into one child renderer and turns them off:

- the character is skinned once;
- submeshes with the same material merge, and with **Atlas** on, materials that share a
  shader, keywords, render queue and tint (`_BaseColor`, `_Color`) merge into one whose
  textures (`_BaseMap`, `_MainTex` by default) are packed into an atlas. Other properties
  come from the first material of the group, so atlas only materials that differ in
  textures. A material whose UVs leave the 0..1 range (tiling) keeps its own draw call.

Requirements and limits:

- the meshes need **Read/Write** on in their import settings; others are skipped, with a
  warning;
- renderers with blend shapes (faces with expressions) are skipped and stay separate;
- the atlases are `RenderTexture`s filled on the GPU, so textures don't need Read/Write.
  Leave normal maps out of the atlas properties: their encoding doesn't survive the copy on
  every platform;
- a bake runs on the main thread: about 20 ms (30 ms with atlases) for the Dress-Up
  character in a suit and boots (25k vertices) on a desktop, several times that on a phone.
  Combine when the look settles (the wardrobe closes, a level loads), not on every change;
  `Separate()` shows the separate renderers again, `Combine()` bakes right away.

`SkinnedMeshCombiner.Combine(renderers, settings)` is the same bake as a utility, for your
own renderers; dispose the `CombinedSkinnedMesh` it returns when it is no longer shown.

## Samples

Import them from the package's page in the Package Manager:

- **Dress-Up**: open `Scenes/DressUpSample` and press Play. A Humanoid MakeHuman character
  plays an idle clip in a photo zone (a concave cylinder whose floor curves into the wall through a
  wide rounded bevel) under three-point lighting: a white key light, a violet fill light and
  a yellow rim light. The panel on the left changes its skin tone, suit, shoes and hat. It shows the whole
  pipeline: skinned suits and shoes exported with their own copy of the `game_engine` rig
  and rebound at runtime, fedoras on a head socket, body parts hidden under the clothes,
  and a simulated load delay that a newer request overtakes. It also shows the 1.1 API:
  skin tones are `Material` items, the natural skin is the wearer's default outfit (**Take
  everything off** returns to it), slots show when they are loading, and **Save look** /
  **Restore look** store the outfit in `PlayerPrefs` as a loadout and apply it through the
  item catalog. No extra packages needed;
  the FBX materials are created for the project's render pipeline (Built-in, URP, HDRP).
- **Addressables Provider**: `AddressablesOutfitAssetProvider` over Hephaestus
  Addressables (it loads each address once and counts its users),
  `HephaestusOutfitAddressablesInstaller` and `OutfitWearerInjector`. Requires
  `com.wtfgames.hephaestus.addressables`, and with it the Unity version that package
  supports.

### Sample art

All the sample art is [CC0](https://creativecommons.org/publicdomain/zero/1.0/). The
character and clothes are generated from the MakeHuman system assets with MPFB, the
MakeHuman plugin for Blender; the idle clip is `Idle_Loop` from Quaternius'
[Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html).
The scripts live in the repository, not in the published package:

```bash
Blender --background --python "Art~/generate_dressup_assets.py" -- <output folder>
```

It needs Blender 4.2+, MPFB 2 and the `makehuman_system_assets_cc0.zip` pack loaded into
MPFB. It writes the FBX files (including the photo zone and `SkinTones.fbx`, which only
carries the extra skin materials), their textures and
`body_parts.json` (which body parts each item hides). The character and every item are
posed in MakeHuman's T-pose, which becomes their rest pose, so Unity's Humanoid retargeting
maps clips from other skeletons correctly.

The idle clip is a Humanoid `Animation/Idle.anim`: muscle curves that play on any Humanoid
avatar. `Art~/Unity/ExtractIdleClip.cs` extracts it in Unity from the library's
`UAL1_Standard.fbx` (the version without root motion), imported with the settings from the
pack's `Unity_Setup.png` (Humanoid, Bake Axis Conversion, root motion node `root`). Don't
re-export the clip through Blender: the re-exported file loses that axis setup, and the
retargeted poses come out mirrored (the body leans backwards).

`Art~/Unity/DressUpSampleBuilder.cs` builds the prefabs, items, lights and scene of the
sample: copy the FBX files to `Models`, the textures to `Models/Textures`, put `Idle.anim`
in `Animation`, put the builder in an `Editor` folder and run `DressUpSampleBuilder.Build`.
It imports the character as Humanoid.

## Tests

The package ships EditMode tests. To run them in a project, add the package to
`testables` in `Packages/manifest.json` and open **Window > General > Test Runner**:

```json
{
  "testables": ["com.wtfgames.hephaestus.3d.customization"]
}
```

## Requirements

- Unity 2019.2 or newer.

## License

Copyright (C) 2026-2026 Serhii Chechui (WTFGames).

Hephaestus 3D Customization is free software: you can redistribute it and/or modify it
under the terms of the GNU General Public License as published by the Free Software
Foundation, either version 3 of the License, or (at your option) any later version
(`GPL-3.0-or-later`). See [LICENSE.md](LICENSE.md) for the full text.
