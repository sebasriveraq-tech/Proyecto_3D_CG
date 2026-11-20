using System.Collections;
using UnityEngine;

/// Contenedor / cajón científico interactivo que desliza su bandeja interna
/// y permite descubrir objetos alojados en su interior sin artefactos geométricos.
public class CajonInteractuable : Interactable
{
    public Transform frente;
    public float distanciaApertura = 0.24f;
    public float duracion = 0.28f;
    public AudioSource audioSource;

    private bool abierto;
    private bool moviendo;
    private Vector3 posicionCerrada;
    private Coroutine animacion;
    private BoxCollider colliderContenedor;
    private Recogible recogibleInterno;
    private Collider colliderRecogible;

    private void Awake()
    {
        colliderContenedor = GetComponent<BoxCollider>();
        AdaptarVisualContenedor3D();
        if (frente != null) posicionCerrada = frente.localPosition;
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
            // Transformar el bloque vertical antiguo en una bandeja deslizante horizontal
            // que encaja limpiamente dentro del cofre/gabinete 3D sin atravesar sus paredes.
            frente.localPosition = new Vector3(0f, -0.04f, -0.02f);
            frente.localRotation = Quaternion.identity;
            frente.localScale = new Vector3(0.66f, 0.035f, 0.38f);

            MeshRenderer rFrente = frente.GetComponent<MeshRenderer>();
            if (rFrente != null)
            {
                rFrente.enabled = true;
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    Material matBandeja = new Material(shader)
                    {
                        name = "MAT_Bandeja_Contenedor_Z2",
                        color = new Color(0.14f, 0.18f, 0.22f)
                    };
                    if (matBandeja.HasProperty("_BaseColor"))
                        matBandeja.SetColor("_BaseColor", new Color(0.14f, 0.18f, 0.22f));
                    if (matBandeja.HasProperty("_Metallic"))
                        matBandeja.SetFloat("_Metallic", 0.72f);
                    if (matBandeja.HasProperty("_Smoothness"))
                        matBandeja.SetFloat("_Smoothness", 0.65f);
                    rFrente.sharedMaterial = matBandeja;
                }
            }
        }
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
        Vector3 destino = posicionCerrada + new Vector3(0f, 0f, abierto ? -distanciaApertura : 0f);

        Vector3 posRecogibleInicio = Vector3.zero;
        Vector3 posRecogibleDestino = Vector3.zero;
        if (recogibleInterno != null && recogibleInterno.transform.parent == transform)
        {
            posRecogibleInicio = recogibleInterno.transform.localPosition;
            posRecogibleDestino = posRecogibleInicio + new Vector3(0f, 0f, abierto ? -distanciaApertura * 0.75f : distanciaApertura * 0.75f);
        }

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracion));
            frente.localPosition = Vector3.Lerp(inicio, destino, t);
            if (recogibleInterno != null && recogibleInterno.transform.parent == transform)
                recogibleInterno.transform.localPosition = Vector3.Lerp(posRecogibleInicio, posRecogibleDestino, t);
            yield return null;
        }

        frente.localPosition = destino;
        if (recogibleInterno != null && recogibleInterno.transform.parent == transform)
            recogibleInterno.transform.localPosition = posRecogibleDestino;

        moviendo = false;
        animacion = null;
        ActualizarAccesibilidadObjetoInterno();

        if (UIManager.Instance != null)
        {
            if (abierto && recogibleInterno != null)
                UIManager.Instance.MostrarMensaje("Contenedor abierto: " + recogibleInterno.nombreVisible + " disponible");
            else
                UIManager.Instance.MostrarMensaje(abierto ? "Contenedor abierto" : "Contenedor cerrado");
        }
    }
}
