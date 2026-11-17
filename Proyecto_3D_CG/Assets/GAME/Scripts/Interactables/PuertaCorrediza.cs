using System.Collections;
using UnityEngine;

/// Puerta corrediza: se abre solo si el jugador tiene el item requerido en el inventario.
public class PuertaCorrediza : Interactable
{
    [Header("Requisito")]
    public string idItemRequerido = "credencial_z1";

    [Header("Movimiento")]
    public Transform hojaPuerta;                              // la hoja que se desliza
    public Vector3 desplazamientoAbierta = new Vector3(-2f, 0f, 0f); // local; ajusta para abrir a la izquierda
    public float tiempoApertura = 0.8f;

    [Header("Audio")]
    public AudioClip sonidoDenegado;

    private Vector3 posCerrada, posAbierta;
    private bool abierta, enMovimiento;

    private void Awake()
    {
        if (hojaPuerta == null) hojaPuerta = transform;
        posCerrada = hojaPuerta.localPosition;
        posAbierta = posCerrada + desplazamientoAbierta;
    }

    public override void Interactuar()
    {
        if (abierta || enMovimiento) return;

        if (!TieneCredencial())
        {
            if (sonidoDenegado != null)
                AudioSource.PlayClipAtPoint(sonidoDenegado, transform.position);

            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Acceso denegado: necesitas una credencial");
            return;
        }

        ReproducirSonidoInteraccion();   // sonido de activación de tu clase base

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Acceso concedido");
            UIManager.Instance.OcultarPrompt();
        }

        abierta = true;
        StartCoroutine(Mover(posCerrada, posAbierta));
    }

    private bool TieneCredencial()
    {
        return GameManager.Instance != null
            && GameManager.Instance.inventario.Contains(idItemRequerido);
    }

    private IEnumerator Mover(Vector3 desde, Vector3 hasta)
    {
        enMovimiento = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / tiempoApertura;
            hojaPuerta.localPosition = Vector3.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        hojaPuerta.localPosition = hasta;
        enMovimiento = false;
    }
}