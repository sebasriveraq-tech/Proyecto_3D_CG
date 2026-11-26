using System;
using System.Collections;
using UnityEngine;

public class PuertaCorrediza : Interactable
{
    [Header("Requisitos")]
    public string idItemRequerido = "credencial_z3";   // pon aquí el Id Item de tu credencial
    public bool requiereEnergia = true;
    [Tooltip("Etapa de código que debe estar completada: 0 = luces/sala, 1 = puertas, 2 = salida final")]
    public int etapaRequerida = 0;
    [Tooltip("Mensaje si no hay energía. Vacío = mensaje por defecto.")]
    public string mensajeSinEnergia = "";
    public Scene3Controller controlador;                // para saber si la etapa ya está completada

    [Header("Movimiento")]
    public Transform hojaPuerta;                                       // la hoja que se desliza
    public Vector3 desplazamientoAbierta = new Vector3(-2f, 0f, 0f);  // local; ajusta para abrir a la izquierda
    public float tiempoApertura = 0.8f;

    [Header("Audio")]
    public AudioClip sonidoDenegado;

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
        if (ArmarioEscondite.JugadorOculto || abierta || enMovimiento) return;

        // 1) Sin energía: falta completar la etapa de código de esta puerta
        if (requiereEnergia && controlador != null && !controlador.EtapaCompletada(etapaRequerida))
        {
            string msg = !string.IsNullOrEmpty(mensajeSinEnergia)
                ? mensajeSinEnergia
                : (etapaRequerida == 0
                    ? "Primero debes encontrar el código y encender las luces"
                    : "Esta puerta no tiene energía. Encuentra el código para restaurarla");
            Denegar(msg);
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
        {
            float factor = GameSettingsManager.FactorEfectos;
            if (factor > 0.0001f)
                AudioSource.PlayClipAtPoint(sonidoDenegado, transform.position, 0.85f * factor);
        }
        else
        {
            LabAudioFeedback.ReproducirAccesoDenegado(transform.position);
        }

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