using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using WTFGames.Hephaestus.Customization3D;
using WTFGames.Hephaestus.Customization3D.Samples.DressUp;

// Builds the Dress-Up sample assets from the FBX files exported by Art~/generate_dressup_assets.py.
public static class DressUpSampleBuilder
{
    private const string Root = "Assets/Samples/DressUp";
    private const string Models = Root + "/Models";
    // Humanoid clip extracted by ExtractIdleClip from Quaternius' Universal Animation Library (CC0).
    private const string IdlePath = Root + "/Animation/Idle.anim";

    private static readonly string[] Suits = { "TShirtJeans", "DenimShirt", "BlackSuit", "Overalls" };
    private static readonly string[] LongSleeveSuits = { "DenimShirt", "BlackSuit" };
    private static readonly string[] Shoes = { "Sneakers", "BrownShoes", "Boots" };
    private static readonly string[] Hats = { "Fedora", "CockedFedora" };

    public static void Build()
    {
        ConfigureImporters();

        foreach (var folder in new[] { "Animation", "Prefabs", "Scenes" })
        {
            EnsureFolder($"{Root}/{folder}");
        }

        var slots = new[] { "Outfit", "Shoes", "Hat" }.ToDictionary(n => n, n => Save(OutfitSlot.Create(n), $"{Root}/Data/Slots/{n}.asset"));
        var parts = new[] { "TorsoAndLegs", "Arms", "Feet" }.ToDictionary(n => n, n => Save(OutfitBodyPart.Create(n), $"{Root}/Data/BodyParts/{n}.asset"));

        var items = new List<OutfitItem>();
        foreach (var suit in Suits)
        {
            var hidden = LongSleeveSuits.Contains(suit) ? new[] { parts["TorsoAndLegs"], parts["Arms"] } : new[] { parts["TorsoAndLegs"] };
            items.Add(Save(OutfitItem.Create(suit, slots["Outfit"], OutfitAttachMode.Skinned, suit, hidden), $"{Root}/Data/Items/{suit}.asset"));
        }
        foreach (var shoes in Shoes)
        {
            items.Add(Save(OutfitItem.Create(shoes, slots["Shoes"], OutfitAttachMode.Skinned, shoes, new[] { parts["Feet"] }), $"{Root}/Data/Items/{shoes}.asset"));
        }
        foreach (var hat in Hats)
        {
            items.Add(Save(OutfitItem.Create(hat, slots["Hat"], OutfitAttachMode.Socket, hat), $"{Root}/Data/Items/{hat}.asset"));
        }

        var preset = Save(OutfitPreset.Create("DefaultOutfit", new[] { items.First(i => i.name == "TShirtJeans"), items.First(i => i.name == "Sneakers") }),
            $"{Root}/Data/DefaultOutfit.asset");

        var socketPosition = GetHatSocketPosition();
        var character = BuildCharacter(slots["Hat"], parts, socketPosition);

        var library = ScriptableObject.CreateInstance<OutfitPrefabLibrary>();
        foreach (var name in Suits.Concat(Shoes))
        {
            library.Entries.Add(new OutfitPrefabEntry { assetKey = name, prefab = LoadModel(name) });
        }
        foreach (var hat in Hats)
        {
            library.Entries.Add(new OutfitPrefabEntry { assetKey = hat, prefab = BuildHat(hat, socketPosition) });
        }
        Save(library, $"{Root}/Data/OutfitPrefabLibrary.asset");

        BuildScene(character, library, slots.Values.ToArray(), items.ToArray(), preset);
        AssetDatabase.SaveAssets();
        Debug.Log("[DressUpSampleBuilder] Done.");
    }

