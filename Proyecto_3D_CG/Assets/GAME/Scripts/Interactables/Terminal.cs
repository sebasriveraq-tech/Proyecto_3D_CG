using UnityEngine;

/// Terminal: entrega un código. En Zona 1 usa PanelTerminalZ1; en Zona 2 abre el minijuego de Hackeo de Frecuencia
/// para desbloquear la compuerta de salida (Código LAB-2381).
public class Terminal : Interactable
{
    [Header("Codigo que entrega")]
    public string idCodigo = "codigo_a";
    public string codigoVisible = "A-4721";
    public bool requiereEnergia;
    public LaboratorioController laboratorio;

    [Header("UI Zona 2")]
    public PanelMiniJuegoZ2 panelServidorZ2;

    [Header("Indicadores Zona 2")]
    public Light luzEstadoZ2;
    public Renderer ledEstadoZ2;
    public Transform balizaFlotanteZ2;
    public Renderer rendererBalizaZ2;

    private const string objetivoReparacionZ1 = "terminal_reparada_z1";
    private readonly string[] modulosZ1 = { "A", "B", "C" };
    private PanelTerminalZ1 panelReparacion;
    private PantallaTerminalZ1 pantalla;
    private float balizaBaseY = 0.85f;

    private void Start()
    {
        if (EsTerminalDeRecepcion())
        {
            // Estos objetos solo se crean para la terminal de la Zona 1.
            mensajePrompt = "Presiona E para usar la terminal";
            if (UIManager.Instance != null)
                panelReparacion = UIManager.Instance.GetComponent<PanelTerminalZ1>();
            pantalla = gameObject.AddComponent<PantallaTerminalZ1>();
            pantalla.Configurar(this);
            ActualizarPantalla();
            return;
        }

        if (gameObject.scene.name == "Zona2_Laboratorio")
        {
            if (balizaFlotanteZ2 != null)
                balizaBaseY = balizaFlotanteZ2.localPosition.y;

            bool yaTieneCodigo = GameManager.Instance != null && GameManager.Instance.TieneCodigo(idCodigo);
            if (panelServidorZ2 != null)
            {
                panelServidorZ2.Configurar(
                    PanelMiniJuegoZ2.TipoMiniJuego.RecuperacionDatos,
                    codigoVisible,
                    yaTieneCodigo,
                    CompletarRecuperacionDatosZ2,
                    transform);
            }
            ConfigurarIndicadorVisualZ2();
        }
    }

    private void CompletarRecuperacionDatosZ2()
    {
        GameManager juego = GameManager.Instance;
        if (juego == null) return;

        juego.AgregarCodigo(idCodigo);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ActualizarCodigos(juego.codigos);
            UIManager.Instance.MostrarMensaje(
                $"<b><color=#38F28D>¡SISTEMA RESTAURADO!</color></b> Compuerta de salida desbloqueada — Código {codigoVisible} registrado",
                4.0f);
        }

        if (laboratorio != null)
            laboratorio.NotificarHackeoServidorCompletado(codigoVisible);

