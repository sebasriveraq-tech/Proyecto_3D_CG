using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class TocadiscosZ1 : MonoBehaviour
{
    [SerializeField] private AudioClip cancion;
    [SerializeField] private AgentController agente;
    [SerializeField] private MeshFilter bocina;
    [SerializeField] private Transform disco;

    private AudioSource audioTocadiscos;
    private AudioDistortionFilter distorsion;
    private AudioLowPassFilter filtroGraves;
    private PlayerHealth jugador;
    private Mesh mallaBocina;
    private float tension;
    private bool pausado;

    private void OnEnable()
    {
        if (bocina != null && mallaBocina == null)
        {
            mallaBocina = CrearBocina();
            mallaBocina.hideFlags = HideFlags.HideAndDontSave;
            bocina.sharedMesh = mallaBocina;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying || cancion == null) return;

        jugador = FindFirstObjectByType<PlayerHealth>();
        if (agente == null)
            agente = FindFirstObjectByType<AgentController>();
        audioTocadiscos = GetComponent<AudioSource>();
        if (audioTocadiscos == null) audioTocadiscos = gameObject.AddComponent<AudioSource>();
        audioTocadiscos.clip = cancion;
        audioTocadiscos.loop = true;
        audioTocadiscos.volume = 0.54f;
        audioTocadiscos.spatialBlend = 1f;
        audioTocadiscos.dopplerLevel = 0f;
        audioTocadiscos.minDistance = 2f;
        audioTocadiscos.maxDistance = 38f;
        audioTocadiscos.rolloffMode = AudioRolloffMode.Custom;
        audioTocadiscos.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.42f, 0.70f), new Keyframe(1f, 0.47f)));
        if (GameSettingsManager.Instance != null)
            GameSettingsManager.Instance.RegistrarFuenteConVolumenBase(audioTocadiscos, 0.54f);

        distorsion = gameObject.AddComponent<AudioDistortionFilter>();
        distorsion.enabled = false;
        filtroGraves = gameObject.AddComponent<AudioLowPassFilter>();
        filtroGraves.enabled = false;
        audioTocadiscos.Play();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || audioTocadiscos == null) return;
        bool debePausar = Time.timeScale == 0f;
        if (debePausar != pausado)
        {
            pausado = debePausar;
            if (pausado) audioTocadiscos.Pause();
            else audioTocadiscos.UnPause();
        }
        if (pausado) return;

        float objetivo = 0f;
        if (jugador != null && jugador.vidaActual > 0 && agente != null && agente.isActiveAndEnabled)
        {
            switch (agente.EstadoActual)
            {
                case AgentState.Persecucion:
                case AgentState.Ataque:
                    float distancia = Vector3.Distance(jugador.transform.position, agente.transform.position);
                    objetivo = Mathf.Max(0.18f, Mathf.InverseLerp(12f, 1.6f, distancia));
                    break;

                case AgentState.AlertaInvestigacion:
                case AgentState.Retorno:
                    // Conserva el efecto alcanzado hasta que vuelva a patrullar.
                    objetivo = Mathf.Max(0.18f, tension);
                    break;
            }
        }

        float velocidadCambio = objetivo > tension ? 1.8f : 0.72f;
        tension = Mathf.MoveTowards(tension, objetivo, Time.deltaTime * velocidadCambio);
        audioTocadiscos.pitch = Mathf.Lerp(1f, 0.64f, tension);
        distorsion.enabled = tension > 0.01f;
        filtroGraves.enabled = distorsion.enabled;
        if (distorsion.enabled)
        {
            distorsion.distortionLevel = 0.68f * tension;
            filtroGraves.cutoffFrequency = Mathf.Lerp(15000f, 1100f, tension);
        }
        if (disco != null) disco.Rotate(0f, 195f * audioTocadiscos.pitch * Time.deltaTime, 0f, Space.Self);
    }

    private void OnDisable()
    {
        if (audioTocadiscos != null) audioTocadiscos.Stop();
        if (mallaBocina == null) return;
        if (bocina != null) bocina.sharedMesh = null;
        if (Application.isPlaying) Destroy(mallaBocina);
        else DestroyImmediate(mallaBocina);
        mallaBocina = null;
    }

    private static Mesh CrearBocina()
    {
        const int aros = 14;
        const int lados = 40;
        var vertices = new List<Vector3>();
        var caras = new List<int>();
        for (int cara = 0; cara < 2; cara++)
        {
            for (int aro = 0; aro <= aros; aro++)
            {
                float t = (float)aro / aros;
                float radio = 0.046f + 0.30f * Mathf.Pow(t, 2.55f);
                Vector3 centro = new Vector3(0f, 0.44f * t, 0.54f * t * t);
                Vector3 eje = new Vector3(0f, 0.44f, 1.08f * t).normalized;
                Vector3 arriba = Vector3.Cross(eje, Vector3.right);
                for (int lado = 0; lado < lados; lado++)
                {
                    float angulo = 2f * Mathf.PI * lado / lados;
                    float borde = (radio - cara * 0.012f) *
                        (1f + 0.065f * Mathf.Pow(t, 4f) * Mathf.Cos(8f * angulo));
                    vertices.Add(centro + borde * (Vector3.right * Mathf.Cos(angulo) + arriba * Mathf.Sin(angulo)));
                }
            }
        }

        int interior = (aros + 1) * lados;
        for (int cara = 0; cara < 2; cara++)
        {
            for (int aro = 0; aro < aros; aro++)
            {
                for (int lado = 0; lado < lados; lado++)
                {
                    int inicio = cara * interior + aro * lados;
                    int a = inicio + lado;
                    int b = inicio + (lado + 1) % lados;
                    int c = a + lados;
                    int d = b + lados;
                    if (cara == 0) caras.AddRange(new[] { a, b, c, b, d, c });
                    else caras.AddRange(new[] { a, c, b, b, c, d });
                }
            }
        }
        for (int lado = 0; lado < lados; lado++)
        {
            int a = aros * lados + lado;
            int b = aros * lados + (lado + 1) % lados;
            caras.AddRange(new[] { a, a + interior, b, b, a + interior, b + interior });
        }

        Mesh malla = new Mesh { name = "Bocina de latón" };
        malla.SetVertices(vertices);
        malla.SetTriangles(caras, 0);
        malla.RecalculateNormals();
        return malla;
    }
}
