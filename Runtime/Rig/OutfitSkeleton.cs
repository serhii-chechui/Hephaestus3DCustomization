using System;
using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Indexes a character's bones by name, its <see cref="OutfitSocket"/>s by slot and its
    /// <see cref="OutfitBodyPartRenderer"/>s by body part.
    /// Skinned outfits are rebound to these bones, so an item's bone names must match the
    /// character's. The index is built on first use; call <see cref="Rebuild"/> after
    /// changing the hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutfitSkeleton : MonoBehaviour
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        [SerializeField]
        [Tooltip("Top bone of the character's skeleton. Defaults to this transform.")]
        private Transform _rootBone;

        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<OutfitSlot, Transform> _sockets = new Dictionary<OutfitSlot, Transform>();
        private readonly Dictionary<OutfitBodyPart, List<Renderer>> _bodyParts = new Dictionary<OutfitBodyPart, List<Renderer>>();
        private bool _isBuilt;

        public Transform RootBone => _rootBone != null ? _rootBone : transform;

        public int BoneCount
        {
            get
            {
                EnsureBuilt();
                return _bones.Count;
            }
        }

        /// <summary>Sets the root bone and rebuilds the index.</summary>
        public void SetRootBone(Transform rootBone)
        {
            _rootBone = rootBone;
            Rebuild();
        }

        /// <summary>Builds the index unless it is already built.</summary>
        public void EnsureBuilt()
        {
            if (!_isBuilt) Rebuild();
        }

        /// <summary>Body parts that have renderers on this character.</summary>
        public IEnumerable<OutfitBodyPart> BodyParts
        {
            get
            {
                EnsureBuilt();
                return _bodyParts.Keys;
            }
        }

        /// <summary>Re-scans the bones under <see cref="RootBone"/> and the sockets and body parts under this object.</summary>
        public void Rebuild()
        {
            _bones.Clear();
            _sockets.Clear();
            _bodyParts.Clear();

            ScanBones(RootBone);
            ScanCharacter(transform);

            _isBuilt = true;
        }

        public bool TryGetBone(string boneName, out Transform bone)
        {
            EnsureBuilt();

            if (!string.IsNullOrEmpty(boneName) && _bones.TryGetValue(boneName, out bone) && bone != null) return true;

            bone = null;
            return false;
        }

        public bool TryGetSocket(OutfitSlot slot, out Transform socket)
        {
            EnsureBuilt();

            if (slot != null && _sockets.TryGetValue(slot, out socket) && socket != null) return true;

            socket = null;
            return false;
        }

        /// <summary>Renderers that show <paramref name="bodyPart"/>; empty when the character has none.</summary>
        public IReadOnlyList<Renderer> GetBodyPartRenderers(OutfitBodyPart bodyPart)
        {
            EnsureBuilt();

            if (bodyPart != null && _bodyParts.TryGetValue(bodyPart, out var renderers)) return renderers;

            return Array.Empty<Renderer>();
        }

        private void ScanBones(Transform bone)
        {
            if (bone.GetComponent<OutfitInstance>() != null) return;

            if (_bones.ContainsKey(bone.name))
            {
                Debug.LogWarning($"{LogTag} Skeleton '{name}' has several bones named '{bone.name}'; outfits bind to the first one.", bone);
            }
            else
            {
                _bones.Add(bone.name, bone);
            }

            for (var i = 0; i < bone.childCount; i++)
            {
                ScanBones(bone.GetChild(i));
            }
        }

        private void ScanCharacter(Transform node)
        {
            if (node.GetComponent<OutfitInstance>() != null) return;

            var bodyPartRenderer = node.GetComponent<OutfitBodyPartRenderer>();

            if (bodyPartRenderer != null && bodyPartRenderer.BodyPart != null)
            {
                if (!_bodyParts.TryGetValue(bodyPartRenderer.BodyPart, out var renderers))
                {
                    renderers = new List<Renderer>();
                    _bodyParts.Add(bodyPartRenderer.BodyPart, renderers);
                }

                renderers.Add(bodyPartRenderer.Renderer);
            }

            var socket = node.GetComponent<OutfitSocket>();

            if (socket != null && socket.Slot != null)
            {
                if (_sockets.ContainsKey(socket.Slot))
                {
                    Debug.LogWarning($"{LogTag} Skeleton '{name}' has several sockets for slot '{socket.Slot.DisplayName}'; items use the first one.", socket);
                }
                else
                {
                    _sockets.Add(socket.Slot, socket.transform);
                }
            }

            for (var i = 0; i < node.childCount; i++)
            {
                ScanCharacter(node.GetChild(i));
            }
        }
    }
}
