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
    private static AudioClip liberacionVapor;
    private static AudioClip agenteAlerta;
    private static AudioClip agenteAtaque;
    private static AudioClip[] impactosJugador;
    private static AudioClip descarga;
    private static AudioClip sistemaRestauradoClip;
    private static AudioClip alarmaEmergenciaElectricaZ2;

    public static void ReproducirInteraccion(Vector3 posicion, string tipo)
    {
        AudioClip clip;
        switch (tipo)
        {
            case "Recogible_Credencial":
            case "Tarjeta":
                clip = Cargar(ref recoger, "SFX/SFX_RecogerCredencial");
                break;
            case "Terminal_Registro":
                clip = Cargar(ref terminal, "SFX/SFX_TerminalConsulta");
                break;
            case "Boton":
                clip = Cargar(ref boton, "SFX/SFX_BotonPresion");
                break;
            case "Interruptor":
                clip = Cargar(ref interruptor, "SFX/SFX_InterruptorEnergia");
                break;
            case "Cajon":
                clip = Cargar(ref cajon, "SFX/SFX_CajonMetalico");
                break;
            case "Puerta_Zona2":
                clip = Cargar(ref puertaDeslizante, "Zona2/Doors/SFX_PuertaDeslizante_47");
                break;
            default:
                clip = Cargar(ref boton, "SFX/SFX_BotonPresion");
                break;
        }

        float factor = GameSettingsManager.FactorEfectos;
        if (factor <= 0.0001f) return;

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, posicion, 0.8f * factor);
        else
            ReproducirTono(posicion, 740f, 0.09f, 0.3f);
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
