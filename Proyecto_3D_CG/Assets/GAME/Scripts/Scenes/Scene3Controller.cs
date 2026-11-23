using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// Una etapa de código: tiene su propio papel con un código aleatorio y efectos al completarse.
/// Etapa 0 = energía de la sala (luces), etapa 1 = energía de las puertas, etapa 2 = salida final.
[System.Serializable]
public class EtapaCodigo
{
    public string nombre = "Etapa";
    [Tooltip("Texto 3D del papel donde se muestra el código de esta etapa")]
    public TMP_Text textoPapel;
    [Tooltip("Mensaje del sistema al aceptar el código")]
    public string mensajeAlCompletar = "Código aceptado.";
    [Tooltip("Efectos extra: luces de puertas, partículas, sonidos, activar agentes...")]
    public UnityEvent alCompletar;

    [HideInInspector] public string codigo;
    [HideInInspector] public bool completada;
}

/// Controlador de la Zona 3 (Sala de control / Contención).
/// Coordina la lógica LOCAL de la escena: etapas de código, restauración de energía, luces,
/// partículas, agentes, mensajes del sistema y victoria.
/// NO guarda copia del estado global (vida, inventario, progreso): eso vive en el GameManager.
public class Scene3Controller : MonoBehaviour
{
    [Header("Etapas de código (0 = luces/sala, 1 = puertas, 2 = salida final)")]
    public List<EtapaCodigo> etapas = new List<EtapaCodigo>();

    [Header("Luces: todas apagadas hasta restaurar la energía (etapa 0)")]
    public bool apagarTodasAlInicio = true;
    [Tooltip("Luces que NO se apagan (ej. linterna del jugador).")]
    public Light[] lucesExcluidas;

    [Header("Ambiente (luz ambiental y reflejos)")]
    public Color ambienteApagado = Color.black;
    public Color ambienteEncendido = new Color(0.25f, 0.25f, 0.28f);

    [Header("Efectos al restaurar la energía (etapa 0)")]
    public ParticleSystem[] particulasEnergia;
    public GameObject[] agentesAlRestaurar;    // agentes que se activan al restaurar
    public AudioSource sonidoEnergia;
    public AudioSource ambienteAlarma;         // se detiene al restaurar

    [Header("Salida / victoria")]
    public PuertaCorrediza puertaSalida;       // debe tener el evento AlAbrirse

    [Header("Mensajes del sistema")]
    public float segundosPorMensaje = 2.5f;

    // ESTRUCTURAS DE DATOS (con función real en la escena)
    private Dictionary<string, EtapaCodigo> etapaPorCodigo = new Dictionary<string, EtapaCodigo>(); // código -> etapa
    private Queue<string> colaMensajes = new Queue<string>();                                        // FIFO: mensajes uno a uno
    private List<Light> lucesApagadas = new List<Light>();                                           // luces que se reencienden

    private bool mostrandoMensajes;

    /// ¿Ya se completó la etapa indicada? (las puertas consultan esto)
    public bool EtapaCompletada(int indice)
    {
        return indice >= 0 && indice < etapas.Count && etapas[indice].completada;
    }

    /// Energía de la sala restaurada (etapa 0). Se mantiene por compatibilidad.
    public bool EnergiaRestaurada => EtapaCompletada(0);

    private void Start()
    {
        GenerarCodigos();
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

    // ---------- Códigos ----------

    // Genera un código aleatorio de 5 dígitos, distinto para cada etapa, y lo muestra en su papel
    private void GenerarCodigos()
    {
        etapaPorCodigo.Clear();

        if (etapas.Count == 0)
            Debug.LogWarning("Scene3Controller: no hay etapas configuradas.");

        foreach (EtapaCodigo etapa in etapas)
        {
            string codigo;
            do { codigo = Random.Range(10000, 100000).ToString(); }
            while (etapaPorCodigo.ContainsKey(codigo));

            etapa.codigo = codigo;
            etapa.completada = false;
            etapaPorCodigo[codigo] = etapa;

            if (etapa.textoPapel != null)
                etapa.textoPapel.text = codigo;
            else
                Debug.LogWarning("Scene3Controller: falta el texto del papel en la etapa '" + etapa.nombre + "'.");
        }
    }

    /// Lo llama la computadora de la etapa 'indice'. Devuelve true si el código fue aceptado.
    public bool IntentarCodigo(int indice, string codigo)
    {
        if (indice < 0 || indice >= etapas.Count) return false;

        EtapaCodigo etapa = etapas[indice];
        if (etapa.completada) return false;

        // Las etapas se completan en orden
        if (indice > 0 && !etapas[indice - 1].completada)
        {
            EncolarMensaje("Este sistema no responde. Restaura primero el sistema anterior.");
            return false;
        }

        // Dictionary: código -> etapa. Cada computadora solo acepta el código de SU etapa.
        if (!etapaPorCodigo.TryGetValue(codigo, out EtapaCodigo encontrada) || encontrada != etapa)
        {
            EncolarMensaje("Código inválido.");
            return false;
        }

        etapa.completada = true;
        EncolarMensaje(etapa.mensajeAlCompletar);

        if (indice == 0)
            RestaurarEnergia();

        etapa.alCompletar?.Invoke();
        return true;
    }

    // ---------- Eventos de la escena ----------

    private void AplicarEstadoSinEnergia()
    {
        // Oscuridad controlada: luz ambiental plana y sin reflejos del skybox
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambienteApagado;
        RenderSettings.reflectionIntensity = 0f;

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

    // Etapa 0: se restaura la energía de la sala
    private void RestaurarEnergia()
    {
        RenderSettings.ambientLight = ambienteEncendido;
        RenderSettings.reflectionIntensity = 1f;

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
        EncolarMensaje("¡Salida principal habilitada!");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarVictoria();
        }
        else
        {
            Debug.LogError("No se encontró el UIManager para mostrar la victoria.");
        }

        Debug.Log("¡Victoria! El jugador abrió la salida final.");
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