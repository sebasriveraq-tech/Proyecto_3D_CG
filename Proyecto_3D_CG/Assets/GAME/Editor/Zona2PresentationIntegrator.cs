using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Zona2PresentationIntegrator
{
    private const string ScenePath = "Assets/GAME/Scenes/Zona2_Laboratorio.unity";
    private const string RootName = "Ambientacion_Industrial_Zona2";
    private const string Models = "Assets/Models/Laboratorio/";

    [MenuItem("Tools/Laboratorio/Iniciar Play Mode Zona 2")]
    public static void StartReviewPlayMode()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorUtility.DisplayDialog("Zona 2", "Abre Zona2_Laboratorio antes de iniciar la prueba.", "Aceptar");
            return;
        }
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Laboratorio/Detener Play Mode Zona 2")]
    public static void StopReviewPlayMode()
    {
        EditorApplication.isPlaying = false;
    }

    [MenuItem("Tools/Laboratorio/Integrar modelos y pulir partículas Zona 2")]
    public static void Integrate()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog("Zona 2", "Abre Zona2_Laboratorio antes de integrar los modelos.", "Aceptar");
            return;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureMaterialsAndImporters();
        var root = GameObject.Find(RootName) ?? new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        CreateBench(root.transform);
        PlaceModel(root.transform, "industrial_microscope", new Vector3(-7f, 0.94f, 10f), 1.1f, false);
        PlaceModel(root.transform, "industrial_storage_cart", new Vector3(7.2f, 0f, 11f), 1.4f, false);
        PlaceModel(root.transform, "portable_generator", new Vector3(7.1f, 0f, 4.2f), 1.6f, false);
        PlaceModel(root.transform, "modular_industrial_pipes_01", new Vector3(8.9f, 1.8f, 9f), 5.5f, true);
        CreatePipeSupports(root.transform);

        ConfigureEnvironmentSurfaces(scene);
        ConfigureSparks(scene);
        AlignDoor(scene);
        RebuildNavMesh(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Zona 2: modelos CC0 integrados, partículas ajustadas y escena guardada.");
    }

    [MenuItem("Tools/Laboratorio/Inspeccionar y ajustar puerta al hueco de Zona 2")]
    public static void InspectAndFitDoorToOpening()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog("Zona 2", "Abre Zona2_Laboratorio antes de ajustar la puerta.", "Aceptar");
            return;
        }

        var leftWall = GameObject.Find("Pared_Salida_Izquierda_Zona2")?.GetComponent<Collider>();
        var rightWall = GameObject.Find("Pared_Salida_Derecha_Zona2")?.GetComponent<Collider>();
        var lintel = GameObject.Find("Dintel_Salida_Zona2")?.GetComponent<Collider>();
        var floor = GameObject.Find("Piso_Laboratorio_Zona2")?.GetComponent<Collider>();
        var door = GameObject.Find("Puerta_Zona2");
        var doorCollider = door != null ? door.GetComponent<BoxCollider>() : null;
        var doorRenderer = door != null ? door.GetComponent<Renderer>() : null;
        var doorLogic = door != null ? door.GetComponent<Puerta>() : null;

        if (leftWall == null || rightWall == null || lintel == null || floor == null ||
            door == null || doorCollider == null || doorRenderer == null || doorLogic == null)
        {
            EditorUtility.DisplayDialog("Zona 2", "Falta la puerta o alguno de los colliders que definen el hueco.", "Aceptar");
            return;
        }

        var leftBounds = leftWall.bounds;
        var rightBounds = rightWall.bounds;
        var lintelBounds = lintel.bounds;
        var floorBounds = floor.bounds;
        var openingWidth = rightBounds.min.x - leftBounds.max.x;
        var openingHeight = lintelBounds.min.y - floorBounds.max.y;
        var before = doorRenderer.bounds.size;
        var boxBefore = doorCollider.bounds.size;
        var animators = door.GetComponentsInChildren<Animator>(true).Length;
        var legacyAnimations = door.GetComponentsInChildren<Animation>(true).Length;

        Debug.Log($"Zona 2 puerta antes del ajuste: hueco {openingWidth:F3} x {openingHeight:F3} m; " +
                  $"Renderer {before.x:F3} x {before.y:F3} x {before.z:F3} m; " +
                  $"BoxCollider {boxBefore.x:F3} x {boxBefore.y:F3} x {boxBefore.z:F3} m; " +
                  $"pivote {door.transform.position}; escala {door.transform.lossyScale}; " +
                  $"Animator={animators}, Animation={legacyAnimations}, " +
                  $"apertura por Puerta.cs ({doorLogic.duracionApertura:F2} s, elevación 2.50 m).");

        if (openingWidth <= 0f || openingHeight <= 0f)
        {
            EditorUtility.DisplayDialog("Zona 2", "Las medidas de los colliders no producen un hueco válido.", "Aceptar");
            return;
        }

        Undo.RecordObject(door.transform, "Ajustar dimensiones de puerta Zona 2");
        Undo.RecordObject(doorCollider, "Ajustar collider de puerta Zona 2");

        const float sideClearance = 0.06f;
        const float topClearance = 0.10f;
        const float liftDistance = 2.5f;
        var targetWidth = openingWidth - sideClearance * 2f;
        var targetHeight = openingHeight - topClearance;
        var targetDepth = Mathf.Min(doorCollider.bounds.size.z, lintelBounds.size.z * 0.4f);
        targetDepth = Mathf.Max(0.05f, targetDepth);

        var parentScale = door.transform.parent != null
            ? door.transform.parent.lossyScale
            : Vector3.one;
        door.transform.localScale = new Vector3(
            targetWidth / Mathf.Max(0.0001f, parentScale.x),
            targetHeight / Mathf.Max(0.0001f, parentScale.y),
            targetDepth / Mathf.Max(0.0001f, parentScale.z));
        door.transform.position = new Vector3(
            (leftBounds.max.x + rightBounds.min.x) * 0.5f,
            floorBounds.max.y + targetHeight * 0.5f,
            lintelBounds.center.z);

        doorCollider.center = Vector3.zero;
        doorCollider.size = Vector3.one;
        doorCollider.isTrigger = false;
        doorCollider.enabled = true;

        PrefabUtility.RecordPrefabInstancePropertyModifications(door.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(doorCollider);
        EditorUtility.SetDirty(door.transform);
        EditorUtility.SetDirty(doorCollider);

        Physics.SyncTransforms();
        var after = doorRenderer.bounds;
        var afterCollider = doorCollider.bounds;
        var openClearHeight = after.min.y + liftDistance - floorBounds.max.y;
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GAME/Prefabs/Player/Player.prefab");
        var playerHeight = playerPrefab != null && playerPrefab.GetComponent<CharacterController>() != null
            ? playerPrefab.GetComponent<CharacterController>().height
            : 2f;

        Debug.Log($"Zona 2 puerta después del ajuste: hueco {openingWidth:F3} x {openingHeight:F3} m; " +
                  $"Renderer {after.size.x:F3} x {after.size.y:F3} x {after.size.z:F3} m; " +
                  $"BoxCollider {afterCollider.size.x:F3} x {afterCollider.size.y:F3} x {afterCollider.size.z:F3} m; " +
                  $"holguras laterales {((openingWidth - after.size.x) * 0.5f):F3} m, superior {(openingHeight - after.size.y):F3} m; " +
                  $"paso calculado al elevar la puerta 2.50 m: {openClearHeight:F3} m para un jugador de {playerHeight:F2} m. " +
                  "Se conservaron el prefab, su pivote central, el collider no-trigger y Puerta.cs.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Laboratorio/Revisión puerta - frente cerrada")]
    public static void ReviewDoorFrontClosed()
    {
        var door = GameObject.Find("Puerta_Zona2");
        var floor = GameObject.Find("Piso_Laboratorio_Zona2")?.GetComponent<Collider>();
        if (door == null || floor == null) return;
        SetDoorClosedPose(door, floor.bounds.max.y);
        SetDoorReviewView(door, new Vector3(0f, 0f, -8f), 4.2f, true);
    }

    [MenuItem("Tools/Laboratorio/Revisión puerta - ángulo lateral cerrada")]
    public static void ReviewDoorSideClosed()
    {
        var door = GameObject.Find("Puerta_Zona2");
        var floor = GameObject.Find("Piso_Laboratorio_Zona2")?.GetComponent<Collider>();
        if (door == null || floor == null) return;
        SetDoorClosedPose(door, floor.bounds.max.y);
        SetDoorReviewView(door, new Vector3(4f, 1.2f, -6f), 8f, false);
    }

    [MenuItem("Tools/Laboratorio/Revisión puerta - vista abierta")]
    public static void ReviewDoorOpen()
    {
        var door = GameObject.Find("Puerta_Zona2");
        var floor = GameObject.Find("Piso_Laboratorio_Zona2")?.GetComponent<Collider>();
        if (door == null || floor == null) return;
        SetDoorClosedPose(door, floor.bounds.max.y);
        var position = door.transform.position;
        position.y += 2.5f;
        door.transform.position = position;
        Physics.SyncTransforms();
        SetDoorReviewView(door, new Vector3(0f, 0f, -8f), 6f, true);
    }

    [MenuItem("Tools/Laboratorio/Revisión puerta - conjunto cerrado")]
    public static void ReviewDoorOverviewClosed()
    {
        var door = GameObject.Find("Puerta_Zona2");
        var floor = GameObject.Find("Piso_Laboratorio_Zona2")?.GetComponent<Collider>();
        if (door == null || floor == null) return;
        SetDoorClosedPose(door, floor.bounds.max.y);
        SetDoorReviewView(door, new Vector3(6f, 4f, -12f), 16f, false);
    }

    [MenuItem("Tools/Laboratorio/Revisión puerta - restaurar cerrada y guardar")]
    public static void RestoreDoorClosed()
    {
        var scene = SceneManager.GetActiveScene();
        var door = GameObject.Find("Puerta_Zona2");
        var floor = GameObject.Find("Piso_Laboratorio_Zona2")?.GetComponent<Collider>();
        if (scene.path != ScenePath || door == null || floor == null) return;
        SetDoorClosedPose(door, floor.bounds.max.y);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void SetDoorClosedPose(GameObject door, float floorHeight)
    {
        var renderer = door.GetComponent<Renderer>();
        if (renderer == null) return;
        var position = door.transform.position;
        position.y = floorHeight + renderer.bounds.size.y * 0.5f;
        door.transform.position = position;
        Physics.SyncTransforms();
    }

    private static void SetDoorReviewView(GameObject door, Vector3 cameraOffset, float size, bool orthographic)
    {
        var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        var target = door.transform.position;
        var cameraPosition = target + cameraOffset;
        view.pivot = target;
        view.rotation = Quaternion.LookRotation(cameraPosition - target, Vector3.up);
        view.size = size;
        view.orthographic = orthographic;
        Selection.activeGameObject = door;
        view.Repaint();
    }

    private static void ConfigureMaterialsAndImporters()
    {
        foreach (var id in new[] { "industrial_microscope", "industrial_storage_cart", "portable_generator", "modular_industrial_pipes_01" })
        {
            var path = Models + id + "/" + id + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
                importer.SaveAndReimport();
            }

            var diffusePath = Models + id + "/textures/" + id + "_diff_1k.jpg";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            var materialFolder = "Assets/Materials/Laboratorio";
            if (!AssetDatabase.IsValidFolder(materialFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
                AssetDatabase.CreateFolder("Assets/Materials", "Laboratorio");
            }
            var matPath = materialFolder + "/MAT_" + id + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            }
            EditorUtility.SetDirty(mat);
        }
    }

    private static void CreateBench(Transform parent)
    {
        var existing = parent.Find("Mesa_Investigacion_Zona2");
        if (existing != null) return;
        var bench = new GameObject("Mesa_Investigacion_Zona2");
        bench.transform.SetParent(parent, false);
        bench.transform.position = new Vector3(-7f, 0f, 10f);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/GAME/Materials/Interactuable_Acero.mat");
        MakePart(bench.transform, "Superficie", new Vector3(0f, 0.88f, 0f), new Vector3(2.2f, 0.12f, 1.1f), material);
        MakePart(bench.transform, "Cuerpo", new Vector3(0f, 0.48f, 0f), new Vector3(1.8f, 0.68f, 0.82f), material);
        MakePart(bench.transform, "Cajonera", new Vector3(-0.72f, 0.43f, 0f), new Vector3(0.42f, 0.62f, 0.86f), material);
        MakePart(bench.transform, "Pata_Derecha", new Vector3(0.86f, 0.42f, 0f), new Vector3(0.12f, 0.84f, 0.9f), material);
        var scope = parent.Find("Modelo_industrial_microscope");
        if (scope != null) scope.position = new Vector3(-7f, 0.94f, 10f);
    }

    private static void MakePart(Transform parent, string name, Vector3 pos, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.isStatic = true;
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void PlaceModel(Transform parent, string id, Vector3 position, float targetSize, bool alignLongAxis)
    {
        var nodeName = "Modelo_" + id;
        var existing = parent.Find(nodeName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + id + "/" + id + ".fbx");
        if (prefab == null) throw new FileNotFoundException("Unity no importó el modelo " + id);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) throw new System.InvalidOperationException("No se pudo crear instancia de " + id);
        instance.name = nodeName;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new System.InvalidOperationException("El FBX no contiene Renderer: " + id);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (alignLongAxis && bounds.size.x > bounds.size.z + 0.05f)
            instance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        bounds = GetBounds(instance);
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest > 0.0001f) instance.transform.localScale *= targetSize / largest;
        bounds = GetBounds(instance);
        instance.transform.position += new Vector3(position.x - bounds.center.x, position.y - bounds.min.y, position.z - bounds.center.z);

        foreach (var c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Laboratorio/MAT_" + id + ".mat");
            if (mat != null) r.sharedMaterial = mat;
        }
        var box = instance.GetComponent<BoxCollider>();
        if (box == null) box = instance.AddComponent<BoxCollider>();
        box.center = instance.transform.InverseTransformPoint(bounds.center);
        var scale = instance.transform.lossyScale;
        box.size = new Vector3(bounds.size.x / Mathf.Max(0.001f, scale.x), bounds.size.y / Mathf.Max(0.001f, scale.y), bounds.size.z / Mathf.Max(0.001f, scale.z));
        box.enabled = true;
        Undo.RegisterCreatedObjectUndo(instance, "Integrar " + id + " en Zona 2");
    }

    private static void ConfigureEnvironmentSurfaces(Scene scene)
    {
        var floor = SurfaceMaterial("MAT_Piso_ConcretoAbandonado", "concrete_floor_worn_001", 0.2f, 0.32f, new Vector2(8f, 10f));
        var wall = SurfaceMaterial("MAT_Pared_MetalDeteriorado", "rusty_metal_03", 0.62f, 0.28f, new Vector2(3f, 2f));
        var floorObject = GameObject.Find("Piso_Laboratorio_Zona2");
        if (floorObject != null)
        {
            AssignMaterial(floorObject, floor);
            if (floorObject.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(floorObject);
        }

        string[] wallNames = { "Pared_Oeste_Zona2", "Pared_Este_Zona2", "Pared_Fondo_Zona2", "Pared_Salida_Izquierda_Zona2", "Pared_Salida_Derecha_Zona2", "Dintel_Salida_Zona2" };
        foreach (var name in wallNames)
        {
            var obj = GameObject.Find(name);
            if (obj == null) continue;
            AssignMaterial(obj, wall);
            if (obj.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(obj);
        }

        var door = GameObject.Find("Puerta_Zona2");
        if (door != null) AssignMaterial(door, wall);

        var agent = GameObject.Find("Agente");
        if (agent != null)
        {
            var body = agent.transform.Find("Cuerpo");
            var renderer = body != null ? body.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                var material = new Material(wall);
                material.name = "MAT_Agente_Experimental";
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.18f, 0.28f, 0.34f));
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.82f);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(0.02f, 0.32f, 0.48f) * 1.6f);
                }
                const string agentMatPath = "Assets/Materials/Laboratorio/MAT_Agente_Experimental.mat";
                AssetDatabase.DeleteAsset(agentMatPath);
                AssetDatabase.CreateAsset(material, agentMatPath);
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
            if (agent.GetComponentInChildren<CapsuleCollider>() == null)
            {
                var capsule = Undo.AddComponent<CapsuleCollider>(agent);
                capsule.height = 2f;
                capsule.radius = 0.42f;
                capsule.center = new Vector3(0f, 1f, 0f);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void CreatePipeSupports(Transform parent)
    {
        var pipes = parent.Find("Soportes_Tuberias_Zona2");
        if (pipes != null) return;
        pipes = new GameObject("Soportes_Tuberias_Zona2").transform;
        pipes.SetParent(parent, false);
        var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Laboratorio/MAT_Pared_MetalDeteriorado.mat");
        foreach (var z in new[] { 6.5f, 11.5f })
        {
            var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.name = "Abrazadera_Tuberia";
            bracket.transform.SetParent(pipes, false);
            bracket.transform.position = new Vector3(9.42f, 2.35f, z);
            bracket.transform.localScale = new Vector3(0.72f, 0.12f, 0.16f);
            bracket.isStatic = true;
            if (metal != null) bracket.GetComponent<Renderer>().sharedMaterial = metal;
        }
    }

    private static Material SurfaceMaterial(string materialName, string textureName, float metallic, float smoothness, Vector2 tiling)
    {
        const string folder = "Assets/Materials/Laboratorio";
        var path = folder + "/" + materialName + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.name = materialName;
            AssetDatabase.CreateAsset(mat, path);
        }
        var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Laboratorio/" + textureName + "_Diffuse_1k.jpg");
        var normalPath = "Assets/Textures/Laboratorio/" + textureName + "_nor_gl_1k.jpg";
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diffuse);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diffuse);
        if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normal);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling);
        if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", tiling);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void AssignMaterial(GameObject obj, Material mat)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null || mat == null) return;
        renderer.sharedMaterial = mat;
        EditorUtility.SetDirty(renderer);
    }

    private static void RebuildNavMesh(Scene scene)
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var surface in surfaces)
        {
            if (surface.gameObject.scene != scene) continue;
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
        }
    }

    private static Bounds GetBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void ConfigureSparks(Scene scene)
    {
        var sparks = GameObject.Find("Chispas_Energia_Zona2");
        if (sparks == null) return;
        var ps = sparks.GetComponent<ParticleSystem>();
        if (ps == null) return;
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(new Gradient
        {
            colorKeys = new[] { new GradientColorKey(new Color(0.25f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.9f, 1f, 1f), 0.35f), new GradientColorKey(new Color(0.1f, 0.45f, 1f), 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.95f, 0.12f), new GradientAlphaKey(0f, 1f) }
        });
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0.08f)));
        EditorUtility.SetDirty(ps);
    }

    private static void AlignDoor(Scene scene)
    {
        var door = GameObject.Find("Puerta_Zona2");
        if (door == null) return;
        Undo.RecordObject(door.transform, "Alinear puerta de Zona 2");
        var p = door.transform.position;
        p.z = 20.75f;
        door.transform.position = p;
        EditorUtility.SetDirty(door.transform);
    }
}



