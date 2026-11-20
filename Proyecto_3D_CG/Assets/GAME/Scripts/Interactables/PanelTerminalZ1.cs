using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Panel de reparacion de la terminal de recepcion. Se controla con el teclado.
public class PanelTerminalZ1 : MonoBehaviour
{
    private static bool abiertoZ1;
    public static bool AbiertoZ2 { get; set; }
    public static bool Abierto
    {
        get => abiertoZ1 || AbiertoZ2;
        private set => abiertoZ1 = value;
    }

    private Terminal terminal;
    private GameObject panel;
    private TextMeshProUGUI titulo;
    private TextMeshProUGUI estado;
    private TextMeshProUGUI instrucciones;
    private TextMeshProUGUI mensaje;
    private Image tarjetaA;
    private Image tarjetaB;
    private Image tarjetaC;
    private float tiempoAnterior;
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
        if (Abierto || terminalActual == null || GameManager.Instance == null || Time.timeScale == 0f) return;
        terminal = terminalActual;
        if (panel == null) CrearPanel();
        if (panel == null) return;

        Abierto = true;
        tiempoAnterior = Time.timeScale;
        bloqueoAnterior = Cursor.lockState;
        cursorAnterior = Cursor.visible;
        Time.timeScale = 0f;
        panel.SetActive(true);
        if (UIManager.Instance != null) UIManager.Instance.OcultarPrompt();
        ActualizarPanel("");
    }

    private void Update()
    {
        if (!Abierto || panel == null || !panel.activeSelf || Keyboard.current == null) return;
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
        bool retirar = GameManager.Instance.cantidadModulosRetiradosZ1 < 3;
        if (!terminal.ProcesarModulo(letra))
        {
            ActualizarPanel("<color=#FF7777>Letra incorrecta. Inténtalo de nuevo.</color>");
            return false;
        }

        string resultado = retirar ? "Módulo " + letra + " retirado." : "Módulo " + letra + " instalado.";
        ActualizarPanel(resultado);
        return true;
    }

    public void Cerrar()
    {
        if (!Abierto) return;
        Abierto = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = tiempoAnterior;
        Cursor.lockState = bloqueoAnterior;
        Cursor.visible = cursorAnterior;
        if (UIManager.Instance != null && terminal != null)
            UIManager.Instance.MostrarPrompt(terminal.mensajePrompt);
    }

    private void OnDestroy()
    {
        if (panel != null && panel.activeSelf && Abierto) Cerrar();
        if (panel != null) Destroy(panel);
    }

    private void ActualizarPanel(string resultado)
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || terminal == null) return;

        if (terminal.ReparacionTerminada())
        {
            titulo.text = "SISTEMA RESTABLECIDO";
            estado.text = "<color=#79F5AD>●  TERMINAL OPERATIVA</color>";
            instrucciones.text = "CÓDIGO DE ACCESO\n<color=#85FFB8><size=145%><b>" + terminal.codigoVisible + "</b></size></color>";
            mensaje.text = "Código disponible. Presiona ESC para cerrar.";
            CambiarTarjetas("");
            return;
        }

        titulo.text = "REPARAR TERMINAL";
        bool retirando = juego.cantidadModulosRetiradosZ1 < 3;
        estado.text = retirando ? "<color=#FFD17A>01  /  RETIRAR MÓDULOS</color>" : "<color=#78E6F2>02  /  REINSTALAR MÓDULOS</color>";
        instrucciones.text = retirando
            ? "Retira los módulos en orden: <b>A → B → C</b>"
            : "Instala primero el último que retiraste: <b>C → B → A</b>";
        string contenido = "";
        foreach (string modulo in juego.modulosTerminalZ1)
            contenido += (contenido == "" ? "" : "   →   ") + modulo;
        if (contenido == "") contenido = "VACÍA";
        mensaje.text = "PILA  /  CIMA → FONDO:  <b>" + contenido + "</b>\n" + resultado;
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

    private void CrearPanel()
    {
        if (UIManager.Instance == null) return;
        Transform canvas = UIManager.Instance.transform;
        panel = CrearImagen("Panel_Reparacion_Terminal", canvas, new Color(0.01f, 0.025f, 0.035f, 0.88f)).gameObject;
        RectTransform fondo = panel.GetComponent<RectTransform>();
        fondo.anchorMin = Vector2.zero;
        fondo.anchorMax = Vector2.one;
        fondo.offsetMin = Vector2.zero;
        fondo.offsetMax = Vector2.zero;

        Image borde = CrearImagen("Marco", panel.transform, new Color(0.18f, 0.70f, 0.66f, 1f));
        Ajustar(borde.rectTransform, Vector2.zero, new Vector2(980f, 600f));
        Image interior = CrearImagen("Fondo", borde.transform, new Color(0.025f, 0.07f, 0.085f, 1f));
        Ajustar(interior.rectTransform, Vector2.zero, new Vector2(970f, 590f));
        Image linea = CrearImagen("Linea", interior.transform, new Color(0.21f, 0.80f, 0.72f, 1f));
        Ajustar(linea.rectTransform, new Vector2(0f, 207f), new Vector2(840f, 3f));

        titulo = CrearTexto("Titulo", interior.transform, new Vector2(0f, 245f), new Vector2(850f, 55f), 37f, FontStyles.Bold);
        estado = CrearTexto("Estado", interior.transform, new Vector2(0f, 164f), new Vector2(850f, 45f), 25f, FontStyles.Bold);
        instrucciones = CrearTexto("Instrucciones", interior.transform, new Vector2(0f, 90f), new Vector2(850f, 85f), 26f, FontStyles.Normal);

        tarjetaA = CrearTarjeta(interior.transform, "A", -160f);
        tarjetaB = CrearTarjeta(interior.transform, "B", 0f);
        tarjetaC = CrearTarjeta(interior.transform, "C", 160f);
        mensaje = CrearTexto("Pila y mensajes", interior.transform, new Vector2(0f, -145f), new Vector2(850f, 90f), 23f, FontStyles.Normal);
        TextMeshProUGUI pie = CrearTexto("Ayuda", interior.transform, new Vector2(0f, -245f), new Vector2(850f, 40f), 20f, FontStyles.Normal);
        pie.text = "PULSA A, B O C EN TU TECLADO     •     ESC PARA CERRAR";
        pie.color = new Color(0.65f, 0.78f, 0.80f);
        panel.SetActive(false);
    }

    private Image CrearTarjeta(Transform padre, string letra, float x)
    {
        Image tarjeta = CrearImagen("Modulo_" + letra, padre, Color.white);
        Ajustar(tarjeta.rectTransform, new Vector2(x, -35f), new Vector2(120f, 95f));
        TextMeshProUGUI texto = CrearTexto("Letra", tarjeta.transform, Vector2.zero, new Vector2(110f, 85f), 46f, FontStyles.Bold);
        texto.text = letra;
        return tarjeta;
    }

    private Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.transform.SetParent(padre, false);
        Image imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private TextMeshProUGUI CrearTexto(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, float fuente, FontStyles estilo)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(padre, false);
        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        Ajustar(texto.rectTransform, posicion, tamano);
        texto.fontSize = fuente;
        texto.fontStyle = estilo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(0.92f, 0.98f, 0.96f);
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.Normal;
        return texto;
    }

    private void Ajustar(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }
}
