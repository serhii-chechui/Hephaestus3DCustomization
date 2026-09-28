# Hephaestus 3D Customization

Hephaestus 3D Customization is a part of the Hephaestus framework. It dresses 3D
characters in Unity: every outfit item is a separate prefab loaded on demand, and
skinned clothes are rebound to the character's skeleton so they animate with the body.

## Features

- One item per slot; equipping an item replaces the one in its slot.
- **Skinned items** (shirts, pants, shoes, hair) are rebound to the character's bones by
  name. The meshes keep the bone weights they were exported with, so they deform exactly
  like the body.
- **Socket items** (hats, watches, glasses) are parented to a socket on the character.
- **Body parts under clothes are hidden**, so the skin never pokes through when the body
  bends.
- Any asset source: Addressables, asset bundles, Resources or procedural content, behind
  the `IOutfitAssetProvider` interface.
- Asynchronous and race-safe: a newer request for a slot supersedes a pending one, and
  every loaded prefab is released exactly once.
- Replaceable attach strategies per attach mode.
- No dependencies.

## How skinned items work

Model the clothes on the character's skeleton in your DCC tool (Maya, Blender) and export
each item with **its own copy of that skeleton**: same bone names, same rest pose. When
the item is equipped:

1. its prefab is instantiated under the character;
2. every `SkinnedMeshRenderer` of the item gets the character's bones with the same names
   (`bones` and `rootBone`); the mesh, its bone weights and bind poses stay untouched;
3. the item's own copy of the skeleton is destroyed.

The character's `Animator` now moves the item's vertices through the same bones as the
body. This works with Generic and Humanoid characters alike: a Humanoid avatar still drives
the character's own bone transforms, which the items are bound to. A bone the character doesn't have is bound to its closest ancestor that the
character has, with a warning.

Import skinned items with **Rig > Animation Type** set to `Generic` (no avatar is needed).
With `None`, Unity imports them as static meshes and the item can't be rebound.

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
- **Outfit Item**: the item's slot, attach mode (`Skinned` or `Socket`), asset key, which
  your provider resolves to the prefab (for Addressables: the address), and the body parts
  it hides.
- **Outfit Preset**: a set of items equipped together, e.g. a default outfit. When several
  items share a slot, the last one wins.

### 2. Character

On the character's root:

- `OutfitSkeleton`: set **Root Bone** to the top bone of the skeleton.
- `OutfitWearer`: finds the `OutfitSkeleton` on the same object or its children.
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
- Destroying the character releases every loaded prefab.

### Custom attach logic

Replace the strategy of an attach mode with your own `IOutfitAttachStrategy`, e.g. to pool
instances or to hide body parts under clothes:

```csharp
wearer.SetStrategy(new MySkinnedStrategy());
```

## Samples

Import them from the package's page in the Package Manager:

- **Dress-Up**: open `Scenes/DressUpSample` and press Play. A Humanoid MakeHuman character
  plays an idle clip in a photo zone (a concave cylinder whose floor curves into the wall through a
  wide rounded bevel) under three-point lighting: a white key light, a violet fill light and
  a yellow rim light. The panel on the left changes its suit, shoes and hat. It shows the whole
  pipeline: skinned suits and shoes exported with their own copy of the `game_engine` rig
  and rebound at runtime, fedoras on a head socket, body parts hidden under the clothes,
  and a simulated load delay that a newer request overtakes. No extra packages needed;
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
MPFB. It writes the FBX files (including the photo zone), their textures and
`body_parts.json` (which body parts each item hides). The character and every item are
posed in MakeHuman's T-pose, which becomes their rest pose, so Unity's Humanoid retargeting
maps clips from other skeletons correctly.

`Art~/Unity/DressUpSampleBuilder.cs` builds the prefabs, items, lights and scene of the
sample from them: copy the FBX files to `Models`, the textures to `Models/Textures`,
`Art~/Animations/Idle.fbx` to `Animation`, put the builder in an `Editor` folder and run
`DressUpSampleBuilder.Build`. It imports the character and the idle clip as Humanoid.

`Art~/Animations/Idle.fbx` is extracted from the library's `UAL1_Standard.fbx` (the version
without root motion):

```bash
Blender --background --factory-startup --python "Art~/extract_idle.py" -- UAL1_Standard.fbx "Art~/Animations/Idle.fbx"
```

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
