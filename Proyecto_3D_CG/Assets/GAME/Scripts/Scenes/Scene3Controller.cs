using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

/// Controlador de la Zona 3 (Sala de control / Contención).
/// Coordina la lógica LOCAL de la escena: códigos, restauración de energía, luces,
/// partículas, agentes, mensajes del sistema y victoria.
/// NO guarda copia del estado global (vida, inventario, progreso): eso vive en el GameManager.
public class Scene3Controller : MonoBehaviour
{
    [Header("Código aleatorio")]
    public TMP_Text textoCodigoPapel;
    private List<string> codigosRequeridos = new List<string>();

    [Header("Luces: todas apagadas hasta restaurar la energía")]
    public bool apagarTodasAlInicio = true;
    [Tooltip("Luces que NO se apagan (ej. linterna del jugador).")]
    public Light[] lucesExcluidas;

    [Header("Ambiente (luz ambiental y reflejos)")]
    public Color ambienteApagado = Color.black;
    public Color ambienteEncendido = new Color(0.25f, 0.25f, 0.28f);

    [Header("Efectos al restaurar la energía")]
    public ParticleSystem[] particulasEnergia;
    public GameObject[] agentesAlRestaurar;    // agentes extra que aparecen al restaurar
    public AudioSource sonidoEnergia;
    public AudioSource ambienteAlarma;         // se detiene al restaurar

    [Header("Salida / victoria")]
    public PuertaCorrediza puertaSalida;       // debe tener el evento AlAbrirse

    [Header("Mensajes del sistema")]
    public float segundosPorMensaje = 2.5f;

    // ESTRUCTURAS DE DATOS (con función real en la escena)
    private Dictionary<string, bool> estadoCodigos = new Dictionary<string, bool>(); // código -> ¿ya usado?
    private Queue<string> colaMensajes = new Queue<string>();                        // FIFO: mensajes uno a uno
    private List<Light> lucesApagadas = new List<Light>();                           // luces que se reencienden

    private bool energiaRestaurada;

    /// Otros objetos (ej. la puerta) consultan si ya se restauró la energía.
    public bool EnergiaRestaurada => energiaRestaurada;
    private bool mostrandoMensajes;

    private void Start()
    {
        // Generar un código aleatorio de 5 dígitos.
        string codigoAleatorio = Random.Range(10000, 100000).ToString();

        // Reemplazar el código anterior por el nuevo.
        codigosRequeridos.Clear();
        codigosRequeridos.Add(codigoAleatorio);

        // Mostrar el código en el papel de la pared.
        if (textoCodigoPapel != null)
        {
            textoCodigoPapel.text = codigoAleatorio;
        }
        else
        {
            Debug.LogWarning("No se ha asignado el texto del papel.");
        }

        // Inicializar los códigos requeridos.
        foreach (string codigo in codigosRequeridos)
            estadoCodigos[codigo] = false;

        AplicarEstadoSinEnergia();

        if (puertaSalida != null)
            puertaSalida.AlAbrirse += OnSalidaAbierta;

        EncolarMensaje("Sistema central sin energía.");
        EncolarMensaje("Encuentra el código y úsalo en la computadora.");
    }

    private void OnDestroy()
    {
        if (puertaSalida != null)
            puertaSalida.AlAbrirse -= OnSalidaAbierta;
    }

    // ---------- Códigos (los llama la computadora/terminal) ----------

    /// Devuelve true si el código fue aceptado.
    public bool IntentarCodigo(string codigo)
    {
        if (energiaRestaurada) return false;

        if (!estadoCodigos.ContainsKey(codigo))
        {
            EncolarMensaje("Código inválido.");
            return false;
        }

        if (estadoCodigos[codigo])
        {
            EncolarMensaje("Ese código ya fue ingresado.");
            return false;
        }

        estadoCodigos[codigo] = true;
        EncolarMensaje("Código aceptado (" + ContarAceptados() + "/" + estadoCodigos.Count + ").");

        if (TodosAceptados())
            RestaurarEnergia();

        return true;
    }

    private int ContarAceptados()
    {
        int n = 0;
        foreach (var par in estadoCodigos)
            if (par.Value) n++;
        return n;
    }

    private bool TodosAceptados()
    {
        return estadoCodigos.Count > 0 && ContarAceptados() == estadoCodigos.Count;
    }

    // ---------- Eventos de la escena ----------

    private void AplicarEstadoSinEnergia()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambienteApagado;
        RenderSettings.reflectionIntensity = 0f;

        energiaRestaurada = false;

        if (apagarTodasAlInicio)
        {
            Light[] todas = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Light l in todas)
            {
                if (System.Array.IndexOf(lucesExcluidas, l) >= 0) continue;

                if (l.enabled)
                {
                    l.enabled = false;
                    lucesApagadas.Add(l);   // recordamos cuáles estaban encendidas
                }
            }
        }

        foreach (GameObject a in agentesAlRestaurar)
            if (a != null) a.SetActive(false);
    }

    private void RestaurarEnergia()
    {
        RenderSettings.ambientLight = ambienteEncendido;
        RenderSettings.reflectionIntensity = 1f;
        energiaRestaurada = true;

        // Cambio de estado de las luces: se encienden las que estaban encendidas originalmente
        foreach (Light l in lucesApagadas)
            if (l != null) l.enabled = true;

        foreach (ParticleSystem ps in particulasEnergia)
            if (ps != null) ps.Play();

        if (ambienteAlarma != null) ambienteAlarma.Stop();
        if (sonidoEnergia != null) sonidoEnergia.Play();

        foreach (GameObject a in agentesAlRestaurar)
            if (a != null) a.SetActive(true);

        EncolarMensaje("Energía restaurada.");
        EncolarMensaje("Alerta: agentes de seguridad reactivados.");

        // TODO: notifica al GameManager el objetivo completado (adapta al método que tenga)
        // GameManager.Instance.CompletarObjetivo("energia_restaurada");
    }

    private void OnSalidaAbierta()
    {
        EncolarMensaje("Salida principal habilitada.");
        // TODO: llama a la victoria del GameManager (adapta al método que tenga)
        // GameManager.Instance.Victoria();
        Debug.Log("Victoria: salida abierta (conecta con el GameManager).");
    }

    // ---------- Cola de mensajes (Queue: Enqueue / Dequeue) ----------

    public void EncolarMensaje(string mensaje)
    {
        colaMensajes.Enqueue(mensaje);
        if (!mostrandoMensajes)
            StartCoroutine(ProcesarMensajes());
    }

    private IEnumerator ProcesarMensajes()
    {
        mostrandoMensajes = true;

        while (colaMensajes.Count > 0)
        {
            string siguiente = colaMensajes.Dequeue();
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje(siguiente);
            yield return new WaitForSeconds(segundosPorMensaje);
        }

        mostrandoMensajes = false;
    }
}