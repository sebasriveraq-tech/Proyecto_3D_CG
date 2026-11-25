using System.Collections;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class MusicaAmbienteZona3 : MonoBehaviour
{
    [Header("Música")]
    public AudioClip musica;
    [Range(0f, 1f)] public float volumenNormal = 0.6f;
    [Tooltip("Volumen cuando ya están activos los enemigos")]
    [Range(0f, 1f)] public float volumenReducido = 0.25f;

    [Header("Transiciones (segundos)")]
    public float duracionCambioVolumen = 1.5f;
    public float duracionFundidoFinal = 2f;

    private AudioSource fuente;
    private Coroutine rutinaVolumen;
    private bool finalizada;

    private void Awake()
    {
        fuente = GetComponent<AudioSource>();
        if (musica != null) fuente.clip = musica;

        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;          // música que se oye igual en toda la sala
        fuente.volume = volumenNormal;
    }

    private void Start()
    {
        if (fuente.clip != null)
            fuente.Play();
        else
            Debug.LogWarning("MusicaAmbienteZona3: falta asignar la música.");
    }

    private void Update()
    {
        if (finalizada) return;

        // Cuando se abre el panel de victoria, la música se apaga
        if (UIManager.Instance != null
            && UIManager.Instance.panelVictoria != null
            && UIManager.Instance.panelVictoria.activeInHierarchy)
        {
            Finalizar();
        }
    }

    
    public void BajarVolumen()
    {
        if (finalizada) return;
        CambiarVolumen(volumenReducido, duracionCambioVolumen, false);
    }

    
    public void SubirVolumen()
    {
        if (finalizada) return;
        CambiarVolumen(volumenNormal, duracionCambioVolumen, false);
    }

    private void Finalizar()
    {
        finalizada = true;
        CambiarVolumen(0f, duracionFundidoFinal, true);
    }

    private void CambiarVolumen(float destino, float duracion, bool detenerAlFinal)
    {
        if (rutinaVolumen != null) StopCoroutine(rutinaVolumen);
        rutinaVolumen = StartCoroutine(Fundir(destino, duracion, detenerAlFinal));
    }

    // Usa tiempo sin escala: el panel de victoria congela el juego (Time.timeScale = 0)
    private IEnumerator Fundir(float destino, float duracion, bool detenerAlFinal)
    {
        float inicio = fuente.volume;
        float total = Mathf.Max(0.01f, duracion);
        float t = 0f;

        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(inicio, destino, t / total);
            yield return null;
        }

        fuente.volume = destino;
        if (detenerAlFinal) fuente.Stop();
        rutinaVolumen = null;
    }
}