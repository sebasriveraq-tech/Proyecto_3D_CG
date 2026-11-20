using System;
using System.Collections;
using UnityEngine;

/// Puerta corrediza: se abre solo si (opcionalmente) hay energía y el jugador tiene la credencial.
public class PuertaCorrediza : Interactable
{
    [Header("Requisitos")]
    public string idItemRequerido = "credencial_z3";   // pon aquí el Id Item de tu credencial
    public bool requiereEnergia = true;
    public Scene3Controller controlador;                // para saber si ya se encendieron las luces

    [Header("Movimiento")]
    public Transform hojaPuerta;                                       // la hoja que se desliza
    public Vector3 desplazamientoAbierta = new Vector3(-2f, 0f, 0f);  // local; ajusta para abrir a la izquierda
    public float tiempoApertura = 0.8f;

    [Header("Audio")]
    public AudioClip sonidoDenegado;

    /// Se dispara cuando la puerta concede el acceso (el Scene3Controller lo escucha).
    public Action AlAbrirse;

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

        // 1) Sin energía: primero hay que encontrar el código y encender las luces
        if (requiereEnergia && controlador != null && !controlador.EnergiaRestaurada)
        {
            Denegar("Primero debes encontrar el código y encender las luces");
            return;
        }

        // 2) Con energía pero sin credencial
        if (!TieneCredencial())
        {
            Denegar("Se necesita la credencial");
            return;
        }

        // 3) Todo en orden: se abre
        ReproducirSonidoInteraccion();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Acceso concedido");
            UIManager.Instance.OcultarPrompt();
        }

        abierta = true;
        StartCoroutine(Mover(posCerrada, posAbierta));
        AlAbrirse?.Invoke();
    }

    private void Denegar(string mensaje)
    {
        if (sonidoDenegado != null)
            AudioSource.PlayClipAtPoint(sonidoDenegado, transform.position);

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarMensaje(mensaje);
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