using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Herramienta de un solo uso para la Zona 1.
// Menú: Laboratorio > Generar partículas Z1
// Crea los materiales y los 4 prefabs de partículas, y los coloca en la escena.
public static class GeneradorParticulasZ1
{
    const string Carpeta     = "Assets/GAME/Particles/Z1_Recepcion";
    const string CarpetaMat  = Carpeta + "/Materials";
    const string CarpetaTex  = Carpeta + "/Textures";
    const string CarpetaPref = Carpeta + "/Prefabs";

    [MenuItem("Laboratorio/Generar partículas Z1")]
    public static void Generar()
    {
        GenerarPrefabs();
        ColocarEnEscena();
        Debug.Log("Partículas de la Zona 1 generadas en " + Carpeta);
    }

    // Solo crea/actualiza materiales y prefabs, sin tocar la escena abierta.
    public static void GenerarPrefabs()
    {
        AsegurarCarpeta(Carpeta, "Materials");
        AsegurarCarpeta(Carpeta, "Prefabs");

        Material matPolvo  = CrearMaterial("mat_polvo",  "tex_particula_polvo",  new Color(0.92f, 0.87f, 0.72f, 0.95f), true);
        Material matChispa = CrearMaterial("mat_chispa", "tex_particula_chispa", new Color(1.00f, 0.75f, 0.35f, 1.0f), true);
        Material matHumo   = CrearMaterial("mat_humo",   "tex_particula_humo",   new Color(0.75f, 0.78f, 0.80f, 1.0f), false);
        Material matBrillo = CrearMaterial("mat_brillo", "tex_particula_brillo", new Color(1.00f, 0.85f, 0.45f, 1.0f), true);

        CrearPolvo(matPolvo);
        CrearChispas(matChispa);
        CrearVapor(matHumo);
        CrearBrillo(matBrillo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ---------- Carpetas, texturas y materiales ----------

    static void AsegurarCarpeta(string padre, string hija)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + hija))
            AssetDatabase.CreateFolder(padre, hija);
    }

    static Texture2D CargarTextura(string nombre)
    {
        string ruta = CarpetaTex + "/" + nombre + ".png";
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    // Crea (o actualiza) el material de partículas URP.
    // aditivo = true suma luz (polvo, chispas, brillo); false mezcla con alfa (humo).
    static Material CrearMaterial(string nombre, string textura, Color color, bool aditivo)
    {
        Texture2D tex = CargarTextura(textura);
        string ruta = CarpetaMat + "/" + nombre + ".mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, ruta);
        }

        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_ColorMode", 0f);   // 0 = multiplicar textura por color
        mat.SetFloat("_Surface", 1f);     // 1 = transparente
        mat.SetFloat("_Blend", aditivo ? 2f : 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_Cull", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", aditivo ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_SoftParticlesEnabled", 0f);
        mat.SetFloat("_CameraFadingEnabled", 0f);
        mat.SetFloat("_DistortionEnabled", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- Prefabs ----------

    // Crea un ParticleSystem con los valores comunes y su material.
    static ParticleSystem Nueva(string nombre, Material material)
    {
        GameObject go = new GameObject(nombre);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    static void Guardar(GameObject raiz, string nombre)
    {
        PrefabUtility.SaveAsPrefabAsset(raiz, CarpetaPref + "/" + nombre + ".prefab");
        Object.DestroyImmediate(raiz);
    }

    // 1) Polvo flotando en el aire de toda la recepción.
    static void CrearPolvo(Material material)
    {
        ParticleSystem ps = Nueva("PS_Polvo_Z1", material);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(11f, 18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.96f, 0.86f, 0.75f));
        main.gravityModifier = 0f;
        main.maxParticles = 900;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 40f;

        // Caja de 12 x 3,6 x 12 m que cubre el cuarto.
        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(12f, 3.6f, 12f);
        forma.position = new Vector3(0f, -0.4f, 0f);

        // Movimiento suave e impredecible.
        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.05f;
        ruido.frequency = 0.25f;
        ruido.scrollSpeed = 0.08f;
        ruido.damping = true;
        ruido.octaveCount = 2;

        ParticleSystem.RotationOverLifetimeModule giro = ps.rotationOverLifetime;
        giro.enabled = true;
        giro.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(1f, 1.35f)));

        // Aparece y desaparece suave para que no se note el reinicio.
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.96f, 0.86f), 0f),
                new GradientColorKey(new Color(1f, 0.92f, 0.78f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0.9f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(degradado);

        ps.GetComponent<ParticleSystemRenderer>().maxParticleSize = 0.2f;

        Guardar(ps.gameObject, "PS_Polvo_Z1");
    }

    // 2) Chispas que caen de una luminaria rota (ráfagas cada 4 s).
    static void CrearChispas(Material material)
    {
        ParticleSystem ps = Nueva("PS_Chispas_Luminaria", material);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f, 1f));
        main.gravityModifier = 1.4f;
        main.maxParticles = 120;

        // Nada continuo: una ráfaga de 10 a 22 chispas por ciclo.
        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 0f;
        ParticleSystem.Burst rafaga = new ParticleSystem.Burst(0.4f, (short)10, (short)22, 1, 4f);
        rafaga.probability = 0.85f;
        emision.SetBursts(new ParticleSystem.Burst[] { rafaga });

        // Cono hacia abajo (el cono nace mirando a +Z, se gira 90 grados).
        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 22f;
        forma.radius = 0.02f;
        forma.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f)));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.97f, 0.75f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.15f), 0.55f),
                new GradientColorKey(new Color(0.6f, 0.15f, 0.03f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(degradado);

        // Estirado según la velocidad: se ven como rayitas.
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.06f;
        r.lengthScale = 1.6f;

        Guardar(ps.gameObject, "PS_Chispas_Luminaria");
    }

    // 3) Vapor que sale de una tubería.
    static void CrearVapor(Material material)
    {
        ParticleSystem ps = Nueva("PS_Vapor_Tuberia", material);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.75f));
        main.gravityModifier = -0.03f;   // sube muy lento
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 5f;

        // Cono hacia arriba.
        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 14f;
        forma.radius = 0.04f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.12f;
        ruido.frequency = 0.35f;
        ruido.scrollSpeed = 0.15f;
        ruido.damping = true;
        ruido.octaveCount = 2;

        ParticleSystem.RotationOverLifetimeModule giro = ps.rotationOverLifetime;
        giro.enabled = true;
        giro.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(1f, 2.6f)));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.85f, 0.88f, 0.9f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.35f, 0.3f),
                new GradientAlphaKey(0.15f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(degradado);

        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        r.maxParticleSize = 1.2f;
        r.sortingFudge = 2f;

        Guardar(ps.gameObject, "PS_Vapor_Tuberia");
    }

    // 4) Halo dorado sobre la credencial + chispitas que suben.
    static void CrearBrillo(Material material)
    {
        ParticleSystem ps = Nueva("PS_Brillo_Credencial", material);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.02f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f, 1f));
        main.gravityModifier = 0f;
        main.maxParticles = 30;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 6f;

        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.05f;

        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 1.25f)));

        // El alfa sube y baja: parece que respira.
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.65f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.5f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.1f, 0f),
                new GradientAlphaKey(0.45f, 0.5f),
                new GradientAlphaKey(0.1f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(degradado);

        // Sistema hijo: chispitas pequeñas que suben.
        ParticleSystem chispas = Nueva("Chispitas", material);
        ParticleSystem.MainModule mainChispas = chispas.main;
        mainChispas.duration = 3f;
        mainChispas.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
        mainChispas.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        mainChispas.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.012f);
        mainChispas.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.8f, 1f));
        mainChispas.gravityModifier = 0f;
        mainChispas.maxParticles = 15;

        ParticleSystem.EmissionModule emisionChispas = chispas.emission;
        emisionChispas.rateOverTime = 2f;

        ParticleSystem.ShapeModule formaChispas = chispas.shape;
        formaChispas.shapeType = ParticleSystemShapeType.Cone;
        formaChispas.angle = 25f;
        formaChispas.radius = 0.05f;
        formaChispas.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorChispas = chispas.colorOverLifetime;
        colorChispas.enabled = true;
        Gradient degradadoChispas = new Gradient();
        degradadoChispas.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.8f, 0.4f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorChispas.color = new ParticleSystem.MinMaxGradient(degradadoChispas);

        chispas.transform.SetParent(ps.transform, false);

        Guardar(ps.gameObject, "PS_Brillo_Credencial");
    }

    // ---------- Colocación en la escena ----------

    static void ColocarEnEscena()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.name != "Zona1_Recepcion")
        {
            Debug.LogWarning("Abre la escena Zona1_Recepcion para colocarlas automáticamente. Los prefabs ya quedaron creados.");
            return;
        }

        GameObject raizVieja = GameObject.Find("Particulas_Z1");
        if (raizVieja != null) Object.DestroyImmediate(raizVieja);

        GameObject raiz = new GameObject("Particulas_Z1");
        Undo.RegisterCreatedObjectUndo(raiz, "Partículas Z1");

        Instanciar("PS_Polvo_Z1", raiz.transform, new Vector3(0f, 2.5f, 3f));
        Instanciar("PS_Chispas_Luminaria", raiz.transform, new Vector3(-2.5f, 2.8f, 2.4f));
        Instanciar("PS_Vapor_Tuberia", raiz.transform, new Vector3(-4.8f, 2.4f, 6.2f));

        // El brillo va dentro del recogible para que desaparezca al recogerlo.
        GameObject recogible = GameObject.Find("Recogible_Credencial");
        if (recogible != null)
        {
            GameObject brilloViejo = GameObject.Find("PS_Brillo_Credencial");
            if (brilloViejo != null) Object.DestroyImmediate(brilloViejo);

            GameObject brillo = Instanciar("PS_Brillo_Credencial", recogible.transform, Vector3.zero);
            float escalaPadre = recogible.transform.lossyScale.x;
            brillo.transform.localScale = Vector3.one / Mathf.Max(0.0001f, escalaPadre);
        }
        else
        {
            Debug.LogWarning("No encontré Recogible_Credencial: el brillo no se colocó.");
        }

        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("Partículas colocadas. Guarda la escena con Ctrl+S.");
    }

    static GameObject Instanciar(string nombre, Transform padre, Vector3 posicion)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPref + "/" + nombre + ".prefab");
        GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        copia.name = nombre;
        copia.transform.localPosition = posicion;
        return copia;
    }
}
