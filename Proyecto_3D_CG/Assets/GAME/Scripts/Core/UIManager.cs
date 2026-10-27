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
    public Color colorVidaAlta = new Color(0.239f, 0.863f, 0.518f);   // verde
    public Color colorVidaMedia = new Color(1f, 0.780f, 0.250f);      // amarillo
    public Color colorVidaBaja = new Color(1f, 0.361f, 0.361f);       // rojo

    private Coroutine rutinaMensaje;

    private void Awake()
    {
        // Cada escena tiene UNA instancia del prefab Canvas.
        Instance = this;

        // Los fondos nacen ocultos: solo aparecen cuando hay texto.
        if (fondoMensaje != null) fondoMensaje.SetActive(false);
        if (fondoPrompt != null) fondoPrompt.SetActive(false);
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
    public void MostrarVictoria()   { MostrarSolo(panelVictoria); }
    public void MostrarDerrota()    { MostrarSolo(panelDerrota); }

    // ---------------------------------------------------------------
    // HUD
    // ---------------------------------------------------------------
    public void ActualizarVida(int vidaActual, int vidaMaxima)
    {
        if (vidaMaxima <= 0) return;

        float proporcion = (float)vidaActual / vidaMaxima;

        if (barraVida != null)
        {
            barraVida.fillAmount = proporcion;

            // Verde con vida alta, amarillo a la mitad, rojo al final.
            if (proporcion > 0.5f)
                barraVida.color = Color.Lerp(colorVidaMedia, colorVidaAlta, (proporcion - 0.5f) * 2f);
            else
                barraVida.color = Color.Lerp(colorVidaBaja, colorVidaMedia, proporcion * 2f);
        }

        if (textoVida != null)
            textoVida.text = "VIDA: " + vidaActual + " / " + vidaMaxima;
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
