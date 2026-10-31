using UnityEditor;
using UnityEngine;

/// Crea la librería de interactuables básicos del proyecto sin duplicar assets existentes.
public static class InteractablePrefabBuilder
{
    private const string Folder = "Assets/GAME/Prefabs/Interactables";
    private const string MaterialFolder = "Assets/GAME/Materials";

    [MenuItem("Tools/Laboratorio/Crear prefabs interactuables")]
    public static void CrearPrefabs()
    {
        EnsureFolder();
        CrearMaterial("Interactuable_Acero", new Color(0.16f, 0.2f, 0.23f));
        CrearMaterial("Interactuable_Gris", new Color(0.25f, 0.29f, 0.31f));
        CrearMaterial("Interactuable_GrisClaro", new Color(0.36f, 0.4f, 0.42f));
        CrearMaterial("Interactuable_Cian", new Color(0.2f, 0.72f, 0.76f));
        CrearMaterial("Interactuable_Oro", new Color(0.85f, 0.72f, 0.35f));
        CrearMaterial("Interactuable_Rojo", Color.red);
        CrearMaterial("Interactuable_Verde", Color.green);
        CrearBoton();
        CrearInterruptor();
        CrearCajon();
        CrearTarjeta();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Librería de prefabs interactuables creada o ya existente en " + Folder);
    }

    private static Material CrearMaterial(string nombre, Color color)
    {
        string path = MaterialFolder + "/" + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = nombre, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/GAME/Editor"))
            AssetDatabase.CreateFolder("Assets/GAME", "Editor");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/GAME", "Materials");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/GAME/Prefabs", "Interactables");
    }

    private static GameObject NuevoInteractuable(string nombre, string prompt, Vector3 colliderSize)
    {
        GameObject root = new GameObject(nombre);
        root.AddComponent<BoxCollider>().size = colliderSize;
        root.AddComponent<AudioSource>().playOnAwake = false;
        var interactuable = root.AddComponent<BotonInteractuable>();
        interactuable.mensajePrompt = prompt;
        return root;
    }

    private static GameObject CrearVisual(GameObject parent, string nombre, PrimitiveType forma,
        Vector3 posicion, Vector3 escala, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(forma);
        visual.name = nombre;
        visual.transform.SetParent(parent.transform, false);
        visual.transform.localPosition = posicion;
        visual.transform.localScale = escala;
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = visual.GetComponent<Renderer>();
        string materialName = color == Color.red ? "Interactuable_Rojo"
            : color == Color.green ? "Interactuable_Verde"
            : color == new Color(0.2f, 0.72f, 0.76f) ? "Interactuable_Cian"
            : color == new Color(0.85f, 0.72f, 0.35f) ? "Interactuable_Oro"
            : color == new Color(0.25f, 0.29f, 0.31f) ? "Interactuable_Gris"
            : color == new Color(0.36f, 0.4f, 0.42f) ? "Interactuable_GrisClaro"
            : "Interactuable_Acero";
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + materialName + ".mat");
        return visual;
    }

    private static void GuardarPrefab(GameObject root, string nombreArchivo)
    {
        string path = Folder + "/" + nombreArchivo + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CrearBoton()
    {
        GameObject root = NuevoInteractuable("Boton", "Presiona E para activar el botón", new Vector3(0.8f, 0.8f, 0.8f));
        CrearVisual(root, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.25f, 0.8f), new Color(0.16f, 0.2f, 0.23f));
        Renderer indicador = CrearVisual(root, "Pulsador", PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f), new Vector3(0.28f, 0.12f, 0.28f), Color.red).GetComponent<Renderer>();
        var boton = root.GetComponent<BotonInteractuable>();
        boton.indicador = indicador;
        boton.soloUnaVez = true;
        GuardarPrefab(root, "Boton");
    }

    private static void CrearInterruptor()
    {
        GameObject root = new GameObject("Interruptor");
        root.AddComponent<BoxCollider>().size = new Vector3(0.6f, 1f, 0.45f);
        var audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        var sw = root.AddComponent<InterruptorInteractuable>();
        sw.audioSource = audio;
        sw.mensajePrompt = "Presiona E para accionar el interruptor";
        CrearVisual(root, "Caja", PrimitiveType.Cube, Vector3.zero, new Vector3(0.55f, 0.85f, 0.25f), new Color(0.2f, 0.24f, 0.27f));
        GameObject palanca = CrearVisual(root, "Palanca", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, -0.18f), new Vector3(0.1f, 0.28f, 0.1f), new Color(0.8f, 0.72f, 0.35f));
        sw.palanca = palanca.transform;
        sw.indicador = CrearVisual(root, "Indicador", PrimitiveType.Sphere, new Vector3(0f, 0.3f, -0.18f), Vector3.one * 0.1f, Color.red).GetComponent<Renderer>();
        GuardarPrefab(root, "Interruptor");
    }

    private static void CrearCajon()
    {
        GameObject root = new GameObject("Cajon");
        root.AddComponent<BoxCollider>().size = new Vector3(1.2f, 0.7f, 0.9f);
        var audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        var cajon = root.AddComponent<CajonInteractuable>();
        cajon.audioSource = audio;
        cajon.mensajePrompt = "Presiona E para abrir o cerrar el cajón";
        CrearVisual(root, "Gabinete", PrimitiveType.Cube, new Vector3(0f, 0f, 0.2f), new Vector3(1.1f, 0.65f, 0.45f), new Color(0.25f, 0.29f, 0.31f));
        GameObject frente = CrearVisual(root, "Frente", PrimitiveType.Cube, new Vector3(0f, 0f, -0.1f), new Vector3(1.05f, 0.55f, 0.16f), new Color(0.36f, 0.4f, 0.42f));
        cajon.frente = frente.transform;
        GuardarPrefab(root, "Cajon");
    }

    private static void CrearTarjeta()
    {
        GameObject root = new GameObject("Tarjeta");
        root.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.12f, 0.55f);
        root.AddComponent<AudioSource>().playOnAwake = false;
        var item = root.AddComponent<Recogible>();
        item.idItem = "tarjeta_generica";
        item.nombreVisible = "Tarjeta de acceso";
        item.mensajePrompt = "Presiona E para recoger la tarjeta";
        CrearVisual(root, "Cuerpo", PrimitiveType.Cube, Vector3.zero, new Vector3(0.75f, 0.08f, 0.5f), new Color(0.2f, 0.72f, 0.76f));
        CrearVisual(root, "Chip", PrimitiveType.Cube, new Vector3(-0.2f, 0.05f, 0f), new Vector3(0.14f, 0.02f, 0.12f), new Color(0.85f, 0.72f, 0.35f));
        GuardarPrefab(root, "Tarjeta");
    }
}
