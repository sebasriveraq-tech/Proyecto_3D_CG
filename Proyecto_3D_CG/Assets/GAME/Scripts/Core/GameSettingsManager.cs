using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    private const string KeyVolumenGeneral = "CFG_VolumenGeneral";
    private const string KeyVolumenMusica = "CFG_VolumenMusica";
    private const string KeyVolumenEfectos = "CFG_VolumenEfectos";
    private const string KeySilenciado = "CFG_Silenciado";
    private const string KeySensibilidad = "CFG_SensibilidadMouse";

    public const float DefaultVolumenGeneral = 0.85f;
    public const float DefaultVolumenMusica = 0.75f;
    public const float DefaultVolumenEfectos = 0.85f;
    public const bool DefaultSilenciado = false;
    public const float DefaultSensibilidadMouse = 0.12f;

    [Range(0f, 1f)] public float volumenGeneral = DefaultVolumenGeneral;
    [Range(0f, 1f)] public float volumenMusica = DefaultVolumenMusica;
    [Range(0f, 1f)] public float volumenEfectos = DefaultVolumenEfectos;
    public bool silenciado = DefaultSilenciado;
    [Range(0.03f, 0.35f)] public float sensibilidadMouse = DefaultSensibilidadMouse;

    public event Action OnConfiguracionCambiada;

    private readonly Dictionary<int, float> volumenesBase = new Dictionary<int, float>();
    private AudioSource fuentePreviewUI;
    private float temporizadorEscaneo;
    private Coroutine rutinaFadeMusica;

    public float MultiplicadorTransicionMusica { get; private set; } = 1f;

    public static float FactorEfectos =>
        Instance != null ? (Instance.silenciado ? 0f : Instance.volumenEfectos) : DefaultVolumenEfectos;

    public static float FactorMusica =>
        Instance != null ? (Instance.silenciado ? 0f : Instance.volumenMusica) : DefaultVolumenMusica;

    public static float FactorGeneral =>
        Instance != null ? (Instance.silenciado ? 0f : Instance.volumenGeneral) : DefaultVolumenGeneral;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InicializarAutomaticamente()
    {
        GarantizarInstancia();
    }

    public static GameSettingsManager GarantizarInstancia()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<GameSettingsManager>();
        if (Instance != null) return Instance;

        GameObject go = new GameObject("GameSettingsManager");
        Instance = go.AddComponent<GameSettingsManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CargarPreferencias();
        ConfigurarFuentesInternas();
        SceneManager.sceneLoaded += AlCargarEscena;
        AplicarConfiguracionEnTiempoReal();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Update()
    {
        temporizadorEscaneo -= Time.unscaledDeltaTime;
        if (temporizadorEscaneo <= 0f)
        {
            temporizadorEscaneo = 0.75f;
            SincronizarFuentesDeEscena();
        }
    }

    private void ConfigurarFuentesInternas()
    {
        fuentePreviewUI = gameObject.AddComponent<AudioSource>();
        fuentePreviewUI.loop = false;
        fuentePreviewUI.playOnAwake = false;
        fuentePreviewUI.spatialBlend = 0f;
        fuentePreviewUI.volume = 0.45f;
        volumenesBase[fuentePreviewUI.GetInstanceID()] = 0.45f;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        AplicarConfiguracionEnTiempoReal();
        if (MultiplicadorTransicionMusica < 0.99f)
        {
            if (rutinaFadeMusica != null) StopCoroutine(rutinaFadeMusica);
            rutinaFadeMusica = StartCoroutine(FundirMusicaA(1f, 0.65f));
        }
    }

    public void CargarPreferencias()
    {
        volumenGeneral = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolumenGeneral, DefaultVolumenGeneral));
        volumenMusica = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolumenMusica, DefaultVolumenMusica));
        volumenEfectos = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolumenEfectos, DefaultVolumenEfectos));
        silenciado = PlayerPrefs.GetInt(KeySilenciado, DefaultSilenciado ? 1 : 0) == 1;
        sensibilidadMouse = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensibilidad, DefaultSensibilidadMouse), 0.03f, 0.35f);
    }

    public void GuardarPreferencias()
    {
        PlayerPrefs.SetFloat(KeyVolumenGeneral, volumenGeneral);
        PlayerPrefs.SetFloat(KeyVolumenMusica, volumenMusica);
        PlayerPrefs.SetFloat(KeyVolumenEfectos, volumenEfectos);
        PlayerPrefs.SetInt(KeySilenciado, silenciado ? 1 : 0);
        PlayerPrefs.SetFloat(KeySensibilidad, sensibilidadMouse);
        PlayerPrefs.Save();
    }

    public void EstablecerVolumenGeneral(float valor)
    {
        volumenGeneral = Mathf.Clamp01(valor);
        GuardarPreferencias();
        AplicarConfiguracionEnTiempoReal();
    }

    public void EstablecerVolumenMusica(float valor)
    {
        volumenMusica = Mathf.Clamp01(valor);
        GuardarPreferencias();
        AplicarConfiguracionEnTiempoReal();
    }

    public void EstablecerVolumenEfectos(float valor)
    {
        volumenEfectos = Mathf.Clamp01(valor);
        GuardarPreferencias();
        AplicarConfiguracionEnTiempoReal();
    }

    public void EstablecerSilencio(bool estado)
    {
        silenciado = estado;
        GuardarPreferencias();
        AplicarConfiguracionEnTiempoReal();
    }

    public void EstablecerSensibilidadMouse(float valor)
    {
        sensibilidadMouse = Mathf.Clamp(valor, 0.03f, 0.35f);
        GuardarPreferencias();
        AplicarConfiguracionEnTiempoReal();
    }

    public void RestablecerPredeterminados()
    {
        volumenGeneral = DefaultVolumenGeneral;
        volumenMusica = DefaultVolumenMusica;
        volumenEfectos = DefaultVolumenEfectos;
        silenciado = DefaultSilenciado;
        sensibilidadMouse = DefaultSensibilidadMouse;

        GuardarPreferencias();
        AplicarConfiguracionEnTiempoReal();
        ReproducirFeedbackUI();
    }

    public void AplicarConfiguracionEnTiempoReal()
    {
        AudioListener.volume = silenciado ? 0f : volumenGeneral;
        SincronizarFuentesDeEscena();

        PlayerController jugador = FindFirstObjectByType<PlayerController>();
        if (jugador != null)
            jugador.sensibilidadMouse = sensibilidadMouse;

        OnConfiguracionCambiada?.Invoke();
    }

    public void SincronizarFuentesDeEscena()
    {
        AudioListener.volume = silenciado ? 0f : volumenGeneral;

        AudioSource[] fuentes = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource fuente in fuentes)
        {
            if (fuente == null) continue;

            if (EsFuenteDinamicaAgente(fuente))
            {
                fuente.mute = silenciado;
                if (MultiplicadorTransicionMusica < 0.999f)
                    fuente.volume = Mathf.Min(fuente.volume, MultiplicadorTransicionMusica);
                continue;
            }

            int id = fuente.GetInstanceID();
            if (!volumenesBase.TryGetValue(id, out float baseVol))
            {
                baseVol = fuente.volume > 0.001f ? fuente.volume : 0.7f;
                volumenesBase[id] = baseVol;
            }

            bool esMusicaOAmbiente = EsFuenteMusicaOAmbiente(fuente);
            float factor = silenciado ? 0f : (esMusicaOAmbiente ? volumenMusica * MultiplicadorTransicionMusica : volumenEfectos);
            fuente.volume = Mathf.Clamp01(baseVol * factor);
            fuente.mute = silenciado;
        }
    }

    public void RegistrarFuenteConVolumenBase(AudioSource fuente, float volumenBase)
    {
        if (fuente == null) return;
        volumenesBase[fuente.GetInstanceID()] = Mathf.Clamp01(volumenBase);
        bool esMusicaOAmbiente = EsFuenteMusicaOAmbiente(fuente);
        float factor = silenciado ? 0f : (esMusicaOAmbiente ? volumenMusica * MultiplicadorTransicionMusica : volumenEfectos);
        fuente.volume = Mathf.Clamp01(volumenBase * factor);
        fuente.mute = silenciado;
    }

    public void EstablecerMultiplicadorTransicionMusica(float valor)
    {
        MultiplicadorTransicionMusica = Mathf.Clamp01(valor);
        SincronizarFuentesDeEscena();
    }

    public void IniciarFadeOutMusica(float duracion = 0.85f)
    {
        if (rutinaFadeMusica != null) StopCoroutine(rutinaFadeMusica);
        rutinaFadeMusica = StartCoroutine(FundirMusicaA(0f, duracion));
    }

    public IEnumerator FundirMusicaA(float destino, float duracion)
    {
        float inicio = MultiplicadorTransicionMusica;
        float total = Mathf.Max(0.05f, duracion);
        float t = 0f;

        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / total));
            EstablecerMultiplicadorTransicionMusica(Mathf.Lerp(inicio, destino, k));
            yield return null;
        }

        EstablecerMultiplicadorTransicionMusica(destino);
        rutinaFadeMusica = null;
    }

    public void ReproducirFeedbackUI()
    {
        ReproducirFeedbackUI(1.08f);
    }

    public void ReproducirFeedbackUI(float pitch)
    {
        if (silenciado || fuentePreviewUI == null) return;

        AudioClip clip = (UIManager.Instance != null && UIManager.Instance.sonidoClicUI != null)
            ? UIManager.Instance.sonidoClicUI
            : Resources.Load<AudioClip>("SFX/SFX_BotonPresion");
        if (clip == null) return;

        fuentePreviewUI.mute = silenciado;
        fuentePreviewUI.volume = 0.42f * volumenEfectos;
        fuentePreviewUI.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        fuentePreviewUI.PlayOneShot(clip);
    }

    private static bool EsFuenteDinamicaAgente(AudioSource fuente)
    {
        if (fuente == null) return false;

        string nombreObj = fuente.gameObject.name;
        if (nombreObj.IndexOf("Audio_Radio_", StringComparison.OrdinalIgnoreCase) >= 0
            || nombreObj.IndexOf("Audio_Musica_Persecucion", StringComparison.OrdinalIgnoreCase) >= 0
            || nombreObj.IndexOf("AudioAgenteZ3", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (fuente.clip == null) return false;

        string nombreClip = fuente.clip.name;
        return nombreClip.IndexOf("Radio_Acercamiento", StringComparison.OrdinalIgnoreCase) >= 0
            || nombreClip.IndexOf("Musica_Persecucion", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool EsFuenteMusicaOAmbiente(AudioSource fuente)
    {
        if (fuente == null) return false;
        if (fuente.loop) return true;

        string nombreObjeto = fuente.gameObject.name.ToLowerInvariant();
        if (nombreObjeto.Contains("ambiente") || nombreObjeto.Contains("music") || nombreObjeto.Contains("alarma"))
            return true;

        if (fuente.clip != null)
        {
            string nombreClip = fuente.clip.name.ToLowerInvariant();
            if (nombreClip.Contains("amb_") || nombreClip.Contains("loop") || nombreClip.Contains("music") ||
                nombreClip.Contains("alarm"))
                return true;
        }

        return false;
    }
}
