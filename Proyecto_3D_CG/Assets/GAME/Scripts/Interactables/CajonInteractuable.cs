using System.Collections;
using UnityEngine;

public class CajonInteractuable : Interactable
{
    private const string ParamAbierto = "Abierto";
    private const string EstadoCerrado = "Idle_Closed";

    public Transform frente;
    public float distanciaApertura = 0.10f;
    public float duracion = 0.63f;
    public AudioSource audioSource;

    [Header("Animación del cofre")]
    public Animator animatorCofre;
    public Recogible recogibleInterno;
    public float elevacionApertura = 0.08f;

    [Header("Audio")]
    public AudioClip sonidoAbrir;
    public AudioClip sonidoCerrar;

    private bool abierto;
    private bool moviendo;
    private Vector3 posicionCerrada;
    private Vector3 posRecogibleCerrada;
    private Coroutine animacion;
    private BoxCollider colliderContenedor;
    private Collider colliderRecogible;

    private void Awake()
    {
        colliderContenedor = GetComponent<BoxCollider>();
        if (frente != null)
            posicionCerrada = frente.localPosition;

        if (animatorCofre != null && animatorCofre.runtimeAnimatorController != null)
        {
            animatorCofre.SetBool(ParamAbierto, false);
            animatorCofre.Play(EstadoCerrado, 0, 0f);
            animatorCofre.Update(0f);
        }
        mensajePrompt = "Presiona E para abrir el gabinete científico";
    }

    private void Start()
    {
        if (recogibleInterno == null)
            recogibleInterno = GetComponentInChildren<Recogible>(true);

        if (recogibleInterno != null)
        {
            colliderRecogible = recogibleInterno.GetComponent<Collider>();
            recogibleInterno.transform.SetParent(transform, true);
            posRecogibleCerrada = recogibleInterno.transform.localPosition;
        }
        ActualizarAccesibilidadObjetoInterno();
    }

    private void Update()
    {
        if (abierto && colliderContenedor != null && !colliderContenedor.enabled && recogibleInterno == null)
            colliderContenedor.enabled = true;
    }

    private void ActualizarAccesibilidadObjetoInterno()
    {
        if (recogibleInterno == null)
        {
            if (colliderContenedor != null) colliderContenedor.enabled = true;
            return;
        }

        if (colliderRecogible != null)
            colliderRecogible.enabled = abierto;

        if (colliderContenedor != null)
            colliderContenedor.enabled = !abierto;
    }

    public override bool PuedeInteractuar(Transform origenJugador)
    {
        return !ArmarioEscondite.JugadorOculto && !moviendo && !(abierto && recogibleInterno != null);
    }

    public override void Interactuar()
    {
        if (ArmarioEscondite.JugadorOculto) return;
        if (moviendo || frente == null) return;

        abierto = !abierto;
        mensajePrompt = abierto
            ? "Presiona E para cerrar el gabinete científico"
            : "Presiona E para abrir el gabinete científico";

        if (animatorCofre != null && animatorCofre.runtimeAnimatorController != null)
            animatorCofre.SetBool(ParamAbierto, abierto);

        LabAudioFeedback.ReproducirContenedor(
            transform.position,
            abierto,
            audioSource,
            abierto ? sonidoAbrir : sonidoCerrar);

        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(Animar());
    }

    private IEnumerator Animar()
    {
        moviendo = true;
        if (!abierto && colliderRecogible != null)
            colliderRecogible.enabled = false;

        Vector3 inicio = frente.localPosition;
        Vector3 destino = posicionCerrada + (abierto
            ? new Vector3(0f, elevacionApertura, -distanciaApertura)
            : Vector3.zero);

        Vector3 posRecogibleInicio = Vector3.zero;
        Vector3 posRecogibleDestino = Vector3.zero;
        bool moverRecogible = recogibleInterno != null && recogibleInterno.transform.parent == transform;
        if (moverRecogible)
        {
            posRecogibleInicio = recogibleInterno.transform.localPosition;
            posRecogibleDestino = posRecogibleCerrada + (abierto
                ? new Vector3(0f, elevacionApertura, -distanciaApertura * 0.85f)
                : Vector3.zero);
        }

        float duracionAnim = abierto
            ? Mathf.Max(0.05f, duracion)
            : Mathf.Max(0.05f, Mathf.Min(duracion, 0.47f));

        float tiempo = 0f;
        while (tiempo < duracionAnim)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracionAnim));
            frente.localPosition = Vector3.Lerp(inicio, destino, t);
            if (moverRecogible && recogibleInterno != null)
                recogibleInterno.transform.localPosition = Vector3.Lerp(posRecogibleInicio, posRecogibleDestino, t);
            yield return null;
        }

        frente.localPosition = destino;
        if (moverRecogible && recogibleInterno != null)
            recogibleInterno.transform.localPosition = posRecogibleDestino;

        moviendo = false;
        animacion = null;
        ActualizarAccesibilidadObjetoInterno();

        if (UIManager.Instance != null)
        {
            if (abierto && recogibleInterno != null)
                UIManager.Instance.MostrarMensaje("Gabinete abierto: " + recogibleInterno.nombreVisible + " disponible");
            else
                UIManager.Instance.MostrarMensaje(abierto ? "Gabinete abierto" : "Gabinete cerrado");
        }
    }
}
