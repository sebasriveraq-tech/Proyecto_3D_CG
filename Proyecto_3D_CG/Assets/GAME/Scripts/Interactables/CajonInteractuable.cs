using System.Collections;
using UnityEngine;

/// Cajón interactivo que se abre y cierra con una animación sencilla.
public class CajonInteractuable : Interactable
{
    public Transform frente;
    public float distanciaApertura = 0.35f;
    public float duracion = 0.25f;
    public AudioSource audioSource;

    private bool abierto;
    private bool moviendo;
    private Vector3 posicionCerrada;
    private Coroutine animacion;

    private void Awake()
    {
        if (frente != null) posicionCerrada = frente.localPosition;
    }

    public override void Interactuar()
    {
        if (moviendo || frente == null) return;
        abierto = !abierto;
        ReproducirSonidoInteraccion(audioSource);
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(Animar());
    }

    private IEnumerator Animar()
    {
        moviendo = true;
        Vector3 inicio = frente.localPosition;
        Vector3 destino = posicionCerrada + Vector3.forward * (abierto ? distanciaApertura : 0f);
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            frente.localPosition = Vector3.Lerp(inicio, destino, Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }
        frente.localPosition = destino;
        moviendo = false;
        animacion = null;
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(abierto ? "Cajón abierto" : "Cajón cerrado");
    }
}
