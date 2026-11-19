using UnityEngine;
using UnityEngine.InputSystem;

/// PlayerVisual: controla lo visual del jugador.
/// 1) Pasa la velocidad real del CharacterController al Animator para que elija
///    entre Idle, Caminar y Correr (y Salto cuando no toca el suelo).
/// 2) Primera persona CON cuerpo: el cuerpo del personaje se ve, pero el hueso
///    de la cabeza se escala a ~0 para que la camara (que sigue ese hueso)
///    quede "en la cara" sin ver el interior de la cabeza.
/// 3) Con la tecla V se pasa a tercera persona: cabeza normal y camara detras.
///    Un rayo hacia atras evita que la camara atraviese paredes.
/// 4) Oculta la capsula primitiva de "Player": el cuerpo real es el modelo.
public class PlayerVisual : MonoBehaviour
{
    private CharacterController controlador;
    private Animator animator;       // Animator dentro del modelo animado
    private Transform camara;        // camara del jugador
    private GameObject modelo;       // cuerpo del personaje (Mixamo)
    private Transform cabeza;        // hueso de la cabeza dentro del modelo

    private bool terceraPersona;
    private readonly Vector3 posicionCamaraTercera = new Vector3(0f, 1.7f, -3.2f);

    public void Configurar(CharacterController controlador, GameObject modelo)
    {
        this.controlador = controlador;
        this.modelo = modelo;
        animator = modelo != null ? modelo.GetComponentInChildren<Animator>() : null;

        // 1) Ocultar la capsula primitiva de una vez: el cuerpo real es el modelo.
        //    GetComponents (sin "In Children") para no tocar los renderers del modelo.
        foreach (MeshRenderer r in GetComponents<MeshRenderer>())
            r.enabled = false;

        // 2) Buscar la camara (mismo criterio que InteractionController).
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null && Camera.main != null) cam = Camera.main;
        camara = cam != null ? cam.transform : null;

        // 3) Buscar el HUESO de la cabeza. En los rigs de Mixamo se llama
        //    "mixamorig:Head". Cuidado: tambien hay mallas con nombre parecido
        //    (EXO_HeadMask); los huesos NO tienen Renderer, por eso se filtra asi.
        if (modelo != null)
        {
            foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("head") && t.GetComponent<Renderer>() == null)
                {
                    cabeza = t;
                    break;
                }
            }
        }

        // El juego arranca en primera persona: la cabeza se encoge desde el inicio.
        if (cabeza != null)
            cabeza.localScale = Vector3.one * 0.001f;

        // Si no se encontro el hueso de la cabeza, en primera persona se oculta
        // todo el modelo (fallback: asi la camara no ve el interior del cuerpo).
        if (cabeza == null && modelo != null)
            modelo.SetActive(false);
    }

    private void Update()
    {
        PasarVelocidadAlAnimator();
        CambiarCamaraConTeclaV();
    }

    // LateUpdate corre despues del Update de PlayerController: la posicion de la
    // camara queda fija en este frame y no "tiembla" con el mouse.
    private void LateUpdate()
    {
        if (terceraPersona) MantenerCamaraDetras();
        else MantenerCamaraEnLaCara();
    }

    // Primera persona: la camara va en la cara, siguiendo el hueso de la cabeza.
    // Como el hueso esta escalado a ~0, la cabeza no se dibuja y no se ve su interior.
    private void MantenerCamaraEnLaCara()
    {
        if (cabeza == null || camara == null) return;
        camara.localPosition = transform.InverseTransformPoint(cabeza.position);
    }

    // Tercera persona: camara detras del jugador mirando su espalda.
    // Un rayo desde la cabeza hacia atras evita que la camara atraviese paredes:
    // si algo la bloquea, se acerca hasta justo antes del golpe.
    private void MantenerCamaraDetras()
    {
        if (camara == null) return;
        Vector3 origen = transform.position + Vector3.up * 1.6f;
        Vector3 detras = transform.position + transform.rotation * posicionCamaraTercera;
        Vector3 direccion = detras - origen;
        float distanciaTotal = direccion.magnitude;
        direccion.Normalize();

        // El rayo empieza 0.5m fuera del centro para no golpear al propio jugador.
        float distancia = distanciaTotal;
        if (Physics.Raycast(origen + direccion * 0.5f, direccion, out RaycastHit golpe,
                            distanciaTotal - 0.5f))
            distancia = Mathf.Max(golpe.distance + 0.5f - 0.2f, 0.4f);

        camara.position = origen + direccion * distancia;
        Vector3 mirar = origen - camara.position;
        if (mirar.sqrMagnitude > 0.001f)
            camara.rotation = Quaternion.LookRotation(mirar.normalized, Vector3.up);
    }

    private void PasarVelocidadAlAnimator()
    {
        if (animator == null || controlador == null) return;
        Vector3 velocidadPlana = controlador.velocity;
        velocidadPlana.y = 0f;
        animator.SetFloat("Velocidad", velocidadPlana.magnitude);
        animator.SetBool("EnElAire", !controlador.isGrounded);
    }

    private void CambiarCamaraConTeclaV()
    {
        if (PanelTerminalZ1.Abierto) return;
        if (Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame) return;

        if (cabeza != null)
        {
            // Con hueso de cabeza: primera persona = cabeza encogida (invisible),
            // tercera persona = cabeza normal. El modelo siempre esta activo.
            terceraPersona = !terceraPersona;
            cabeza.localScale = terceraPersona ? Vector3.one : Vector3.one * 0.001f;
        }
        else if (modelo != null && camara != null)
        {
            // Sin hueso: primera persona oculta todo el modelo.
            terceraPersona = !terceraPersona;
            modelo.SetActive(terceraPersona);
        }
    }
}
