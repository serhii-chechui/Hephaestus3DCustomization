using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// A renderer's own materials and the material items layered over them. The renderer
    /// shows the top layer, or its own materials when no layer is left.
    /// </summary>
    internal sealed class RendererMaterialStack
    {
        private readonly Material[] _original;
        private readonly List<MaterialLayer> _layers = new List<MaterialLayer>();

        public RendererMaterialStack(Renderer renderer)
        {
            Renderer = renderer;
            _original = renderer.sharedMaterials;
        }

        public Renderer Renderer { get; }

        public bool IsEmpty => _layers.Count == 0;

        public void Push(GameObject owner, Material[] materials)
        {
            _layers.Add(new MaterialLayer(owner, materials));
            Apply();
        }

        public void Remove(GameObject owner)
        {
            _layers.RemoveAll(layer => layer.Owner == owner);
            Apply();
        }

        private void Apply()
        {
            if (Renderer == null) return;

            Renderer.sharedMaterials = _layers.Count > 0 ? _layers[_layers.Count - 1].Materials : _original;
        }
    }
}
