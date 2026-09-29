using System;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.DressUp
{
    /// <summary>
    /// Gives the character's wearer a prefab provider, puts on the default outfit and draws
    /// a panel that changes the item in every slot, shows which slots are loading, and saves
    /// and restores the look as a loadout.
    /// </summary>
    public class DressUpSampleController : MonoBehaviour
    {
        private const float ReferenceHeight = 720f;
        private const string SavedLookKey = "Hephaestus3DCustomization.DressUp.SavedLook";

        [SerializeField]
        private OutfitWearer _wearer;

        [SerializeField]
        private OutfitPrefabLibrary _library;

        [SerializeField]
        [Tooltip("Resolves the item ids of a saved look.")]
        private OutfitItemCatalog _catalog;

        [SerializeField]
        [Tooltip("Slots in the order the panel lists them.")]
        private OutfitSlot[] _slots = new OutfitSlot[0];

        [SerializeField]
        private OutfitItem[] _items = new OutfitItem[0];

        [SerializeField]
        [Tooltip("The look put on at start and by the Default outfit button.")]
        private OutfitPreset _defaultOutfit;

        [SerializeField]
        [Tooltip("Delay of every load, to show that a newer request replaces a pending one.")]
        private float _simulatedLatency = 0.2f;

        private int _pendingRequests;
        private string _status = string.Empty;

        public OutfitWearer Wearer => _wearer;

        private void Start()
        {
            _wearer.Construct(new PrefabOutfitAssetProvider(_library, _simulatedLatency));
            _wearer.EquipFailed += OnEquipFailed;

            // The wearer's own default outfit (the natural skin) fills the empty slots first.
            Run(_wearer.EquipDefaultOutfitAsync());

            if (_defaultOutfit != null) Run(_wearer.EquipAsync(_defaultOutfit));
        }

        private void OnDestroy()
        {
            if (_wearer != null) _wearer.EquipFailed -= OnEquipFailed;
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

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save look")) SaveLook();

            GUI.enabled = _catalog != null && PlayerPrefs.HasKey(SavedLookKey);

            if (GUILayout.Button("Restore look")) RestoreLook();

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_status.Length > 0) GUILayout.Label(_status);

            GUILayout.EndArea();
        }

        private void DrawSlot(OutfitSlot slot)
        {
            _wearer.TryGetEquipped(slot, out var equipped);

            GUILayout.Space(6f);
            GUILayout.Label(_wearer.IsLoading(slot) ? slot.DisplayName + "  (loading…)" : slot.DisplayName);
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

        private void SaveLook()
        {
            var loadout = _wearer.GetLoadout();

            PlayerPrefs.SetString(SavedLookKey, JsonUtility.ToJson(loadout));
            PlayerPrefs.Save();
            _status = $"Saved a look of {loadout.ItemIds.Count} items.";
        }

        private void RestoreLook()
        {
            var loadout = JsonUtility.FromJson<OutfitLoadout>(PlayerPrefs.GetString(SavedLookKey));

            Run(_wearer.ApplyLoadoutAsync(loadout, _catalog));
            _status = $"Restored a look of {loadout.ItemIds.Count} items.";
        }

        private void OnEquipFailed(OutfitItem item, Exception exception)
        {
            _status = exception == null ? $"Couldn't put on {item.name}." : $"Couldn't put on {item.name}: {exception.Message}";
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
