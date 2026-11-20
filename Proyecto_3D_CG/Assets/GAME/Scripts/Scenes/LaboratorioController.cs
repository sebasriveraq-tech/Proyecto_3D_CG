using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Controla la mecánica local del laboratorio (Zona 2): energía, alarma, condiciones de salida,
/// mamparas de vidrio de Contención, luces parpadeantes en techo (Energía, Servidores y pasillo central),
/// chispas de cortocircuito y sonido ambiental de emergencia, panel visual de objetivos,
/// prevención de culling/muros invisibles y ajuste estricto de colliders.
/// </summary>
public class LaboratorioController : MonoBehaviour
{
    [Header("Referencias de Zona 2")]
    public InterruptorInteractuable interruptorEnergia;
    public BotonInteractuable botonAlarma;
    public Terminal terminal;
    public Puerta puertaSalida;
    public AgentController agente;
    public Light luzEmergencia;
    public ParticleSystem chispasEnergia;
    public AudioSource audioAmbiente;
    public AudioSource audioAlarmaEmergencia;

    public bool EnergiaRestaurada { get; private set; }

    private struct LuzTechoParpadeante
    {
        public Light luz;
        public Renderer tuboVisual;
        public Material materialTubo;
        public float semilla;
        public Vector3 posicionChispa;
    }

    private readonly List<LuzTechoParpadeante> lucesTechoParpadeantes = new List<LuzTechoParpadeante>();
    private float temporizadorChispaElectrica = 2.5f;
    private bool alarmaPausadaPorTiempo;

    [Header("UI de Objetivos (Zona 2)")]
    public GameObject panelObjetivosHUD;
    public Image fondoObjetivoHUD;
    public TextMeshProUGUI headerObjetivoHUD;
    public TextMeshProUGUI textoObjetivoHUD;
    public Image barraAcentoObjetivoHUD;
    public Outline bordeObjetivoHUD;

    private bool credencialDetectadaPrev;

    private void Awake()
    {
        GarantizarAudioListenerUnico();
        CorregirVisibilidadYEvitarDesaparicionDeObjetos();
        ConstruirMamparasVidrioContencion();
        ConfigurarLucesParpadeantesTecho();
        GarantizarCollidersMobiliarioZona2();

        if (interruptorEnergia == null)
            interruptorEnergia = FindFirstObjectByType<InterruptorInteractuable>();
        if (botonAlarma == null)
            botonAlarma = FindFirstObjectByType<BotonInteractuable>();
        if (terminal == null)
            terminal = FindFirstObjectByType<Terminal>();
        if (puertaSalida == null)
            puertaSalida = FindFirstObjectByType<Puerta>();
        if (agente == null)
            agente = FindFirstObjectByType<AgentController>();
        if (luzEmergencia == null)
        {
            GameObject luz = GameObject.Find("Luz_Emergencia_Zona2");
            if (luz != null) luzEmergencia = luz.GetComponent<Light>();
        }
        if (chispasEnergia == null)
        {
            GameObject efecto = GameObject.Find("Chispas_Energia_Zona2");
            if (efecto != null) chispasEnergia = efecto.GetComponent<ParticleSystem>();
        }

        ConfigurarAudioAmbienteYAlarmaEmergencia();
    }

    private void ConfigurarAudioAmbienteYAlarmaEmergencia()
    {
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();

        if (audioAmbiente == null) audioAmbiente = GetComponent<AudioSource>();
        if (audioAmbiente == null) audioAmbiente = gameObject.AddComponent<AudioSource>();

        if (audioAmbiente.clip == null)
            audioAmbiente.clip = LabAudioFeedback.ObtenerAmbiente();
        audioAmbiente.loop = true;
        audioAmbiente.playOnAwake = false;
        audioAmbiente.spatialBlend = 0f;
        audioAmbiente.mute = false;
        audioAmbiente.volume = 0.16f;
        cfg.RegistrarFuenteConVolumenBase(audioAmbiente, 0.16f);

        if (audioAlarmaEmergencia == null)
        {
            Transform hijoAlarma = transform.Find("Audio_Alarma_Emergencia_Z2");
            if (hijoAlarma == null)
            {
                GameObject goAlarma = new GameObject("Audio_Alarma_Emergencia_Z2");
                goAlarma.transform.SetParent(transform, false);
                audioAlarmaEmergencia = goAlarma.AddComponent<AudioSource>();
            }
            else
            {
                audioAlarmaEmergencia = hijoAlarma.GetComponent<AudioSource>() ?? hijoAlarma.gameObject.AddComponent<AudioSource>();
            }
        }

        if (audioAlarmaEmergencia.clip == null)
            audioAlarmaEmergencia.clip = LabAudioFeedback.ObtenerAlarmaEmergenciaElectricaZ2();
        audioAlarmaEmergencia.enabled = true;
        audioAlarmaEmergencia.loop = true;
        audioAlarmaEmergencia.playOnAwake = true;
        audioAlarmaEmergencia.spatialBlend = 0f;
        audioAlarmaEmergencia.mute = false;
        const float volBaseEmergencia = 0.30f;
        audioAlarmaEmergencia.volume = volBaseEmergencia;
        cfg.RegistrarFuenteConVolumenBase(audioAlarmaEmergencia, volBaseEmergencia);
    }

