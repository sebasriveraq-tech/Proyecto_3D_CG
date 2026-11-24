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
    public GameObject panelComoJugar;
    public GameObject panelAjustes;
    public GameObject panelPausa;
    public GameObject panelInventario;
    public GameObject panelVictoria;
    [SerializeField] private TMP_Text estadisticasVictoria;
    [SerializeField] private TMP_Text rutaVictoria;
    public GameObject panelDerrota;
    [SerializeField] private TMP_Text estadisticasDerrota;
    [SerializeField] private TMP_Text rutaDerrota;
    public PanelTeclado panelTeclado;
    [SerializeField] private GameObject panelesZona2;
    [SerializeField] private GameObject panelObjetivosZona1;
    [SerializeField] private TMP_Text textoObjetivosZona1;
    [SerializeField] private GameObject panelObjetivosZona3;
    [SerializeField] private TMP_Text textoObjetivosZona3;


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

    private GameObject panelObjetivosActual;
    private TMP_Text textoObjetivosActual;
    private Scene3Controller controlZona3;
    private (bool primero, bool credencial, bool segundo)? progresoObjetivos;
    private bool esZona3;

    private bool resultadoFinalRegistrado;
    private Coroutine rutinaMensaje;
    private Coroutine rutinaTransicionVida;
    private Coroutine rutinaPulsoDanio;
    private Coroutine rutinaTransicionPanel;
    private bool vidaInicializada;
    private int vidaAnterior;
    private float proporcionVida = 1f;
    private Vector3 escalaBaseBarra = Vector3.one;
    private Vector3 escalaBaseTexto = Vector3.one;

    private void Awake()
    {
        // Cada escena tiene UNA instancia del prefab Canvas.
        Instance = this;
        if (panelesZona2 != null)
            panelesZona2.SetActive(gameObject.scene.name == "Zona2_Laboratorio");

        // Los fondos nacen ocultos: solo aparecen cuando hay texto.
        if (fondoMensaje != null) fondoMensaje.SetActive(false);
        if (fondoPrompt != null) fondoPrompt.SetActive(false);
        if (barraVida != null) escalaBaseBarra = barraVida.rectTransform.localScale;
        if (textoVida != null) escalaBaseTexto = textoVida.rectTransform.localScale;

        AjustesUI ajustes = GetComponent<AjustesUI>();
        if (ajustes != null) ajustes.Inicializar();
    }

    private void LateUpdate()
    {
        ActualizarObjetivosDeZona();
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
        string escena = gameObject.scene.name;
        esZona3 = escena == "Zona3_Control";
        if (escena == "Zona1_Recepcion" || esZona3)
        {
            panelObjetivosActual = esZona3 ? panelObjetivosZona3 : panelObjetivosZona1;
            textoObjetivosActual = esZona3 ? textoObjetivosZona3 : textoObjetivosZona1;
            if (esZona3) controlZona3 = FindFirstObjectByType<Scene3Controller>();
            ActualizarObjetivosDeZona();
        }

        // El menu principal se maneja con el mouse: el cursor debe verse al cargar la escena.
        if (panelMenu != null && panelMenu.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Al entrar a una zona, el HUD muestra lo que ya se consiguio antes.
        if (GameManager.Instance == null) return;
        ActualizarVida(GameManager.Instance.vidaJugador, 100);
        ActualizarInventario(GameManager.Instance.inventario);
        ActualizarCodigos(GameManager.Instance.codigos);
    }

    // ---------------------------------------------------------------
    // PANELES
    // ---------------------------------------------------------------
    private void ActualizarObjetivosDeZona()
    {
        if (panelObjetivosActual == null || textoObjetivosActual == null) return;

        bool visible = panelHUD != null && panelHUD.activeInHierarchy
            && !PanelTerminalZ1.Abierto
            && (panelTeclado == null || !panelTeclado.gameObject.activeInHierarchy);
        if (panelObjetivosActual.activeSelf != visible)
            panelObjetivosActual.SetActive(visible);

        GameManager juego = GameManager.Instance;
        if (!visible || juego == null) return;

        bool primero = esZona3
            ? controlZona3 != null && controlZona3.EnergiaRestaurada
            : juego.TieneCodigo("codigo_a");
        bool credencial = juego.TieneItem(esZona3 ? "credencial_z3" : "credencial_z1");
        bool segundo = esZona3 && controlZona3 != null && controlZona3.EtapaCompletada(1);
        var progreso = (primero, credencial, segundo);
        if (progresoObjetivos == progreso) return;
        progresoObjetivos = progreso;

        if (esZona3)
        {
            textoObjetivosActual.text =
                LineaObjetivo(primero, "1. Usa el primer código para restaurar la energía", "1. Energía restaurada") + "\n" +
                LineaObjetivo(credencial, "2. Recoge la credencial para abrir las puertas", "2. Credencial obtenida") + "\n" +
                LineaObjetivo(segundo, "3. Encuentra y usa el segundo código del corredor", "3. Código del corredor aceptado") + "\n" +
                LineaSalida(primero && credencial && segundo, "4. Escapa por la salida final");
        }
        else
        {
            textoObjetivosActual.text =
                LineaObjetivo(primero, "1. Obtén el código de la terminal", "1. Código de la terminal obtenido") + "\n" +
                LineaObjetivo(credencial, "2. Recoge la credencial de Recepción", "2. Credencial obtenida") + "\n" +
                LineaSalida(primero && credencial, "3. Abre la puerta al Laboratorio");
        }
    }

    private static string LineaObjetivo(bool completado, string pendiente, string cumplido)
    {
        return completado
            ? $"<color=#46E896>[OK]</color> <color=#B8DCE8>{cumplido}</color>"
            : $"<color=#F4C452>[ ]</color>  <color=#EAF2F7>{pendiente}</color>";
    }

    private static string LineaSalida(bool listo, string objetivo)
    {
        return listo
            ? $"<color=#46E896><b>[>>] {objetivo}</b></color>"
            : $"<color=#7E93A4>[ ]  {objetivo}</color>";
    }

    public void MostrarSolo(GameObject panel)
    {
        if (panelHUD != null) panelHUD.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelComoJugar != null) panelComoJugar.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelInventario != null) panelInventario.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (panel != null)
        {
            panel.SetActive(true);
            if (panel != panelHUD && isActiveAndEnabled)
            {
                if (rutinaTransicionPanel != null) StopCoroutine(rutinaTransicionPanel);
                rutinaTransicionPanel = StartCoroutine(TransicionarEntradaPanel(panel));
            }
        }
    }

    private IEnumerator TransicionarEntradaPanel(GameObject panel)
    {
        if (panel == null) yield break;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        float duracion = 0.16f;
        float t = 0f;
        cg.alpha = 0f;
        while (t < duracion && panel != null && panel.activeSelf)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracion));
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
        rutinaTransicionPanel = null;
    }

    public void MostrarHUD()        { MostrarSolo(panelHUD); }

    // El menu principal y su ayuda se manejan con el mouse: el cursor debe verse.
    public void MostrarMenu()
    {
        PanelTeclado.CerrarPanelActivoPorEventoExterno();
        PanelTerminalZ1.CerrarPanelActivoPorEventoExterno();
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        MostrarSolo(panelMenu);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarComoJugar()
    {
        MostrarSolo(panelComoJugar);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarAjustes()
    {
        MostrarSolo(panelAjustes);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarPausa()
    {
        PanelTeclado.CerrarPanelActivoPorEventoExterno();
        PanelTerminalZ1.CerrarPanelActivoPorEventoExterno();
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        MostrarSolo(panelPausa);
    }

    public void MostrarInventario() { MostrarSolo(panelInventario); }

    public void AbrirTeclado(int longitud, System.Func<string, bool> validar, System.Action alCerrar)
    {
        if (panelTeclado != null) panelTeclado.Abrir(longitud, validar, alCerrar);
    }

    // Los paneles finales tienen botones: se congela el juego y se libera el mouse para poder pulsarlos.
    public void MostrarVictoria()
    {
        PanelTeclado.CerrarPanelActivoPorEventoExterno();
        PanelTerminalZ1.CerrarPanelActivoPorEventoExterno();
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        MostrarResultadoFinal("Victoria", estadisticasVictoria, rutaVictoria);
        MostrarSolo(panelVictoria);
        CongelarParaBoton();
    }

    private void MostrarResultadoFinal(string estado, TMP_Text estadisticas, TMP_Text textoRuta)
    {
        GameManager juego = GameManager.Instance;
        if (resultadoFinalRegistrado || juego == null) return;
        resultadoFinalRegistrado = true;

        PlayerHealth salud = FindFirstObjectByType<PlayerHealth>();
        ResultadoPartida resultado = new ResultadoPartida
        {
            resultado = estado,
            vidaRestante = salud != null ? salud.vidaActual : juego.vidaJugador,
            vidaMaxima = salud != null ? salud.vidaMaxima : 100,
            muertes = juego.muertes,
            inventario = new List<string>(juego.inventario)
        };

        bool guardado = SaveManager.GuardarPuntuacion(resultado, out string ruta);
        if (estadisticas != null)
        {
            string objetos = resultado.inventario.Count == 0
                ? "Sin objetos"
                : string.Join("  •  ", resultado.inventario).Replace('_', ' ');
            estadisticas.text =
                $"<color=#7CDED4>VIDA RESTANTE</color>  {resultado.vidaRestante}/{resultado.vidaMaxima}    " +
                $"<color=#7CDED4>MUERTES</color>  {resultado.muertes}\n\n" +
                $"<color=#7CDED4>INVENTARIO</color>\n{objetos}";
        }
        if (textoRuta != null)
            textoRuta.text = guardado
                ? "Resultado guardado en\n" + ruta
                : "No se pudo guardar el resultado en\n" + ruta;
    }

    public void MostrarDerrota()
    {
        PanelTeclado.CerrarPanelActivoPorEventoExterno();
        PanelTerminalZ1.CerrarPanelActivoPorEventoExterno();
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        MostrarResultadoFinal("Derrota", estadisticasDerrota, rutaDerrota);
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
