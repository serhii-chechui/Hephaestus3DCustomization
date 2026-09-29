using System.Threading.Tasks;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>An item on its way into a slot: the request that loads it and its result.</summary>
    internal sealed class PendingEquip
    {
        public PendingEquip(OutfitItem item, int requestId, Task<bool> task)
        {
            Item = item;
            RequestId = requestId;
            Task = task;
        }

        public OutfitItem Item { get; }

        public int RequestId { get; }

        public Task<bool> Task { get; }
    }
}
