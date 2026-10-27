using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [Header("Deteccion")]
    public Transform camara;
    public float distancia = 3f;
    public float grosorRayo = 0.35f;   // radio de la esfera del SphereCast (evita exigir punteria perfecta)

    private Interactable objetivoActual;

    private void Start()
    {
        // 1) Si la camara no se asigno en el Inspector, se busca en los hijos del Player.
        if (camara == null)
        {
            Camera camaraHija = GetComponentInChildren<Camera>();
            if (camaraHija != null) camara = camaraHija.transform;
        }

        // 2) Ultimo recurso: la camara marcada como MainCamera.
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        if (camara == null)
            Debug.LogWarning("InteractionController: no hay camara asignada.");
    }

    private void Update()
    {
        DetectarObjeto();

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            objetivoActual != null)
        {
            objetivoActual.Interactuar();
        }
    }

    private void DetectarObjeto()
    {
        Interactable nuevoObjetivo = null;

        if (camara != null)
        {
            // El rayo sale del centro de la camara hacia adelante.
            Ray rayo = new Ray(camara.position, camara.forward);
            RaycastHit golpe;

            // SphereCast: un rayo con grosor. Si la camara apunta cerca del objeto, lo detecta igual.
            if (Physics.SphereCast(rayo, grosorRayo, out golpe, distancia, ~0, QueryTriggerInteraction.Ignore))
                nuevoObjetivo = golpe.collider.GetComponentInParent<Interactable>();
        }

        // Solo se actualiza la interfaz cuando el objetivo cambia.
        if (nuevoObjetivo != objetivoActual)
        {
            objetivoActual = nuevoObjetivo;

            if (UIManager.Instance != null)
            {
                if (objetivoActual != null)
                    UIManager.Instance.MostrarPrompt(objetivoActual.mensajePrompt);
                else
                    UIManager.Instance.OcultarPrompt();
            }
        }
    }

    private void OnDisable()
    {
        // Al cambiar de escena el prompt no debe quedar pegado en pantalla.
        if (UIManager.Instance != null) UIManager.Instance.OcultarPrompt();
    }
}
