using UnityEngine;
using UnityEngine.Events;

/// Interruptor persistente mientras permanezca en la escena.
public class InterruptorInteractuable : Interactable
{
    [Header("Estado")]
    public bool encendido;
    public UnityEvent alEncender;
    public UnityEvent alApagar;
    public AudioSource audioSource;
    public Transform palanca;
    public Renderer indicador;
    public Color colorApagado = Color.red;
    public Color colorEncendido = Color.green;

    private void Awake() => ActualizarVisual();

    public override void Interactuar()
    {
        encendido = !encendido;
        if (audioSource != null) audioSource.Play();
        if (encendido) alEncender?.Invoke();
        else alApagar?.Invoke();
        ActualizarVisual();
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(encendido ? "Sistema encendido" : "Sistema apagado");
    }

    private void ActualizarVisual()
    {
        if (palanca != null)
            palanca.localRotation = Quaternion.Euler(encendido ? -35f : 35f, 0f, 0f);
        if (indicador != null)
            indicador.material.color = encendido ? colorEncendido : colorApagado;
    }
}
