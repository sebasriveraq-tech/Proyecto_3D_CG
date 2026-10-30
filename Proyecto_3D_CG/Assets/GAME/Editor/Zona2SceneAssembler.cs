using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca los prefabs reutilizables y el agente de prueba en la escena de Zona 2.
/// El comando es idempotente: puede ejecutarse de nuevo sin duplicar objetos.
/// </summary>
public static class Zona2SceneAssembler
{
    private const string ScenePath = "Assets/GAME/Scenes/Zona2_Laboratorio.unity";
    private const string InteractablesFolder = "Assets/GAME/Prefabs/Interactables/";
    private const string GroupName = "Interactuables_Zona2";

    [MenuItem("Tools/Laboratorio/Acomodar prefabs en Zona 2")]
    public static void AcomodarPrefabs()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog("Zona 2", "Abre la escena Zona2_Laboratorio antes de acomodar los prefabs.", "Aceptar");
            return;
        }

        GameObject group = FindRoot(scene, GroupName);
        if (group == null)
        {
            group = new GameObject(GroupName);
            SceneManager.MoveGameObjectToScene(group, scene);
            Undo.RegisterCreatedObjectUndo(group, "Crear grupo de interactuables de Zona 2");
        }

        CrearPiso(scene);
        DesactivarCamaraSobrante();

        ColocarPrefab(scene, group, "Puerta_Zona2", new Vector3(0f, 1.3f, 18.5f), 180f);
        ColocarPrefab(scene, group, "Terminal_Registro", new Vector3(-5f, 0.7f, 12f), 150f);
        ColocarPrefab(scene, group, "Recogible_Credencial", new Vector3(-2f, 0.2f, 6f), 20f);
        ColocarPrefab(scene, group, "Boton", new Vector3(-2f, 0.05f, 16f), 0f);
        ColocarPrefab(scene, group, "Interruptor", new Vector3(2f, 0.45f, 16f), 0f);
        ColocarPrefab(scene, group, "Cajon", new Vector3(5f, 0.325f, 8f), 180f);
        ColocarPrefab(scene, group, "Tarjeta", new Vector3(2f, 0.05f, 7f), 35f);

        ConfigurarObjetosZona2(scene);
        ColocarAgente(scene, group);
        CrearNavMesh(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Zona 2 preparada: piso, interactuables, agente y NavMesh guardados.");
    }

    private static void CrearPiso(Scene scene)
    {
        if (FindInScene(scene, "Piso_Laboratorio_Zona2") != null) return;

        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piso.name = "Piso_Laboratorio_Zona2";
        piso.transform.position = new Vector3(0f, -0.25f, 8f);
        piso.transform.localScale = new Vector3(20f, 0.5f, 26f);
        piso.isStatic = true;
        Renderer renderer = piso.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/GAME/Materials/Interactuable_Gris.mat");
        SceneManager.MoveGameObjectToScene(piso, scene);
        Undo.RegisterCreatedObjectUndo(piso, "Crear piso de Zona 2");
    }

    private static void DesactivarCamaraSobrante()
    {
        GameObject camera = GameObject.Find("Main Camera");
        if (camera != null && camera.activeSelf)
        {
            Undo.RecordObject(camera, "Desactivar cámara duplicada de Zona 2");
            camera.SetActive(false);
            EditorUtility.SetDirty(camera);
        }
    }

    private static void ColocarPrefab(Scene scene, GameObject group, string prefabName,
        Vector3 position, float yaw)
    {
        if (FindInScene(scene, prefabName) != null) return;

        string path = InteractablesFolder + prefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("No se encontró el prefab: " + path);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null) return;
        Undo.RegisterCreatedObjectUndo(instance, "Colocar " + prefabName + " en Zona 2");
        instance.transform.SetParent(group.transform, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static void ConfigurarObjetosZona2(Scene scene)
    {
        Puerta puerta = FindInScene(scene, "Puerta_Zona2")?.GetComponent<Puerta>();
        if (puerta != null)
        {
            puerta.idLlave = "credencial_z2";
            puerta.idPuerta = "puerta_z3";
            puerta.escenaDestino = "Zona3_Control";
            puerta.mensajePrompt = "Presiona E para salir al siguiente sector";
            PrefabUtility.RecordPrefabInstancePropertyModifications(puerta);
        }

        Recogible credencial = FindInScene(scene, "Recogible_Credencial")?.GetComponent<Recogible>();
        if (credencial != null)
        {
            credencial.idItem = "credencial_z2";
            credencial.nombreVisible = "Credencial del laboratorio";
            PrefabUtility.RecordPrefabInstancePropertyModifications(credencial);
        }

        Terminal terminal = FindInScene(scene, "Terminal_Registro")?.GetComponent<Terminal>();
        if (terminal != null)
        {
            terminal.idCodigo = "codigo_z2";
            terminal.codigoVisible = "LAB-2381";
            PrefabUtility.RecordPrefabInstancePropertyModifications(terminal);
        }

        Recogible tarjeta = FindInScene(scene, "Tarjeta")?.GetComponent<Recogible>();
        if (tarjeta != null)
        {
            tarjeta.idItem = "tarjeta_acceso_z2";
            tarjeta.nombreVisible = "Tarjeta de acceso Z2";
            PrefabUtility.RecordPrefabInstancePropertyModifications(tarjeta);
        }
    }

    private static void ColocarAgente(Scene scene, GameObject group)
    {
        if (FindInScene(scene, "Agente") != null) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GAME/Prefabs/Agents/Agente.prefab");
        if (prefab == null)
        {
            Debug.LogError("No se encontró Assets/GAME/Prefabs/Agents/Agente.prefab");
            return;
        }

        GameObject agente = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (agente == null) return;
        Undo.RegisterCreatedObjectUndo(agente, "Colocar agente en Zona 2");
        agente.transform.SetParent(group.transform, false);
        agente.transform.SetPositionAndRotation(new Vector3(4f, 0f, 14f), Quaternion.Euler(0f, 210f, 0f));
    }

    private static void CrearNavMesh(Scene scene)
    {
        GameObject navMeshObject = FindRoot(scene, "NavMesh_Zona2");
        if (navMeshObject == null)
        {
            navMeshObject = new GameObject("NavMesh_Zona2");
            SceneManager.MoveGameObjectToScene(navMeshObject, scene);
            Undo.RegisterCreatedObjectUndo(navMeshObject, "Crear NavMesh de Zona 2");
        }

        NavMeshSurface surface = navMeshObject.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(navMeshObject);
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        if (surface.navMeshData == null)
            surface.BuildNavMesh();

        if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
        {
            const string folder = "Assets/GAME/Scenes/Zona2_Laboratorio";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/GAME/Scenes", "Zona2_Laboratorio");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/NavMesh-Zona2.asset");
            AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
            AssetDatabase.SaveAssets();
        }
        EditorUtility.SetDirty(navMeshObject);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item.gameObject;
        }
        return null;
    }
}
