using UnityEngine;
using UnityEngine.Events;

/// Botón de mundo reutilizable: ofrece una señal visual, sonido y un evento configurable.
public class BotonInteractuable : Interactable
{
    [Header("Acción")]
    public UnityEvent alPresionar = new UnityEvent();
    public bool soloUnaVez;
    public AudioSource audioSource;
    public Renderer indicador;
    public Color colorReposo = Color.red;
    public Color colorActivado = Color.green;

    private bool activado;

    private void Awake()
    {
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
        if (indicador != null) indicador.material.color = activado ? colorActivado : colorReposo;
    }
}
