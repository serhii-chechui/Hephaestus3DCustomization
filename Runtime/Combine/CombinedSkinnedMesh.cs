using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// The result of <see cref="SkinnedMeshCombiner.Combine"/>: one mesh skinned to the bones
    /// of all its sources, its materials and the atlases made for them. It owns the mesh, the
    /// atlas materials and the atlas textures; <see cref="Dispose"/> destroys them.
    /// </summary>
    public sealed class CombinedSkinnedMesh : IDisposable
    {
        private readonly List<Object> _generated;

        internal CombinedSkinnedMesh(
            Mesh mesh,
            Material[] materials,
            Transform[] bones,
            Transform rootBone,
            Bounds localBounds,
            IReadOnlyList<SkinnedMeshRenderer> sources,
            List<Object> generated)
        {
            Mesh = mesh;
            Materials = materials;
            Bones = bones;
            RootBone = rootBone;
            LocalBounds = localBounds;
            Sources = sources;
            _generated = generated;
        }

        /// <summary>The combined mesh: one submesh per material.</summary>
        public Mesh Mesh { get; private set; }

        /// <summary>One material per submesh of <see cref="Mesh"/>.</summary>
        public Material[] Materials { get; }

        public Transform[] Bones { get; }

        public Transform RootBone { get; }

        /// <summary>Bounds of the sources when combined, relative to <see cref="RootBone"/>.</summary>
        public Bounds LocalBounds { get; }

        /// <summary>The renderers that went into the mesh; renderers that were skipped aren't here.</summary>
        public IReadOnlyList<SkinnedMeshRenderer> Sources { get; }

        public bool IsDisposed => Mesh == null;

        /// <summary>
        /// Shows the combined mesh on <paramref name="target"/>, with the render settings of the
        /// first source (shadows, probes, skin quality).
        /// </summary>
        public void ApplyTo(SkinnedMeshRenderer target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (IsDisposed) throw new ObjectDisposedException(nameof(CombinedSkinnedMesh));

            var first = Sources[0];

            target.sharedMesh = Mesh;
            target.bones = Bones;
            target.rootBone = RootBone;
            target.sharedMaterials = Materials;
            target.localBounds = LocalBounds;
            target.quality = first.quality;
            target.updateWhenOffscreen = first.updateWhenOffscreen;
            target.skinnedMotionVectors = first.skinnedMotionVectors;
            target.shadowCastingMode = first.shadowCastingMode;
            target.receiveShadows = first.receiveShadows;
            target.lightProbeUsage = first.lightProbeUsage;
            target.reflectionProbeUsage = first.reflectionProbeUsage;
            target.probeAnchor = first.probeAnchor;
            target.renderingLayerMask = first.renderingLayerMask;
        }

        /// <summary>Destroys the mesh, the atlas materials and the atlas textures.</summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            foreach (var generated in _generated)
            {
                var texture = generated as RenderTexture;
                if (texture != null) texture.Release();

                OutfitObjectUtility.Destroy(generated);
            }

            _generated.Clear();
            Mesh = null;
        }
    }
}
