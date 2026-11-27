
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class AgitacionPorAgenteEtapa1 : MonoBehaviour
{
    private const float DistanciaInicio = 8f;
    private const float DistanciaMaximaAgitacion = 2f;

    private readonly Image[] franjas = new Image[12];

    private AgentController[] agentesOriginales =
        System.Array.Empty<AgentController>();

    private AgentControllerEtapa1[] agentesEtapa1 =
        System.Array.Empty<AgentControllerEtapa1>();

    private Camera camara;
    private UniversalAdditionalCameraData ajusteCamara;
    private Volume volumenVista;
    private VolumeProfile perfilVista;
    private Vignette vineta;
    private ChromaticAberration aberracion;
    private FilmGrain grano;

    private LayerMask mascaraOriginal;
    private bool postprocesadoOriginal;
    private bool efectoVistaActivo;

    private PlayerHealth salud;
    private float proximaBusqueda;
    private float intensidad;
    private Quaternion rotacionNormal;
    private bool camaraAgitada;

    public void Inicializar(Transform vista)
    {
        if (vista == null)
        {
            Debug.LogWarning("No se asignó la cámara del jugador.");
            return;
        }

        camara = vista.GetComponent<Camera>();
        salud = GetComponent<PlayerHealth>();

        PrepararEfectoVista();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += AgitarCamara;
        RenderPipelineManager.endCameraRendering += RestaurarCamara;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= AgitarCamara;
        RenderPipelineManager.endCameraRendering -= RestaurarCamara;

        RecuperarRotacion();
        ColorearFranjas(0f);
        ActualizarEfectoVista(0f);
    }

    private void LateUpdate()
    {
        GameObject hud = UIManager.Instance != null
            ? UIManager.Instance.panelHUD
            : null;

        bool jugando = Time.timeScale > 0f
            && (salud == null || salud.vidaActual > 0);

        if (jugando && Time.time >= proximaBusqueda)
        {
            agentesOriginales = FindObjectsByType<AgentController>(
    FindObjectsSortMode.None
);

            agentesEtapa1 = FindObjectsByType<AgentControllerEtapa1>(
                FindObjectsSortMode.None
            );

            proximaBusqueda = Time.time + 0.4f;
        }

        float objetivo = 0f;

        if (jugando)
        {
            foreach (AgentController agente in agentesOriginales)
            {
                if (agente == null || !agente.isActiveAndEnabled)
                    continue;

                float distancia = Vector3.Distance(
                    transform.position,
                    agente.transform.position
                );

                objetivo = Mathf.Max(
                    objetivo,
                    Mathf.InverseLerp(
                        DistanciaInicio,
                        DistanciaMaximaAgitacion,
                        distancia
                    )
                );
            }

            foreach (AgentControllerEtapa1 agente in agentesEtapa1)
            {
                if (agente == null ||
                    !agente.isActiveAndEnabled ||
                    !agente.EstaActivado)
                    continue;

                float distancia = Vector3.Distance(
                    transform.position,
                    agente.transform.position
                );

                objetivo = Mathf.Max(
                    objetivo,
                    Mathf.InverseLerp(
                        DistanciaInicio,
                        DistanciaMaximaAgitacion,
                        distancia
                    )
                );
            }
        }

        intensidad = jugando
            ? Mathf.MoveTowards(
                intensidad,
                objetivo,
                Time.deltaTime * 3f
            )
            : 0f;

        if (hud != null && franjas[0] == null)
            CrearFranjas(hud.transform);

        float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * 10f);

        ColorearFranjas(0.42f * intensidad * pulso);
        ActualizarEfectoVista(intensidad);
    }

    private void PrepararEfectoVista()
    {
        if (camara == null) return;

        int capa = LayerMask.NameToLayer("EfectoCamaraJugador");
        if (capa < 0)
            capa = 0;

        ajusteCamara = camara.GetUniversalAdditionalCameraData();

        GameObject objetoVolumen = new GameObject("Efecto de proximidad");
        objetoVolumen.layer = capa;
        objetoVolumen.transform.SetParent(transform, false);

        volumenVista = objetoVolumen.AddComponent<Volume>();
        volumenVista.isGlobal = true;
        volumenVista.priority = 100f;
        volumenVista.weight = 0f;

        perfilVista = ScriptableObject.CreateInstance<VolumeProfile>();

        vineta = perfilVista.Add<Vignette>();
        vineta.color.Override(Color.black);
        vineta.smoothness.Override(0.48f);

        aberracion = perfilVista.Add<ChromaticAberration>();
        grano = perfilVista.Add<FilmGrain>();

        volumenVista.sharedProfile = perfilVista;
    }

    private void ActualizarEfectoVista(float cercania)
    {
        if (volumenVista == null || ajusteCamara == null)
            return;

        if (cercania > 0f)
        {
            if (!efectoVistaActivo)
            {
                mascaraOriginal = ajusteCamara.volumeLayerMask;
                postprocesadoOriginal = ajusteCamara.renderPostProcessing;

                int mascaraEfecto = 1 << volumenVista.gameObject.layer;

                ajusteCamara.volumeLayerMask = postprocesadoOriginal
                    ? mascaraOriginal.value | mascaraEfecto
                    : mascaraEfecto;

                ajusteCamara.renderPostProcessing = true;
                efectoVistaActivo = true;
            }

            volumenVista.weight = 1f;

            vineta.intensity.Override(0.94f * cercania);
            aberracion.intensity.Override(0.38f * cercania);
            grano.intensity.Override(0.22f * cercania);
        }
        else if (efectoVistaActivo)
        {
            volumenVista.weight = 0f;

            ajusteCamara.volumeLayerMask = mascaraOriginal;
            ajusteCamara.renderPostProcessing = postprocesadoOriginal;

            efectoVistaActivo = false;
        }
    }

    private void OnDestroy()
    {
        if (volumenVista != null)
            Destroy(volumenVista.gameObject);

        if (perfilVista == null)
            return;

        foreach (VolumeComponent componente in perfilVista.components)
            Destroy(componente);

        Destroy(perfilVista);
    }

    private void AgitarCamara(
        ScriptableRenderContext contexto,
        Camera vista
    )
    {
        if (vista != camara || intensidad <= 0f || camaraAgitada)
            return;

        rotacionNormal = vista.transform.localRotation;

        float tiempo = Time.time;
        float amplitud = intensidad * intensidad;

        vista.transform.localRotation = rotacionNormal * Quaternion.Euler(
            Mathf.Sin(tiempo * 47f) * 1.1f * amplitud,
            Mathf.Sin(tiempo * 39f) * 0.85f * amplitud,
            Mathf.Sin(tiempo * 23f) * 1.8f * amplitud
        );

        camaraAgitada = true;
    }

    private void RestaurarCamara(
        ScriptableRenderContext contexto,
        Camera vista
    )
    {
        if (vista == camara)
            RecuperarRotacion();
    }

    private void RecuperarRotacion()
    {
        if (camaraAgitada && camara != null)
            camara.transform.localRotation = rotacionNormal;

        camaraAgitada = false;
    }

    private void CrearFranjas(Transform hud)
    {
        for (int esquina = 0; esquina < 4; esquina++)
        {
            bool derecha = esquina % 2 == 1;
            bool arriba = esquina >= 2;

            Vector2 ancla = new Vector2(
                derecha ? 1f : 0f,
                arriba ? 1f : 0f
            );

            for (int indice = 0; indice < 3; indice++)
            {
                GameObject objeto = new GameObject(
                    "Franja de agitación",
                    typeof(RectTransform),
                    typeof(Image)
                );

                objeto.transform.SetParent(hud, false);
                objeto.transform.SetAsFirstSibling();

                Image franja = objeto.GetComponent<Image>();
                franja.raycastTarget = false;

                RectTransform rect = franja.rectTransform;

                rect.anchorMin = ancla;
                rect.anchorMax = ancla;

                rect.sizeDelta = new Vector2(
                    122f - indice * 18f,
                    7f - indice
                );

                rect.anchoredPosition = new Vector2(
                    (derecha ? -1f : 1f) * (24f + indice * 17f),
                    (arriba ? -1f : 1f) * (31f + indice * 16f)
                );

                rect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    derecha == arriba ? -38f : 38f
                );

                franjas[esquina * 3 + indice] = franja;
            }
        }
    }

    private void ColorearFranjas(float opacidad)
    {
        for (int i = 0; i < franjas.Length; i++)
        {
            if (franjas[i] != null)
            {
                franjas[i].color = new Color(
                    0.88f,
                    0.035f,
                    0.055f,
                    opacidad * (1f - i % 3 * 0.22f)
                );
            }
        }
    }
}
