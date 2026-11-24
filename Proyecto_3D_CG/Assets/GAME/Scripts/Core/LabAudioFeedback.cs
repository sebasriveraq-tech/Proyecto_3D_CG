using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de audio livianos generados en runtime para que los prototipos
/// tengan retroalimentacion incluso antes de importar bancos de sonido.
/// </summary>
public static class LabAudioFeedback
{
    private static readonly Dictionary<int, AudioClip> tonos = new Dictionary<int, AudioClip>();
    private static AudioClip ambiente;
    private static AudioClip recoger;
    private static AudioClip terminal;
    private static AudioClip boton;
    private static AudioClip interruptor;
    private static AudioClip cajon;
    private static AudioClip puertaDeslizante;
    private static AudioClip puertaMetalAbrir;
    private static AudioClip puertaMetalCerrar;
    private static AudioClip liberacionVapor;
    private static AudioClip agenteAlerta;
    private static AudioClip agenteAtaque;
    private static AudioClip[] impactosJugador;
    private static AudioClip descarga;
    private static AudioClip accesoDenegadoClip;
    private static AudioClip sistemaRestauradoClip;
    private static AudioClip alarmaEmergenciaElectricaZ2;

    public static void ReproducirInteraccion(Vector3 posicion, string tipo)
    {
        string clave = string.IsNullOrEmpty(tipo) ? string.Empty : tipo.ToLowerInvariant();
        AudioClip clip;

        if (clave.Contains("credencial") || clave.Contains("recogible") || clave.Contains("tarjeta"))
            clip = Cargar(ref recoger, "SFX/SFX_RecogerCredencial");
        else if (clave.Contains("terminal") || clave.Contains("computadora"))
            clip = Cargar(ref terminal, "SFX/SFX_TerminalConsulta");
        else if (clave.Contains("interruptor"))
            clip = Cargar(ref interruptor, "SFX/SFX_InterruptorEnergia");
        else if (clave.Contains("cajon") || clave.Contains("contenedor") || clave.Contains("gabinete") || clave.Contains("cofre"))
            clip = Cargar(ref cajon, "SFX/SFX_CajonMetalico");
        else if (clave.Contains("armario"))
            clip = Cargar(ref puertaMetalAbrir, "Zona2/Doors/SFX_PuertaMetal_Abrir");
        else if (clave.Contains("puerta"))
            clip = Cargar(ref puertaDeslizante, "Zona2/Doors/SFX_PuertaDeslizante_47");
        else if (clave.Contains("boton"))
            clip = Cargar(ref boton, "SFX/SFX_BotonPresion");
        else
            clip = Cargar(ref boton, "SFX/SFX_BotonPresion");

        if (clip != null)
            ReproducirClipEspacial(clip, posicion, 0.8f, 1f, 0.9f, 1.8f, 18f);
        else
            ReproducirTono(posicion, 740f, 0.09f, 0.3f);
    }

    public static void ReproducirPuertaArmario(Vector3 posicion, bool abrir, AudioSource fuente = null, AudioClip clipOverride = null)
    {
        AudioClip clip = clipOverride != null
            ? clipOverride
            : (abrir
                ? Cargar(ref puertaMetalAbrir, "Zona2/Doors/SFX_PuertaMetal_Abrir")
                : Cargar(ref puertaMetalCerrar, "Zona2/Doors/SFX_PuertaMetal_Cerrar"));

        float volumenBase = abrir ? 0.78f : 0.84f;
        if (clip == null)
        {
            ReproducirTono(posicion, abrir ? 310f : 220f, 0.14f, 0.3f);
            return;
        }

        if (fuente != null)
            ReproducirEnFuente(fuente, clip, volumenBase, 1f, 1.8f, 16f);
        else
            ReproducirClipEspacial(clip, posicion, volumenBase, 1f, 1f, 1.8f, 16f);
    }

    public static void ReproducirContenedor(Vector3 posicion, bool abrir, AudioSource fuente = null, AudioClip clipOverride = null)
    {
        AudioClip clip = clipOverride != null
            ? clipOverride
            : (abrir
                ? Cargar(ref cajon, "SFX/SFX_CajonMetalico")
                : Cargar(ref puertaMetalCerrar, "Zona2/Doors/SFX_PuertaMetal_Cerrar"));

        float volumenBase = abrir ? 0.78f : 0.70f;
        float pitch = abrir ? 1f : 1.16f;
        if (clip == null)
        {
            ReproducirTono(posicion, abrir ? 380f : 260f, 0.12f, 0.28f);
            return;
        }

        if (fuente != null)
            ReproducirEnFuente(fuente, clip, volumenBase, pitch, 1.6f, 14f);
        else
            ReproducirClipEspacial(clip, posicion, volumenBase, pitch, 1f, 1.6f, 14f);
    }

