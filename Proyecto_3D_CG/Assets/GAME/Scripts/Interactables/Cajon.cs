using System.Collections;
using UnityEngine;

/// Cajón/caja: al interactuar abre la tapa (levantándola o girándola) y revela su contenido.
public class Cajon : Interactable
{
    public enum TipoApertura { Levantar, Girar }

    [Header("Tapa")]
    public Transform tapa;                                              // "Bisagra" (vacío padre de Crate_Top)
    public TipoApertura tipo = TipoApertura.Levantar;
    public Vector3 desplazamientoAbierta = new Vector3(0f, 0.6f, 0f);   // modo Levantar (local)
    public Vector3 rotacionAbierta = new Vector3(100f, 0f, 0f);         // modo Girar (grados locales)
    public float tiempoApertura = 0.6f;

    [Header("Contenido (opcional)")]
    public GameObject contenido;                                        // ej. la credencial (Recogible)

    [Header("Collider")]
    public Collider colliderCajon;                                      // Box Collider de la raíz; se apaga al abrir

    private Vector3 posCerrada, posAbierta;
    private Quaternion rotCerrada, rotAbierta;
    private bool abierto, enMovimiento;

    private void Awake()
    {
        if (tapa == null) tapa = transform;

        posCerrada = tapa.localPosition;
        posAbierta = posCerrada + desplazamientoAbierta;
        rotCerrada = tapa.localRotation;
        rotAbierta = rotCerrada * Quaternion.Euler(rotacionAbierta);

        if (contenido != null) contenido.SetActive(false);
    }

    public override void Interactuar()
    {
        if (abierto || enMovimiento) return;

        ReproducirSonidoInteraccion();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Abriste el cajón");
            UIManager.Instance.OcultarPrompt();
        }

        abierto = true;
        StartCoroutine(Abrir());
    }

    private IEnumerator Abrir()
    {
        enMovimiento = true;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / tiempoApertura;
            float k = Mathf.SmoothStep(0f, 1f, t);

            if (tipo == TipoApertura.Levantar)
                tapa.localPosition = Vector3.Lerp(posCerrada, posAbierta, k);
            else
                tapa.localRotation = Quaternion.Slerp(rotCerrada, rotAbierta, k);

            yield return null;
        }

        if (tipo == TipoApertura.Levantar) tapa.localPosition = posAbierta;
        else tapa.localRotation = rotAbierta;

        enMovimiento = false;

        // Deja pasar el raycast hacia lo que hay dentro
        if (colliderCajon != null) colliderCajon.enabled = false;

        if (contenido != null && !ContenidoYaRecogido())
            contenido.SetActive(true);
    }

    // Evita que la credencial reaparezca si el jugador ya la tiene (por ejemplo al cargar partida)
    private bool ContenidoYaRecogido()
    {
        Recogible r = contenido.GetComponent<Recogible>();
        return r != null
            && GameManager.Instance != null
            && GameManager.Instance.inventario.Contains(r.idItem);
    }
}