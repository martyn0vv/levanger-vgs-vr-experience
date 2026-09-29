using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.Grab;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.OVR.Editor.QuickActions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRClassroom;
using MetaCharacterController = Oculus.Interaction.Locomotion.CharacterController;
using Object = UnityEngine.Object;

/// <summary>Builds the prototype with the public Meta Interaction SDK 207 editor APIs.</summary>
public static class QuestClassroomSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Classroom.unity";
    private const string CompletionMarker = "__QuestClassroomSetup_v1";

    [MenuItem("Tools/VR Classroom/Configure Quest Scene")]
    public static void ConfigureScene()
    {
        ClassroomSceneBuilder.CreateTestRoom();
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "ClassroomSceneBuilder did not create/open Classroom.unity.");

        // Meta's Quick Actions search the loaded scenes. Keep this scene isolated.
        for (int i = SceneManager.sceneCount - 1; i >= 0; --i)
        {
            Scene other = SceneManager.GetSceneAt(i);
            if (other == scene) continue;
            Require(!other.isDirty, "Save or close the other modified scene before configuring Classroom.");
            Require(EditorSceneManager.CloseScene(other, true), "Could not close another scene.");
        }

        if (Root(scene, CompletionMarker, false) != null)
        {
            ValidateScene(scene);
            Debug.Log("Classroom Quest scene already configured; validation passed, no changes made.");
            return;
        }

        Require(Components<OVRCameraRig>(scene).Length == 0,
            "A rig already exists without the setup marker; inspect the scene instead of adding another rig.");
        Require(Components<ClassroomLocomotionControls>(scene).Length == 0,
            "Locomotion controls already exist; inspect the scene before continuing.");
        Transform teleportRoot = Root(scene, "TeleportTargets");
        Require(teleportRoot.childCount == 0, "TeleportTargets already contains custom content.");
        Transform room = Root(scene, "Classroom");
        Transform cube = Child(room, "InteractiveObjects/TestCube");
        Require(cube.GetComponentInChildren<GrabInteractable>(true) == null,
            "The cube already has grab interaction; inspect the scene before continuing.");

        int walkable = EnsureLayer("Walkable");
        int worldSolid = EnsureLayer("WorldSolid");
        int interactable = EnsureLayer("Interactable");
        int collisionMask = (1 << walkable) | (1 << worldSolid);
        Child(room, "Environment/Floor").gameObject.layer = walkable;
        SetLayerRecursively(Child(room, "Environment/Walls"), worldSolid);
        SetLayerRecursively(Child(room, "Environment/StaticFurniture"), worldSolid);
        Child(room, "Environment/ScaleReference").gameObject.layer = worldSolid;
        cube.gameObject.layer = interactable;

        OVRQuickActionsAPI.AddOVRInteractionRig(generateAsEditableCopy: true);
        OVRCameraRig rig = One<OVRCameraRig>(scene);
        rig.transform.SetParent(Root(scene, "XR"), false);
        rig.transform.localPosition = Vector3.zero;
        rig.transform.localRotation = Quaternion.identity;
        rig.transform.localScale = Vector3.one;
        OVRManager manager = rig.GetComponent<OVRManager>();
        Require(manager != null, "The generated rig has no OVRManager.");
        var managerProperties = new SerializedObject(manager);
        Property(managerProperties, "_trackingOriginType").intValue = (int)OVRManager.TrackingOrigin.FloorLevel;
        managerProperties.ApplyModifiedPropertiesWithoutUndo();

        OVRProjectConfig projectConfig = OVRProjectConfig.CachedProjectConfig;
        Require(projectConfig != null, "OVRProjectConfig is unavailable.");
        projectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
        OVRProjectConfig.CommitProjectConfig(projectConfig);

        MetaCharacterController controller = One<MetaCharacterController>(scene);
        controller.LayerMask = collisionMask;
        EditorUtility.SetDirty(controller);
        MovingSetting moving = One<MovingSetting>(scene);
        TurningSetting turning = One<TurningSetting>(scene);
        moving.ControllerMovement.Value = MovingSetting.MovementStyle.Teleport;
        turning.ControllerTurn.Value = TurningSetting.RotationStyle.Snap;
        turning.RotationSnapAngle.Value = 30f;
        EditorUtility.SetDirty(moving);
        EditorUtility.SetDirty(turning);

        // Disable optional game-like actions for the school tour, preserving slide and turn.
        foreach (Transform child in rig.GetComponentsInChildren<Transform>(true))
            if (child.parent != null && child.parent.name == "ControllerLocomotionSlideActions"
                && (child.name == "Jump" || child.name == "Run" || child.name == "Crouch"))
                child.gameObject.SetActive(false);

        var controlsObject = new GameObject("LocomotionControls");
        controlsObject.transform.SetParent(Root(scene, "Managers"), false);
        var controls = controlsObject.AddComponent<ClassroomLocomotionControls>();
        var controlsProperties = new SerializedObject(controls);
        Property(controlsProperties, "movingSetting").objectReferenceValue = moving;
        Property(controlsProperties, "turningSetting").objectReferenceValue = turning;
        controlsProperties.ApplyModifiedPropertiesWithoutUndo();

        QuickActionsAPI.AddGrabInteraction(cube.gameObject);
        SetLayerRecursively(cube, interactable);
        Rigidbody body = cube.GetComponent<Rigidbody>();
        Require(body != null && body.useGravity && !body.isKinematic,
            "The cube must retain its original dynamic Rigidbody with gravity.");

        CreateHotspot(teleportRoot, "Start", new Vector3(0f, 0f, -1.5f));
        CreateHotspot(teleportRoot, "FrontOfTable", new Vector3(1.5f, 0f, 0f));
        CreateHotspot(teleportRoot, "LeftSide", new Vector3(-2f, 0f, 1f));
        var blocker = new GameObject("WorldBlocker");
        blocker.transform.SetParent(teleportRoot, false);
        QuickActionsAPI.AddTeleportInteraction(blocker, TeleportSurfaceType.PhysicsLayer, layerMask: collisionMask);
        TeleportInteractable blockerInteraction = blocker.GetComponentInChildren<TeleportInteractable>(true);
        Require(blockerInteraction != null, "WorldBlocker Quick Action failed.");
        blockerInteraction.AllowTeleport = false;
        blockerInteraction.TieBreakerScore = -100;
        EditorUtility.SetDirty(blockerInteraction);

        ValidateScene(scene);
        new GameObject(CompletionMarker).transform.SetParent(Root(scene, "Managers"), false);
        // The marker is a child of Managers; Root() below also locates it explicitly on reruns.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Could not save the configured Classroom scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("QUEST_CLASSROOM_SETUP_OK: real rig, collision locomotion, cube grab, 3 hotspots and blocker saved.");
    }

    private static void CreateHotspot(Transform parent, string name, Vector3 position)
    {
        var target = new GameObject(name);
        target.transform.SetParent(parent, false);
        target.transform.localPosition = position;
        var triggerObject = new GameObject("Teleport Collider");
        triggerObject.transform.SetParent(target.transform, false);
        BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.size = new Vector3(0.6f, 0.2f, 0.6f);
        trigger.center = new Vector3(0f, 0.1f, 0f);
        trigger.isTrigger = true;
        QuickActionsAPI.AddTeleportInteraction(target, TeleportSurfaceType.Hotspot, TeleportHotspotSnap.SnapPosition);

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Visible Marker";
        disc.transform.SetParent(target.transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.006f, 0f);
        disc.transform.localScale = new Vector3(0.3f, 0.005f, 0.3f);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TestCube.mat");
        Require(material != null, "The hotspot marker material is missing.");
        disc.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void ValidateScene(Scene scene)
    {
        OVRCameraRig rig = One<OVRCameraRig>(scene);
        OVRCameraRigRef rigReference = One<OVRCameraRigRef>(scene);
        Require(rigReference.CameraRig == rig, "The interaction rig is not wired to the camera rig.");
        FirstPersonLocomotor locomotor = One<FirstPersonLocomotor>(scene);
        Require(Components<PlayerLocomotor>(scene).Length == 0, "Unexpected second locomotion handler.");
        CheckReferences(locomotor, "_characterController", "_playerOrigin", "_playerEyes");
        MetaCharacterController controller = One<MetaCharacterController>(scene);
        CheckReferences(controller, "_capsule");
        int mask = LayerMask.GetMask("Walkable", "WorldSolid");
        Require(mask != 0 && controller.LayerMask.value == mask, "Incorrect player collision mask.");
        One<MovingSetting>(scene);
        One<TurningSetting>(scene);
        CheckReferences(One<ClassroomLocomotionControls>(scene), "movingSetting", "turningSetting");
        Transform cube = Child(Root(scene, "Classroom"), "InteractiveObjects/TestCube");
        Require(cube.GetComponent<Grabbable>() != null
                && cube.GetComponentsInChildren<GrabInteractable>(true).Length == 1,
            "The cube is missing its Meta grab interaction.");
        Require(Components<TeleportInteractable>(scene).Length == 4, "Expected three teleport hotspots and one blocker.");
        Transform teleportRoot = Root(scene, "TeleportTargets");
        foreach (string name in new[] { "Start", "FrontOfTable", "LeftSide" })
        {
            Transform target = Child(teleportRoot, name);
            TeleportInteractable hotspot = target.GetComponentInChildren<TeleportInteractable>(true);
            Require(hotspot != null && hotspot.AllowTeleport && !hotspot.EyeLevel && !hotspot.FaceTargetDirection,
                "Invalid hotspot: " + name);
            CheckReferences(hotspot, "_surface", "_targetPoint");
            var targetProperty = Property(new SerializedObject(hotspot), "_targetPoint");
            Require(targetProperty.objectReferenceValue == target, "Hotspot target points to the wrong transform: " + name);
        }
        TeleportInteractable blocker = Child(teleportRoot, "WorldBlocker").GetComponentInChildren<TeleportInteractable>(true);
        Require(blocker != null && !blocker.AllowTeleport, "The world teleport blocker is missing or allows teleport.");
        foreach (Transform transform in Components<Transform>(scene))
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                "Missing script on " + transform.name);
    }

    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = Property(settings, "layers");
        for (int i = 8; i < layers.arraySize; ++i)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;
            layer.stringValue = name;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return i;
        }
        throw new InvalidOperationException("No free user layer for " + name);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }

    private static T[] Components<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static T One<T>(Scene scene) where T : Component
    {
        T[] found = Components<T>(scene);
        Require(found.Length == 1, "Expected one " + typeof(T).Name + ", found " + found.Length);
        return found[0];
    }

    private static Transform Root(Scene scene, string name, bool required = true)
    {
        Transform found = name == CompletionMarker
            ? Components<Transform>(scene).FirstOrDefault(transform => transform.name == name)
            : scene.GetRootGameObjects().Where(root => root.name == name).Select(root => root.transform).SingleOrDefault();
        Require(!required || found != null, "Missing scene object: " + name);
        return found;
    }

    private static Transform Child(Transform parent, string path)
    {
        Transform found = parent.Find(path);
        Require(found != null, "Missing scene object: " + parent.name + "/" + path);
        return found;
    }

    private static SerializedProperty Property(SerializedObject target, string name)
    {
        SerializedProperty property = target.FindProperty(name);
        Require(property != null, "Missing serialized field: " + target.targetObject.GetType().Name + "." + name);
        return property;
    }

    private static void CheckReferences(Object component, params string[] names)
    {
        var serialized = new SerializedObject(component);
        foreach (string name in names)
            Require(Property(serialized, name).objectReferenceValue != null,
                "Missing reference: " + component.GetType().Name + "." + name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
