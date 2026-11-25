using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// Puerta: pide un item al GameManager. Si el jugador lo tiene, se abre y carga la siguiente zona.
/// Incluye humo volumétrico 3D denso sin bordes cuadrados + overlay UI de niebla que oculta por completo el corte de escena.
public class Puerta : Interactable
{
    [Header("Condicion")]
    public string idLlave = "credencial_z1";   // item que debe tener el jugador
    public string idPuerta = "puerta_z2";      // id que queda guardado en el GameManager
    public string idCodigoRequerido;

    [Header("Destino")]
    public string escenaDestino = "Zona2_Laboratorio";
    public float duracionApertura = 0.8f;

    private bool abierta = false;
    private static Texture2D texturaHumoCache;
    private static Material materialHumoCache;
    private static Sprite spriteHumoCache;

    public override void Interactuar()
    {
        if (abierta || GameManager.Instance == null) return;

        // Sin la credencial no pasa nada: solo se avisa.
        if (!GameManager.Instance.TieneItem(idLlave))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Necesitas: " + idLlave.Replace("_", " "));
            LabAudioFeedback.ReproducirAccesoDenegado(transform.position);
            return;
        }

        if (!string.IsNullOrEmpty(idCodigoRequerido) &&
            !GameManager.Instance.TieneCodigo(idCodigoRequerido))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Consulta la terminal para obtener el código de acceso");
            LabAudioFeedback.ReproducirAccesoDenegado(transform.position);
            return;
        }

        LabAudioFeedback.ReproducirAperturaPuerta(transform.position);
        LabAudioFeedback.ReproducirLiberacionVapor(transform.position);
        abierta = true;
        GameManager.Instance.AbrirPuerta(idPuerta);

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarPrompt("Acceso concedido. Abriendo...");

        CrearHumoTransicion();
        CortinaNieblaTransicion.IniciarCortina(duracionApertura + 0.50f, ObtenerSpriteHumo());
        StartCoroutine(AbrirYCambiar());
    }

    // La puerta sube como una compuerta mientras el humo 3D y el overlay UI cubren toda la vista antes del cambio de escena.
    private IEnumerator AbrirYCambiar()
    {
        GameSettingsManager.GarantizarInstancia().IniciarFadeOutMusica(Mathf.Max(0.6f, duracionApertura + 0.45f));
        Vector3 inicio = transform.position;
        Vector3 fin = inicio + Vector3.up * 2.5f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.1f, duracionApertura);
            float suavizado = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(inicio, fin, suavizado);
            yield return null;
        }

        yield return new WaitForSeconds(0.55f);

        GameManager.Instance.CambiarEscena(escenaDestino);
    }

    private void CrearHumoTransicion()
    {
        Material material = ObtenerMaterialHumo();
        if (material == null) return;

        Bounds limitesPuerta = ObtenerLimitesPuerta();
        float ancho = Mathf.Clamp(limitesPuerta.size.x, 2.2f, 4.5f);
        float alto = Mathf.Clamp(limitesPuerta.size.y, 2.5f, 4.5f);
        float pisoY = limitesPuerta.min.y;

        GameObject raizHumo = new GameObject("Humo_Transicion_Puerta");
        raizHumo.transform.position = new Vector3(limitesPuerta.center.x, pisoY, limitesPuerta.center.z);
        raizHumo.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        DontDestroyOnLoad(raizHumo);
        Destroy(raizHumo, 4.8f);

        // 1) Bocanada neumática lateral izquierda (densa y amplia)
        CrearEmisorConico(
            raizHumo.transform,
            "Vapor_Valvula_Izquierda",
            material,
            posicionLocal: new Vector3(-ancho * 0.44f, 0.22f, 0f),
            rotacionLocal: new Vector3(-58f, 36f, 0f),
            anguloCono: 34f,
            radioCono: 0.22f,
            vidaMin: 1.9f,
            vidaMax: 3.1f,
            tamanoMin: 0.72f,
            tamanoMax: 1.65f,
            velocidadMin: 0.75f,
            velocidadMax: 1.75f,
            rafagaInicial: 38,
            rafagaSecundaria: 26,
            tasaContinua: 32f,
            opacidadMax: 0.72f,
            gravedad: -0.05f,
            turbulencia: 0.48f);

        // 2) Bocanada neumática lateral derecha (densa y amplia)
        CrearEmisorConico(
            raizHumo.transform,
            "Vapor_Valvula_Derecha",
            material,
            posicionLocal: new Vector3(ancho * 0.44f, 0.22f, 0f),
            rotacionLocal: new Vector3(-58f, -36f, 0f),
            anguloCono: 34f,
            radioCono: 0.22f,
            vidaMin: 1.9f,
            vidaMax: 3.1f,
            tamanoMin: 0.72f,
            tamanoMax: 1.65f,
            velocidadMin: 0.75f,
            velocidadMax: 1.75f,
            rafagaInicial: 38,
            rafagaSecundaria: 26,
            tasaContinua: 32f,
            opacidadMax: 0.72f,
            gravedad: -0.05f,
            turbulencia: 0.48f);

        // 3) Nube envolvente principal desde el umbral (cubre todo el vano y avanza hacia la cámara)
        CrearEmisorEsferico(
            raizHumo.transform,
            "Bocanadas_Ascendentes_Umbral",
            material,
            posicionLocal: new Vector3(0f, 0.45f, 0f),
            escalaEsfera: new Vector3(ancho * 0.75f, 0.45f, 0.65f),
            vidaMin: 2.3f,
            vidaMax: 3.7f,
            tamanoMin: 1.05f,
            tamanoMax: 2.35f,
            velocidadMin: 0.45f,
            velocidadMax: 1.15f,
            rafagaInicial: 56,
            rafagaSecundaria: 38,
            tasaContinua: 42f,
            opacidadMax: 0.78f,
            elevacionYMin: 0.35f,
            elevacionYMax: 0.95f,
            turbulencia: 0.56f);

        // 4) Remolinos densos a media altura (ocultan por completo el hueco de transición)
        CrearEmisorEsferico(
            raizHumo.transform,
            "Remolinos_Vapor_Flotante",
            material,
            posicionLocal: new Vector3(0f, alto * 0.44f, 0f),
            escalaEsfera: new Vector3(ancho * 0.72f, alto * 0.48f, 0.85f),
            vidaMin: 2.1f,
            vidaMax: 3.5f,
            tamanoMin: 0.85f,
            tamanoMax: 2.10f,
            velocidadMin: 0.35f,
            velocidadMax: 0.95f,
            rafagaInicial: 52,
            rafagaSecundaria: 34,
            tasaContinua: 38f,
            opacidadMax: 0.74f,
            elevacionYMin: 0.18f,
            elevacionYMax: 0.68f,
            turbulencia: 0.65f);

        // 5) Cortina de despresurización superior desde el dintel
        CrearEmisorConico(
            raizHumo.transform,
            "Escape_Pistones_Superiores",
            material,
            posicionLocal: new Vector3(0f, alto * 0.82f, 0f),
            rotacionLocal: new Vector3(52f, 0f, 0f),
            anguloCono: 42f,
            radioCono: ancho * 0.36f,
            vidaMin: 1.8f,
            vidaMax: 2.9f,
            tamanoMin: 0.78f,
            tamanoMax: 1.75f,
            velocidadMin: 0.55f,
            velocidadMax: 1.25f,
            rafagaInicial: 36,
            rafagaSecundaria: 24,
            tasaContinua: 28f,
            opacidadMax: 0.68f,
            gravedad: -0.02f,
            turbulencia: 0.52f);
    }

    private Bounds ObtenerLimitesPuerta()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) return col.bounds;

        return new Bounds(transform.position, new Vector3(2.8f, 3.2f, 0.35f));
    }

    private static void CrearEmisorConico(
        Transform padre, string nombre, Material material,
        Vector3 posicionLocal, Vector3 rotacionLocal,
        float anguloCono, float radioCono,
        float vidaMin, float vidaMax,
        float tamanoMin, float tamanoMax,
        float velocidadMin, float velocidadMax,
        short rafagaInicial, short rafagaSecundaria,
        float tasaContinua, float opacidadMax,
        float gravedad, float turbulencia)
    {
        ParticleSystem ps = CrearSistemaParticulasBase(
            padre, nombre, material, posicionLocal,
            vidaMin, vidaMax, tamanoMin, tamanoMax,
            velocidadMin, velocidadMax, rafagaInicial, rafagaSecundaria,
            tasaContinua, opacidadMax, gravedad, turbulencia);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = anguloCono;
        shape.radius = radioCono;
        shape.rotation = rotacionLocal;
        shape.randomDirectionAmount = 0.35f;

        ps.Play();
    }

    private static void CrearEmisorEsferico(
        Transform padre, string nombre, Material material,
        Vector3 posicionLocal, Vector3 escalaEsfera,
        float vidaMin, float vidaMax,
        float tamanoMin, float tamanoMax,
        float velocidadMin, float velocidadMax,
        short rafagaInicial, short rafagaSecundaria,
        float tasaContinua, float opacidadMax,
        float elevacionYMin, float elevacionYMax,
        float turbulencia)
    {
        ParticleSystem ps = CrearSistemaParticulasBase(
            padre, nombre, material, posicionLocal,
            vidaMin, vidaMax, tamanoMin, tamanoMax,
            velocidadMin, velocidadMax, rafagaInicial, rafagaSecundaria,
            tasaContinua, opacidadMax, -0.035f, turbulencia);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.55f;
        shape.scale = escalaEsfera;
        shape.randomDirectionAmount = 0.60f;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);
        vel.y = new ParticleSystem.MinMaxCurve(elevacionYMin, elevacionYMax);
        vel.z = new ParticleSystem.MinMaxCurve(-0.95f, 0.95f);

        ps.Play();
    }

    private static ParticleSystem CrearSistemaParticulasBase(
        Transform padre, string nombre, Material material, Vector3 posicionLocal,
        float vidaMin, float vidaMax,
        float tamanoMin, float tamanoMax,
        float velocidadMin, float velocidadMax,
        short rafagaInicial, short rafagaSecundaria,
        float tasaContinua, float opacidadMax,
        float gravedad, float turbulencia)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicionLocal;

        ParticleSystem humo = go.AddComponent<ParticleSystem>();
        humo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = humo.main;
        main.duration = 0.95f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadMin, velocidadMax);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoMin, tamanoMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = gravedad;
        main.maxParticles = 220;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = humo.emission;
        emission.rateOverTime = tasaContinua;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0.00f, (short)(rafagaInicial * 0.8f), rafagaInicial),
            new ParticleSystem.Burst(0.18f, (short)(rafagaSecundaria * 0.8f), rafagaSecundaria),
            new ParticleSystem.Burst(0.42f, (short)(rafagaSecundaria * 0.6f), rafagaSecundaria)
        });

        ParticleSystem.LimitVelocityOverLifetimeModule limitVel = humo.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = 0.42f;
        limitVel.dampen = 0.20f;

        ParticleSystem.RotationOverLifetimeModule rot = humo.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.68f, 0.68f);

        ParticleSystem.SizeOverLifetimeModule size = humo.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curvaTamano = new AnimationCurve(
            new Keyframe(0.00f, 0.48f, 1.8f, 1.8f),
            new Keyframe(0.28f, 1.15f, 1.2f, 1.2f),
            new Keyframe(0.68f, 1.85f, 0.8f, 0.8f),
            new Keyframe(1.00f, 2.45f, 0.4f, 0.4f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curvaTamano);

        ParticleSystem.ColorOverLifetimeModule color = humo.colorOverLifetime;
        color.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.90f, 0.94f, 0.97f), 0.0f),
                new GradientColorKey(new Color(0.76f, 0.82f, 0.87f), 0.45f),
                new GradientColorKey(new Color(0.58f, 0.64f, 0.70f), 1.0f)
            },
            new[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(opacidadMax * 0.82f, 0.12f),
                new GradientAlphaKey(opacidadMax, 0.28f),
                new GradientAlphaKey(opacidadMax * 0.72f, 0.60f),
                new GradientAlphaKey(opacidadMax * 0.25f, 0.84f),
                new GradientAlphaKey(0.00f, 1.00f)
            });
        color.color = new ParticleSystem.MinMaxGradient(gradiente);

        ParticleSystem.NoiseModule noise = humo.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = turbulencia;
        noise.strengthY = turbulencia * 0.75f;
        noise.strengthZ = turbulencia;
        noise.frequency = 0.36f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.26f);
        noise.damping = true;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        ParticleSystemRenderer renderer = humo.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.maxParticleSize = 2.2f;
        renderer.sortingFudge = 2f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = material;

        return humo;
    }

    private static Sprite ObtenerSpriteHumo()
    {
        ObtenerMaterialHumo();
        if (spriteHumoCache == null && texturaHumoCache != null)
        {
            spriteHumoCache = Sprite.Create(
                texturaHumoCache,
                new Rect(0f, 0f, texturaHumoCache.width, texturaHumoCache.height),
                new Vector2(0.5f, 0.5f));
        }
        return spriteHumoCache;
    }

    private static Material ObtenerMaterialHumo()
    {
        if (materialHumoCache != null && texturaHumoCache != null)
            return materialHumoCache;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            return null;

        const int lado = 256;
        texturaHumoCache = new Texture2D(lado, lado, TextureFormat.RGBA32, true)
        {
            name = "Textura_Humo_Organico_Puerta",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixeles = new Color[lado * lado];
        for (int y = 0; y < lado; y++)
        {
            float ny = (y + 0.5f) / lado * 2f - 1f;
            for (int x = 0; x < lado; x++)
            {
                float nx = (x + 0.5f) / lado * 2f - 1f;
                float r2 = nx * nx + ny * ny;
                float r = Mathf.Sqrt(r2);

                if (r >= 0.82f)
                {
                    pixeles[y * lado + x] = Color.clear;
                    continue;
                }

                float warpX = Mathf.PerlinNoise(nx * 1.9f + 13.7f, ny * 1.9f + 8.3f) - 0.5f;
                float warpY = Mathf.PerlinNoise(nx * 1.9f + 31.4f, ny * 1.9f + 22.9f) - 0.5f;
                float wx = nx + warpX * 0.22f;
                float wy = ny + warpY * 0.22f;

                float n1 = Mathf.PerlinNoise(wx * 2.4f + 4.1f, wy * 2.4f + 7.2f);
                float n2 = Mathf.PerlinNoise(wx * 4.8f + 19.3f, wy * 4.8f + 11.8f);
                float n3 = Mathf.PerlinNoise(wx * 9.6f + 37.5f, wy * 9.6f + 29.1f);
                float fbm = n1 * 0.58f + n2 * 0.28f + n3 * 0.14f;

                float tBorde = Mathf.Clamp01(1f - r / 0.82f);
                float ventanaSuave = tBorde * tBorde * tBorde * (tBorde * (tBorde * 6f - 15f) + 10f);
                float gauss = Mathf.Exp(-3.5f * (wx * wx + wy * wy));

                float densidadNube = Mathf.SmoothStep(0.15f, 0.85f, fbm);
                float alpha = Mathf.Clamp01(ventanaSuave * gauss * (0.42f + densidadNube * 0.58f));

                float luz = Mathf.Clamp01(0.84f + (-ny * 0.08f) + (fbm - 0.5f) * 0.20f);
                pixeles[y * lado + x] = new Color(luz * 0.94f, luz * 0.97f, luz, alpha);
            }
        }

        texturaHumoCache.SetPixels(pixeles);
        texturaHumoCache.Apply(true, false);

        materialHumoCache = new Material(shader) { name = "MAT_Humo_Organico_Puerta" };
        materialHumoCache.SetTexture("_BaseMap", texturaHumoCache);
        materialHumoCache.mainTexture = texturaHumoCache;
        materialHumoCache.SetColor("_BaseColor", Color.white);
        materialHumoCache.SetColor("_Color", Color.white);

        materialHumoCache.SetFloat("_ColorMode", 0f);
        materialHumoCache.SetFloat("_Surface", 1f);
        materialHumoCache.SetFloat("_Blend", 0f);
        materialHumoCache.SetFloat("_BlendOp", 0f);
        materialHumoCache.SetFloat("_AlphaClip", 0f);
        materialHumoCache.SetFloat("_AlphaToMask", 0f);
        materialHumoCache.SetFloat("_Cull", 0f);
        materialHumoCache.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        materialHumoCache.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        materialHumoCache.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        materialHumoCache.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        materialHumoCache.SetFloat("_ZWrite", 0f);
        materialHumoCache.SetFloat("_SoftParticlesEnabled", 0f);
        materialHumoCache.SetFloat("_CameraFadingEnabled", 0f);
        materialHumoCache.SetFloat("_DistortionEnabled", 0f);

        materialHumoCache.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        materialHumoCache.DisableKeyword("_ALPHATEST_ON");
        materialHumoCache.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        materialHumoCache.DisableKeyword("_ALPHAMODULATE_ON");
        materialHumoCache.SetShaderPassEnabled("DepthOnly", false);
        materialHumoCache.SetShaderPassEnabled("DepthNormals", false);
        materialHumoCache.SetShaderPassEnabled("SHADOWCASTER", false);
        materialHumoCache.SetOverrideTag("RenderType", "Transparent");
        materialHumoCache.renderQueue = (int)RenderQueue.Transparent;

        return materialHumoCache;
    }

    /// <summary>
    /// Overlay UI de niebla suave que acompaña al humo 3D al abrir la puerta,
    /// cubriendo progresivamente el 100% de la vista justo en el instante del cambio de escena.
    /// </summary>
    private class CortinaNieblaTransicion : MonoBehaviour
    {
        private CanvasGroup grupo;
        private RectTransform nubeIzq;
        private RectTransform nubeDer;
        private float duracionEntrada;

        public static void IniciarCortina(float duracionSubida, Sprite spriteNube)
        {
            if (UIManager.Instance == null) return;

            GameObject go = new GameObject("Overlay_Niebla_Transicion_UI", typeof(RectTransform));
            AttachAlCanvasPrincipal(go.GetComponent<RectTransform>());

            CortinaNieblaTransicion cortina = go.AddComponent<CortinaNieblaTransicion>();
            cortina.grupo = go.AddComponent<CanvasGroup>();
            cortina.grupo.alpha = 0f;
            cortina.grupo.blocksRaycasts = false;
            cortina.grupo.interactable = false;
            cortina.duracionEntrada = Mathf.Max(0.4f, duracionSubida);

            // Base de vapor denso que garantiza ocultación total en el pico de la transición
            GameObject fondoObj = new GameObject("Base_Niebla", typeof(RectTransform), typeof(Image));
            fondoObj.transform.SetParent(go.transform, false);
            RectTransform rtFondo = fondoObj.GetComponent<RectTransform>();
            rtFondo.anchorMin = Vector2.zero;
            rtFondo.anchorMax = Vector2.one;
            rtFondo.offsetMin = Vector2.zero;
            rtFondo.offsetMax = Vector2.zero;
            Image imgFondo = fondoObj.GetComponent<Image>();
            imgFondo.color = new Color(0.66f, 0.73f, 0.78f, 0.98f);
            imgFondo.raycastTarget = false;

            if (spriteNube != null)
            {
                cortina.nubeIzq = CrearNubeMovil(go.transform, "Nube_UI_Izq", spriteNube, new Vector2(-320f, -60f), new Vector2(2100f, 1500f));
                cortina.nubeDer = CrearNubeMovil(go.transform, "Nube_UI_Der", spriteNube, new Vector2(320f, 60f), new Vector2(2100f, 1500f));
            }

            cortina.StartCoroutine(cortina.AnimarCortina());
        }

        private static void AttachAlCanvasPrincipal(RectTransform rt)
        {
            if (rt == null || UIManager.Instance == null) return;
            rt.SetParent(UIManager.Instance.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

        private static RectTransform CrearNubeMovil(Transform padre, string nombre, Sprite sprite, Vector2 pos, Vector2 tam)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(0.84f, 0.90f, 0.94f, 0.95f);
            img.raycastTarget = false;
            return rt;
        }

        private IEnumerator AnimarCortina()
        {
            float t = 0f;
            while (t < duracionEntrada)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionEntrada));
                if (grupo != null) grupo.alpha = k;
                MoverNubes(Time.unscaledDeltaTime, 1f + k * 0.35f);
                yield return null;
            }

            if (grupo != null) grupo.alpha = 1f;

            // Mantener cubierta la pantalla durante el frame de carga de la nueva escena
            float esperaCarga = 0f;
            while (esperaCarga < 0.28f)
            {
                esperaCarga += Time.unscaledDeltaTime;
                MoverNubes(Time.unscaledDeltaTime, 1.35f);
                yield return null;
            }

            // Disipar progresivamente en la nueva escena
            float duracionSalida = 0.85f;
            float s = 0f;
            while (s < duracionSalida)
            {
                s += Time.unscaledDeltaTime;
                float k = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(s / duracionSalida));
                if (grupo != null) grupo.alpha = k;
                MoverNubes(Time.unscaledDeltaTime, 1.35f);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void MoverNubes(float dt, float escala)
        {
            if (nubeIzq != null)
            {
                nubeIzq.anchoredPosition += new Vector2(95f * dt, 22f * dt);
                nubeIzq.localScale = Vector3.one * escala;
            }
            if (nubeDer != null)
            {
                nubeDer.anchoredPosition += new Vector2(-95f * dt, -18f * dt);
                nubeDer.localScale = Vector3.one * escala;
            }
        }
    }
}
