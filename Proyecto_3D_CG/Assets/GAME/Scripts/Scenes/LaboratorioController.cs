using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Controla la mecánica local del laboratorio (Zona 2): energía, alarma, condiciones de salida,
/// iluminación de emergencia, chispas y panel de objetivos del HUD.
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

    [Header("Luces de Techo (Zona 2)")]
    public Light[] lucesTecho;
    public Renderer[] tubosTecho;

    [Header("UI de Objetivos (Zona 2)")]
    public GameObject panelObjetivosHUD;
    public Image fondoObjetivoHUD;
    public TextMeshProUGUI headerObjetivoHUD;
    public TextMeshProUGUI textoObjetivoHUD;
    public Image barraAcentoObjetivoHUD;
    public Outline bordeObjetivoHUD;

    public bool EnergiaRestaurada { get; private set; }

    private readonly float[] semillasLuces = { 3.17f, 11.41f, 27.83f };
    private float temporizadorChispaElectrica = 2.5f;
    private bool ambientePausado;
    private bool credencialDetectadaPrev;
    private bool sonidoTareasCompletadasReproducido;

    private void Awake()
    {
        if (audioAmbiente != null)
        {
            float volBase = audioAmbiente.volume > 0.01f ? audioAmbiente.volume : 0.32f;
            GameSettingsManager.GarantizarInstancia().RegistrarFuenteConVolumenBase(audioAmbiente, volBase);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.TieneItem("credencial_z1"))
                GameManager.Instance.AgregarItem("credencial_z1");
            if (!GameManager.Instance.TieneCodigo("codigo_a"))
                GameManager.Instance.AgregarCodigo("codigo_a");
        }

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

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(
                "Zona 2: Usa el Terminal de Energía (Norte) para restaurar la corriente y el Terminal de Servidores (Oeste) para desbloquear la compuerta",
                4.2f);
    }

    private void Update()
    {
        ActualizarParpadeoTechoYChispas();
        ActualizarPausaAudio();
        SincronizarVisibilidadObjetivosHUD();

        bool tieneCredencial = GameManager.Instance != null && GameManager.Instance.TieneItem("credencial_z2");
        if (tieneCredencial != credencialDetectadaPrev)
        {
            credencialDetectadaPrev = tieneCredencial;
            ActualizarPanelObjetivosHUD(false);
        }
    }

    public void RestaurarEnergia()
    {
        EnergiaRestaurada = true;
        ActualizarIluminacion();
        if (chispasEnergia != null)
        {
            chispasEnergia.Play();
            LabAudioFeedback.ReproducirDescarga(chispasEnergia.transform.position);
        }
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
                : "<color=#B8CDD9>[ ]  2. Servidores (Oeste): Desbloquear compuerta (requiere energía)</color>");

        string l3 = credencialOK
            ? "<color=#46E896>[OK]</color> <color=#B8DCE8>3. Contención (Este): Credencial obtenida</color>"
            : "<color=#E2EEF5>[ ]</color>  <color=#E8F3FA>3. Contención (Este): Recuperar credencial en el cofre</color>";

        string l4 = (energiaOK && codigoOK && credencialOK)
            ? "<color=#46E896><b>[>>] 4. Salida (Norte): Cruzar hacia el Sector 3</b></color>"
            : "<color=#B8CDD9>[ ]  4. Salida (Norte): Abrir compuerta principal</color>";

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

        if (energiaOK && codigoOK && credencialOK && !sonidoTareasCompletadasReproducido)
        {
            sonidoTareasCompletadasReproducido = true;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ReproducirSonidoVictoria();
                UIManager.Instance.MostrarMensaje(
                    "<b><color=#46E896>¡TAREAS COMPLETADAS!</color></b> Compuerta principal desbloqueada — Dirígete a la salida (Norte)",
                    5.0f);
            }
        }
    }

    public void NotificarHackeoServidorCompletado(string codigo)
    {
        ActualizarPanelObjetivosHUD(true);
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

    private void ActualizarPausaAudio()
    {
        if (audioAmbiente == null) return;

        if (Time.timeScale == 0f && !ambientePausado)
        {
            audioAmbiente.Pause();
            ambientePausado = true;
        }
        else if (Time.timeScale > 0f && ambientePausado)
        {
            audioAmbiente.UnPause();
            ambientePausado = false;
        }
    }

    private void ActualizarParpadeoTechoYChispas()
    {
        if (lucesTecho == null || lucesTecho.Length == 0) return;

        bool datosRecuperados = GameManager.Instance != null && GameManager.Instance.TieneCodigo("codigo_z2");
        bool todoEstable = datosRecuperados && EnergiaRestaurada;

        float t = Time.time;
        for (int i = 0; i < lucesTecho.Length; i++)
        {
            Light luz = lucesTecho[i];
            if (luz == null) continue;

            Renderer tubo = (tubosTecho != null && i < tubosTecho.Length) ? tubosTecho[i] : null;
            float semilla = semillasLuces[i % semillasLuces.Length];

            if (todoEstable)
            {
                Color estable = new Color(0.45f, 0.92f, 1f);
                luz.intensity = 2.4f;
                luz.color = estable;
                if (tubo != null && tubo.material != null)
                {
                    tubo.material.color = estable;
                    if (tubo.material.HasProperty("_BaseColor"))
                        tubo.material.SetColor("_BaseColor", estable);
                }
                continue;
            }

            float ruido = Mathf.PerlinNoise(t * 7.8f, semilla);
            float microCorte = Mathf.Sin(t * 21f + semilla) * Mathf.Sin(t * 11.7f + semilla * 0.5f);
            bool apagadoBreve = ruido < 0.28f || microCorte > 0.70f;

            float factor = apagadoBreve ? 0.08f : Mathf.Lerp(0.42f, 1.38f, ruido);
            Color colorBase = EnergiaRestaurada
                ? new Color(0.36f, 0.86f, 1f)
                : new Color(1f, 0.50f, 0.18f);

            luz.intensity = 2.45f * factor;
            luz.color = colorBase;

            if (tubo != null && tubo.material != null)
            {
                Color cVisual = colorBase * Mathf.Clamp01(factor);
                tubo.material.color = cVisual;
                if (tubo.material.HasProperty("_BaseColor"))
                    tubo.material.SetColor("_BaseColor", cVisual);
            }
        }

        if (!todoEstable && Time.timeScale > 0f)
        {
            temporizadorChispaElectrica -= Time.deltaTime;
            if (temporizadorChispaElectrica <= 0f)
            {
                temporizadorChispaElectrica = Random.Range(3.2f, 6.0f);
                Light luzElegida = lucesTecho[Random.Range(0, lucesTecho.Length)];
                if (luzElegida != null)
                    LabAudioFeedback.ReproducirDescarga(luzElegida.transform.position);
            }
        }
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