    private void Start()
    {
        EnergiaRestaurada = interruptorEnergia != null && interruptorEnergia.encendido;
        if (interruptorEnergia != null)
        {
            interruptorEnergia.alEncender.AddListener(RestaurarEnergia);
            interruptorEnergia.alApagar.AddListener(CortarEnergia);
        }
        if (botonAlarma != null)
            botonAlarma.alPresionar.AddListener(ActivarAlarma);

        if (terminal != null)
        {
            terminal.laboratorio = this;
            terminal.requiereEnergia = true;
        }
        if (puertaSalida != null)
        {
            puertaSalida.idLlave = "credencial_z2";
            puertaSalida.idCodigoRequerido = "codigo_z2";
            puertaSalida.escenaDestino = "Zona3_Control";
        }

        ActualizarIluminacion();
        ActualizarPanelObjetivosHUD(false);
        SincronizarVisibilidadObjetivosHUD();

        if (audioAmbiente != null && !audioAmbiente.isPlaying)
            audioAmbiente.Play();
        if (audioAlarmaEmergencia != null && !audioAlarmaEmergencia.isPlaying)
            audioAlarmaEmergencia.Play();

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(
                "Zona 2: Usa el Terminal de Energía (Norte) para restaurar la corriente y el Terminal de Servidores (Oeste) para desbloquear la compuerta",
                4.2f);
    }

    private void Update()
    {
        ActualizarParpadeoTechoYChispas();
        ActualizarEstadoAlarmaPorTiempo();
        SincronizarVisibilidadObjetivosHUD();

        bool tieneCredencial = GameManager.Instance != null && GameManager.Instance.TieneItem("credencial_z2");
        if (tieneCredencial != credencialDetectadaPrev)
        {
            credencialDetectadaPrev = tieneCredencial;
            ActualizarPanelObjetivosHUD(false);
        }
    }

    private void SincronizarVisibilidadObjetivosHUD()
    {
        if (panelObjetivosHUD == null) return;
        bool mostrar = UIManager.Instance != null
            && UIManager.Instance.panelHUD != null
            && UIManager.Instance.panelHUD.activeInHierarchy
            && !PanelTerminalZ1.AbiertoZ2;
        if (panelObjetivosHUD.activeSelf != mostrar)
            panelObjetivosHUD.SetActive(mostrar);
    }

    private void ActualizarEstadoAlarmaPorTiempo()
    {
        if (audioAlarmaEmergencia == null) return;

        if (Time.timeScale == 0f && !alarmaPausadaPorTiempo)
        {
            audioAlarmaEmergencia.Pause();
            alarmaPausadaPorTiempo = true;
        }
        else if (Time.timeScale > 0f && alarmaPausadaPorTiempo)
        {
            audioAlarmaEmergencia.UnPause();
            alarmaPausadaPorTiempo = false;
        }
    }

