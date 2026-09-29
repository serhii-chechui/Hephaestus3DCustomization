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
    /// deforms exactly like the body under it. Animators on the item are removed: without its
    /// own skeleton they have nothing to drive.
    /// </summary>
    public class SkinnedOutfitAttachStrategy : IOutfitAttachStrategy
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        private readonly Dictionary<Transform, Transform> _resolved = new Dictionary<Transform, Transform>();
        private readonly List<string> _missingBones = new List<string>();
        private readonly HashSet<Transform> _ownBones = new HashSet<Transform>();

        public OutfitAttachMode Mode => OutfitAttachMode.Skinned;

        /// <summary>
        /// Remove <see cref="Animator"/> and <see cref="Animation"/> components from the item.
        /// Models imported as Generic with an avatar get an Animator on their root; on a worn
        /// item it only costs time every frame. On by default.
        /// </summary>
        public bool RemoveAnimators { get; set; } = true;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            context.Skeleton.EnsureBuilt();

            var instance = Object.Instantiate(prefab, context.Root, false);
            instance.name = prefab.name;

            if (RemoveAnimators) RemoveAnimation(instance);

            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"{LogTag} '{prefab.name}' has no SkinnedMeshRenderer; it is attached to the character root as a static object.", prefab);
                return instance;
            }

            try
            {
                foreach (var renderer in renderers)
                {
                    Rebind(renderer, instance.transform, context.Skeleton);
                }

                if (_missingBones.Count > 0)
                {
                    Debug.LogWarning($"{LogTag} '{prefab.name}' has {_missingBones.Count} bone(s) missing on skeleton '{context.Skeleton.name}': {string.Join(", ", _missingBones)}.", prefab);
                }

                RemoveOwnSkeleton(instance.transform, _ownBones);
            }
            finally
            {
                _resolved.Clear();
                _missingBones.Clear();
                _ownBones.Clear();
            }

            return instance;
        }

        public void Detach(GameObject instance)
        {
            OutfitObjectUtility.Destroy(instance);
        }

        private static void RemoveAnimation(GameObject instance)
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                OutfitObjectUtility.DestroyNow(animator);
            }

            foreach (var animation in instance.GetComponentsInChildren<Animation>(true))
            {
                OutfitObjectUtility.DestroyNow(animation);
            }
        }

        private void Rebind(SkinnedMeshRenderer renderer, Transform instanceRoot, OutfitSkeleton skeleton)
        {
            var bones = renderer.bones;

            for (var i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null) _ownBones.Add(bones[i]);
                bones[i] = Resolve(bones[i], instanceRoot, skeleton);
            }

            var sourceRootBone = renderer.rootBone;

            if (sourceRootBone != null) _ownBones.Add(sourceRootBone);

            renderer.rootBone = sourceRootBone != null
                ? Resolve(sourceRootBone, instanceRoot, skeleton)
                : skeleton.RootBone;
            renderer.bones = bones;
        }

        // Maps an item bone to the character bone with the same name. When the character lacks
        // it, the closest ancestor the character has is used, so the vertices still follow the
        // body instead of freezing in place. Bones shared by several renderers resolve once and
        // are reported once.
        private Transform Resolve(Transform bone, Transform instanceRoot, OutfitSkeleton skeleton)
        {
            if (bone == null) return skeleton.RootBone;

            if (_resolved.TryGetValue(bone, out var resolved)) return resolved;

            resolved = skeleton.RootBone;
            var found = false;

            for (var current = bone; current != null && current != instanceRoot; current = current.parent)
            {
                if (!skeleton.TryGetBone(current.name, out var match)) continue;

                resolved = match;
                found = current == bone;
                break;
            }

            if (!found) _missingBones.Add($"'{bone.name}' (bound to '{resolved.name}')");

            _resolved.Add(bone, resolved);
            return resolved;
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
