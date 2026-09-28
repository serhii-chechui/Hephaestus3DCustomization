using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attaches rigid items to the character's <see cref="OutfitSocket"/> for the item's slot.
    /// The prefab keeps its local position, rotation and scale relative to the socket.
    /// </summary>
    public class SocketOutfitAttachStrategy : IOutfitAttachStrategy
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        public OutfitAttachMode Mode => OutfitAttachMode.Socket;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            var slot = context.Item.Slot;

            if (!context.Skeleton.TryGetSocket(slot, out var socket))
            {
                Debug.LogWarning($"{LogTag} Skeleton '{context.Skeleton.name}' has no socket for slot '{slot.DisplayName}'; '{context.Item.name}' isn't attached.", context.Skeleton);
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
