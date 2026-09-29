# Changelog

All notable changes to this project will be documented in this file in accordance with the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) guidelines.

## [1.0.2] - 2026-09-29

### fix
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
