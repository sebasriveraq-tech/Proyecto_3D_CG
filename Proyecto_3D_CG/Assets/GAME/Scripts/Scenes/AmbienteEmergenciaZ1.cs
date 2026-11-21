using UnityEngine;

/// <summary>
/// Hace pulsar las luces de emergencia y pausa la alarma cuando se pausa el juego.
/// Este componente se utiliza solamente en la Zona 1.
/// </summary>
public class AmbienteEmergenciaZ1 : MonoBehaviour
{
    public Light[] luces;
    public AudioSource alarma;

    [Header("Luces de emergencia")]
    public float intensidadMinima = 0f;
    public float intensidadMaxima = 1.6f;
    public float pulsosPorSegundo = 0.65f;
    public Light luzDireccional;
    [Min(0f)] public float intensidadRelleno = 0.24f;
    public float intensidadDireccionalMaxima = 0.08f;
    public Color luzAmbienteMinima = new Color(0.11f, 0.11f, 0.11f);
    public Color luzAmbienteMaxima = new Color(0.12f, 0.025f, 0.02f);

    private bool alarmaPausada;

    private void Update()
    {
        float onda = (Mathf.Sin(Time.time * pulsosPorSegundo * 2f * Mathf.PI) + 1f) * 0.5f;
        // Mantiene un instante de apagado total antes de volver a encender.
        float pulso = Mathf.Clamp01(onda * 1.25f - 0.25f);
        float intensidad = Mathf.Lerp(intensidadMinima, intensidadMaxima, pulso);

        foreach (Light luz in luces)
        {
            if (luz != null) luz.intensity = intensidad;
        }

        if (luzDireccional != null)
        {
            // El techo no debe bloquear la luz de relleno de Recepción.
            luzDireccional.shadows = LightShadows.None;
            luzDireccional.intensity = intensidadRelleno + intensidadDireccionalMaxima * pulso;
        }

        // El pulso suma un tono rojo a la luz ambiental base.
        RenderSettings.ambientLight = luzAmbienteMinima + luzAmbienteMaxima * pulso;
        RenderSettings.reflectionIntensity = 0.25f * pulso;

        if (alarma == null) return;

        if (Time.timeScale == 0f && !alarmaPausada)
        {
            alarma.Pause();
            alarmaPausada = true;
        }
        else if (Time.timeScale > 0f && alarmaPausada)
        {
            alarma.UnPause();
            alarmaPausada = false;
        }
    }
}
