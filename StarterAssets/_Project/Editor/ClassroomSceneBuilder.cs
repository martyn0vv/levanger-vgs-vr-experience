using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Copy the _Project folder into the Assets folder of a Unity URP project.
public static class ClassroomSceneBuilder
{
    private const string ProjectPath = "Assets/_Project";
    private const string ScenePath = ProjectPath + "/Scenes/Classroom.unity";

    [MenuItem("Tools/VR Classroom/Create Test Room")]
    public static void CreateTestRoom()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("Комната не создана: требуется URP и шейдер Universal Render Pipeline/Lit.");
            return;
        }

        // Never replace an existing scene, including a scene with unsaved edits.
        if (File.Exists(ScenePath))
        {
            Scene existing = SceneManager.GetSceneByPath(ScenePath);
            if (!existing.IsValid() || !existing.isLoaded)
                existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(existing);
            Debug.Log("Classroom.unity уже существует: сцена открыта без перезаписи.");
            return;
        }

        EnsureFolder(ProjectPath);
        EnsureFolder(ProjectPath + "/Scenes");
        EnsureFolder(ProjectPath + "/Materials");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        Material floorMaterial = MakeMaterial(shader, "RoomFloor", new Color(0.55f, 0.59f, 0.63f));
        Material wallMaterial = MakeMaterial(shader, "RoomWalls", new Color(0.82f, 0.84f, 0.86f));
        Material furnitureMaterial = MakeMaterial(shader, "RoomFurniture", new Color(0.24f, 0.37f, 0.45f));
        Material cubeMaterial = MakeMaterial(shader, "TestCube", new Color(1f, 0.35f, 0.07f));
        Material rulerMaterial = MakeMaterial(shader, "ScaleReference", new Color(1f, 0.82f, 0.08f));

        Transform classroom = MakeEmpty("Classroom");
        Transform environment = MakeEmpty("Environment", classroom);
        MakeBox("Floor", environment, new Vector3(0f, -0.1f, 0f),
            new Vector3(8f, 0.2f, 6f), floorMaterial);
        Transform walls = MakeEmpty("Walls", environment);
        // The inner wall faces define an 8 x 6 m room; the floor's top is y = 0.
        MakeBox("West", walls, new Vector3(-4.1f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 6.4f), wallMaterial);
        MakeBox("East", walls, new Vector3(4.1f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 6.4f), wallMaterial);
        MakeBox("North", walls, new Vector3(0f, 1.5f, 3.1f),
            new Vector3(8f, 3f, 0.2f), wallMaterial);
        MakeBox("South", walls, new Vector3(0f, 1.5f, -3.1f),
            new Vector3(8f, 3f, 0.2f), wallMaterial);

        MakeEmpty("ScanRoot", environment);
        Transform furniture = MakeEmpty("StaticFurniture", environment);
        MakeBox("TestTable", furniture, new Vector3(1.5f, 0.4f, 0.8f),
            new Vector3(1.2f, 0.8f, 0.8f), furnitureMaterial);
        MakeBox("ScaleReference", environment, new Vector3(-3f, 0.5f, 2f),
            new Vector3(0.04f, 1f, 0.04f), rulerMaterial);

        Transform interactiveObjects = MakeEmpty("InteractiveObjects", classroom);
        GameObject cube = MakeBox("TestCube", interactiveObjects, new Vector3(1.5f, 1.05f, 0.8f),
            Vector3.one * 0.2f, cubeMaterial);
        Rigidbody body = cube.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.useGravity = true;
        body.isKinematic = false;

        // This is only a spawn marker/container: add the real XR Origin separately.
        Transform xr = MakeEmpty("XR");
        xr.position = new Vector3(0f, 0f, -1.5f);
        Transform lighting = MakeEmpty("Lighting");
        Transform sunTransform = MakeEmpty("Directional Light", lighting);
        sunTransform.rotation = Quaternion.Euler(45f, -30f, 0f);
        Light sun = sunTransform.gameObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1f;
        sun.shadows = LightShadows.None;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
        MakeEmpty("Audio");
        MakeEmpty("Managers");
        MakeEmpty("TeleportTargets");

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("Не удалось сохранить Classroom.unity. Созданная сцена остаётся открытой.");
            return;
        }
        Selection.activeGameObject = classroom.gameObject;
        Debug.Log("Тестовая комната сохранена: " + ScenePath
            + ". Размер 8 × 6 м; 1 Unity unit = 1 м; XR spawn (0, 0, -1.5)."
            + " Добавьте VR-риг, захват предметов и телепортацию по руководству.");
    }

    private static Transform MakeEmpty(string name, Transform parent = null)
    {
        Transform result = new GameObject(name).transform;
        result.SetParent(parent, false);
        return result;
    }

    private static GameObject MakeBox(string name, Transform parent, Vector3 position,
        Vector3 size, Material material)
    {
        GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
        result.name = name;
        result.transform.SetParent(parent, false);
        result.transform.localPosition = position;
        result.transform.localScale = size;
        result.GetComponent<Renderer>().sharedMaterial = material;
        // A Cube primitive already has its BoxCollider; do not add a second one.
        return result;
    }

    private static Material MakeMaterial(Shader shader, string name, Color color)
    {
        Material material = new Material(shader) { name = name, color = color };
        string path = AssetDatabase.GenerateUniqueAssetPath(ProjectPath + "/Materials/" + name + ".mat");
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
