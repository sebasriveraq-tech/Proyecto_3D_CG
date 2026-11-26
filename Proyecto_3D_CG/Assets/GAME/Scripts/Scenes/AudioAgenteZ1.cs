using UnityEngine;

public sealed class AudioAgenteZ1 : MonoBehaviour
{
    [SerializeField] private AgentController agente;
    [SerializeField] private AudioClip radioAcercamiento;
    [SerializeField] private AudioClip musicaPersecucion;

    private const float AlcanceRadio = 12f;
    private const float VolumenRadio = 0.70f;
    [SerializeField, Range(0f, 1f)] private float volumenMusica = 0.65f;
    private const float DuracionDesvanecimiento = 2f;

    private PlayerHealth jugador;
    private AudioSource radio;
    private AudioSource musica;
    private float nivelMusica;
    private bool enPersecucion;
    private bool audioPausado;

    private void Start()
    {
        if (agente == null)
            agente = FindFirstObjectByType<AgentController>();

        if (agente == null || radioAcercamiento == null || musicaPersecucion == null)
        {
            enabled = false;
            return;
        }

        jugador = FindFirstObjectByType<PlayerHealth>();
        radio = CrearFuenteHija(agente.transform, "Audio_Radio_Acercamiento", radioAcercamiento, 1f);
        // La distancia al personaje controla el volumen. La fuente 3D aporta la dirección.
        radio.rolloffMode = AudioRolloffMode.Custom;
        radio.maxDistance = AlcanceRadio;
        radio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));
        musica = CrearFuenteHija(transform, "Audio_Musica_Persecucion", musicaPersecucion, 0f);
    }

    private void LateUpdate()
    {
        if (jugador == null || jugador.vidaActual <= 0 || agente == null || !agente.isActiveAndEnabled)
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

        AgentState estado = agente.EstadoActual;
        float volBaseMusica = Mathf.Max(0.55f, volumenMusica);
        float factorTransicion = GameSettingsManager.Instance != null
            ? GameSettingsManager.Instance.MultiplicadorTransicionMusica
            : 1f;

        if (estado == AgentState.Persecucion || estado == AgentState.Ataque)
        {
            enPersecucion = true;
            nivelMusica = 1f;
            if (radio.isPlaying) radio.Stop();
            musica.volume = volBaseMusica * GameSettingsManager.FactorMusica * factorTransicion;
            if (!musica.isPlaying) musica.Play();
            return;
        }

        if (estado == AgentState.Patrulla) enPersecucion = false;
        if (enPersecucion || nivelMusica > 0f)
        {
            if (!enPersecucion)
                nivelMusica = Mathf.MoveTowards(nivelMusica, 0f, Time.deltaTime / DuracionDesvanecimiento);
            musica.volume = nivelMusica * volBaseMusica * GameSettingsManager.FactorMusica * factorTransicion;
            if (nivelMusica > 0f) return;
            musica.Stop();
        }

        float distancia = Vector3.Distance(jugador.transform.position, agente.transform.position);
        float cercania = Mathf.InverseLerp(AlcanceRadio, 1.5f, distancia);
        radio.volume = cercania * VolumenRadio * GameSettingsManager.FactorEfectos;
        if (cercania > 0f)
        {
            if (!radio.isPlaying) radio.Play();
        }
        else if (radio.isPlaying)
        {
            radio.Stop();
        }
    }

    private void OnDisable() => DetenerAudios();

    private void DetenerAudios()
    {
        if (radio != null) radio.Stop();
        if (musica != null) musica.Stop();
        nivelMusica = 0f;
        enPersecucion = false;
        audioPausado = false;
    }

    private static AudioSource CrearFuenteHija(Transform padre, string nombreHijo, AudioClip clip, float mezclaEspacial)
    {
        GameObject contenedor = new GameObject(nombreHijo);
        contenedor.transform.SetParent(padre, false);
        contenedor.transform.localPosition = Vector3.zero;

        AudioSource fuente = contenedor.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.clip = clip;
        fuente.loop = true;
        fuente.spatialBlend = mezclaEspacial;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
        return fuente;
    }
}
