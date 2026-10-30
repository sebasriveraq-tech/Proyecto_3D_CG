using UnityEngine;

/// <summary>
/// Controla la mecanica local del laboratorio: energia, alarma y condiciones de salida.
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

    public bool EnergiaRestaurada { get; private set; }

    private void Awake()
    {
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
        if (audioAmbiente == null) audioAmbiente = GetComponent<AudioSource>();
        if (audioAmbiente == null) audioAmbiente = gameObject.AddComponent<AudioSource>();

        audioAmbiente.clip = LabAudioFeedback.ObtenerAmbiente();
        audioAmbiente.loop = true;
        audioAmbiente.playOnAwake = false;
        audioAmbiente.spatialBlend = 0f;
        audioAmbiente.volume = 0.3f;
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
        if (!audioAmbiente.isPlaying)
            audioAmbiente.Play();

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(
                "Zona 2: restablece la energía, recupera la credencial y consulta la terminal");
    }

    public void RestaurarEnergia()
    {
        EnergiaRestaurada = true;
        ActualizarIluminacion();
        if (chispasEnergia != null) chispasEnergia.Play();
        if (GameManager.Instance != null)
            GameManager.Instance.CompletarObjetivo("energia_z2");
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje("Energía restaurada. La terminal está disponible");
    }

    public void CortarEnergia()
    {
        EnergiaRestaurada = false;
        ActualizarIluminacion();
        if (GameManager.Instance != null)
            GameManager.Instance.objetivos["energia_z2"] = false;
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
        luzEmergencia.enabled = EnergiaRestaurada;
        luzEmergencia.intensity = EnergiaRestaurada ? 2.2f : 0.35f;
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
