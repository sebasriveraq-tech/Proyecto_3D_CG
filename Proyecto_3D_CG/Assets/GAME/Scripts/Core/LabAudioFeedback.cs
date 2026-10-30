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

    public static void ReproducirInteraccion(Vector3 posicion)
    {
        ReproducirTono(posicion, 740f, 0.09f, 0.3f);
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
