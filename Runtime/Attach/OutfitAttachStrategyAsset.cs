using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Base for attach strategies that live in assets, so items can bring their own way of
    /// being put on (<see cref="OutfitAttachMode.Custom"/>). One asset serves every wearer:
    /// keep per-character state on the instance it returns, not in the asset.
    /// </summary>
    public abstract class OutfitAttachStrategyAsset : ScriptableObject, IOutfitAttachStrategy
    {
        public OutfitAttachMode Mode => OutfitAttachMode.Custom;

        public abstract GameObject Attach(GameObject prefab, OutfitAttachContext context);

        public virtual void Detach(GameObject instance)
        {
            OutfitObjectUtility.Destroy(instance);
        }
    }
}