    private static void ConfigureImporters()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Models }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;

            if (path.EndsWith("/Character.fbx"))
            {
                // Humanoid, so clips made for other skeletons (e.g. Mixamo) retarget onto it.
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
            }
            else if (path.Contains("Fedora") || path.Contains("PhotoZone"))
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }
            else
            {
                // Skinned items need a rig type other than None, or Unity imports them as static meshes.
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                importer.importAnimation = false;
            }

            importer.SaveAndReimport();
        }

        var avatar = AssetDatabase.LoadAllAssetsAtPath($"{Models}/Character.fbx").OfType<Avatar>().First();
        Debug.Log($"[DressUpSampleBuilder] Character avatar: human {avatar.isHuman}, valid {avatar.isValid}");
    }

    private static GameObject BuildCharacter(OutfitSlot hatSlot, Dictionary<string, OutfitBodyPart> parts, Vector3 socketPosition)
    {
        var character = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel("Character"));
        character.name = "Character";

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
        var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip($"{Root}/Animation/Character.controller", clip);
        var animator = character.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        var skeleton = character.AddComponent<OutfitSkeleton>();
        SetField(skeleton, "_rootBone", character.transform.Find("Rig"));

        var wearer = character.AddComponent<OutfitWearer>();
        SetField(wearer, "_skeleton", skeleton);

        foreach (var part in parts)
        {
            var body = character.transform.Find("Body_" + part.Key);
            body.gameObject.AddComponent<OutfitBodyPartRenderer>().BodyPart = part.Value;
        }

        var socket = new GameObject("HatSocket").transform;
        socket.SetPositionAndRotation(socketPosition, Quaternion.identity);
        socket.SetParent(FindDeep(character.transform, "head"), true);
        socket.gameObject.AddComponent<OutfitSocket>().Slot = hatSlot;

        character.AddComponent<Turntable>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(character, $"{Root}/Prefabs/Character.prefab");
        Object.DestroyImmediate(character);
        return prefab;
    }

    // The hats are exported where they sit on the head of the rest pose; the socket goes to the
    // bottom centre of the hat, so every hat prefab only needs the opposite offset.
    private static Vector3 GetHatSocketPosition()
    {
        var hat = Object.Instantiate(LoadModel(Hats[0]));
        var bounds = hat.GetComponentInChildren<Renderer>().bounds;
        Object.DestroyImmediate(hat);
        return new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);
    }

    private static GameObject BuildHat(string hatName, Vector3 socketPosition)
    {
        var root = new GameObject(hatName);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(hatName));
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition -= socketPosition;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/{hatName}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildScene(GameObject characterPrefab, OutfitPrefabLibrary library, OutfitSlot[] slots, OutfitItem[] items, OutfitPreset preset)
    {
        // A new scene unloads the assets built so far; reload them by path afterwards.
        var characterPath = AssetDatabase.GetAssetPath(characterPrefab);
        var libraryPath = AssetDatabase.GetAssetPath(library);
        var presetPath = AssetDatabase.GetAssetPath(preset);
        var slotPaths = slots.Select(AssetDatabase.GetAssetPath).ToArray();
        var itemPaths = items.Select(AssetDatabase.GetAssetPath).ToArray();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
        library = AssetDatabase.LoadAssetAtPath<OutfitPrefabLibrary>(libraryPath);
        preset = AssetDatabase.LoadAssetAtPath<OutfitPreset>(presetPath);
        slots = slotPaths.Select(AssetDatabase.LoadAssetAtPath<OutfitSlot>).ToArray();
        items = itemPaths.Select(AssetDatabase.LoadAssetAtPath<OutfitItem>).ToArray();

        var character = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
        var front = FindDeep(character.transform, "ball_l").position.z > FindDeep(character.transform, "foot_l").position.z ? 1f : -1f;

        var camera = Camera.main;
        camera.transform.position = new Vector3(0.35f, 1.2f, 2.5f * front);
        camera.transform.LookAt(new Vector3(0.35f, 0.88f, 0f));
        camera.fieldOfView = 50f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.04f, 0.07f);

        BuildPhotoZone(front);

        var sample = new GameObject("DressUpSample").AddComponent<DressUpSampleController>();
        var serialized = new SerializedObject(sample);
        serialized.FindProperty("_wearer").objectReferenceValue = character.GetComponent<OutfitWearer>();
        serialized.FindProperty("_library").objectReferenceValue = library;
        serialized.FindProperty("_defaultOutfit").objectReferenceValue = preset;
        SetArray(serialized.FindProperty("_slots"), slots);
        SetArray(serialized.FindProperty("_items"), items);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, $"{Root}/Scenes/DressUpSample.unity");
    }

    // A cyclorama lit by three lights: a white key light, a violet fill light and a yellow rim light.
    private static void BuildPhotoZone(float front)
    {
        Object.DestroyImmediate(Object.FindFirstObjectByType<Light>().gameObject);

        var zone = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel("PhotoZone"));
        zone.name = "PhotoZone";

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.09f, 0.12f);

        var lights = new GameObject("Lights").transform;
        AddSpotLight(lights, "KeyLight", Color.white, 2.2f, new Vector3(1.7f, 2.7f, 1.9f * front), new Vector3(0f, 1.2f, 0f), 50f, LightShadows.Soft);
        AddSpotLight(lights, "FillLight", new Color(0.56f, 0.26f, 1f), 1.8f, new Vector3(-2.1f, 1.7f, 1.3f * front), new Vector3(0f, 1.1f, 0f), 60f, LightShadows.None);
        AddSpotLight(lights, "RimLight", new Color(1f, 0.74f, 0.18f), 6f, new Vector3(2.3f, 2.3f, -1.0f * front), new Vector3(0f, 1.2f, 0f), 45f, LightShadows.None);
    }

    private static void AddSpotLight(Transform parent, string name, Color color, float intensity, Vector3 position, Vector3 target, float angle, LightShadows shadows)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.position = position;
        light.transform.LookAt(target);
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = 9f;
        light.spotAngle = angle;
        light.innerSpotAngle = angle * 0.6f;
        light.shadows = shadows;
        // The Built-in pipeline renders only a few lights per pixel; all three must be.
        light.renderMode = LightRenderMode.ForcePixel;
    }

    private static GameObject LoadModel(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{name}.fbx");
    }

    private static T Save<T>(T asset, string path) where T : Object
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }

    private static void SetField(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    }
}
