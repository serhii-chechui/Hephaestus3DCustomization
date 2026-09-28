using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attachment point for <see cref="OutfitAttachMode.Socket"/> items. Put it on a transform
    /// under the bone the item should follow (e.g. a "HatSocket" under the head bone); the
    /// item's prefab keeps its own local offset from this transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutfitSocket : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Slot whose socket items are attached here.")]
        private OutfitSlot _slot;

        public OutfitSlot Slot
        {
            get => _slot;
            set => _slot = value;
        }
    }
}