    public static void ReproducirInterruptor(Vector3 posicion, bool encender, AudioSource fuente = null)
    {
        AudioClip clip = Cargar(ref interruptor, "SFX/SFX_InterruptorEnergia");
        float pitch = encender ? 1.04f : 0.84f;
        if (clip == null)
        {
            ReproducirTono(posicion, encender ? 540f : 360f, 0.10f, 0.3f);
            return;
        }

        if (fuente != null)
            ReproducirEnFuente(fuente, clip, 0.78f, pitch, 1.8f, 16f);
        else
            ReproducirClipEspacial(clip, posicion, 0.78f, pitch, 0.9f, 1.8f, 16f);
    }

    public static void ReproducirAccesoDenegado(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        if (accesoDenegadoClip == null)
        {
            const int sr = 22050;
            const float duracionTotal = 0.27f;
            int muestras = Mathf.CeilToInt(sr * duracionTotal);
            float[] datos = new float[muestras];

            for (int i = 0; i < muestras; i++)
            {
                float t = (float)i / sr;
                float muestra = 0f;
                if (t < 0.10f)
                {
                    float u = t / 0.10f;
                    float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 4f)) * (1f - 0.55f * u);
                    muestra = (Mathf.Sin(2f * Mathf.PI * 220f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 440f * t)) * env * 0.38f;
                }
                else if (t >= 0.13f)
                {
                    float u = (t - 0.13f) / 0.14f;
                    float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 4f)) * (1f - 0.75f * u);
                    muestra = (Mathf.Sin(2f * Mathf.PI * 165f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 330f * t)) * env * 0.40f;
                }
                datos[i] = Mathf.Clamp(muestra, -0.75f, 0.75f);
            }

            accesoDenegadoClip = AudioClip.Create("SFX_Acceso_Denegado", muestras, 1, sr, false);
            accesoDenegadoClip.SetData(datos, 0);
        }

        ReproducirClipEspacial(accesoDenegadoClip, posicion, 0.55f, 1f, 0.45f, 1.8f, 16f);
    }

    public static void ReproducirPanelUI(bool abrir)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        if (UIManager.Instance != null && UIManager.Instance.sonidoClicUI != null)
        {
            UIManager.Instance.ReproducirClicUI(abrir ? 1.08f : 0.88f);
            return;
        }

        float pitch = abrir ? 1.14f : 0.86f;
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.ReproducirFeedbackUI(pitch);
            return;
        }

        AudioClip clip = Cargar(ref boton, "SFX/SFX_BotonPresion");
        if (clip != null)
        {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            ReproducirClipEspacial(clip, pos, 0.36f, pitch, 0f, 1f, 20f);
        }
    }

    private static void ReproducirEnFuente(AudioSource fuente, AudioClip clip, float volumenBase, float pitch, float minDistance, float maxDistance)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (fuente == null || clip == null || factor <= 0.0001f) return;

        if (fuente.isPlaying)
            fuente.Stop();

        fuente.clip = clip;
        fuente.loop = false;
        fuente.playOnAwake = false;
        fuente.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        fuente.spatialBlend = 1f;
        fuente.minDistance = minDistance;
        fuente.maxDistance = maxDistance;
        fuente.dopplerLevel = 0f;

        if (GameSettingsManager.Instance != null)
            GameSettingsManager.Instance.RegistrarFuenteConVolumenBase(fuente, volumenBase);
        else
            fuente.volume = Mathf.Clamp01(volumenBase * factor);

        fuente.Play();
    }

    private static void ReproducirClipEspacial(AudioClip clip, Vector3 posicion, float volumenBase, float pitch = 1f, float spatialBlend = 0.9f, float minDistance = 1.8f, float maxDistance = 18f)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (clip == null || factor <= 0.0001f) return;

        GameObject go = new GameObject("Audio_SFX_Temp");
        go.transform.position = posicion;
        AudioSource fuente = go.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        fuente.spatialBlend = Mathf.Clamp01(spatialBlend);
        fuente.minDistance = minDistance;
        fuente.maxDistance = maxDistance;
        fuente.dopplerLevel = 0f;
        fuente.volume = Mathf.Clamp01(volumenBase * factor);
        fuente.Play();

        Object.Destroy(go, clip.length / Mathf.Max(0.25f, Mathf.Abs(fuente.pitch)) + 0.1f);
    }

    public static void ReproducirAlertaAgente(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        AudioClip clip = Cargar(ref agenteAlerta, "SFX/SFX_AgenteAlerta");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.9f * factor);
        else ReproducirTono(posicion, 520f, 0.18f, 0.35f);
    }

    public static void ReproducirAperturaPuerta(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        AudioClip clip = Cargar(ref puertaDeslizante, "Zona2/Doors/SFX_PuertaDeslizante_47");
        if (clip == null)
        {
            ReproducirTono(posicion, 145f, 0.9f, 0.28f);
            return;
        }

        GameObject sonido = new GameObject("Audio_Puerta_Transicion");
        sonido.transform.position = posicion;
        AudioSource fuente = sonido.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.volume = 0.72f * factor;
        if (GameSettingsManager.Instance != null)
            GameSettingsManager.Instance.RegistrarFuenteConVolumenBase(fuente, 0.72f);
        fuente.spatialBlend = 1f;
        fuente.minDistance = 2f;
        fuente.maxDistance = 24f;
        fuente.Play();
        Object.DontDestroyOnLoad(sonido);
        Object.Destroy(sonido, 1.8f);
    }

    public static void ReproducirLiberacionVapor(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        AudioClip clip = Cargar(ref liberacionVapor, "Zona2/Doors/SFX_Liberacion_Vapor_Airlock");
        if (clip == null) return;

        GameObject sonido = new GameObject("Audio_Liberacion_Vapor_Puerta");
        sonido.transform.position = posicion;
        AudioSource fuente = sonido.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.volume = 0.9f * factor;
        if (GameSettingsManager.Instance != null)
            GameSettingsManager.Instance.RegistrarFuenteConVolumenBase(fuente, 0.9f);
        fuente.pitch = 0.96f;
        fuente.spatialBlend = 1f;
        fuente.minDistance = 2f;
        fuente.maxDistance = 28f;
        fuente.Play();
        Object.DontDestroyOnLoad(sonido);
        Object.Destroy(sonido, 3.2f);
    }

    public static void ReproducirAtaqueAgente(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        AudioClip clip = Cargar(ref agenteAtaque, "SFX/SFX_AgenteAtaque");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.9f * factor);
        else ReproducirTono(posicion, 180f, 0.16f, 0.45f);
    }

    public static void ReproducirTono(Vector3 posicion, float frecuencia, float duracion, float volumen)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        int clave = Mathf.RoundToInt(frecuencia);
        if (!tonos.TryGetValue(clave, out AudioClip clip) || clip == null)
        {
            clip = CrearTono("Tono_Lab_" + clave, frecuencia, duracion);
            tonos[clave] = clip;
        }
        AudioSource.PlayClipAtPoint(clip, posicion, volumen * factor);
    }

    public static AudioClip ObtenerAmbiente()
    {
        if (Cargar(ref ambiente, "Zona2/Ambience/AMB_Fondo_Laboratorio_Z2") != null)
            return ambiente;
        return Cargar(ref ambiente, "Zona2/Ambience/AMB_Sala_Servidores_Loop");
    }

    public static AudioClip ObtenerAlarmaEmergenciaElectricaZ2()
    {
        if (alarmaEmergenciaElectricaZ2 != null)
            return alarmaEmergenciaElectricaZ2;

        const int sr = 22050;
        const float duracion = 4.0f;
        int muestras = Mathf.RoundToInt(sr * duracion);
        float[] datos = new float[muestras];

        float picoMax = 0f;
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sr;
            // 1) Zumbido eléctrico industrial de red (60 Hz + 120 Hz + armónico leve 180 Hz)
            float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.24f
                      + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.13f
                      + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.06f;

            // 2) Baliza de emergencia bitonal cada 2.0 segundos (clara, sin saturar ni resultar estridente)
            float ciclo = t % 2.0f;
            float baliza = 0f;
            if (ciclo < 0.56f)
            {
                float u = ciclo / 0.56f;
                float env = Mathf.Sin(Mathf.PI * u);
                float freq = ciclo < 0.28f ? 420f : 340f;
                baliza = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.40f;
            }

            // 3) Chisporroteo eléctrico intermitente determinista en ciclos de 4s
            float ruido = Mathf.Sin(i * 12.9898f) * 43758.5453f;
            float frac = ruido - Mathf.Floor(ruido);
            bool ventanaChispa = (t > 0.72f && t < 0.86f) || (t > 2.38f && t < 2.55f);
            float microChispa = ventanaChispa ? ((frac * 2f - 1f) * 0.16f) : 0f;

            float muestra = hum + baliza + microChispa;
            datos[i] = muestra;
            float absM = Mathf.Abs(muestra);
            if (absM > picoMax) picoMax = absM;
        }

        if (picoMax > 0.0001f)
        {
            float escala = 0.82f / picoMax;
            for (int i = 0; i < muestras; i++)
                datos[i] = Mathf.Clamp(datos[i] * escala, -0.85f, 0.85f);
        }

        alarmaEmergenciaElectricaZ2 = AudioClip.Create("AMB_Alarma_Emergencia_Z2", muestras, 1, sr, false);
        alarmaEmergenciaElectricaZ2.SetData(datos, 0);
        return alarmaEmergenciaElectricaZ2;
    }

    public static AudioClip ObtenerMaquinaria()
    {
        return ObtenerAmbiente();
    }

    public static void ReproducirDanioJugador(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        if (impactosJugador == null)
        {
            impactosJugador = new[]
            {
                Resources.Load<AudioClip>("Zona2/Player/SFX_Impacto_Corporal_01"),
                Resources.Load<AudioClip>("Zona2/Player/SFX_Impacto_Corporal_02")
            };
        }
        AudioClip clip = impactosJugador[Random.Range(0, impactosJugador.Length)];
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.55f * factor);
    }

    public static void ReproducirDescarga(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        AudioClip clip = Cargar(ref descarga, "Zona2/Electrical/SFX_Descarga_Electrica");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.35f * factor);
    }

    public static void ReproducirSistemaRestaurado(Vector3 posicion)
    {
        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        if (sistemaRestauradoClip == null)
        {
            const int sr = 22050;
            const float duracion = 0.68f;
            int muestras = Mathf.CeilToInt(sr * duracion);
            float[] datos = new float[muestras];
            float[] notas = { 523.25f, 659.25f, 783.99f, 1046.50f };

            for (int i = 0; i < muestras; i++)
            {
                float t = (float)i / sr;
                int idxNota = Mathf.Clamp((int)(t / (duracion / notas.Length)), 0, notas.Length - 1);
                float tLocal = t - idxNota * (duracion / notas.Length);
                float env = Mathf.Clamp01(1f - (tLocal / (duracion / notas.Length)) * 0.65f);
                float f = notas[idxNota];
                float onda = Mathf.Sin(2f * Mathf.PI * f * t) * 0.65f
                           + Mathf.Sin(2f * Mathf.PI * (f * 2f) * t) * 0.22f;
                datos[i] = onda * env * 0.42f;
            }

            sistemaRestauradoClip = AudioClip.Create("SFX_Sistema_Restaurado_Z2", muestras, 1, sr, false);
            sistemaRestauradoClip.SetData(datos, 0);
        }

        AudioSource.PlayClipAtPoint(sistemaRestauradoClip, posicion, 0.88f * factor);

        AudioClip clipTerminal = Cargar(ref terminal, "SFX/SFX_TerminalConsulta");
        if (clipTerminal != null)
            AudioSource.PlayClipAtPoint(clipTerminal, posicion, 0.65f * factor);
    }

    private static AudioClip Cargar(ref AudioClip cache, string ruta)
    {
        if (cache == null)
            cache = Resources.Load<AudioClip>(ruta);
        return cache;
    }

    private static AudioClip CrearTono(string nombre, float frecuencia, float duracion)
    {
        const int frecuenciaMuestreo = 22050;
        int muestras = Mathf.Max(1, Mathf.CeilToInt(frecuenciaMuestreo * duracion));
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / frecuenciaMuestreo;
            float progreso = (float)i / muestras;
            float envolvente = (1f - progreso) * (1f - progreso);
            datos[i] = Mathf.Sin(2f * Mathf.PI * frecuencia * t) * envolvente * 0.35f;
        }

        AudioClip clip = AudioClip.Create(nombre, muestras, 1, frecuenciaMuestreo, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
