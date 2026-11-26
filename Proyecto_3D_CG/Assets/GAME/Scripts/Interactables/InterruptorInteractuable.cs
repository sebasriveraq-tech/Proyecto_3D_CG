using UnityEngine;
using UnityEngine.Events;

public class InterruptorInteractuable : Interactable
{
    [Header("Estado")]
    public bool encendido;
    public UnityEvent alEncender = new UnityEvent();
    public UnityEvent alApagar = new UnityEvent();
    public AudioSource audioSource;
    public Transform palanca;
    public Renderer indicador;
    public Color colorApagado = new Color(0.95f, 0.28f, 0.12f);
    public Color colorEncendido = new Color(0.16f, 0.95f, 0.52f);

    [Header("UI Zona 2")]
    public PanelMiniJuegoZ2 panelCircuitosZ2;

    [Header("Indicadores Zona 2")]
    public Light luzEstado;
    public Transform balizaFlotanteZ2;
    public Renderer rendererBalizaZ2;
    public Renderer rendererPantallaZ2;

    private float balizaBaseY = 1.68f;

    private void Awake()
    {
        if (balizaFlotanteZ2 != null)
            balizaBaseY = balizaFlotanteZ2.localPosition.y;

        if (panelCircuitosZ2 != null)
        {
            panelCircuitosZ2.Configurar(
                PanelMiniJuegoZ2.TipoMiniJuego.ReconexionCircuitos,
                "",
                encendido,
                CompletarReconexionCircuitos,
                transform);
        }
        ActualizarVisual();
    }

    private void Update()
    {
        if (balizaFlotanteZ2 != null)
        {
            float yOffset = balizaBaseY + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.06f;
            Vector3 p = balizaFlotanteZ2.localPosition;
            balizaFlotanteZ2.localPosition = new Vector3(p.x, yOffset, p.z);
            balizaFlotanteZ2.Rotate(0f, 55f * Time.unscaledDeltaTime, 0f, Space.Self);
        }
    }

    private void CompletarReconexionCircuitos()
    {
        if (!encendido)
        {
            encendido = true;
            alEncender?.Invoke();
            ActualizarVisual();
        }
    }

    public override void Interactuar()
    {
        if (ArmarioEscondite.JugadorOculto) return;

        if (panelCircuitosZ2 != null)
        {
            LabAudioFeedback.ReproducirInterruptor(transform.position, true, audioSource);
            panelCircuitosZ2.MarcarCompletadoExternamente(encendido);
            if (UIManager.Instance != null)
                UIManager.Instance.AbrirMiniJuegoZ2(panelCircuitosZ2);
            else
                panelCircuitosZ2.Abrir();
            return;
        }

        encendido = !encendido;
        LabAudioFeedback.ReproducirInterruptor(transform.position, encendido, audioSource);
        if (encendido) alEncender?.Invoke();
        else alApagar?.Invoke();
        ActualizarVisual();
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(encendido ? "Sistema encendido" : "Sistema apagado");
    }

    private void ActualizarVisual()
    {
        if (panelCircuitosZ2 != null)
        {
            mensajePrompt = encendido
                ? "SISTEMA RESTAURADO — Energía de la salida y servidores activa (E para ver circuito)"
                : "Presiona E — Terminal de Energía: Restaurar energía de la salida y servidores";
        }

        if (palanca != null)
            palanca.localRotation = Quaternion.Euler(encendido ? -35f : 35f, 0f, 0f);

        Color c = encendido ? colorEncendido : colorApagado;
        AplicarColorEmisivo(indicador, c);

        Color cTerminal = encendido
            ? new Color(0.18f, 0.95f, 0.54f)
            : new Color(1f, 0.68f, 0.16f);

        AplicarColorEmisivo(rendererPantallaZ2, cTerminal);
        AplicarColorEmisivo(rendererBalizaZ2, cTerminal);

        if (luzEstado != null)
            luzEstado.color = cTerminal;
    }

    private static void AplicarColorEmisivo(Renderer r, Color c)
    {
        if (r == null) return;
        Material m = r.material;
        if (m == null) return;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.8f);
    }
}
