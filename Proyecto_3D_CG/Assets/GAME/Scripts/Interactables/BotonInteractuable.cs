using UnityEngine;
using UnityEngine.Events;

/// Botón de mundo reutilizable: ofrece una señal visual integrada con su consola 3D, sonido y evento configurable.
public class BotonInteractuable : Interactable
{
    [Header("Acción")]
    public UnityEvent alPresionar = new UnityEvent();
    public bool soloUnaVez;
    public AudioSource audioSource;
    public Renderer indicador;
    public Color colorReposo = new Color(0.95f, 0.14f, 0.12f);
    public Color colorActivado = new Color(0.12f, 0.95f, 0.42f);

    private bool activado;
    private Light luzEstado;
    private Transform pulsadorTransform;
    private Vector3 posPulsadorReposo;

    private void Awake()
    {
        IntegrarConConsola3D();
        ActualizarIndicador();
    }

    private void IntegrarConConsola3D()
    {
        Transform baseObj = transform.Find("Base");
        Transform pulsadorObj = transform.Find("Pulsador");
        pulsadorTransform = pulsadorObj;

        bool tieneConsola3D = false;
        GameObject consolaExterna = GameObject.Find("Consola_Control_Energia");
        if (consolaExterna != null && consolaExterna.transform.parent != transform)
        {
            consolaExterna.transform.SetParent(transform, true);
        }

        foreach (Transform hijo in transform)
        {
            if (hijo != baseObj && hijo != pulsadorObj && hijo.GetComponentInChildren<Renderer>(true) != null)
            {
                tieneConsola3D = true;
                break;
            }
        }

        if (tieneConsola3D)
        {
            if (baseObj != null)
            {
                baseObj.localPosition = new Vector3(0f, 0.72f, -0.205f);
                baseObj.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                baseObj.localScale = new Vector3(0.18f, 0.028f, 0.18f);
            }

            if (pulsadorObj != null)
            {
                posPulsadorReposo = new Vector3(0f, 0.742f, -0.215f);
                pulsadorObj.localPosition = posPulsadorReposo;
                pulsadorObj.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                pulsadorObj.localScale = new Vector3(0.115f, 0.024f, 0.115f);
            }

            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 0.72f, 0f);
                col.size = new Vector3(0.75f, 1.45f, 0.75f);
            }
        }
        else if (pulsadorObj != null)
        {
            posPulsadorReposo = pulsadorObj.localPosition;
        }

        if (indicador == null && pulsadorObj != null)
            indicador = pulsadorObj.GetComponent<Renderer>();

        Transform luzExistente = transform.Find("Luz_Estado_Boton");
        if (luzExistente == null)
        {
            GameObject goLuz = new GameObject("Luz_Estado_Boton");
            goLuz.transform.SetParent(transform, false);
            goLuz.transform.localPosition = tieneConsola3D
                ? new Vector3(0f, 0.80f, -0.30f)
                : new Vector3(0f, 0.48f, 0f);
            luzEstado = goLuz.AddComponent<Light>();
            luzEstado.type = LightType.Point;
            luzEstado.range = 1.6f;
            luzEstado.intensity = 0.55f;
            luzEstado.shadows = LightShadows.None;
        }
        else
        {
            luzEstado = luzExistente.GetComponent<Light>();
        }
    }

    public override void Interactuar()
    {
        if (soloUnaVez && activado) return;
        activado = true;
        ReproducirSonidoInteraccion(audioSource);
        if (alPresionar != null) alPresionar.Invoke();
        ActualizarIndicador();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Botón activado");
            UIManager.Instance.OcultarPrompt();
        }
    }

    private void ActualizarIndicador()
    {
        Color c = activado ? colorActivado : colorReposo;
        if (indicador != null)
        {
            Material mat = indicador.material;
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.6f);
        }

        if (pulsadorTransform != null && posPulsadorReposo != Vector3.zero)
        {
            pulsadorTransform.localPosition = activado
                ? posPulsadorReposo + new Vector3(0f, -0.008f, 0.004f)
                : posPulsadorReposo;
        }

        if (luzEstado != null)
            luzEstado.color = c;
    }
}
