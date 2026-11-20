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

    private Light luzEstado;
    private Transform balizaFlotanteZ2;
    private Renderer rendererBalizaZ2;
    private Renderer rendererPantallaZ2;

    private void Awake()
    {
        IntegrarConConsola3D();
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
            CrearIndicadorClaridadZ2();
        }
        ActualizarVisual();
    }

    private void Update()
    {
        if (balizaFlotanteZ2 != null)
        {
            float yOffset = 1.68f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.06f;
            balizaFlotanteZ2.localPosition = new Vector3(0f, yOffset, -0.05f);
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

        Transform luzExistente = transform.Find("Luz_Estado_Interruptor");
        if (luzExistente == null)
        {
            GameObject goLuz = new GameObject("Luz_Estado_Interruptor");
            goLuz.transform.SetParent(transform, false);
            goLuz.transform.localPosition = tieneConsola3D
                ? new Vector3(0f, 1.05f, -0.36f)
                : new Vector3(0f, 0.3f, -0.28f);
            luzEstado = goLuz.AddComponent<Light>();
            luzEstado.type = LightType.Point;
            luzEstado.range = 3.2f;
            luzEstado.intensity = 1.15f;
            luzEstado.shadows = LightShadows.None;
        }
        else
        {
            luzEstado = luzExistente.GetComponent<Light>();
        }
    }

    private void CrearIndicadorClaridadZ2()
    {
        if (transform.Find("Baliza_Terminal_Energia_Z2") != null) return;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Pantalla luminosa en el monitor de la consola de energía
        GameObject pantallaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pantallaObj.name = "Pantalla_Luminosa_Energia_Z2";
        pantallaObj.transform.SetParent(transform, false);
        pantallaObj.transform.localPosition = new Vector3(0f, 1.06f, -0.18f);
        pantallaObj.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
        pantallaObj.transform.localScale = new Vector3(0.42f, 0.24f, 0.02f);
        Collider colP = pantallaObj.GetComponent<Collider>();
        if (colP != null) Destroy(colP);
        rendererPantallaZ2 = pantallaObj.GetComponent<Renderer>();
        if (sh != null && rendererPantallaZ2 != null)
            rendererPantallaZ2.sharedMaterial = new Material(sh) { name = "MAT_Pantalla_Energia_Z2" };

        // Baliza flotante 3D (rombo indicador sobre el terminal)
        GameObject balizaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        balizaObj.name = "Baliza_Terminal_Energia_Z2";
        balizaObj.transform.SetParent(transform, false);
        balizaObj.transform.localPosition = new Vector3(0f, 1.68f, -0.05f);
        balizaObj.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        balizaObj.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
        Collider colB = balizaObj.GetComponent<Collider>();
        if (colB != null) Destroy(colB);
        balizaFlotanteZ2 = balizaObj.transform;
        rendererBalizaZ2 = balizaObj.GetComponent<Renderer>();
        if (sh != null && rendererBalizaZ2 != null)
            rendererBalizaZ2.sharedMaterial = new Material(sh) { name = "MAT_Baliza_Energia_Z2" };
    }

    public override void Interactuar()
    {
        if (panelCircuitosZ2 != null)
        {
            ReproducirSonidoInteraccion(audioSource);
            panelCircuitosZ2.MarcarCompletadoExternamente(encendido);
            panelCircuitosZ2.Abrir();
            return;
        }

        encendido = !encendido;
        ReproducirSonidoInteraccion(audioSource);
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
        if (r == null || r.sharedMaterial == null) return;
        Material m = r.sharedMaterial;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.8f);
    }
}
