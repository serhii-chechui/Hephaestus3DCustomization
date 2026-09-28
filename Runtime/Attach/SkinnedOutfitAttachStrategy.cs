using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attaches skinned items. The item's prefab is exported with its own copy of the
    /// character's skeleton (same bone names, same rest pose) and its meshes skinned to it.
    /// After instantiation every <see cref="SkinnedMeshRenderer"/> is rebound to the
    /// character's bones with the same names, and the item's own skeleton is removed. The
    /// meshes keep the bone weights and bind poses they were exported with, so the item
    /// deforms exactly like the body under it.
    /// </summary>
    public class SkinnedOutfitAttachStrategy : IOutfitAttachStrategy
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        public OutfitAttachMode Mode => OutfitAttachMode.Skinned;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            context.Skeleton.EnsureBuilt();

            var instance = Object.Instantiate(prefab, context.Root, false);
            instance.name = prefab.name;

            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"{LogTag} '{prefab.name}' has no SkinnedMeshRenderer; it is attached to the character root as a static object.", prefab);
                return instance;
            }

            var ownBones = new HashSet<Transform>();

            foreach (var renderer in renderers)
            {
                Rebind(renderer, instance.transform, context.Skeleton, ownBones);
            }

            RemoveOwnSkeleton(instance.transform, ownBones);

            return instance;
        }

        public void Detach(GameObject instance)
        {
            OutfitObjectUtility.Destroy(instance);
        }

        private static void Rebind(SkinnedMeshRenderer renderer, Transform instanceRoot, OutfitSkeleton skeleton, HashSet<Transform> ownBones)
        {
            var sourceBones = renderer.bones;
            var bones = new Transform[sourceBones.Length];

            for (var i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] != null) ownBones.Add(sourceBones[i]);
                bones[i] = Resolve(sourceBones[i], instanceRoot, skeleton, renderer);
            }

            var sourceRootBone = renderer.rootBone;

            if (sourceRootBone != null) ownBones.Add(sourceRootBone);

            renderer.rootBone = sourceRootBone != null
                ? Resolve(sourceRootBone, instanceRoot, skeleton, renderer)
                : skeleton.RootBone;
            renderer.bones = bones;
        }

        // Maps an item bone to the character bone with the same name. When the character lacks
        // it, the closest ancestor the character has is used, so the vertices still follow the
        // body instead of freezing in place.
        private static Transform Resolve(Transform bone, Transform instanceRoot, OutfitSkeleton skeleton, Object context)
        {
            if (bone == null) return skeleton.RootBone;

            for (var current = bone; current != null && current != instanceRoot; current = current.parent)
            {
                if (!skeleton.TryGetBone(current.name, out var match)) continue;

                if (current != bone)
                {
                    Debug.LogWarning($"{LogTag} Bone '{bone.name}' of '{context.name}' is missing on skeleton '{skeleton.name}'; bound to '{match.name}' instead.", context);
                }

                return match;
            }

            Debug.LogWarning($"{LogTag} Bone '{bone.name}' of '{context.name}' is missing on skeleton '{skeleton.name}'; bound to the root bone.", context);
            return skeleton.RootBone;
        }

        // Removes the top-level branches that held the item's bones. A branch that also holds
        // renderers is kept, because some exporters put meshes under the armature.
        private static void RemoveOwnSkeleton(Transform instanceRoot, HashSet<Transform> ownBones)
        {
            var branches = new HashSet<Transform>();

            foreach (var bone in ownBones)
            {
                var branch = GetTopBranch(bone, instanceRoot);
                if (branch != null) branches.Add(branch);
            }

            foreach (var branch in branches)
            {
                if (branch.GetComponentInChildren<Renderer>(true) == null)
                {
                    OutfitObjectUtility.Destroy(branch.gameObject);
                }
            }
        }

        private static Transform GetTopBranch(Transform node, Transform root)
        {
            if (node == root) return null;

            while (node.parent != null && node.parent != root)
            {
                node = node.parent;
            }

            return node.parent == root ? node : null;
        }
    }
}
