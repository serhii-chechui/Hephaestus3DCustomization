using System;
using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Indexes a character's bones by name, its <see cref="OutfitSocket"/>s by slot and its
    /// <see cref="OutfitBodyPartRenderer"/>s by body part.
    /// Skinned outfits are rebound to these bones by name. Names match exactly first; aliases,
    /// case-insensitive matching and namespace stripping (e.g. "mixamorig:Hips" → "Hips")
    /// help with items made on another rig. The index is built on first use; call
    /// <see cref="Rebuild"/> after changing the hierarchy or the matching settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutfitSkeleton : MonoBehaviour
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        [SerializeField]
        [Tooltip("Top bone of the character's skeleton. Defaults to this transform.")]
        private Transform _rootBone;

        [SerializeField]
        [Tooltip("Bone names used by items that differ from the character's bone names.")]
        private List<OutfitBoneAlias> _boneAliases = new List<OutfitBoneAlias>();

        [SerializeField]
        [Tooltip("Match bone names regardless of case when there's no exact match.")]
        private bool _ignoreCase;

        [SerializeField]
        [Tooltip("Ignore a namespace prefix such as \"mixamorig:\" when there's no exact match.")]
        private bool _ignoreNamespaces;

        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> _bonesIgnoringCase = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<(OutfitSlot slot, string id), Transform> _sockets = new Dictionary<(OutfitSlot slot, string id), Transform>();
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

        public List<OutfitBoneAlias> BoneAliases => _boneAliases;

        public bool IgnoreCase
        {
            get => _ignoreCase;
            set => _ignoreCase = value;
        }

        public bool IgnoreNamespaces
        {
            get => _ignoreNamespaces;
            set => _ignoreNamespaces = value;
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
            _bonesIgnoringCase.Clear();
            _aliases.Clear();
            _sockets.Clear();
            _bodyParts.Clear();

            ScanBones(RootBone);
            ScanCharacter(transform);

            foreach (var entry in _boneAliases)
            {
                if (entry == null || string.IsNullOrEmpty(entry.alias) || string.IsNullOrEmpty(entry.bone)) continue;
                _aliases[entry.alias] = entry.bone;
            }

            _isBuilt = true;
        }

        public bool TryGetBone(string boneName, out Transform bone)
        {
            EnsureBuilt();
            bone = null;

            if (string.IsNullOrEmpty(boneName)) return false;

            if (Find(boneName, out bone)) return true;

            if (_aliases.TryGetValue(boneName, out var aliased) && Find(aliased, out bone)) return true;

            if (_ignoreNamespaces)
            {
                var separator = boneName.LastIndexOf(':');

                if (separator >= 0 && separator < boneName.Length - 1)
                {
                    var local = boneName.Substring(separator + 1);

                    if (Find(local, out bone)) return true;
                    if (_aliases.TryGetValue(local, out aliased) && Find(aliased, out bone)) return true;
                }
            }

            return false;
        }

        /// <summary>The slot's default socket (empty socket id).</summary>
        public bool TryGetSocket(OutfitSlot slot, out Transform socket)
        {
            return TryGetSocket(slot, string.Empty, out socket);
        }

        public bool TryGetSocket(OutfitSlot slot, string socketId, out Transform socket)
        {
            EnsureBuilt();

            if (slot != null && _sockets.TryGetValue((slot, socketId ?? string.Empty), out socket) && socket != null) return true;

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

        private bool Find(string boneName, out Transform bone)
        {
            if (_bones.TryGetValue(boneName, out bone) && bone != null) return true;
            if (_ignoreCase && _bonesIgnoringCase.TryGetValue(boneName, out bone) && bone != null) return true;

            bone = null;
            return false;
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

            if (!_bonesIgnoringCase.ContainsKey(bone.name)) _bonesIgnoringCase.Add(bone.name, bone);

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
                var key = (socket.Slot, socket.SocketId);

                if (_sockets.ContainsKey(key))
                {
                    Debug.LogWarning($"{LogTag} Skeleton '{name}' has several sockets for slot '{socket.Slot.DisplayName}' with id '{socket.SocketId}'; items use the first one.", socket);
                }
                else
                {
                    _sockets.Add(key, socket.transform);
                }
            }

            for (var i = 0; i < node.childCount; i++)
            {
                ScanCharacter(node.GetChild(i));
            }
        }
    }
}
