
using UnityEngine;

public class AudioAgenteZ3 : MonoBehaviour
{
    [Header("Enemigos de la etapa 1")]
    [SerializeField] private AgentControllerEtapa1[] enemigos;

    [Header("Audios")]
    [SerializeField] private AudioClip radioAcercamiento;
    [SerializeField] private AudioClip musicaPersecucion;

    [Header("Configuracion")]
    [SerializeField] private float alcanceRadio = 12f;
    [SerializeField] private float volumenRadio = 0.70f;
    [SerializeField] private float volumenMusica = 0.65f;

    private const float DuracionDesvanecimiento = 2f;

    private PlayerHealth jugador;
    private AgentController[] agentesPrincipales = System.Array.Empty<AgentController>();
    private AudioSource radio;
    private AudioSource musica;

    private float nivelMusica;
    private bool enPersecucion;
    private bool audioPausado;

    private void Start()
    {
        jugador = GetComponent<PlayerHealth>();
        if (jugador == null)
            jugador = FindFirstObjectByType<PlayerHealth>();

        agentesPrincipales = FindObjectsByType<AgentController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (enemigos == null || enemigos.Length == 0)
            enemigos = FindObjectsByType<AgentControllerEtapa1>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (jugador == null || radioAcercamiento == null || musicaPersecucion == null)
        {
            Debug.LogWarning("AudioAgenteZ3: faltan referencias.");
            enabled = false;
            return;
        }

        radio = CrearFuente("Audio_Radio_Acercamiento_Z3", radioAcercamiento, 1f);
        radio.rolloffMode = AudioRolloffMode.Custom;
        radio.maxDistance = alcanceRadio;
        radio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));

        musica = CrearFuente("Audio_Musica_Persecucion_Z3", musicaPersecucion, 0f);
    }

    private void LateUpdate()
    {
        if (jugador == null || jugador.vidaActual <= 0)
        {
            DetenerAudios();
            return;
        }

        if (Time.timeScale == 0f)
        {
            if (!audioPausado)
            {
                radio.Pause();
                musica.Pause();
                audioPausado = true;
            }

            radio.volume = 0f;
            musica.volume = 0f;
            return;
        }

        if (audioPausado)
        {
            radio.UnPause();
            musica.UnPause();
            audioPausado = false;
        }

        Transform enemigoCercano = null;
        float menorDistancia = alcanceRadio;
        bool hayPersecucionActiva = false;
        bool todosEnPatrulla = true;

        if (agentesPrincipales != null)
        {
            for (int i = 0; i < agentesPrincipales.Length; i++)
            {
                AgentController ag = agentesPrincipales[i];
                if (ag == null || !ag.isActiveAndEnabled)
                    continue;

                AgentState est = ag.EstadoActual;
                if (est == AgentState.Persecucion || est == AgentState.Ataque)
                    hayPersecucionActiva = true;
                if (est != AgentState.Patrulla)
                    todosEnPatrulla = false;

                float d = Vector3.Distance(jugador.transform.position, ag.transform.position);
                if (d < menorDistancia)
                {
                    menorDistancia = d;
                    enemigoCercano = ag.transform;
                }
            }
        }

        if (enemigos != null)
        {
            for (int i = 0; i < enemigos.Length; i++)
            {
                AgentControllerEtapa1 enemigo = enemigos[i];
                if (enemigo == null || !enemigo.isActiveAndEnabled || !enemigo.EstaActivado)
                    continue;

                AgentState est = enemigo.EstadoActual;
                if (est == AgentState.Persecucion || est == AgentState.Ataque)
                    hayPersecucionActiva = true;
                if (est != AgentState.Patrulla)
                    todosEnPatrulla = false;

                float d = Vector3.Distance(jugador.transform.position, enemigo.transform.position);
                if (d < menorDistancia)
                {
                    menorDistancia = d;
                    enemigoCercano = enemigo.transform;
                }
            }
        }

        float factorTransicion = GameSettingsManager.Instance != null
            ? GameSettingsManager.Instance.MultiplicadorTransicionMusica
            : 1f;

        if (hayPersecucionActiva)
        {
            enPersecucion = true;
            nivelMusica = 1f;
            if (radio.isPlaying) radio.Stop();
            musica.volume = volumenMusica * GameSettingsManager.FactorMusica * factorTransicion;
            if (!musica.isPlaying)
                musica.Play();
            return;
        }

        if (todosEnPatrulla)
            enPersecucion = false;

        if (enPersecucion || nivelMusica > 0f)
        {
            if (!enPersecucion)
                nivelMusica = Mathf.MoveTowards(nivelMusica, 0f, Time.deltaTime / DuracionDesvanecimiento);

            musica.volume = nivelMusica * volumenMusica * GameSettingsManager.FactorMusica * factorTransicion;
            if (nivelMusica > 0f)
                return;

            if (musica.isPlaying)
                musica.Stop();
        }

        if (enemigoCercano != null)
        {
            radio.transform.position = enemigoCercano.position;
            float cercania = Mathf.InverseLerp(alcanceRadio, 1.5f, menorDistancia);
            radio.volume = cercania * volumenRadio * GameSettingsManager.FactorEfectos;

            if (cercania > 0f)
            {
                if (!radio.isPlaying)
                    radio.Play();
            }
            else if (radio.isPlaying)
            {
                radio.Stop();
            }
        }
        else if (radio.isPlaying)
        {
            radio.Stop();
        }
    }

    private AudioSource CrearFuente(string nombreObjeto, AudioClip clip, float mezclaEspacial)
    {
        GameObject objeto = new GameObject(nombreObjeto);
        objeto.transform.SetParent(transform, false);
        objeto.transform.localPosition = Vector3.zero;

        AudioSource fuente = objeto.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = mezclaEspacial;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
        return fuente;
    }

    private void OnDisable()
    {
        DetenerAudios();
    }

    private void DetenerAudios()
    {
        if (radio != null)
            radio.Stop();

        if (musica != null)
            musica.Stop();

        nivelMusica = 0f;
        enPersecucion = false;
        audioPausado = false;
    }
}
