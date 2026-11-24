
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

    private PlayerHealth jugador;
    private AudioSource radio;
    private AudioSource musica;

    private bool musicaIniciada;
    private bool audioPausado;

    private void Start()
    {
        jugador = GetComponent<PlayerHealth>();

        if (jugador == null)
            jugador = FindFirstObjectByType<PlayerHealth>();

        if (jugador == null || radioAcercamiento == null ||
            musicaPersecucion == null)
        {
            Debug.LogWarning("AudioAgenteZ3: faltan referencias.");
            enabled = false;
            return;
        }

        radio = CrearFuente(
            radioAcercamiento,
            1f
        );

        radio.rolloffMode = AudioRolloffMode.Linear;
        radio.minDistance = 1.5f;
        radio.maxDistance = alcanceRadio;

        musica = CrearFuente(
            musicaPersecucion,
            0f
        );
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

        AgentControllerEtapa1 enemigoCercano = null;
        float menorDistancia = alcanceRadio;

        bool hayPersecucion = false;

        foreach (AgentControllerEtapa1 enemigo in enemigos)
        {
            if (enemigo == null || !enemigo.isActiveAndEnabled)
                continue;

            if (enemigo.EstaActivado)
            {
                hayPersecucion = true;
                continue;
            }

            float distancia = Vector3.Distance(
                jugador.transform.position,
                enemigo.transform.position
            );

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                enemigoCercano = enemigo;
            }
        }

        // La persecucion tiene prioridad sobre la radio.
        if (hayPersecucion)
        {
            radio.Stop();
            musica.volume = volumenMusica *
                GameSettingsManager.FactorMusica;

            if (!musicaIniciada)
            {
                musica.Play();
                musicaIniciada = true;
            }

            return;
        }

        // Si no hay persecucion, se controla la radio por cercania.
        musica.volume = 0f;

        if (musica.isPlaying)
            musica.Stop();

        musicaIniciada = false;

        if (enemigoCercano != null)
        {
            radio.transform.SetParent(enemigoCercano.transform);
            radio.transform.localPosition = Vector3.zero;

            float cercania = Mathf.InverseLerp(
                alcanceRadio,
                1.5f,
                menorDistancia
            );

            radio.volume = cercania * volumenRadio *
                GameSettingsManager.FactorEfectos;

            if (!radio.isPlaying)
                radio.Play();
        }
        else
        {
            radio.Stop();
        }
    }

    private AudioSource CrearFuente(AudioClip clip, float mezclaEspacial)
    {
        GameObject objeto = new GameObject("AudioAgenteZ3");
        objeto.transform.SetParent(transform);
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

        musicaIniciada = false;
        audioPausado = false;
    }
}
