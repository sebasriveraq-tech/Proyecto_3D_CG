using System.Collections;
using UnityEngine;

public class Puerta : Interactable
{
    [Header("Condicion")]
    public string idLlave = "credencial_z1";   // item que debe tener el jugador
    public string idPuerta = "puerta_z2";      // id que queda guardado en el GameManager
    public string idCodigoRequerido;

    [Header("Destino")]
    public string escenaDestino = "Zona2_Laboratorio";
    public float duracionApertura = 0.8f;

    [Header("Efectos")]
    public ParticleSystem particulasVapor;

    private bool abierta = false;

    public override void Interactuar()
    {
        if (abierta || GameManager.Instance == null) return;

        // Sin la credencial no pasa nada: solo se avisa.
        if (!GameManager.Instance.TieneItem(idLlave))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Necesitas: " + idLlave.Replace("_", " "));
            LabAudioFeedback.ReproducirAccesoDenegado(transform.position);
            return;
        }

        if (!string.IsNullOrEmpty(idCodigoRequerido) &&
            !GameManager.Instance.TieneCodigo(idCodigoRequerido))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Consulta la terminal para obtener el código de acceso");
            LabAudioFeedback.ReproducirAccesoDenegado(transform.position);
            return;
        }

        LabAudioFeedback.ReproducirAperturaPuerta(transform.position);
        LabAudioFeedback.ReproducirLiberacionVapor(transform.position);
        abierta = true;
        GameManager.Instance.AbrirPuerta(idPuerta);

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarPrompt("Acceso concedido. Abriendo...");

        if (particulasVapor != null)
        {
            ParticleSystem vapor = Instantiate(particulasVapor, transform.position, transform.rotation);
            DontDestroyOnLoad(vapor.gameObject);
            vapor.Play();
            vapor.Emit(140);
            Destroy(vapor.gameObject, 3.2f);
        }

        StartCoroutine(AbrirYCambiar());
    }

    private IEnumerator AbrirYCambiar()
    {
        GameSettingsManager.GarantizarInstancia().IniciarFadeOutMusica(Mathf.Max(0.6f, duracionApertura + 0.45f));
        Vector3 inicio = transform.position;
        Vector3 fin = inicio + Vector3.up * 2.5f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.1f, duracionApertura);
            float suavizado = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(inicio, fin, suavizado);
            yield return null;
        }

        yield return new WaitForSeconds(0.55f);

        GameManager.Instance.CambiarEscena(escenaDestino);
    }
}
