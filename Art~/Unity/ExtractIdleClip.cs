using System.Linq;
using UnityEditor;
using UnityEngine;

// Extracts Idle_Loop from Quaternius' Universal Animation Library (CC0) as a Humanoid .anim.
// Put UAL1_Standard.fbx (the version without root motion) at SourcePath and run Extract.
// A Humanoid clip stores muscle curves, so it plays on any Humanoid avatar without the source skeleton.
public static class ExtractIdleClip
{
    private const string SourcePath = "Assets/UALTest/UAL1_Standard.fbx";
    private const string TargetPath = "Assets/Samples/DressUp/Animation/Idle.anim";
    private const string SourceClip = "Idle_Loop";

    public static void Extract()
    {
        // Import settings from the pack's Unity_Setup.png.
        var importer = (ModelImporter)AssetImporter.GetAtPath(SourcePath);
        importer.bakeAxisConversion = true;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.SaveAndReimport();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        var root = model.GetComponentsInChildren<Transform>().First(t => t.name == "root");
        importer.motionNodeName = AnimationUtility.CalculateTransformPath(root, model.transform);

        var clips = importer.defaultClipAnimations.Where(c => c.name.Split('|').Last() == SourceClip).ToArray();
        foreach (var clip in clips)
        {
            clip.name = "Idle";
            clip.loopTime = true;
            // Keep the character in place: root rotation, height and position are baked into the pose.
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = false;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = false;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        var source = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().First(c => c.name == "Idle");
        var copy = Object.Instantiate(source);
        copy.name = "Idle";
        AssetDatabase.DeleteAsset(TargetPath);
        AssetDatabase.CreateAsset(copy, TargetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ExtractIdleClip] {TargetPath}: humanoid {copy.humanMotion}, length {copy.length:0.00}s, loop {AnimationUtility.GetAnimationClipSettings(copy).loopTime}");
    }
}
