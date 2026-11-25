
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// Una etapa de código: tiene su propio papel con un código aleatorio y efectos al completarse.
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

public class Scene3Controller : MonoBehaviour
{
    [Header("Etapas de código")]
    public List<EtapaCodigo> etapas = new List<EtapaCodigo>();

    [Header("Luces")]
    public bool apagarTodasAlInicio = true;
    public Light[] lucesExcluidas;

    [Header("Ambiente")]
    public Color ambienteApagado = Color.black;
    public Color ambienteEncendido = new Color(0.25f, 0.25f, 0.28f);

    [Header("Efectos al restaurar la energía")]
    public ParticleSystem[] particulasEnergia;
    public GameObject[] agentesAlRestaurar;
    public AudioSource sonidoEnergia;
    public AudioSource ambienteAlarma;

    [Header("Salida / victoria")]
    public PuertaCorrediza puertaSalida;
    public AudioClip sonidoVictoria;

    [Header("Mensajes del sistema")]
    public float segundosPorMensaje = 2.5f;

    private Dictionary<string, EtapaCodigo> etapaPorCodigo =
        new Dictionary<string, EtapaCodigo>();

    private Queue<string> colaMensajes = new Queue<string>();

    private List<Light> lucesApagadas = new List<Light>();

    private bool mostrandoMensajes;
    private bool victoriaActivada;

    private AudioSource audioVictoria;

    public bool EtapaCompletada(int indice)
    {
        return indice >= 0 &&
               indice < etapas.Count &&
               etapas[indice].completada;
    }

    public bool EnergiaRestaurada => EtapaCompletada(0);

    private void Awake()
    {
        audioVictoria = gameObject.AddComponent<AudioSource>();

        audioVictoria.playOnAwake = false;
        audioVictoria.spatialBlend = 0f;
        audioVictoria.volume = 1f;
        audioVictoria.loop = false;
        audioVictoria.clip = sonidoVictoria;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.TieneItem("credencial_z2"))
                GameManager.Instance.AgregarItem("credencial_z2");
            if (!GameManager.Instance.TieneCodigo("codigo_z2"))
                GameManager.Instance.AgregarCodigo("codigo_z2");
        }

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

    private void GenerarCodigos()
    {
        etapaPorCodigo.Clear();

        if (etapas.Count == 0)
            Debug.LogWarning("Scene3Controller: no hay etapas configuradas.");

        foreach (EtapaCodigo etapa in etapas)
        {
            string codigo;

            do
            {
                codigo = Random.Range(10000, 100000).ToString();
            }
            while (etapaPorCodigo.ContainsKey(codigo));

            etapa.codigo = codigo;
            etapa.completada = false;

            etapaPorCodigo[codigo] = etapa;

            if (etapa.textoPapel != null)
                etapa.textoPapel.text = codigo;
            else
                Debug.LogWarning(
                    "Scene3Controller: falta el texto del papel en la etapa '" +
                    etapa.nombre + "'."
                );
        }
    }

    public bool IntentarCodigo(int indice, string codigo)
    {
        if (indice < 0 || indice >= etapas.Count)
            return false;

        EtapaCodigo etapa = etapas[indice];

        if (etapa.completada)
            return false;

        if (indice > 0 && !etapas[indice - 1].completada)
        {
            EncolarMensaje(
                "Este sistema no responde. Restaura primero el sistema anterior."
            );

            return false;
        }

        if (!etapaPorCodigo.TryGetValue(codigo, out EtapaCodigo encontrada) ||
            encontrada != etapa)
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
        RenderSettings.ambientMode =
            UnityEngine.Rendering.AmbientMode.Flat;

        RenderSettings.ambientLight = new Color(
            Mathf.Max(ambienteApagado.r, 0.10f),
            Mathf.Max(ambienteApagado.g, 0.11f),
            Mathf.Max(ambienteApagado.b, 0.13f));
        RenderSettings.reflectionIntensity = 0.2f;

        if (apagarTodasAlInicio)
        {
            Light[] todas = FindObjectsByType<Light>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            foreach (Light l in todas)
            {
                if (System.Array.IndexOf(lucesExcluidas, l) >= 0)
                    continue;

                if (l.enabled)
                {
                    l.enabled = false;
                    lucesApagadas.Add(l);
                }
            }
        }

        foreach (GameObject a in agentesAlRestaurar)
        {
            if (a != null)
                a.SetActive(false);
        }
    }

    private void RestaurarEnergia()
    {
        RenderSettings.ambientLight = new Color(
            Mathf.Max(ambienteEncendido.r, 0.36f),
            Mathf.Max(ambienteEncendido.g, 0.37f),
            Mathf.Max(ambienteEncendido.b, 0.40f));
        RenderSettings.reflectionIntensity = 1f;

        foreach (Light l in lucesApagadas)
        {
            if (l != null)
                l.enabled = true;
        }

        foreach (ParticleSystem ps in particulasEnergia)
        {
            if (ps != null)
                ps.Play();
        }

        if (ambienteAlarma != null)
            ambienteAlarma.Stop();

        if (sonidoEnergia != null)
            sonidoEnergia.Play();

        foreach (GameObject a in agentesAlRestaurar)
        {
            if (a != null)
                a.SetActive(true);
        }

        EncolarMensaje("Energía restaurada.");
        EncolarMensaje("Alerta: agentes de seguridad reactivados.");
    }

    // ---------- Victoria ----------

    private void OnSalidaAbierta()
    {
        if (victoriaActivada)
            return;

        victoriaActivada = true;

        EncolarMensaje("¡Salida principal habilitada!");

        DetenerAudioDeJuego();

        if (sonidoVictoria != null)
            audioVictoria.Play();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarVictoria();
        }
        else
        {
            Debug.LogError(
                "No se encontró el UIManager para mostrar la victoria."
            );
        }

        Debug.Log("¡Victoria! El jugador abrió la salida final.");
    }

    private void DetenerAudioDeJuego()
    {
        AudioSource[] fuentes = FindObjectsByType<AudioSource>(
            FindObjectsSortMode.None
        );

        foreach (AudioSource fuente in fuentes)
        {
            if (fuente != audioVictoria)
                fuente.Stop();
        }

        AudioAgenteZ3[] audiosAgentes =
            FindObjectsByType<AudioAgenteZ3>(
                FindObjectsSortMode.None
            );

        foreach (AudioAgenteZ3 audio in audiosAgentes)
        {
            audio.enabled = false;
        }

        EfectosZona3[] efectosJugador =
            FindObjectsByType<EfectosZona3>(
                FindObjectsSortMode.None
            );

        foreach (EfectosZona3 efecto in efectosJugador)
        {
            efecto.enabled = false;
        }

        AgentControllerEtapa1[] enemigos =
            FindObjectsByType<AgentControllerEtapa1>(
                FindObjectsSortMode.None
            );

        foreach (AgentControllerEtapa1 enemigo in enemigos)
        {
            enemigo.enabled = false;
        }
    }

    // ---------- Cola de mensajes ----------

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
            string siguiente = colaMensajes.Peek();
            colaMensajes.Dequeue();

            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje(siguiente);

            yield return new WaitForSeconds(segundosPorMensaje);
        }

        mostrandoMensajes = false;
    }
}
