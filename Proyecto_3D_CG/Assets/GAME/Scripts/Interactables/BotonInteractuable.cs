using UnityEngine;
using UnityEngine.Events;

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
    private Transform pulsadorTransform;
    private Vector3 posPulsadorReposo;

    private void Awake()
    {
        pulsadorTransform = transform.Find("Pulsador");
        if (pulsadorTransform != null)
        {
            posPulsadorReposo = pulsadorTransform.localPosition;
            if (indicador == null)
                indicador = pulsadorTransform.GetComponent<Renderer>();
        }
        ActualizarIndicador();
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
    }
}