        ActualizarIndicadorVisualZ2();
    }

    private void Update()
    {
        if (luzEstadoZ2 != null)
            ActualizarIndicadorVisualZ2();

        if (balizaFlotanteZ2 != null)
        {
            float yOffset = balizaBaseY + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.05f;
            Vector3 p = balizaFlotanteZ2.localPosition;
            balizaFlotanteZ2.localPosition = new Vector3(p.x, yOffset, p.z);
            balizaFlotanteZ2.Rotate(0f, 55f * Time.unscaledDeltaTime, 0f, Space.Self);
        }
    }

    private void ConfigurarIndicadorVisualZ2()
    {
        MeshRenderer raizRenderer = GetComponent<MeshRenderer>();
        if (raizRenderer != null)
            raizRenderer.enabled = false;

        // Ajustar BoxCollider al volumen exacto de la consola/estación sin bloquear pasillos vecinos
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = new Vector3(0f, 0.05f, 0f);
            box.size = new Vector3(0.88f, 1.05f, 1.25f);
        }

        ActualizarIndicadorVisualZ2();
    }

    private void ActualizarIndicadorVisualZ2()
    {
        bool conEnergia = !requiereEnergia || (laboratorio != null && laboratorio.PuedeConsultarTerminal());
        bool consultada = GameManager.Instance != null && GameManager.Instance.TieneCodigo(idCodigo);

        if (!conEnergia)
        {
            mensajePrompt = "Terminal de Servidores (Sin corriente) — Primero restaura la energía en el sector Norte";
        }
        else if (consultada)
        {
            mensajePrompt = $"SISTEMA RESTAURADO — Compuerta desbloqueada con código {codigoVisible} (E para ver terminal)";
        }
        else
        {
            mensajePrompt = $"Presiona E — Terminal de Servidores: Desbloquear compuerta de salida (Código {codigoVisible})";
        }

        Color c = !conEnergia
            ? new Color(0.95f, 0.24f, 0.14f)
            : (consultada ? new Color(0.18f, 0.95f, 0.54f) : new Color(0.18f, 0.88f, 1f));

        if (luzEstadoZ2 != null)
            luzEstadoZ2.color = c;

        AplicarColor(ledEstadoZ2, c);
        AplicarColor(rendererBalizaZ2, c);
    }

    private static void AplicarColor(Renderer r, Color c)
    {
        if (r == null) return;
        Material mat = r.material;
        if (mat == null) return;
        mat.color = c;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.9f);
    }

    public override void Interactuar()
    {
        if (ArmarioEscondite.JugadorOculto) return;

        if (requiereEnergia &&
            (laboratorio == null || !laboratorio.PuedeConsultarTerminal()))
        {
            LabAudioFeedback.ReproducirAccesoDenegado(transform.position);
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje(
                    "<color=#FFAD3B><b>TERMINAL SIN CORRIENTE:</b></color> Dirígete al Terminal de Energía (Norte) para restaurar los circuitos.",
                    3.2f);
            return;
        }

        ReproducirSonidoInteraccion();

        GameManager juego = GameManager.Instance;
        if (juego == null) return;

        if (EsTerminalDeRecepcion())
        {
            if (juego.ObjetivoCompletado(objetivoReparacionZ1))
                juego.AgregarCodigo(idCodigo);
            ActualizarPantalla();
            if (UIManager.Instance != null)
                UIManager.Instance.AbrirTerminalZ1(panelReparacion, this);
            else if (panelReparacion != null)
                panelReparacion.Abrir(this);
            return;
        }

        if (panelServidorZ2 != null)
        {
            if (juego.TieneCodigo(idCodigo))
                panelServidorZ2.MarcarCompletadoExternamente(true);
            if (UIManager.Instance != null)
                UIManager.Instance.AbrirMiniJuegoZ2(panelServidorZ2);
            else
                panelServidorZ2.Abrir();
            return;
        }

        bool yaTeniaCodigo = juego.TieneCodigo(idCodigo);
        juego.AgregarCodigo(idCodigo);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ActualizarCodigos(juego.codigos);
            UIManager.Instance.MostrarMensaje((yaTeniaCodigo ? "Código consultado: " : "Código obtenido: ") + codigoVisible);
        }
    }

    private bool EsTerminalDeRecepcion()
    {
        return gameObject.scene.name == "Zona1_Recepcion" && idCodigo == "codigo_a";
    }

    public bool ReparacionTerminada()
    {
        GameManager juego = GameManager.Instance;
        return juego != null &&
            (juego.TieneCodigo(idCodigo) || juego.ObjetivoCompletado(objetivoReparacionZ1));
    }

    public string SiguienteModulo()
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || ReparacionTerminada()) return "";
        if (juego.cantidadModulosRetiradosZ1 < modulosZ1.Length)
            return modulosZ1[juego.cantidadModulosRetiradosZ1];
        if (juego.modulosTerminalZ1.Count > 0)
            return juego.modulosTerminalZ1.Peek();
        return "";
    }

    public bool ProcesarModulo(string letra)
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || ReparacionTerminada() || letra != SiguienteModulo()) return false;

        if (juego.cantidadModulosRetiradosZ1 < modulosZ1.Length)
        {
            juego.modulosTerminalZ1.Push(letra);
            juego.cantidadModulosRetiradosZ1++;
        }
        else
        {
            juego.modulosTerminalZ1.Pop();
            if (juego.modulosTerminalZ1.Count == 0)
            {
                juego.CompletarObjetivo(objetivoReparacionZ1);
                juego.AgregarCodigo(idCodigo);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ActualizarCodigos(juego.codigos);
                    UIManager.Instance.MostrarMensaje("Panel reparado. Código obtenido: " + codigoVisible);
                }
                ActualizarPantalla();
            }
        }
        return true;
    }

    private void ActualizarPantalla()
    {
        if (pantalla == null) return;
        GameManager juego = GameManager.Instance;
        int pasosHechos = 0;
        if (juego != null)
            pasosHechos = juego.cantidadModulosRetiradosZ1 + (modulosZ1.Length - juego.modulosTerminalZ1.Count);
        pantalla.ActualizarEstado(ReparacionTerminada(), codigoVisible, pasosHechos, modulosZ1.Length * 2);
    }
}
