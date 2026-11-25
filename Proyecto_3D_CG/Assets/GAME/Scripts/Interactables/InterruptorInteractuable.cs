using UnityEngine;
using UnityEngine.Events;

/// Interruptor / Terminal de Energía integrado visualmente con su computador y consola eléctrica 3D.
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
        IntegrarConConsola3D();
        if (balizaFlotanteZ2 != null)
            balizaBaseY = balizaFlotanteZ2.localPosition.y;

        if (gameObject.scene.name == "Zona2_Laboratorio")
        {
            if (panelCircuitosZ2 != null)
            {
                panelCircuitosZ2.Configurar(
                    PanelMiniJuegoZ2.TipoMiniJuego.ReconexionCircuitos,
                    "",
                    encendido,
                    CompletarReconexionCircuitos,
                    transform);
            }
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

    private void IntegrarConConsola3D()
    {
        Transform cajaObj = transform.Find("Caja");
        if (palanca == null) palanca = transform.Find("Palanca");
        Transform indObj = transform.Find("Indicador");
        if (indicador == null && indObj != null) indicador = indObj.GetComponent<Renderer>();

        bool tieneConsola3D = GameObject.Find("Consola_Interruptor") != null;
        if (!tieneConsola3D)
        {
            foreach (Transform hijo in transform)
            {
                if (hijo != cajaObj && hijo != palanca && hijo != indObj && hijo.GetComponentInChildren<Renderer>(true) != null)
                {
                    tieneConsola3D = true;
                    break;
                }
            }
        }

        if (tieneConsola3D)
        {
            if (cajaObj != null)
            {
                cajaObj.localPosition = new Vector3(0f, 0.66f, -0.195f);
                cajaObj.localRotation = Quaternion.identity;
                cajaObj.localScale = new Vector3(0.24f, 0.28f, 0.045f);
            }

            if (palanca != null)
            {
                palanca.localPosition = new Vector3(0f, 0.63f, -0.235f);
                palanca.localScale = new Vector3(0.042f, 0.135f, 0.042f);
            }

            if (indObj != null)
            {
                indObj.localPosition = new Vector3(0f, 0.755f, -0.222f);
                indObj.localScale = new Vector3(0.055f, 0.055f, 0.055f);
            }

            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 0.72f, 0f);
                col.size = new Vector3(0.72f, 1.42f, 0.58f);
            }
        }

        if (luzEstado == null)
        {
            Transform luzExistente = transform.Find("Luz_Estado_Interruptor");
            if (luzExistente != null)
                luzEstado = luzExistente.GetComponent<Light>();
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
        if (gameObject.scene.name == "Zona2_Laboratorio")
        {
            mensajePrompt = encendido
                ? "SISTEMA RESTAURADO — Energía de la salida y servidores activa (E para ver circuito)"
                : "Presiona E — Terminal de Energía: Restaurar energía de la salida y servidores";
        }

        if (palanca != null)
            palanca.localRotation = Quaternion.Euler(encendido ? -35f : 35f, 0f, 0f);

        Color c = encendido ? colorEncendido : colorApagado;
        if (indicador != null)
        {
            Material mat = indicador.material;
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.8f);
        }

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
