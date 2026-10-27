using System.Collections;
using UnityEngine;

/// Puerta: pide un item al GameManager. Si el jugador lo tiene, se abre y carga la siguiente zona.
public class Puerta : Interactable
{
    [Header("Condicion")]
    public string idLlave = "credencial_z1";   // item que debe tener el jugador
    public string idPuerta = "puerta_z2";      // id que queda guardado en el GameManager

    [Header("Destino")]
    public string escenaDestino = "Zona2_Laboratorio";
    public float duracionApertura = 0.8f;

    private bool abierta = false;

    public override void Interactuar()
    {
        if (abierta || GameManager.Instance == null) return;

        // Sin la credencial no pasa nada: solo se avisa.
        if (!GameManager.Instance.TieneItem(idLlave))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Necesitas la credencial Z1");
            return;
        }

        abierta = true;
        GameManager.Instance.AbrirPuerta(idPuerta);

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarPrompt("Acceso concedido. Abriendo...");

        StartCoroutine(AbrirYCambiar());
    }

    // La puerta sube como una compuerta y despues carga la siguiente zona.
    private IEnumerator AbrirYCambiar()
    {
        Vector3 inicio = transform.position;
        Vector3 fin = inicio + Vector3.up * 2.5f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionApertura;
            transform.position = Vector3.Lerp(inicio, fin, t);
            yield return null;
        }

        GameManager.Instance.CambiarEscena(escenaDestino);
    }
}
