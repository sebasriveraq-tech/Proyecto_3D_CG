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
    public float intensidadDireccionalMaxima = 0.08f;
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

        // La luz blanca y el ambiente tambien bajan, sin tocar otras luces de la escena.
        if (luzDireccional != null) luzDireccional.intensity = intensidadDireccionalMaxima * pulso;
        RenderSettings.ambientLight = luzAmbienteMaxima * pulso;
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
