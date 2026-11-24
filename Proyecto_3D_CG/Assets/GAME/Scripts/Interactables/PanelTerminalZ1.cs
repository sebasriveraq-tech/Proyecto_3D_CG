using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Panel de reparacion de la terminal de recepcion. Se controla con el teclado.
public class PanelTerminalZ1 : MonoBehaviour
{
    private static PanelTerminalZ1 panelActivoZ1;
    public static bool AbiertoZ2 { get; set; }
    public static bool Abierto => panelActivoZ1 != null || AbiertoZ2;

    private Terminal terminal;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titulo;
    [SerializeField] private TextMeshProUGUI estado;
    [SerializeField] private TextMeshProUGUI instrucciones;
    [SerializeField] private TextMeshProUGUI mensaje;
    [SerializeField] private Image tarjetaA;
    [SerializeField] private Image tarjetaB;
    [SerializeField] private Image tarjetaC;
    private CursorLockMode bloqueoAnterior;
    private bool cursorAnterior;

    // Textos visibles: sirven para revisar el panel desde pruebas.
    public string TituloTexto => titulo != null ? titulo.text : "";
    public string EstadoTexto => estado != null ? estado.text : "";
    public string InstruccionesTexto => instrucciones != null ? instrucciones.text : "";
    public string MensajeTexto => mensaje != null ? mensaje.text : "";
    public bool PanelActivo => panel != null && panel.activeSelf;

    public void Abrir(Terminal terminalActual)
    {
        if (Abierto || terminalActual == null || GameManager.Instance == null || Time.timeScale == 0f || ArmarioEscondite.JugadorOculto) return;
        if (panel == null || gameObject.scene.name != "Zona1_Recepcion") return;
        terminal = terminalActual;

        panelActivoZ1 = this;
        bloqueoAnterior = Cursor.lockState;
        cursorAnterior = Cursor.visible;
        panel.SetActive(true);
        if (UIManager.Instance != null) UIManager.Instance.OcultarPrompt();
        ActualizarPanel("");
    }

    private void LateUpdate()
    {
        if (panelActivoZ1 != this || !PanelActivo) return;
        if (ArmarioEscondite.JugadorOculto)
        {
            Cerrar(false);
            return;
        }
        if (Keyboard.current == null) return;
        Keyboard teclado = Keyboard.current;
        if (teclado.escapeKey.wasPressedThisFrame)
        {
            Cerrar();
            return;
        }

        if (terminal.ReparacionTerminada()) return;
        string letra = "";
        if (teclado.aKey.wasPressedThisFrame) letra = "A";
        else if (teclado.bKey.wasPressedThisFrame) letra = "B";
        else if (teclado.cKey.wasPressedThisFrame) letra = "C";
        if (letra == "") return;
        ProcesarLetra(letra);
    }

    // Recibe la letra pulsada. Devuelve false si no era la esperada.
    public bool ProcesarLetra(string letra)
    {
        if (terminal == null || terminal.ReparacionTerminada()) return false;
        Vector3 posAudio = terminal.transform.position;
        bool retirar = GameManager.Instance.cantidadModulosRetiradosZ1 < 3;
        if (!terminal.ProcesarModulo(letra))
        {
            LabAudioFeedback.ReproducirAccesoDenegado(posAudio);
            ActualizarPanel("<color=#FF7777>Letra incorrecta. Inténtalo de nuevo.</color>");
            return false;
        }

        if (terminal.ReparacionTerminada())
            LabAudioFeedback.ReproducirSistemaRestaurado(posAudio);
        else
            LabAudioFeedback.ReproducirTono(posAudio, retirar ? 620f : 820f, 0.08f, 0.34f);

        string resultado = retirar ? "Módulo " + letra + " retirado." : "Módulo " + letra + " instalado.";
        ActualizarPanel(resultado);
        return true;
    }

    public static void CerrarPanelActivoPorEventoExterno()
    {
        if (panelActivoZ1 != null) panelActivoZ1.Cerrar(false);
    }

    public void Cerrar() => Cerrar(true);

    private void Cerrar(bool mostrarPrompt)
    {
        if (panelActivoZ1 != this) return;
        panelActivoZ1 = null;
        if (panel != null) panel.SetActive(false);
        Cursor.lockState = bloqueoAnterior;
        Cursor.visible = cursorAnterior;
        if (mostrarPrompt)
            LabAudioFeedback.ReproducirPanelUI(false);
        if (mostrarPrompt && UIManager.Instance != null && terminal != null)
            UIManager.Instance.MostrarPrompt(terminal.mensajePrompt);
    }

    private void OnDisable() => Cerrar(false);

    private void ActualizarPanel(string resultado)
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || terminal == null) return;

        if (terminal.ReparacionTerminada())
        {
            titulo.text = "SISTEMA RESTABLECIDO";
            estado.text = "<color=#79F5AD>•  TERMINAL OPERATIVA</color>";
            instrucciones.text = "CÓDIGO DE ACCESO\n<color=#85FFB8><size=145%><b>" + terminal.codigoVisible + "</b></size></color>";
            mensaje.text = "Código disponible. Presiona ESC para cerrar.";
            CambiarTarjetas("");
            return;
        }

        titulo.text = "REPARAR TERMINAL";
        bool retirando = juego.cantidadModulosRetiradosZ1 < 3;
        estado.text = retirando ? "<color=#FFD17A>01  /  RETIRAR MÓDULOS</color>" : "<color=#78E6F2>02  /  REINSTALAR MÓDULOS</color>";
        instrucciones.text = retirando
            ? "Retira los módulos en orden: <b>A -> B -> C</b>"
            : "Instala primero el último que retiraste: <b>C -> B -> A</b>";
        string contenido = "";
        foreach (string modulo in juego.modulosTerminalZ1)
            contenido += (contenido == "" ? "" : "   ->   ") + modulo;
        if (contenido == "") contenido = "VACÍA";
        mensaje.text = "PILA  /  CIMA -> FONDO:  <b>" + contenido + "</b>\n" + resultado;
        CambiarTarjetas(terminal.SiguienteModulo());
    }

    private void CambiarTarjetas(string siguiente)
    {
        Color normal = new Color(0.10f, 0.18f, 0.22f, 1f);
        Color activa = new Color(0.10f, 0.46f, 0.43f, 1f);
        if (tarjetaA != null) tarjetaA.color = siguiente == "A" ? activa : normal;
        if (tarjetaB != null) tarjetaB.color = siguiente == "B" ? activa : normal;
        if (tarjetaC != null) tarjetaC.color = siguiente == "C" ? activa : normal;
    }
}
