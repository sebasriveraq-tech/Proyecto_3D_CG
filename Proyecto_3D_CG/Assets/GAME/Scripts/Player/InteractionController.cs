using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [Header("Deteccion")]
    public Transform camara;
    public float distancia = 3f;
    public float grosorRayo = 0.35f;   // radio de la esfera del SphereCast (evita exigir punteria perfecta)

    private Interactable objetivoActual;
    private string ultimoPromptMostrado;

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
        // Si el juego esta en pausa, un panel interactivo esta abierto o el jugador esta dentro de un armario,
        // no se detectan ni activan otros interactuables del entorno.
        if (Time.timeScale == 0f || PanelTerminalZ1.Abierto || PanelTeclado.Abierto ||
            ArmarioEscondite.JugadorOculto || ArmarioEscondite.EstaJugadorDentroDeArmario(transform))
        {
            if (objetivoActual != null)
            {
                objetivoActual = null;
                ultimoPromptMostrado = null;
                if (!ArmarioEscondite.JugadorOculto && !ArmarioEscondite.EstaJugadorDentroDeArmario(transform) && UIManager.Instance != null)
                    UIManager.Instance.OcultarPrompt();
            }
            return;
        }

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
            // En tercera persona la camara se ubica detras del personaje; compensamos esa distancia
            // y filtramos el propio CharacterController del jugador para que no bloquee el rayo.
            Vector3 referenciaOjos = transform.position + Vector3.up * 1.55f;
            float offsetTerceraPersona = Mathf.Max(0f, Vector3.Dot(referenciaOjos - camara.position, camara.forward));
            float distanciaEfectiva = distancia + offsetTerceraPersona;

            Ray rayo = new Ray(camara.position, camara.forward);

            // 1) Primero priorizamos el impacto directo en la mira central.
            RaycastHit[] impactosDirectos = Physics.RaycastAll(
                rayo, distanciaEfectiva, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(impactosDirectos, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < impactosDirectos.Length; i++)
            {
                Collider col = impactosDirectos[i].collider;
                if (col == null || col.transform.IsChildOf(transform)) continue;

                Interactable candidato = col.GetComponentInParent<Interactable>();
                if (candidato != null && candidato.PuedeInteractuar(transform))
                    nuevoObjetivo = candidato;
                break; // Si el primer obstaculo solido no es interactuable desde aqui, bloquea la linea de vision.
            }

            // 2) Si el rayo fino no toco un interactuable, usamos SphereCast filtrando al propio jugador.
            if (nuevoObjetivo == null)
            {
                RaycastHit[] impactosEsfera = Physics.SphereCastAll(
                    rayo, grosorRayo, distanciaEfectiva, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(impactosEsfera, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < impactosEsfera.Length; i++)
                {
                    Collider col = impactosEsfera[i].collider;
                    if (col == null || col.transform.IsChildOf(transform)) continue;

                    Interactable candidato = col.GetComponentInParent<Interactable>();
                    if (candidato != null && candidato.PuedeInteractuar(transform))
                    {
                        Vector3 puntoObjetivo = impactosEsfera[i].distance > 0.001f
                            ? impactosEsfera[i].point
                            : ((col is BoxCollider || col is SphereCollider || col is CapsuleCollider || (col is MeshCollider mc && mc.convex))
                                ? col.ClosestPoint(referenciaOjos)
                                : col.bounds.center);
                        if (Physics.Linecast(referenciaOjos, puntoObjetivo, out RaycastHit bloqueo, ~0, QueryTriggerInteraction.Ignore))
                        {
                            Collider colBloqueo = bloqueo.collider;
                            if (colBloqueo != null && !colBloqueo.transform.IsChildOf(transform) && !colBloqueo.transform.IsChildOf(candidato.transform))
                                continue;
                        }
                        nuevoObjetivo = candidato;
                        break;
                    }
                }
            }
        }

        // Actualizar la interfaz cuando cambia el objetivo o su texto de prompt dinamico.
        if (nuevoObjetivo != objetivoActual)
        {
            objetivoActual = nuevoObjetivo;

            if (UIManager.Instance != null)
            {
                if (objetivoActual != null)
                {
                    ultimoPromptMostrado = objetivoActual.mensajePrompt;
                    UIManager.Instance.MostrarPrompt(ultimoPromptMostrado);
                }
                else
                {
                    ultimoPromptMostrado = null;
                    UIManager.Instance.OcultarPrompt();
                }
            }
        }
        else if (objetivoActual != null && objetivoActual.mensajePrompt != ultimoPromptMostrado)
        {
            ultimoPromptMostrado = objetivoActual.mensajePrompt;
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarPrompt(ultimoPromptMostrado);
        }
    }

    private void OnDisable()
    {
        // Al cambiar de escena el prompt no debe quedar pegado en pantalla.
        ultimoPromptMostrado = null;
        if (UIManager.Instance != null) UIManager.Instance.OcultarPrompt();
    }
}
