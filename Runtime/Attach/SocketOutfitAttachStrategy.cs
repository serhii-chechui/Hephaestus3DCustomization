using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attaches rigid items to the character's <see cref="OutfitSocket"/> for the item's slot
    /// and socket id.
    /// The prefab keeps its local position, rotation and scale relative to the socket.
    /// </summary>
    public class SocketOutfitAttachStrategy : IOutfitAttachStrategy
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        public OutfitAttachMode Mode => OutfitAttachMode.Socket;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            var slot = context.Item.Slot;

            var socketId = context.Item.SocketId;

            if (!context.Skeleton.TryGetSocket(slot, socketId, out var socket))
            {
                var socketName = socketId.Length == 0 ? $"slot '{slot.DisplayName}'" : $"slot '{slot.DisplayName}', id '{socketId}'";
                Debug.LogWarning($"{LogTag} Skeleton '{context.Skeleton.name}' has no socket for {socketName}; '{context.Item.name}' isn't attached.", context.Skeleton);
                return null;
            }

            var instance = Object.Instantiate(prefab, socket, false);
            instance.name = prefab.name;
            return instance;
        }

        public void Detach(GameObject instance)
        {
            OutfitObjectUtility.Destroy(instance);
        }
    }
}
