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
    private static AudioClip puerta;
    private static AudioClip agenteAlerta;
    private static AudioClip agenteAtaque;

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
                clip = Cargar(ref puerta, "SFX/SFX_PuertaLaboratorio");
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
        AudioClip archivo = Cargar(ref ambiente, "SFX/AMB_LaboratorioHum");
        if (archivo != null)
            return archivo;

        if (ambiente != null)
            return ambiente;

        const int frecuenciaMuestreo = 22050;
        const float segundos = 4f;
        int muestras = Mathf.CeilToInt(frecuenciaMuestreo * segundos);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / frecuenciaMuestreo;
            float modulacion = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.18f * t);
            datos[i] = modulacion *
                (0.018f * Mathf.Sin(2f * Mathf.PI * 55f * t) +
                 0.009f * Mathf.Sin(2f * Mathf.PI * 83f * t));
        }

        ambiente = AudioClip.Create("Ambiente_Laboratorio_Runtime", muestras, 1, frecuenciaMuestreo, false);
        ambiente.SetData(datos, 0);
        return ambiente;
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
