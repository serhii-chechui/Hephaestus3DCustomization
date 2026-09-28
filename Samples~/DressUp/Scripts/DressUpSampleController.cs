using System;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.DressUp
{
    /// <summary>
    /// Gives the character's wearer a prefab provider, puts on the default outfit and draws
    /// buttons that change the item in every slot.
    /// </summary>
    public class DressUpSampleController : MonoBehaviour
    {
        private const float ReferenceHeight = 720f;

        [SerializeField]
        private OutfitWearer _wearer;

        [SerializeField]
        private OutfitPrefabLibrary _library;

        [SerializeField]
        [Tooltip("Slots in the order the panel lists them.")]
        private OutfitSlot[] _slots = new OutfitSlot[0];

        [SerializeField]
        private OutfitItem[] _items = new OutfitItem[0];

        [SerializeField]
        private OutfitPreset _defaultOutfit;

        [SerializeField]
        [Tooltip("Delay of every load, to show that a newer request replaces a pending one.")]
        private float _simulatedLatency = 0.2f;

        private int _pendingRequests;

        public OutfitWearer Wearer => _wearer;

        private void Start()
        {
            _wearer.Construct(new PrefabOutfitAssetProvider(_library, _simulatedLatency));

            if (_defaultOutfit != null) Run(_wearer.EquipAsync(_defaultOutfit));
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.height / ReferenceHeight);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            GUILayout.BeginArea(new Rect(12f, 12f, 360f, Screen.height / scale - 24f), GUI.skin.box);
            GUILayout.Label(_pendingRequests > 0 ? "Loading…" : "Outfit");

            foreach (var slot in _slots)
            {
                if (slot != null) DrawSlot(slot);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Take everything off")) _wearer.UnequipAll();

            if (_defaultOutfit != null && GUILayout.Button("Default outfit")) Run(_wearer.EquipAsync(_defaultOutfit));

            GUILayout.EndArea();
        }

        private void DrawSlot(OutfitSlot slot)
        {
            _wearer.TryGetEquipped(slot, out var equipped);

            GUILayout.Space(6f);
            GUILayout.Label(slot.DisplayName);
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(equipped == null, "None", GUI.skin.button) && equipped != null) _wearer.Unequip(slot);

            foreach (var item in _items)
            {
                if (item == null || item.Slot != slot) continue;

                if (GUILayout.Toggle(equipped == item, item.name, GUI.skin.button) && equipped != item)
                {
                    Run(_wearer.EquipAsync(item));
                }
            }

            GUILayout.EndHorizontal();
        }

        private async void Run(Task<bool> request)
        {
            _pendingRequests++;

            try
            {
                await request;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _pendingRequests--;
            }
        }
    }
}
