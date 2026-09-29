using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class QuestProjectSettings
{
    private const BuildTargetGroup Target = BuildTargetGroup.Android;
    private const string ScenePath = "Assets/_Project/Scenes/Classroom.unity";
    private const string MetaFeatureId = "com.meta.openxr.feature.metaxr";
    private const string LoaderName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    public static void ConfigurePrototype()
    {
        // Batch startup may contain an untitled scene, which Unity cannot keep additively.
        if (Application.isBatchMode && string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
            EditorSceneManager.OpenScene(File.Exists(ScenePath) ? ScenePath : "Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        Configure();
        QuestClassroomSetup.ConfigureScene();
        ValidateReport();
    }

    public static void GenerateMetaReport()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        OVRProjectSetupCLI.GenerateProjectSetupReport();
    }

    [MenuItem("Tools/VR Classroom/Configure Quest Project")]
    public static void Configure()
    {
        PlayerSettings.companyName = "VR School";
        PlayerSettings.productName = "VR Classroom Prototype";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.vrschool.classroomprototype");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32; // Meta Core SDK 207 minimum.
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        PlayerSettings.Android.androidTVCompatibility = false;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.use32BitDisplayBuffer = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.buildAppBundle = false;

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (pipeline == null) throw new InvalidOperationException("URP Mobile_RPAsset.asset is missing.");
        pipeline.supportsHDR = false;
        pipeline.supportsCameraDepthTexture = false;
        pipeline.supportsCameraOpaqueTexture = false;
        pipeline.msaaSampleCount = 4;
        pipeline.renderScale = 1f;
        pipeline.shadowDistance = 15f;
        GraphicsSettings.defaultRenderPipeline = pipeline;
        int currentQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.vSyncCount = 0;
        }
        QualitySettings.SetQualityLevel(currentQuality, true);
        EditorUtility.SetDirty(pipeline);

        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget targets))
        {
            targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            const string path = "Assets/XR";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets", "XR");
            AssetDatabase.CreateAsset(targets, path + "/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, targets, true);
        }
        if (!targets.HasManagerSettingsForBuildTarget(Target)) targets.CreateDefaultManagerSettingsForBuildTarget(Target);
        var general = targets.SettingsForBuildTarget(Target);
        general.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, LoaderName, Target))
            throw new InvalidOperationException("Could not assign the Android OpenXR loader.");
        EditorUtility.SetDirty(general);

        // Public discovery API creates OpenXR settings only for installed build targets.
        FeatureHelpers.RefreshFeatures(Target);
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(Target);
        if (openXR != null)
        {
            openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXR.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            foreach (string id in new[] { MetaFeatureId, OculusTouchControllerProfile.featureId })
            {
                var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(Target, id);
                if (feature == null) throw new InvalidOperationException("Missing OpenXR feature: " + id);
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
            EditorUtility.SetDirty(openXR);
        }
        else Debug.LogWarning("Install Android Build Support, then run Configure Quest Project again to enable OpenXR features.");

        var config = OVRProjectConfig.CachedProjectConfig;
        if (config == null) throw new InvalidOperationException("Meta project configuration is not ready; rerun after import completes.");
        config.targetDeviceTypes = new List<OVRProjectConfig.DeviceType> { OVRProjectConfig.DeviceType.Quest3 };
        config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
        OVRProjectConfig.CommitProjectConfig(config);
        OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(true);
        EditorUtility.SetDirty(targets);
        AssetDatabase.SaveAssets();
        Debug.Log("Quest project configuration saved. Run Validate Quest Project before building.");
    }

    [MenuItem("Tools/VR Classroom/Validate Quest Project")]
    public static void ValidateReport()
    {
        var errors = new List<string>();
        void Require(bool valid, string message) { if (!valid) errors.Add(message); }
        Require(BuildPipeline.IsBuildTargetSupported(Target, BuildTarget.Android), "Android Build Support is not installed.");
        Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "IL2CPP is required.");
        Require(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "ARM64 is required.");
        Require((int)PlayerSettings.Android.minSdkVersion >= 32, "Minimum Android API must be at least 32.");
        Require((int)PlayerSettings.Android.targetSdkVersion >= 34, "Target Android API must be at least 34.");
        Require(PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.GameActivity, "GameActivity is required.");
        Require(PlayerSettings.colorSpace == ColorSpace.Linear, "Linear color space is required.");
        Require(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.Vulkan }), "Vulkan must be the Android graphics API.");
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(Target);
        Require(general != null && general.InitManagerOnStart && general.Manager != null && general.Manager.activeLoaders.Any(x => x is OpenXRLoader), "Android OpenXR must initialize on startup.");
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(Target);
        Require(openXR != null && openXR.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced, "OpenXR Single Pass Instanced is required.");
        foreach (string id in new[] { MetaFeatureId, OculusTouchControllerProfile.featureId })
        {
            var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(Target, id);
            Require(feature != null && feature.enabled, "OpenXR feature is disabled or missing: " + id);
        }
        var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        Require(pipeline != null && !pipeline.supportsHDR && pipeline.msaaSampleCount == 4, "Mobile URP with HDR off and 4x MSAA is required.");
        Require(File.Exists(ScenePath), "Classroom scene is missing: " + ScenePath);
        // Use the installed OpenXR package's own enabled-feature validation rules.
        // Meta's separate Project Setup Tool report is also checked before the first APK.
        if (general != null && general.Manager != null)
        {
            var issues = new List<UnityEngine.XR.OpenXR.Features.OpenXRFeature.ValidationRule>();
            UnityEditor.XR.OpenXR.OpenXRProjectValidation.GetCurrentValidationIssues(issues, Target);
            foreach (var issue in issues)
            {
                if (issue.error) errors.Add("OpenXR: " + issue.message);
                else Debug.LogWarning("OpenXR: " + issue.message);
            }
        }
        if (errors.Count != 0) throw new InvalidOperationException("Quest validation failed:\n- " + string.Join("\n- ", errors));
        Debug.Log("QUEST_SETTINGS_VALID: Android, ARM64/IL2CPP, OpenXR, Meta XR, Oculus Touch, mobile URP and Classroom scene configured. Headset testing is still required.");
    }

    [MenuItem("Tools/VR Classroom/Build Development APK")]
    public static void BuildApk()
    {
        ValidateReport();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Switch the active build target to Android before building (batch mode: -buildTarget Android).");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/ClassroomPrototype-v0.1.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = output,
            target = BuildTarget.Android, options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("APK build failed: " + report.summary.result + ", errors: " + report.summary.totalErrors);
        Debug.Log("QUEST_APK_BUILT: " + output);
    }
}
