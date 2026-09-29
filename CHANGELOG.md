# Changelog

All notable changes to this project will be documented in this file in accordance with the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) guidelines.

## [Unreleased]

### feat
- Dress-Up sample shows the 1.1 API: skin tone `Material` items (MakeHuman CC0 skins), the natural skin as the wearer's default outfit, loading indicators per slot and failure messages, and Save look / Restore look through loadouts and an item catalog.

### fix
- `MaterialOutfitAttachStrategy` layers material items per renderer, so replacing a material item keeps the new item's materials. The wearer attaches the new item before it takes the old one off, and taking the old one off used to restore the materials it had replaced over the new ones.

## [1.1.0] - 2026-09-29

### feat
- Item ids and loadouts: `OutfitItem.Id` (defaults to the asset name), `GetLoadout()` and `ApplyLoadoutAsync()` on the new `IOutfitLoadoutWearer` (extends `IOutfitWearer`), resolving ids through `IOutfitItemCatalog` / the `OutfitItemCatalog` asset. `OutfitLoadout` serializes with JsonUtility.
- Default outfit: `OutfitWearer.DefaultOutfit` is what slots fall back to; `Unequip` puts the slot's default item back on, `UnequipAll` returns to the default outfit, `EquipDefaultOutfitAsync` fills empty slots.
- `Material` attach mode: `MaterialOutfitAttachStrategy` puts the materials of the prefab's `OutfitMaterialSet` on body part renderers and restores them on detach.
- `Custom` attach mode: items can bring their own `OutfitAttachStrategyAsset`.
- Several sockets per slot: `OutfitSocket.SocketId` and `OutfitItem.SocketId`; `OutfitSkeleton.TryGetSocket(slot, socketId, …)`.
- Bone matching for items from other rigs: `OutfitSkeleton` bone aliases, case-insensitive and namespace-free matching.
- `IsLoading(slot)` and the `EquipFailed` event on the new `IOutfitLoadingStatus`.
- `IOutfitWearer` is unchanged; `OutfitWearer` implements the new interfaces.

### fix
- `OutfitWearer` releases the loaded prefab when an attach strategy throws, instead of leaking it.
- A load that fails after a newer request superseded it no longer faults the superseded `EquipAsync`; it returns `false`.
- Requesting an item that is already loading into its slot returns that pending request instead of loading the item twice.
- `OutfitWearer` implements `IDisposable`: `Dispose()` takes every item off, releases the prefabs and cancels pending loads, for characters destroyed without ever being active (Unity skips `OnDestroy` for them).
- Dress-Up sample: Foot IK is on for the idle state, so the feet stay planted on the floor.

## [1.0.1] - 2026-09-29

### fix
- Dress-Up sample: the idle clip no longer tilts the body backwards or the preview model. The Quaternius clip was re-exported through Blender, which lost the axis setup its Humanoid avatar relies on, so every retargeted pose was mirrored; it now ships as a Humanoid `Idle.anim` extracted in Unity from the original file.

## [1.0.0] - 2026-09-28

### feat
- `OutfitWearer` equips `OutfitItem`s and `OutfitPreset`s, one item per `OutfitSlot`, loading prefabs through `IOutfitAssetProvider`. A newer request for a slot supersedes a pending one, and every loaded prefab is released exactly once.
- `SkinnedOutfitAttachStrategy` rebinds the item's skinned meshes to the character's bones by name and removes the item's own skeleton; missing bones fall back to their closest ancestor on the character.
- `SocketOutfitAttachStrategy` parents rigid items to the character's `OutfitSocket` for the item's slot.
- Body parts: `OutfitItem.HiddenBodyParts` lists the `OutfitBodyPart`s an item covers; the wearer turns off their `OutfitBodyPartRenderer`s while any worn item hides them.
- `OutfitSkeleton` indexes the character's bones, sockets and body part renderers, skipping equipped items.
- Samples: a runnable Dress-Up scene with a Humanoid MakeHuman character (CC0) playing a Quaternius idle (CC0) in a photo zone with three-point lighting, and an Addressables asset provider with a Zenject installer.
