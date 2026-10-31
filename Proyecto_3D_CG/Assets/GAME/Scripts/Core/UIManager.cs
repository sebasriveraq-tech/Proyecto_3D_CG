using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles (hijos del Canvas)")]
    public GameObject panelHUD;
    public GameObject panelMenu;
    public GameObject panelPausa;
    public GameObject panelInventario;
    public GameObject panelVictoria;
    public GameObject panelDerrota;

    [Header("Elementos del HUD")]
    public Image barraVida;              // Image con Image Type = Filled (Horizontal)
    public TextMeshProUGUI textoVida;
    public TextMeshProUGUI textoMensaje;
    public GameObject fondoMensaje;      // Fondo oscuro detras del mensaje
    public TextMeshProUGUI textoInventario;
    public TextMeshProUGUI textoPrompt;
    public GameObject fondoPrompt;       // Fondo oscuro detras del prompt

    [Header("Panel de inventario")]
    public TextMeshProUGUI textoInventarioPanel;
    public TextMeshProUGUI textoCodigosPanel;

    [Header("Colores de la barra de vida")]
    public Color colorVidaAlta = new Color(0.208f, 0.769f, 0.745f);   // cian tecnologico
    public Color colorVidaMedia = new Color(1f, 0.780f, 0.250f);      // amarillo
    public Color colorVidaBaja = new Color(1f, 0.361f, 0.361f);       // rojo

    [Header("Health feedback")]
    [SerializeField] private float duracionTransicionVida = 0.28f;
    [SerializeField] private float duracionPulsoDanio = 0.2f;
    [SerializeField] private float intensidadPulsoDanio = 0.07f;
    [SerializeField] private float umbralVidaCritica = 0.2f;

    private Coroutine rutinaMensaje;
    private Coroutine rutinaTransicionVida;
    private Coroutine rutinaPulsoDanio;
    private bool vidaInicializada;
    private int vidaAnterior;
    private float proporcionVida = 1f;
    private Vector3 escalaBaseBarra = Vector3.one;
    private Vector3 escalaBaseTexto = Vector3.one;

    private void Awake()
    {
        // Cada escena tiene UNA instancia del prefab Canvas.
        Instance = this;

        // Los fondos nacen ocultos: solo aparecen cuando hay texto.
        if (fondoMensaje != null) fondoMensaje.SetActive(false);
        if (fondoPrompt != null) fondoPrompt.SetActive(false);
        if (barraVida != null) escalaBaseBarra = barraVida.rectTransform.localScale;
        if (textoVida != null) escalaBaseTexto = textoVida.rectTransform.localScale;
    }

    private void LateUpdate()
    {
        bool estadoCritico = proporcionVida <= umbralVidaCritica;
        float pulso = estadoCritico
            ? 0.86f + 0.14f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f))
            : 1f;

        if (barraVida != null)
        {
            Color colorBarra = barraVida.color;
            colorBarra.a = pulso;
            barraVida.color = colorBarra;
        }

        if (textoVida != null)
        {
            Color colorTexto = textoVida.color;
            colorTexto.a = pulso;
            textoVida.color = colorTexto;
        }
    }

    private void Start()
    {
        // Al entrar a una zona, el HUD muestra lo que ya se consiguio antes.
        if (GameManager.Instance == null) return;
        ActualizarVida(GameManager.Instance.vidaJugador, 100);
        ActualizarInventario(GameManager.Instance.inventario);
        ActualizarCodigos(GameManager.Instance.codigos);
    }

    // ---------------------------------------------------------------
    // PANELES
    // ---------------------------------------------------------------
    public void MostrarSolo(GameObject panel)
    {
        if (panelHUD != null) panelHUD.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelInventario != null) panelInventario.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (panel != null) panel.SetActive(true);
    }

    public void MostrarHUD()        { MostrarSolo(panelHUD); }
    public void MostrarMenu()       { MostrarSolo(panelMenu); }
    public void MostrarPausa()      { MostrarSolo(panelPausa); }
    public void MostrarInventario() { MostrarSolo(panelInventario); }

    // Los paneles finales tienen botones: se congela el juego y se libera el mouse para poder pulsarlos.
    public void MostrarVictoria()
    {
        MostrarSolo(panelVictoria);
        CongelarParaBoton();
    }

    public void MostrarDerrota()
    {
        MostrarSolo(panelDerrota);
        CongelarParaBoton();
    }

    private void CongelarParaBoton()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ---------------------------------------------------------------
    // HUD
    // ---------------------------------------------------------------
    public void ActualizarVida(int vidaActual, int vidaMaxima)
    {
        if (vidaMaxima <= 0) return;

        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        float proporcion = Mathf.Clamp01((float)vidaActual / vidaMaxima);
        bool primeraLectura = !vidaInicializada;
        bool recibioDanio = vidaInicializada && vidaActual < vidaAnterior;
        float fillInicial = barraVida != null ? barraVida.fillAmount : proporcion;
        Color colorInicial = barraVida != null ? barraVida.color : colorVidaAlta;
        Color colorObjetivo;

        if (proporcion <= umbralVidaCritica)
            colorObjetivo = colorVidaBaja;
        else if (proporcion > 0.5f)
            colorObjetivo = Color.Lerp(colorVidaMedia, colorVidaAlta, (proporcion - 0.5f) * 2f);
        else
            colorObjetivo = Color.Lerp(colorVidaBaja, colorVidaMedia, proporcion * 2f);

        vidaAnterior = vidaActual;
        vidaInicializada = true;
        proporcionVida = proporcion;

        if (barraVida != null)
        {
            if (rutinaTransicionVida != null) StopCoroutine(rutinaTransicionVida);

            if (primeraLectura)
            {
                barraVida.fillAmount = proporcion;
                barraVida.color = colorObjetivo;
            }
            else
            {
                rutinaTransicionVida = StartCoroutine(
                    TransicionarVida(fillInicial, proporcion, colorInicial, colorObjetivo, recibioDanio));
            }
        }

        if (recibioDanio) ReproducirPulsoDanio();

        if (textoVida != null)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(colorObjetivo);
            string etiqueta = proporcion <= umbralVidaCritica
                ? "<color=#F26B70><b>ALERTA</b></color>  INTEGRIDAD  "
                : "INTEGRIDAD  ";
            textoVida.text = etiqueta + "<color=#" + colorHex + "><b>" + vidaActual +
                "</b></color><color=#A7BCC8> / " + vidaMaxima + "</color>";
        }
    }

    private IEnumerator TransicionarVida(float fillInicial, float fillObjetivo,
        Color colorInicial, Color colorObjetivo, bool recibioDanio)
    {
        float duracion = Mathf.Max(0.01f, duracionTransicionVida);
        float tiempo = 0f;
        const float duracionDestello = 0.07f;

        while (tiempo < duracion)
        {
            float t = Mathf.Clamp01(tiempo / duracion);
            float suave = t * t * (3f - 2f * t);

            if (barraVida != null)
            {
                barraVida.fillAmount = Mathf.Lerp(fillInicial, fillObjetivo, suave);
                Color color = Color.Lerp(colorInicial, colorObjetivo, suave);
                if (recibioDanio && tiempo < duracionDestello)
                    color = Color.Lerp(color, Color.white, 1f - tiempo / duracionDestello);
                barraVida.color = color;
            }

            tiempo += Time.unscaledDeltaTime;
            yield return null;
        }

        if (barraVida != null)
        {
            barraVida.fillAmount = fillObjetivo;
            barraVida.color = colorObjetivo;
        }
        rutinaTransicionVida = null;
    }

    private void ReproducirPulsoDanio()
    {
        if (rutinaPulsoDanio != null) StopCoroutine(rutinaPulsoDanio);
        rutinaPulsoDanio = StartCoroutine(PulsoDanio());
    }

    private IEnumerator PulsoDanio()
    {
        float duracion = Mathf.Max(0.01f, duracionPulsoDanio);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            float t = Mathf.Clamp01(tiempo / duracion);
            float escala = 1f + intensidadPulsoDanio * Mathf.Sin(t * Mathf.PI);
            if (barraVida != null)
                barraVida.rectTransform.localScale = escalaBaseBarra * escala;
            if (textoVida != null)
                textoVida.rectTransform.localScale = escalaBaseTexto * escala;

            tiempo += Time.unscaledDeltaTime;
            yield return null;
        }

        if (barraVida != null) barraVida.rectTransform.localScale = escalaBaseBarra;
        if (textoVida != null) textoVida.rectTransform.localScale = escalaBaseTexto;
        rutinaPulsoDanio = null;
    }
    public void ActualizarInventario(List<string> items)
    {
        string contenido = "";
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
                contenido += "- " + FormatearItem(items[i]) + "\n";
        }
        if (contenido == "") contenido = "Sin objetos";

        // Version corta (esquina superior derecha).
        if (textoInventario != null)
            textoInventario.text = "INVENTARIO\n" + contenido;

        // Version grande (Panel_Inventario).
        if (textoInventarioPanel != null)
            textoInventarioPanel.text = contenido;
    }

    // "credencial_z1" se muestra como "Credencial Z1" sin cambiar el ID interno.
    private string FormatearItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        string nombre = id.Replace("_", " ");
        return char.ToUpper(nombre[0]) + nombre.Substring(1);
    }

    // Codigos descubiertos: "codigo_a" se muestra como "CÓDIGO A".
    // El ID completo queda en el GameManager para que Z3 lo pueda consultar.
    public void ActualizarCodigos(List<string> codigos)
    {
        string contenido = "";
        if (codigos != null)
        {
            for (int i = 0; i < codigos.Count; i++)
                contenido += "- CÓDIGO " + FormatearCodigo(codigos[i]) + "\n";
        }
        if (contenido == "") contenido = "Sin códigos";

        if (textoCodigosPanel != null)
            textoCodigosPanel.text = "CÓDIGOS\n" + contenido;
    }

    private string FormatearCodigo(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        return id.Replace("codigo_", "").ToUpper();
    }

    public void MostrarMensaje(string mensaje, float duracion = 2f)
    {
        if (textoMensaje == null) return;
        if (rutinaMensaje != null) StopCoroutine(rutinaMensaje);
        rutinaMensaje = StartCoroutine(RutinaMensaje(mensaje, duracion));
    }

    private IEnumerator RutinaMensaje(string mensaje, float duracion)
    {
        textoMensaje.text = mensaje;
        if (fondoMensaje != null) fondoMensaje.SetActive(true);

        yield return new WaitForSeconds(duracion);

        textoMensaje.text = "";
        if (fondoMensaje != null) fondoMensaje.SetActive(false);
        rutinaMensaje = null;
    }

    public void MostrarPrompt(string mensaje)
    {
        if (textoPrompt != null) textoPrompt.text = mensaje;
        if (fondoPrompt != null) fondoPrompt.SetActive(true);
    }

    public void OcultarPrompt()
    {
        if (textoPrompt != null) textoPrompt.text = "";
        if (fondoPrompt != null) fondoPrompt.SetActive(false);
    }
}