    // -------------------------------------------------------------------------
    // VISIBILIDAD, NEAR CLIP, CHARACTER CONTROLLER Y COLLIDERS
    // -------------------------------------------------------------------------
    private void CorregirVisibilidadYEvitarDesaparicionDeObjetos()
    {
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cam == null) continue;
            cam.useOcclusionCulling = false;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 250f;
        }

        GameObject jugador = GameObject.Find("Player");
        if (jugador != null)
        {
            CharacterController cc = jugador.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.radius = 0.32f;
                cc.skinWidth = 0.04f;
                cc.stepOffset = 0.30f;
            }
        }

        foreach (SkinnedMeshRenderer smr in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (smr == null) continue;
            smr.updateWhenOffscreen = true;
            smr.allowOcclusionWhenDynamic = false;
        }

        foreach (LODGroup lod in FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lod != null)
                lod.enabled = false;
        }

        foreach (MeshRenderer mr in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mr == null) continue;
            mr.allowOcclusionWhenDynamic = false;
        }

        // Desactivar objetos obsoletos o sobredimensionados y sus colliders residuales
        string[] obsoletos =
        {
            "Mesa_Investigacion_Zona2",
            "Soportes_Tuberias_Zona2",
            "Letrero_Lab_Salida",
            "Letrero_Precaucion_Contencion",
            "Letrero_Peligro_Contencion",
            "Unidad_Turbina_Vapor_NE",
            "Intercom_Salida_Norte"
        };
        foreach (string nombre in obsoletos)
        {
            GameObject go = GameObject.Find(nombre);
            if (go != null)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                    c.enabled = false;
                go.SetActive(false);
            }
        }

        // Eliminar colliders residuales de tuberías elevadas o accesorios pequeños
        string[] sinColliders = { "Modelo_modular_industrial_pipes_01", "Modelo_industrial_microscope" };
        foreach (string nombre in sinColliders)
        {
            GameObject obj = GameObject.Find(nombre);
            if (obj != null)
            {
                foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
                    Destroy(c);
            }
        }
    }

    private void GarantizarCollidersMobiliarioZona2()
    {
        string[] grupos = { "Mobiliario_de_Laboratorio", "Ambientacion_Industrial_Zona2" };
        foreach (string nombreGrupo in grupos)
        {
            GameObject grupo = GameObject.Find(nombreGrupo);
            if (grupo == null) continue;

            foreach (Transform hijo in grupo.transform)
            {
                if (hijo == null || !hijo.gameObject.activeInHierarchy) continue;

                string n = hijo.name;

                // Excluir accesorios, monitores, teclados, sillas pequeñas y modelos acoplados a interactuables
                // para eliminar cualquier collider residual en los pasillos alrededor de terminales y armarios.
                if (n.Contains("Credencial") || n.Contains("Tarjeta") || n.Contains("PS_") ||
                    n.Contains("Letrero") || n.Contains("Sign_") || n.Contains("Word_") ||
                    n.Contains("Intercom") || n.Contains("pipes") || n.Contains("Abrazadera") ||
                    n.Contains("Monitor") || n.Contains("Teclado") || n.Contains("Raton") ||
                    n.Contains("microscope") || n.Contains("Basurero") || n.Contains("Extintor") ||
                    n.Contains("Turbina") || n.Contains("Airing") ||
                    n == "Consola_Interruptor" || n == "Consola_Control_Energia" ||
                    n == "Terminal_Cientifico_3D" || n == "Gabinete_Cientifico_Interactivo")
                {
                    foreach (Collider c in hijo.GetComponentsInChildren<Collider>(true))
                        Destroy(c);
                    continue;
                }

                AjustarBoxColliderExactoPorMallas(hijo, n.StartsWith("Silla_") ? 0.78f : 0.88f);

                BoxCollider col = hijo.GetComponent<BoxCollider>();
                if (col != null && hijo.position.y < 0.45f && hijo.GetComponent<NavMeshObstacle>() == null)
                {
                    NavMeshObstacle obstaculo = hijo.gameObject.AddComponent<NavMeshObstacle>();
                    obstaculo.shape = NavMeshObstacleShape.Box;
                    obstaculo.center = col.center;
                    obstaculo.size = col.size;
                    obstaculo.carving = true;
                    obstaculo.carveOnlyStationary = true;
                }
            }
        }

        foreach (ArmarioEscondite armario in FindObjectsByType<ArmarioEscondite>(FindObjectsSortMode.None))
        {
            if (armario != null && armario.GetComponent<NavMeshObstacle>() == null)
            {
                NavMeshObstacle obs = armario.gameObject.AddComponent<NavMeshObstacle>();
                obs.shape = NavMeshObstacleShape.Box;
                obs.center = new Vector3(0f, 1.2f, 0f);
                obs.size = new Vector3(1.5f, 2.4f, 1.1f);
                obs.carving = true;
                obs.carveOnlyStationary = true;
            }
        }

        foreach (Cajon contenedor in FindObjectsByType<Cajon>(FindObjectsSortMode.None))
        {
            BoxCollider col = contenedor != null ? contenedor.GetComponent<BoxCollider>() : null;
            if (col != null && contenedor.GetComponent<NavMeshObstacle>() == null)
            {
                NavMeshObstacle obs = contenedor.gameObject.AddComponent<NavMeshObstacle>();
                obs.shape = NavMeshObstacleShape.Box;
                obs.center = col.center;
                obs.size = col.size;
                obs.carving = true;
                obs.carveOnlyStationary = true;
            }
        }
    }

    private static void AjustarBoxColliderExactoPorMallas(Transform raiz, float factorPlanta)
    {
        MeshFilter[] filtros = raiz.GetComponentsInChildren<MeshFilter>(true);
        if (filtros == null || filtros.Length == 0) return;

        bool inicializado = false;
        Bounds limitesLocales = default;

        foreach (MeshFilter mf in filtros)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            Renderer r = mf.GetComponent<Renderer>();
            if (r != null && !r.enabled) continue;

            Bounds mb = mf.sharedMesh.bounds;
            Vector3 c = mb.center;
            Vector3 e = mb.extents;

            for (int ix = -1; ix <= 1; ix += 2)
            for (int iy = -1; iy <= 1; iy += 2)
            for (int iz = -1; iz <= 1; iz += 2)
            {
                Vector3 esquinaMalla = c + new Vector3(e.x * ix, e.y * iy, e.z * iz);
                Vector3 puntoMundo = mf.transform.TransformPoint(esquinaMalla);
                Vector3 puntoLocalRaiz = raiz.InverseTransformPoint(puntoMundo);

                if (!inicializado)
                {
                    limitesLocales = new Bounds(puntoLocalRaiz, Vector3.zero);
                    inicializado = true;
                }
                else
                {
                    limitesLocales.Encapsulate(puntoLocalRaiz);
                }
            }
        }

        if (!inicializado) return;

        BoxCollider box = raiz.GetComponent<BoxCollider>();
        if (box == null) box = raiz.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.center = limitesLocales.center;
        box.size = new Vector3(
            Mathf.Max(0.05f, limitesLocales.size.x * factorPlanta),
            Mathf.Max(0.05f, limitesLocales.size.y),
            Mathf.Max(0.05f, limitesLocales.size.z * factorPlanta));
    }

    // -------------------------------------------------------------------------
    // MAMPARAS DE VIDRIO PARA LA ZONA DE CONTENCIÓN (CON ENTRADA AMPLIA DE 4.0m)
    // -------------------------------------------------------------------------
    private void ConstruirMamparasVidrioContencion()
    {
        if (GameObject.Find("Mamparas_Vidrio_Contencion_Z2") != null)
            return;

        GameObject raiz = new GameObject("Mamparas_Vidrio_Contencion_Z2");

        Shader shLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material matMarco = new Material(shLit)
        {
            name = "MAT_Marco_Contencion_Z2",
            color = new Color(0.16f, 0.22f, 0.26f, 1f)
        };
        if (matMarco.HasProperty("_BaseColor")) matMarco.SetColor("_BaseColor", new Color(0.16f, 0.22f, 0.26f, 1f));
        if (matMarco.HasProperty("_Metallic")) matMarco.SetFloat("_Metallic", 0.78f);
        if (matMarco.HasProperty("_Smoothness")) matMarco.SetFloat("_Smoothness", 0.62f);

        Shader shUnlit = Shader.Find("Universal Render Pipeline/Unlit") ?? shLit;
        Material matVidrio = new Material(shUnlit)
        {
            name = "MAT_Vidrio_Contencion_Z2",
            color = new Color(0.32f, 0.78f, 0.88f, 0.24f)
        };
        if (matVidrio.HasProperty("_BaseColor"))
            matVidrio.SetColor("_BaseColor", new Color(0.32f, 0.78f, 0.88f, 0.24f));
        if (matVidrio.HasProperty("_Surface")) matVidrio.SetFloat("_Surface", 1f);
        if (matVidrio.HasProperty("_Blend")) matVidrio.SetFloat("_Blend", 0f);
        if (matVidrio.HasProperty("_SrcBlend")) matVidrio.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (matVidrio.HasProperty("_DstBlend")) matVidrio.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (matVidrio.HasProperty("_ZWrite")) matVidrio.SetFloat("_ZWrite", 0f);
        matVidrio.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        matVidrio.SetShaderPassEnabled("DepthOnly", false);
        matVidrio.SetShaderPassEnabled("SHADOWCASTER", false);
        matVidrio.SetOverrideTag("RenderType", "Transparent");
        matVidrio.renderQueue = (int)RenderQueue.Transparent;

        CrearModuloMampara(raiz.transform, "Mampara_Sur", new Vector3(5.75f, 0f, 4.00f), 0f, 6.20f, 2.75f, matMarco, matVidrio);
        CrearModuloMampara(raiz.transform, "Mampara_Norte", new Vector3(5.75f, 0f, 14.00f), 0f, 6.20f, 2.75f, matMarco, matVidrio);
        CrearModuloMampara(raiz.transform, "Mampara_Oeste_Sur", new Vector3(2.65f, 0f, 5.50f), 90f, 3.00f, 2.75f, matMarco, matVidrio);
        CrearModuloMampara(raiz.transform, "Mampara_Oeste_Norte", new Vector3(2.65f, 0f, 12.50f), 90f, 3.00f, 2.75f, matMarco, matVidrio);
        CrearPiezaMampara(raiz.transform, "Dintel_Entrada_Contencion", new Vector3(2.65f, 2.65f, 9.00f), new Vector3(0.12f, 0.20f, 4.00f), matMarco, false);
    }

    private static void CrearModuloMampara(
        Transform padre, string nombre, Vector3 posBase, float rotY, float ancho, float alto,
        Material matMarco, Material matVidrio)
    {
        GameObject mod = new GameObject(nombre);
        mod.transform.SetParent(padre, false);
        mod.transform.localPosition = posBase;
        mod.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        CrearPiezaMampara(mod.transform, "Zocalo_Inferior", new Vector3(0f, 0.14f, 0f), new Vector3(ancho, 0.28f, 0.10f), matMarco, false);
        CrearPiezaMampara(mod.transform, "Viga_Superior", new Vector3(0f, alto - 0.08f, 0f), new Vector3(ancho, 0.16f, 0.10f), matMarco, false);

        CrearPiezaMampara(mod.transform, "Poste_Izq", new Vector3(-ancho * 0.5f + 0.06f, alto * 0.5f, 0f), new Vector3(0.12f, alto, 0.12f), matMarco, false);
        CrearPiezaMampara(mod.transform, "Poste_Der", new Vector3(ancho * 0.5f - 0.06f, alto * 0.5f, 0f), new Vector3(0.12f, alto, 0.12f), matMarco, false);
        CrearPiezaMampara(mod.transform, "Poste_Central", new Vector3(0f, alto * 0.5f, 0f), new Vector3(0.09f, alto, 0.11f), matMarco, false);

        GameObject vidrio = CrearPiezaMampara(
            mod.transform, "Cristal_Blindado",
            new Vector3(0f, 0.28f + (alto - 0.44f) * 0.5f, 0f),
            new Vector3(Mathf.Max(0.2f, ancho - 0.24f), alto - 0.44f, 0.035f),
            matVidrio, false);
        MeshRenderer mrVidrio = vidrio.GetComponent<MeshRenderer>();
        if (mrVidrio != null)
        {
            mrVidrio.shadowCastingMode = ShadowCastingMode.Off;
            mrVidrio.receiveShadows = false;
        }

        BoxCollider col = mod.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, alto * 0.5f, 0f);
        col.size = new Vector3(ancho, alto, 0.10f);

        NavMeshObstacle obs = mod.AddComponent<NavMeshObstacle>();
        obs.shape = NavMeshObstacleShape.Box;
        obs.center = col.center;
        obs.size = col.size;
        obs.carving = true;
        obs.carveOnlyStationary = true;
    }

    private static GameObject CrearPiezaMampara(
        Transform padre, string nombre, Vector3 posLocal, Vector3 escalaLocal, Material mat, bool conCollider)
    {
        GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pieza.name = nombre;
        pieza.transform.SetParent(padre, false);
        pieza.transform.localPosition = posLocal;
        pieza.transform.localScale = escalaLocal;
        if (!conCollider)
        {
            Collider c = pieza.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }
        MeshRenderer mr = pieza.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;
        return pieza;
    }

    // -------------------------------------------------------------------------
    // PANEL VISUAL DE OBJETIVOS DEL NIVEL EN EL HUD (ENLAZADO EN INSPECTOR)
    // -------------------------------------------------------------------------
    public void ActualizarPanelObjetivosHUD(bool destelloSistemaRestaurado)
    {
        if (textoObjetivoHUD == null) return;

        bool energiaOK = EnergiaRestaurada || (GameManager.Instance != null && GameManager.Instance.ObjetivoCompletado("energia_z2"));
        bool codigoOK = GameManager.Instance != null && GameManager.Instance.TieneCodigo("codigo_z2");
        bool credencialOK = GameManager.Instance != null && GameManager.Instance.TieneItem("credencial_z2");

        if (headerObjetivoHUD != null)
        {
            headerObjetivoHUD.text = destelloSistemaRestaurado
                ? "PROTOCOLO ZONA 2  //  SISTEMA RESTAURADO"
                : "OBJETIVOS DEL LABORATORIO  //  ZONA 2";
            headerObjetivoHUD.color = destelloSistemaRestaurado
                ? new Color(0.36f, 0.95f, 0.62f, 0.98f)
                : new Color(0.46f, 0.84f, 0.90f, 0.95f);
        }

        string l1 = energiaOK
            ? "<color=#46E896>[OK]</color> <color=#B8DCE8>1. Energía (Norte): Energía restaurada</color>"
            : "<color=#F4C452>[ ]</color>  <color=#EAF2F7>1. Energía (Norte): Restaurar energía de salida</color>";

        string l2 = codigoOK
            ? "<color=#46E896>[OK]</color> <color=#B8DCE8>2. Servidores (Oeste): Compuerta desbloqueada</color>"
            : (energiaOK
                ? "<color=#F4C452>[ ]</color>  <color=#EAF2F7>2. Servidores (Oeste): Descifrar secuencia (4 símbolos)</color>"
                : "<color=#7E93A4>[ ]  2. Servidores (Oeste): Desbloquear compuerta (requiere energía)</color>");

        string l3 = credencialOK
            ? "<color=#46E896>[OK]</color> <color=#B8DCE8>3. Contención (Este): Credencial obtenida</color>"
            : "<color=#D2E0EA>[ ]</color>  <color=#C6D6E2>3. Contención (Este): Recuperar credencial en el cofre</color>";

        string l4 = (energiaOK && codigoOK && credencialOK)
            ? "<color=#46E896><b>[>>] 4. Salida (Norte): Cruzar hacia el Sector 3</b></color>"
            : "<color=#7E93A4>[ ]  4. Salida (Norte): Abrir compuerta principal</color>";

        textoObjetivoHUD.text = $"{l1}\n{l2}\n{l3}\n{l4}";

        if (fondoObjetivoHUD != null)
        {
            fondoObjetivoHUD.color = destelloSistemaRestaurado
                ? new Color(0.035f, 0.125f, 0.088f, 0.85f)
                : new Color(0.030f, 0.046f, 0.070f, 0.78f);
        }
        if (barraAcentoObjetivoHUD != null)
        {
            barraAcentoObjetivoHUD.color = destelloSistemaRestaurado
                ? new Color(0.28f, 0.92f, 0.56f, 0.95f)
                : new Color(0.20f, 0.78f, 0.83f, 0.92f);
        }
        if (bordeObjetivoHUD != null)
        {
            bordeObjetivoHUD.effectColor = destelloSistemaRestaurado
                ? new Color(0.28f, 0.92f, 0.56f, 0.36f)
                : new Color(0.22f, 0.78f, 0.84f, 0.24f);
        }
    }

    public void NotificarHackeoServidorCompletado(string codigo)
    {
        ActualizarPanelObjetivosHUD(true);
    }

    // -------------------------------------------------------------------------
    // LUCES PARPADEANTES EN EL TECHO Y CHISPAS DE CORTOCIRCUITO
    // -------------------------------------------------------------------------
    private void ConfigurarLucesParpadeantesTecho()
    {
        lucesTechoParpadeantes.Clear();

        CrearLuzTechoParpadeante("Luz_Fallo_Servidores_Z2", new Vector3(-5.40f, 4.72f, 9.60f), 3.17f, new Vector3(-8.15f, 2.15f, 10.40f));
        CrearLuzTechoParpadeante("Luz_Fallo_Pasillo_Z2", new Vector3(0.00f, 4.72f, 9.60f), 11.41f, new Vector3(0.00f, 4.82f, 9.60f));
        CrearLuzTechoParpadeante("Luz_Fallo_Energia_Z2", new Vector3(-4.60f, 4.72f, 17.80f), 27.83f, new Vector3(4.50f, 1.25f, 18.10f));
    }

    private void CrearLuzTechoParpadeante(string nombre, Vector3 posicion, float semilla, Vector3 posChispa)
    {
        GameObject go = GameObject.Find(nombre);
        Light luz;
        Renderer rend;
        Material mat;

        if (go == null)
        {
            go = new GameObject(nombre);
            go.transform.position = posicion;
            luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.range = 13.5f;
            luz.intensity = 2.4f;
            luz.color = new Color(0.38f, 0.86f, 1f);
            luz.shadows = LightShadows.None;

            Shader shLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shLit != null)
            {
                GameObject soporte = GameObject.CreatePrimitive(PrimitiveType.Cube);
                soporte.name = "Soporte_Luminaria_Fallo";
                soporte.transform.SetParent(go.transform, false);
                soporte.transform.localPosition = new Vector3(0f, 0.195f, 0f);
                soporte.transform.localScale = new Vector3(0.30f, 0.03f, 1.65f);
                Collider colSoporte = soporte.GetComponent<Collider>();
                if (colSoporte != null) Destroy(colSoporte);
                Renderer rendSoporte = soporte.GetComponent<Renderer>();
                Material matSoporte = new Material(shLit) { name = "MAT_Soporte_" + nombre, color = new Color(0.12f, 0.15f, 0.18f, 1f) };
                if (matSoporte.HasProperty("_BaseColor")) matSoporte.SetColor("_BaseColor", new Color(0.12f, 0.15f, 0.18f, 1f));
                if (rendSoporte != null) rendSoporte.sharedMaterial = matSoporte;
            }

            GameObject tubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tubo.name = "Luminaria_Fallo_Visual";
            tubo.transform.SetParent(go.transform, false);
            tubo.transform.localPosition = new Vector3(0f, 0.175f, 0f);
            tubo.transform.localScale = new Vector3(0.24f, 0.045f, 1.55f);
            Collider c = tubo.GetComponent<Collider>();
            if (c != null) Destroy(c);
            rend = tubo.GetComponent<Renderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            mat = sh != null ? new Material(sh) { name = "MAT_" + nombre } : null;
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }
        else
        {
            luz = go.GetComponent<Light>() ?? go.AddComponent<Light>();
            rend = go.GetComponentInChildren<Renderer>();
            mat = rend != null ? rend.sharedMaterial : null;
        }

        lucesTechoParpadeantes.Add(new LuzTechoParpadeante
        {
            luz = luz,
            tuboVisual = rend,
            materialTubo = mat,
            semilla = semilla,
            posicionChispa = posChispa
        });
    }

    private void ActualizarParpadeoTechoYChispas()
    {
        bool datosRecuperados = GameManager.Instance != null && GameManager.Instance.TieneCodigo("codigo_z2");
        bool todoEstable = datosRecuperados && EnergiaRestaurada;

        float t = Time.time;
        for (int i = 0; i < lucesTechoParpadeantes.Count; i++)
        {
            LuzTechoParpadeante item = lucesTechoParpadeantes[i];
            if (item.luz == null) continue;

            if (todoEstable)
            {
                Color estable = new Color(0.45f, 0.92f, 1f);
                item.luz.intensity = 2.4f;
                item.luz.color = estable;
                if (item.materialTubo != null)
                {
                    item.materialTubo.color = estable;
                    if (item.materialTubo.HasProperty("_BaseColor")) item.materialTubo.SetColor("_BaseColor", estable);
                }
                continue;
            }

            float ruido = Mathf.PerlinNoise(t * 7.8f, item.semilla);
            float microCorte = Mathf.Sin(t * 21f + item.semilla) * Mathf.Sin(t * 11.7f + item.semilla * 0.5f);
            bool apagadoBreve = ruido < 0.28f || microCorte > 0.70f;

            float factor = apagadoBreve ? 0.08f : Mathf.Lerp(0.42f, 1.38f, ruido);
            Color colorBase = EnergiaRestaurada
                ? new Color(0.36f, 0.86f, 1f)
                : new Color(1f, 0.50f, 0.18f);

            item.luz.intensity = 2.45f * factor;
            item.luz.color = colorBase;

            if (item.materialTubo != null)
            {
                Color cVisual = colorBase * Mathf.Clamp01(factor);
                item.materialTubo.color = cVisual;
                if (item.materialTubo.HasProperty("_BaseColor")) item.materialTubo.SetColor("_BaseColor", cVisual);
            }
        }

        // Sonido espacial periódico de descarga/cortocircuito eléctrico en paneles y cables dañados
        if (!todoEstable && Time.timeScale > 0f)
        {
            temporizadorChispaElectrica -= Time.deltaTime;
            if (temporizadorChispaElectrica <= 0f)
            {
                temporizadorChispaElectrica = Random.Range(3.2f, 6.0f);
                if (lucesTechoParpadeantes.Count > 0)
                {
                    Vector3 posChispa = lucesTechoParpadeantes[Random.Range(0, lucesTechoParpadeantes.Count)].posicionChispa;
                    LabAudioFeedback.ReproducirDescarga(posChispa);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.45f);
        Gizmos.DrawWireCube(new Vector3(0.05f, 1.2f, 9.0f), new Vector3(5.2f, 2.4f, 16.0f));

        foreach (ArmarioEscondite armario in FindObjectsByType<ArmarioEscondite>(FindObjectsSortMode.None))
        {
            if (armario == null) continue;
            Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.55f);
            Gizmos.DrawWireSphere(armario.transform.position + armario.transform.forward * 1.15f + Vector3.up * 0.9f, 0.45f);
        }
    }

    private void GarantizarAudioListenerUnico()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        GameObject jugador = GameObject.Find("Player");
        AudioListener listenerJugador = jugador != null
            ? jugador.GetComponentInChildren<AudioListener>(true)
            : null;

        if (listenerJugador == null || !listenerJugador.isActiveAndEnabled)
        {
            listenerJugador = null;
            foreach (AudioListener listener in listeners)
            {
                if (listener != null && listener.isActiveAndEnabled)
                {
                    listenerJugador = listener;
                    break;
                }
            }
        }

        foreach (AudioListener listener in listeners)
        {
            if (listener == null || !listener.isActiveAndEnabled || listener == listenerJugador)
                continue;

            listener.enabled = false;
        }
    }

    public void RestaurarEnergia()
    {
        EnergiaRestaurada = true;
        ActualizarIluminacion();
        if (chispasEnergia != null) chispasEnergia.Play();
        if (chispasEnergia != null) LabAudioFeedback.ReproducirDescarga(chispasEnergia.transform.position);
        if (audioAlarmaEmergencia != null)
            GameSettingsManager.GarantizarInstancia().RegistrarFuenteConVolumenBase(audioAlarmaEmergencia, 0.20f);
        if (GameManager.Instance != null)
            GameManager.Instance.CompletarObjetivo("energia_z2");
        ActualizarPanelObjetivosHUD(true);
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(
                "<b><color=#38F28D>¡SISTEMA RESTAURADO!</color></b> Energía de la salida activa — Ya puedes usar el Terminal de Servidores (Oeste)",
                4.0f);
    }

    public void CortarEnergia()
    {
        EnergiaRestaurada = false;
        ActualizarIluminacion();
        if (audioAlarmaEmergencia != null)
            GameSettingsManager.GarantizarInstancia().RegistrarFuenteConVolumenBase(audioAlarmaEmergencia, 0.30f);
        if (GameManager.Instance != null)
            GameManager.Instance.objetivos["energia_z2"] = false;
        ActualizarPanelObjetivosHUD(false);
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje("Energía desconectada");
    }

    public bool PuedeConsultarTerminal()
    {
        return EnergiaRestaurada;
    }

    private void ActivarAlarma()
    {
        if (agente != null && botonAlarma != null)
            agente.Investigar(botonAlarma.transform.position);
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje("Alarma de prueba activada: el guardia investiga el ruido");
    }

    private void ActualizarIluminacion()
    {
        if (luzEmergencia == null) return;
        luzEmergencia.enabled = true;
        luzEmergencia.intensity = EnergiaRestaurada ? 2.2f : 0.65f;
        luzEmergencia.color = EnergiaRestaurada
            ? new Color(0.42f, 0.85f, 1f)
            : new Color(1f, 0.16f, 0.12f);
    }

    private void OnDestroy()
    {
        if (interruptorEnergia != null)
        {
            interruptorEnergia.alEncender.RemoveListener(RestaurarEnergia);
            interruptorEnergia.alApagar.RemoveListener(CortarEnergia);
        }
        if (botonAlarma != null)
            botonAlarma.alPresionar.RemoveListener(ActivarAlarma);
    }
}
