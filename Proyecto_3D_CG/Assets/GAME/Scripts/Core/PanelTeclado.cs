using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Panel de teclado numérico (vive dentro del Canvas prefab, desactivado por defecto).
/// Crea las teclas 1-9 y 0 a partir de un botón plantilla, y se maneja con el mouse.
/// Stack<char>: cada dígito se apila (Push) y el botón Borrar quita el último (Pop).
public class PanelTeclado : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoPantalla;
    public Button plantillaTecla;        // botón modelo: el script crea 10 copias y lo oculta
    public Transform contenedorTeclas;   // padre de las teclas (con Grid Layout Group)
    public Button botonBorrar;
    public Button botonCerrar;

    [Header("Audio (opcional)")]
    public AudioSource fuente;
    public AudioClip sonidoTecla;
    public AudioClip sonidoError;
    public AudioClip sonidoAcierto;

    [Header("Colores de la pantalla")]
    public Color colorNormal = Color.white;
    public Color colorError = new Color(1f, 0.25f, 0.25f);
    public Color colorAcierto = new Color(0.3f, 1f, 0.4f);

    // Stack (LIFO): historial de dígitos ingresados; Borrar hace Pop del último
    private Stack<char> historial = new Stack<char>();
    private string ingreso = "";
    private int longitud;
    private Func<string, bool> validar;
    private Action alCerrar;
    private bool construido;
    private bool bloqueado;

    // Mismo patrón que PanelMiniJuegoZ2: el UIManager cierra el panel activo ante eventos externos
    private static PanelTeclado activo;

    public static void CerrarPanelActivoPorEventoExterno()
    {
        if (activo != null) activo.Cerrar();
    }

    private void Construir()
    {
        if (construido) return;
        construido = true;

        foreach (char c in "1234567890")
        {
            Button tecla = Instantiate(plantillaTecla, contenedorTeclas);
            tecla.gameObject.SetActive(true);

            TMP_Text etiqueta = tecla.GetComponentInChildren<TMP_Text>();
            if (etiqueta != null) etiqueta.text = c.ToString();

            char digito = c;
            tecla.onClick.AddListener(() => PulsarDigito(digito));
        }
        plantillaTecla.gameObject.SetActive(false);

        if (botonBorrar != null) botonBorrar.onClick.AddListener(Borrar);
        if (botonCerrar != null) botonCerrar.onClick.AddListener(Cerrar);
    }

    /// Abre el panel. 'validador' recibe el código completo y devuelve true si es correcto.
    public void Abrir(int longitudCodigo, Func<string, bool> validador, Action alCerrarPanel)
    {
        longitud = longitudCodigo;
        validar = validador;
        alCerrar = alCerrarPanel;

        ingreso = "";
        historial.Clear();
        bloqueado = false;
        activo = this;

        Construir();
        gameObject.SetActive(true);
        ActualizarPantalla(colorNormal);
    }

    private void PulsarDigito(char digito)
    {
        if (bloqueado || ingreso.Length >= longitud) return;

        historial.Push(digito);          // Push: el último dígito queda en el tope
        ingreso += digito;
        Sonar(sonidoTecla);
        ActualizarPantalla(colorNormal);

        if (ingreso.Length == longitud)
            StartCoroutine(Enviar());
    }

    private void Borrar()
    {
        if (bloqueado || historial.Count == 0) return;

        historial.Pop();                 // Pop: se quita el último dígito ingresado
        ingreso = ingreso.Substring(0, ingreso.Length - 1);
        Sonar(sonidoTecla);
        ActualizarPantalla(colorNormal);
    }

    private IEnumerator Enviar()
    {
        bloqueado = true;
        bool correcto = validar != null && validar(ingreso);

        if (correcto)
        {
            textoPantalla.text = "ACCESO CONCEDIDO";
            textoPantalla.color = colorAcierto;
            Sonar(sonidoAcierto);
            yield return new WaitForSeconds(1.2f);
            Cerrar();
        }
        else
        {
            textoPantalla.text = "CÓDIGO INCORRECTO";
            textoPantalla.color = colorError;
            Sonar(sonidoError);
            yield return new WaitForSeconds(1.2f);

            ingreso = "";
            historial.Clear();
            bloqueado = false;
            ActualizarPantalla(colorNormal);
        }
    }

    public void Cerrar()
    {
        if (activo == this) activo = null;
        gameObject.SetActive(false);
        Action callback = alCerrar;
        alCerrar = null;
        callback?.Invoke();
    }

    private void ActualizarPantalla(Color color)
    {
        string s = "";
        for (int i = 0; i < longitud; i++)
            s += (i < ingreso.Length ? ingreso[i] : '_') + " ";

        textoPantalla.text = s.Trim();
        textoPantalla.color = color;
    }

    private void Sonar(AudioClip clip)
    {
        if (fuente != null && clip != null)
            fuente.PlayOneShot(clip);
    }
}