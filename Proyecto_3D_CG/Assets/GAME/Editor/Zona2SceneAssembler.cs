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
        CrearParedes(scene);
        DesactivarCamaraSobrante();

        ColocarPrefab(scene, group, "Puerta_Zona2", new Vector3(0f, 1.3f, 20.65f), 180f);
        ColocarPrefab(scene, group, "Terminal_Registro", new Vector3(-5f, 0.7f, 12f), 150f);
        ColocarPrefab(scene, group, "Recogible_Credencial", new Vector3(-2f, 0.2f, 6f), 20f);
        ColocarPrefab(scene, group, "Boton", new Vector3(-2f, 0.05f, 16f), 0f);
        ColocarPrefab(scene, group, "Interruptor", new Vector3(2f, 0.45f, 16f), 0f);
        ColocarPrefab(scene, group, "Cajon", new Vector3(5f, 0.325f, 8f), 180f);
        ColocarPrefab(scene, group, "Tarjeta", new Vector3(2f, 0.05f, 7f), 35f);

        ConfigurarObjetosZona2(scene);
        ColocarAgente(scene, group);
        ConfigurarRutaPatrulla(scene, group);
        ConfigurarControladorZona2(scene);
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

    private static void CrearParedes(Scene scene)
    {
        Material acero = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/GAME/Materials/Interactuable_Acero.mat");

        CrearPared(scene, "Pared_Oeste_Zona2",
            new Vector3(-10f, 2.5f, 8f), new Vector3(0.5f, 5f, 26f), acero);
        CrearPared(scene, "Pared_Este_Zona2",
            new Vector3(10f, 2.5f, 8f), new Vector3(0.5f, 5f, 26f), acero);
        CrearPared(scene, "Pared_Fondo_Zona2",
            new Vector3(0f, 2.5f, -5f), new Vector3(20f, 5f, 0.5f), acero);
        CrearPared(scene, "Pared_Salida_Izquierda_Zona2",
            new Vector3(-5.75f, 2.5f, 20.75f), new Vector3(8.5f, 5f, 0.5f), acero);
        CrearPared(scene, "Pared_Salida_Derecha_Zona2",
            new Vector3(5.75f, 2.5f, 20.75f), new Vector3(8.5f, 5f, 0.5f), acero);
        CrearPared(scene, "Dintel_Salida_Zona2",
            new Vector3(0f, 4.3f, 20.75f), new Vector3(3f, 1.4f, 0.5f), acero);
    }

    private static void CrearPared(Scene scene, string nombre, Vector3 position,
        Vector3 scale, Material material)
    {
        if (FindInScene(scene, nombre) != null) return;

        GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pared.name = nombre;
        pared.transform.position = position;
        pared.transform.localScale = scale;
        pared.isStatic = true;
        Renderer renderer = pared.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        SceneManager.MoveGameObjectToScene(pared, scene);
        Undo.RegisterCreatedObjectUndo(pared, "Crear " + nombre);
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

    private static void ConfigurarRutaPatrulla(Scene scene, GameObject group)
    {
        GameObject ruta = FindInScene(scene, "PuntosPatrulla_Zona2");
        if (ruta == null)
        {
            ruta = new GameObject("PuntosPatrulla_Zona2");
            ruta.transform.SetParent(group.transform, false);
            Undo.RegisterCreatedObjectUndo(ruta, "Crear ruta de patrulla de Zona 2");
        }

        Vector3[] posiciones =
        {
            new Vector3(4f, 0f, 14f),
            new Vector3(4f, 0f, 8f),
            new Vector3(-4f, 0f, 8f),
            new Vector3(-4f, 0f, 14f)
        };
        Transform[] puntos = new Transform[posiciones.Length];
        for (int i = 0; i < posiciones.Length; i++)
        {
            string nombre = "Punto_Patrulla_Z2_" + (i + 1);
            Transform punto = ruta.transform.Find(nombre);
            if (punto == null)
            {
                GameObject nuevoPunto = new GameObject(nombre);
                nuevoPunto.transform.SetParent(ruta.transform, false);
                nuevoPunto.transform.position = posiciones[i];
                punto = nuevoPunto.transform;
            }
            puntos[i] = punto;
        }

        AgentController controladorAgente = FindInScene(scene, "Agente")?.GetComponent<AgentController>();
        if (controladorAgente != null)
        {
            controladorAgente.puntosPatrulla = puntos;
            EditorUtility.SetDirty(controladorAgente);
        }
    }

    private static void ConfigurarControladorZona2(Scene scene)
    {
        GameObject objeto = FindInScene(scene, "LaboratorioController_Zona2");
        if (objeto == null)
        {
            objeto = new GameObject("LaboratorioController_Zona2");
            SceneManager.MoveGameObjectToScene(objeto, scene);
            Undo.RegisterCreatedObjectUndo(objeto, "Crear controlador de Zona 2");
        }

        LaboratorioController controlador = objeto.GetComponent<LaboratorioController>();
        if (controlador == null)
            controlador = Undo.AddComponent<LaboratorioController>(objeto);

        GameObject luzObjeto = FindInScene(scene, "Luz_Emergencia_Zona2");
        if (luzObjeto == null)
        {
            luzObjeto = new GameObject("Luz_Emergencia_Zona2");
            SceneManager.MoveGameObjectToScene(luzObjeto, scene);
            Undo.RegisterCreatedObjectUndo(luzObjeto, "Crear luz de emergencia");
        }
        luzObjeto.transform.position = new Vector3(0f, 4.2f, 12f);
        Light luz = luzObjeto.GetComponent<Light>();
        if (luz == null) luz = Undo.AddComponent<Light>(luzObjeto);
        luz.type = LightType.Point;
        luz.range = 15f;
        luz.intensity = 0.35f;
        luz.color = new Color(1f, 0.16f, 0.12f);
        luz.enabled = false;

        GameObject objetoChispas = FindInScene(scene, "Chispas_Energia_Zona2");
        if (objetoChispas == null)
        {
            objetoChispas = new GameObject("Chispas_Energia_Zona2");
            SceneManager.MoveGameObjectToScene(objetoChispas, scene);
            Undo.RegisterCreatedObjectUndo(objetoChispas, "Crear chispas de energía");
        }
        InterruptorInteractuable interruptor = FindInScene(scene, "Interruptor")?.GetComponent<InterruptorInteractuable>();
        if (interruptor != null)
            objetoChispas.transform.position = interruptor.transform.position + Vector3.up * 0.6f;

        ParticleSystem chispas = objetoChispas.GetComponent<ParticleSystem>();
        if (chispas == null) chispas = Undo.AddComponent<ParticleSystem>(objetoChispas);
        ParticleSystem.MainModule main = chispas.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.8f;
        main.startLifetime = 0.45f;
        main.startSpeed = 1.6f;
        main.startSize = 0.09f;
        main.startColor = new Color(0.2f, 0.85f, 1f);
        ParticleSystem.EmissionModule emission = chispas.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
        ParticleSystem.ShapeModule shape = chispas.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        controlador.interruptorEnergia = interruptor;
        controlador.botonAlarma = FindInScene(scene, "Boton")?.GetComponent<BotonInteractuable>();
        controlador.terminal = FindInScene(scene, "Terminal_Registro")?.GetComponent<Terminal>();
        controlador.puertaSalida = FindInScene(scene, "Puerta_Zona2")?.GetComponent<Puerta>();
        controlador.agente = FindInScene(scene, "Agente")?.GetComponent<AgentController>();
        controlador.luzEmergencia = luz;
        controlador.chispasEnergia = chispas;
        if (controlador.puertaSalida != null)
            controlador.puertaSalida.idCodigoRequerido = "codigo_z2";
        if (controlador.terminal != null)
        {
            controlador.terminal.idCodigo = "codigo_z2";
            controlador.terminal.codigoVisible = "LAB-2381";
            controlador.terminal.requiereEnergia = true;
        }
        EditorUtility.SetDirty(controlador);
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
        surface.BuildNavMesh();

        if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
        {
            const string folder = "Assets/GAME/Scenes/Zona2_Laboratorio";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/GAME/Scenes", "Zona2_Laboratorio");
            string assetPath = folder + "/NavMesh-Zona2.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
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
