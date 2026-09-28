using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Marks a renderer of the character's body as a <see cref="OutfitBodyPart"/>. The wearer
    /// turns the renderer off while an equipped item hides that part. Split the body mesh
    /// into one renderer per part that clothes can cover.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class OutfitBodyPartRenderer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Body part this renderer shows.")]
        private OutfitBodyPart _bodyPart;

        private Renderer _renderer;

        public OutfitBodyPart BodyPart
        {
            get => _bodyPart;
            set => _bodyPart = value;
        }

        public Renderer Renderer
        {
            get
            {
                if (_renderer == null) _renderer = GetComponent<Renderer>();
                return _renderer;
            }
        }
    }
}
