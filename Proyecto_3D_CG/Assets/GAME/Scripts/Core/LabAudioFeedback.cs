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

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, posicion, 0.8f);
        else
            ReproducirTono(posicion, 740f, 0.09f, 0.3f);
    }

    public static void ReproducirAlertaAgente(Vector3 posicion)
    {
        AudioClip clip = Cargar(ref agenteAlerta, "SFX/SFX_AgenteAlerta");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.9f);
        else ReproducirTono(posicion, 520f, 0.18f, 0.35f);
    }

    public static void ReproducirAperturaPuerta(Vector3 posicion)
    {
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
        fuente.volume = 0.72f;
        fuente.spatialBlend = 1f;
        fuente.minDistance = 2f;
        fuente.maxDistance = 24f;
        fuente.Play();
        Object.DontDestroyOnLoad(sonido);
        Object.Destroy(sonido, 1.8f);
    }

    public static void ReproducirLiberacionVapor(Vector3 posicion)
    {
        AudioClip clip = Cargar(ref liberacionVapor, "Zona2/Doors/SFX_Liberacion_Vapor_Airlock");
        if (clip == null) return;

        GameObject sonido = new GameObject("Audio_Liberacion_Vapor_Puerta");
        sonido.transform.position = posicion;
        AudioSource fuente = sonido.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.volume = 0.9f;
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
        AudioClip clip = Cargar(ref agenteAtaque, "SFX/SFX_AgenteAtaque");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.9f);
        else ReproducirTono(posicion, 180f, 0.16f, 0.45f);
    }

    public static void ReproducirTono(Vector3 posicion, float frecuencia, float duracion, float volumen)
    {
        int clave = Mathf.RoundToInt(frecuencia);
        if (!tonos.TryGetValue(clave, out AudioClip clip) || clip == null)
        {
            clip = CrearTono("Tono_Lab_" + clave, frecuencia, duracion);
            tonos[clave] = clip;
        }
        AudioSource.PlayClipAtPoint(clip, posicion, volumen);
    }

    public static AudioClip ObtenerAmbiente()
    {
        return Cargar(ref ambiente, "Zona2/Ambience/AMB_Sala_Servidores_Loop");
    }

    public static AudioClip ObtenerMaquinaria()
    {
        return ObtenerAmbiente();
    }

    public static void ReproducirDanioJugador(Vector3 posicion)
    {
        if (impactosJugador == null)
        {
            impactosJugador = new[]
            {
                Resources.Load<AudioClip>("Zona2/Player/SFX_Impacto_Corporal_01"),
                Resources.Load<AudioClip>("Zona2/Player/SFX_Impacto_Corporal_02")
            };
        }
        AudioClip clip = impactosJugador[Random.Range(0, impactosJugador.Length)];
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.55f);
    }

    public static void ReproducirDescarga(Vector3 posicion)
    {
        AudioClip clip = Cargar(ref descarga, "Zona2/Electrical/SFX_Descarga_Electrica");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, posicion, 0.35f);
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
