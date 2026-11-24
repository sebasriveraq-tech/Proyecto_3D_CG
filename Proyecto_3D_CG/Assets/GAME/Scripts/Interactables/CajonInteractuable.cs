using System.Collections;
using UnityEngine;

/// Contenedor / gabinete científico interactivo que coordina la apertura del cofre 3D,
/// el deslizamiento de su bandeja interna y el acceso al objeto contenido.
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
    public float elevacionApertura = 0.08f;

    [Header("Audio")]
    public AudioClip sonidoAbrir;
    public AudioClip sonidoCerrar;

    private bool abierto;
    private bool moviendo;
    private Vector3 posicionCerrada;
    private Vector3 posRecogibleCerrada;
    private Transform huesoTapa;
    private Coroutine animacion;
    private BoxCollider colliderContenedor;
    private Recogible recogibleInterno;
    private Collider colliderRecogible;

    private void Awake()
    {
        colliderContenedor = GetComponent<BoxCollider>();
        AdaptarVisualContenedor3D();
        if (frente != null) posicionCerrada = frente.localPosition;
        InicializarCofreCerrado();
        mensajePrompt = "Presiona E para abrir el gabinete científico";
    }

    private void Start()
    {
        VincularObjetoInterno();
        ActualizarAccesibilidadObjetoInterno();
    }

    private void Update()
    {
        // Si el jugador ya recogió el objeto interno, reactivar el collider del contenedor.
        if (abierto && colliderContenedor != null && !colliderContenedor.enabled && recogibleInterno == null)
        {
            colliderContenedor.enabled = true;
        }
    }

    private void AdaptarVisualContenedor3D()
    {
        Transform gabinetePrimitivo = transform.Find("Gabinete");
        if (gabinetePrimitivo != null)
        {
            MeshRenderer rGab = gabinetePrimitivo.GetComponent<MeshRenderer>();
            if (rGab != null) rGab.enabled = false;
        }

        if (frente == null)
            frente = transform.Find("Frente");

        if (frente != null)
        {
            // Mantener la bandeja deslizante ajustada al interior del cofre 3D.
            frente.localPosition = new Vector3(0f, -0.04f, -0.02f);
            frente.localRotation = Quaternion.identity;
            frente.localScale = new Vector3(0.66f, 0.035f, 0.38f);

            MeshRenderer rFrente = frente.GetComponent<MeshRenderer>();
            if (rFrente != null)
                rFrente.enabled = true;
        }
    }

    private void InicializarCofreCerrado()
    {
        if (animatorCofre == null)
        {
            GameObject gabinete3D = GameObject.Find("Gabinete_Cientifico_Interactivo");
            if (gabinete3D != null)
                animatorCofre = gabinete3D.GetComponent<Animator>();
        }

        if (animatorCofre != null)
            huesoTapa = BuscarDescendientePorNombre(animatorCofre.transform, "Chest_Top");

        if (animatorCofre != null && animatorCofre.runtimeAnimatorController != null)
        {
            animatorCofre.SetBool(ParamAbierto, false);
            animatorCofre.Play(EstadoCerrado, 0, 0f);
            animatorCofre.Update(0f);
        }
        else if (huesoTapa != null)
        {
            huesoTapa.localRotation = Quaternion.identity;
        }
    }

    private static Transform BuscarDescendientePorNombre(Transform raiz, string nombre)
    {
        if (raiz == null) return null;
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == nombre)
                return t;
        }
        return null;
    }

    private void VincularObjetoInterno()
    {
        recogibleInterno = GetComponentInChildren<Recogible>(true);
        if (recogibleInterno == null)
        {
            Recogible[] todos = FindObjectsByType<Recogible>(FindObjectsSortMode.None);
            float mejorDist = 0.95f;
            foreach (Recogible r in todos)
            {
                if (r == null) continue;
                float d = Vector3.Distance(transform.position, r.transform.position);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    recogibleInterno = r;
                }
            }
        }

        if (recogibleInterno != null)
        {
            colliderRecogible = recogibleInterno.GetComponent<Collider>();
            recogibleInterno.transform.SetParent(transform, true);
            posRecogibleCerrada = recogibleInterno.transform.localPosition;
        }
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

        // Cuando el contenedor está abierto y aún contiene el objeto, cedemos prioridad
        // al collider del objeto para que el jugador pueda recogerlo directamente con E.
        if (colliderContenedor != null)
            colliderContenedor.enabled = !abierto;
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

        bool usarRotacionManualTapa =
            huesoTapa != null && (animatorCofre == null || animatorCofre.runtimeAnimatorController == null);
        Quaternion rotTapaInicio = usarRotacionManualTapa ? huesoTapa.localRotation : Quaternion.identity;
        Quaternion rotTapaDestino = abierto ? Quaternion.Euler(-122.37f, 0f, 0f) : Quaternion.identity;

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
            if (usarRotacionManualTapa)
                huesoTapa.localRotation = Quaternion.Slerp(rotTapaInicio, rotTapaDestino, t);
            yield return null;
        }

        frente.localPosition = destino;
        if (moverRecogible && recogibleInterno != null)
            recogibleInterno.transform.localPosition = posRecogibleDestino;
        if (usarRotacionManualTapa)
            huesoTapa.localRotation = rotTapaDestino;

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
