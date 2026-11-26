using UnityEngine;


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
    private bool sistemaEstabilizado;

    private void Start()
    {
        if (GameManager.Instance != null && !GameManager.Instance.ObjetivoCompletado("recepcion_iniciada_z1"))
            GameManager.Instance.CompletarObjetivo("recepcion_iniciada_z1");
    }

    private void Update()
    {
        bool reparada = GameManager.Instance != null &&
            (GameManager.Instance.ObjetivoCompletado("terminal_reparada_z1") ||
             GameManager.Instance.TieneCodigo("codigo_a"));

        if (reparada)
        {
            if (!sistemaEstabilizado)
            {
                sistemaEstabilizado = true;
                Color colorEstable = new Color(0.76f, 0.94f, 1f);
                foreach (Light luz in luces)
                {
                    if (luz == null) continue;
                    luz.color = colorEstable;
                    luz.intensity = Mathf.Max(1.35f, intensidadMaxima * 0.9f);
                }

                if (luzDireccional != null)
                {
                    luzDireccional.shadows = LightShadows.None;
                    luzDireccional.intensity = intensidadRelleno + intensidadDireccionalMaxima;
                }

                RenderSettings.ambientLight = new Color(0.20f, 0.24f, 0.27f);
                RenderSettings.reflectionIntensity = 0.35f;
            }
        }
        else
        {
            float onda = (Mathf.Sin(Time.time * pulsosPorSegundo * 2f * Mathf.PI) + 1f) * 0.5f;
            float pulso = Mathf.Clamp01(onda * 1.25f - 0.25f);
            float intensidad = Mathf.Lerp(intensidadMinima, intensidadMaxima, pulso);

            foreach (Light luz in luces)
            {
                if (luz != null) luz.intensity = intensidad;
            }

            if (luzDireccional != null)
            {
                luzDireccional.shadows = LightShadows.None;
                luzDireccional.intensity = intensidadRelleno + intensidadDireccionalMaxima * pulso;
            }

            RenderSettings.ambientLight = luzAmbienteMinima + luzAmbienteMaxima * pulso;
            RenderSettings.reflectionIntensity = 0.25f * pulso;
        }

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
