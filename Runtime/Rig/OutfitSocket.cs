using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attachment point for <see cref="OutfitAttachMode.Socket"/> items. Put it on a transform
    /// under the bone the item should follow (e.g. a "HatSocket" under the head bone); the
    /// item's prefab keeps its own local offset from this transform. A slot can have several
    /// sockets with different ids (e.g. "Left" and "Right" for earrings).
    /// </summary>
    [DisallowMultipleComponent]
    public class OutfitSocket : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Slot whose socket items are attached here.")]
        private OutfitSlot _slot;

        [SerializeField]
        [Tooltip("Id within the slot; items pick a socket by it. Empty for the slot's default socket.")]
        private string _socketId;

        public OutfitSlot Slot
        {
            get => _slot;
            set => _slot = value;
        }

        public string SocketId
        {
            get => _socketId ?? string.Empty;
            set => _socketId = value;
        }
    }
}
