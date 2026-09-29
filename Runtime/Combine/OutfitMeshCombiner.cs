using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Keeps a dressed character in one skinned mesh: after the worn items change, it bakes
    /// the character's visible skinned renderers with <see cref="SkinnedMeshCombiner"/> into a
    /// child renderer and turns the sources off. Many characters on screen then cost one
    /// skinning and a few draw calls each. A bake takes tens of milliseconds on the main
    /// thread, so it suits characters that change clothes now and then, not every frame.
    /// </summary>
    /// <remarks>
    /// The combined mesh is rebuilt in LateUpdate once nothing is loading; meanwhile the
    /// separate renderers show. While combined, turn
    /// the character's renderers on or off only after <see cref="Separate"/>, or call
    /// <see cref="MarkDirty"/> afterwards. Disabling the component shows the separate renderers
    /// again.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OutfitWearer))]
    public class OutfitMeshCombiner : MonoBehaviour
    {
        [SerializeField]
        private SkinnedMeshCombineSettings _settings = new SkinnedMeshCombineSettings();

        [SerializeField]
        [Tooltip("Wait until every slot has loaded before combining, so dressing a character bakes once.")]
        private bool _waitForLoads = true;

        private readonly List<SkinnedMeshRenderer> _sources = new List<SkinnedMeshRenderer>();
        private readonly List<SkinnedMeshRenderer> _hidden = new List<SkinnedMeshRenderer>();

        private OutfitWearer _wearer;
        private CombinedSkinnedMesh _combined;
        private SkinnedMeshRenderer _renderer;
        private bool _isDirty;

        public SkinnedMeshCombineSettings Settings => _settings;

        public bool WaitForLoads
        {
            get => _waitForLoads;
            set => _waitForLoads = value;
        }

        public bool IsCombined => _combined != null;

        /// <summary>The renderer that shows the combined mesh; null until the first combine.</summary>
        public SkinnedMeshRenderer CombinedRenderer => _renderer;

        private OutfitWearer Wearer
        {
            get
            {
                if (_wearer == null) _wearer = GetComponent<OutfitWearer>();
                return _wearer;
            }
        }

        /// <summary>Rebuilds the combined mesh in the next LateUpdate.</summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Bakes the character's visible skinned renderers into one right now and turns them off.
        /// Needs at least two renderers that can be combined.
        /// </summary>
        public bool Combine()
        {
            _isDirty = false;

            Separate();

            _sources.Clear();

            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (renderer != _renderer && renderer.enabled && renderer.sharedMesh != null) _sources.Add(renderer);
            }

            if (_sources.Count < 2) return false;

            var combined = SkinnedMeshCombiner.Combine(_sources, _settings, this);

            _sources.Clear();

            if (combined == null) return false;

            if (combined.Sources.Count < 2)
            {
                combined.Dispose();
                return false;
            }

            var target = GetOrCreateRenderer();
            combined.ApplyTo(target);
            target.enabled = true;

            foreach (var source in combined.Sources)
            {
                source.enabled = false;
                _hidden.Add(source);
            }

            _combined = combined;
            return true;
        }

        /// <summary>Shows the separate renderers again and frees the combined mesh.</summary>
        public void Separate()
        {
            if (_combined == null) return;

            foreach (var source in _hidden)
            {
                if (source != null) source.enabled = true;
            }

            _hidden.Clear();

            // The sources went back on; the worn items decide again which body parts show.
            Wearer.RefreshBodyParts();

            if (_renderer != null)
            {
                _renderer.enabled = false;
                _renderer.sharedMesh = null;
                _renderer.sharedMaterials = new Material[0];
            }

            _combined.Dispose();
            _combined = null;
        }

        private void OnEnable()
        {
            Wearer.Equipped += OnOutfitChanged;
            Wearer.Unequipped += OnOutfitChanged;
            _isDirty = true;
        }

        private void OnDisable()
        {
            if (_wearer != null)
            {
                _wearer.Equipped -= OnOutfitChanged;
                _wearer.Unequipped -= OnOutfitChanged;
            }

            Separate();
        }

        private void OnDestroy()
        {
            Separate();

            if (_renderer != null) OutfitObjectUtility.Destroy(_renderer.gameObject);
        }

        private void LateUpdate()
        {
            if (!_isDirty) return;

            if (_waitForLoads && Wearer.IsLoadingAny)
            {
                // The combined mesh may still show an item that was taken off; the separate
                // renderers are right until the loads finish.
                Separate();
                return;
            }

            Combine();
        }

        private void OnOutfitChanged(OutfitItem item)
        {
            _isDirty = true;
        }

        private SkinnedMeshRenderer GetOrCreateRenderer()
        {
            if (_renderer != null) return _renderer;

            var child = new GameObject("Combined Outfit") { layer = gameObject.layer };
            child.transform.SetParent(transform, false);

            _renderer = child.AddComponent<SkinnedMeshRenderer>();
            return _renderer;
        }
    }
}
