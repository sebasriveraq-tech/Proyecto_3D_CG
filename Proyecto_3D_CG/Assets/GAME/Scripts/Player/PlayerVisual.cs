using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// PlayerVisual: controla lo visual del jugador y la transición fluida entre 1ª y 3ª persona (tecla V).
/// Coloca las mallas de la cabeza/rostro/modelo del jugador en la capa dedicada "PlayerHead" excluida del
/// Culling Mask en 1ª persona y además cambia ShadowCastingMode a ShadowsOnly en 1ª persona (y On en 3ª persona),
/// garantizando que nunca se vean ojos, boca ni dientes dentro de la cámara y siempre conserve la cabeza en 3ª persona.
public class PlayerVisual : MonoBehaviour
{
    private CharacterController controlador;
    private PlayerController controladorJugador;
    private Animator animator;       // Animator dentro del modelo animado
    private Transform camara;        // cámara del jugador
    private Camera camaraComponente;
    private GameObject modelo;       // cuerpo del personaje (Mixamo)
    private Transform cabeza;        // hueso de la cabeza ("mixamorig:Head")
    private Vector3 escalaOriginalCabeza = Vector3.one;

    private int layerPlayerHead = -1;
    private int cullingMaskOriginal = ~0;
    private readonly List<Renderer> todosLosRenderersModelo = new List<Renderer>();

    private bool terceraPersona;
    private float factorTransicionTercera; // 0 = 1ª persona, 1 = 3ª persona
    private float distanciaActualTercera = 2.65f;

    [Header("Configuracion Tercera Persona")]
    public float distanciaTercera = 2.65f;
    public float alturaPivoteTercera = 1.56f;
    public float desplazamientoHombro = 0.26f;
    public float radioColisionCamara = 0.22f;

    public bool EsTerceraPersona => terceraPersona;

    public void Configurar(CharacterController controlador, GameObject modelo)
    {
        this.controlador = controlador;
        this.modelo = modelo;
        controladorJugador = GetComponent<PlayerController>();
        animator = modelo != null ? modelo.GetComponentInChildren<Animator>(true) : null;

        // Ajustar CharacterController para paso fluido por puertas y pasillos sin atascos
        if (this.controlador != null)
        {
            this.controlador.radius = 0.32f;
            this.controlador.skinWidth = 0.04f;
            this.controlador.stepOffset = 0.30f;
        }

        // 1) Ocultar la cápsula primitiva raíz del Player: el cuerpo real es el modelo Mixamo.
        foreach (MeshRenderer r in GetComponents<MeshRenderer>())
            r.enabled = false;

        // 2) Buscar la cámara y ajustar Near Clip Plane para evitar atravesar muros.
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null && Camera.main != null) cam = Camera.main;
        camaraComponente = cam;
        camara = cam != null ? cam.transform : null;

        layerPlayerHead = LayerMask.NameToLayer("PlayerHead");
        if (cam != null)
        {
            cam.nearClipPlane = 0.05f;
            cam.useOcclusionCulling = false;
            cullingMaskOriginal = cam.cullingMask;
        }

        // 3) Buscar el hueso de la cabeza ("mixamorig:Head") y mover TODAS las mallas de PlayerMixamo
        //    (cabeza, ojos, boca, máscara y traje facial) a la capa dedicada PlayerHead.
        todosLosRenderersModelo.Clear();

        if (modelo != null)
        {
            modelo.SetActive(true);

            foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLowerInvariant().Contains("head") && t.GetComponent<Renderer>() == null)
                {
                    cabeza = t;
                    escalaOriginalCabeza = cabeza.localScale;
                    if (escalaOriginalCabeza.sqrMagnitude < 0.01f)
                        escalaOriginalCabeza = Vector3.one;
                    cabeza.localScale = escalaOriginalCabeza;
                    break;
                }
            }

            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                r.gameObject.SetActive(true);
                r.enabled = true;
                r.allowOcclusionWhenDynamic = false;

                if (r is SkinnedMeshRenderer smr)
                    smr.updateWhenOffscreen = true;

                if (layerPlayerHead >= 0)
                    r.gameObject.layer = layerPlayerHead;

                todosLosRenderersModelo.Add(r);
            }
        }

        distanciaActualTercera = distanciaTercera;
        factorTransicionTercera = 0f;

        // Arranca en 1ª persona: capa PlayerHead excluida del Culling Mask y renderers en ShadowsOnly.
        AplicarVisibilidadModelo(false);
    }

    private void AplicarVisibilidadModelo(bool mostrarModeloEnCamara)
    {
        if (modelo != null && !modelo.activeSelf)
            modelo.SetActive(true);

        // Mantener siempre la escala original del hueso de la cabeza para no deformar ni perder la cabeza en 3ª persona
        if (cabeza != null)
            cabeza.localScale = escalaOriginalCabeza;

        ShadowCastingMode modo = mostrarModeloEnCamara
            ? ShadowCastingMode.On
            : ShadowCastingMode.ShadowsOnly;

        for (int i = 0; i < todosLosRenderersModelo.Count; i++)
        {
            Renderer r = todosLosRenderersModelo[i];
            if (r == null) continue;
            if (!r.gameObject.activeSelf) r.gameObject.SetActive(true);
            r.enabled = true;
            r.shadowCastingMode = modo;
        }

        if (camaraComponente != null && layerPlayerHead >= 0)
        {
            int mascaraCapa = 1 << layerPlayerHead;
            camaraComponente.cullingMask = mostrarModeloEnCamara
                ? (cullingMaskOriginal | mascaraCapa)
                : (cullingMaskOriginal & ~mascaraCapa);
        }
    }

    private void Update()
    {
        PasarVelocidadAlAnimator();
        CambiarCamaraConTeclaV();
    }

    private void LateUpdate()
    {
        if (camara == null) return;
        if (camaraComponente != null && camaraComponente.nearClipPlane > 0.06f)
            camaraComponente.nearClipPlane = 0.05f;

        // Si el jugador está escondido en un armario, fijar cámara en el punto interno seguro sin ojos/boca visibles
        if (ArmarioEscondite.JugadorOculto && ArmarioEscondite.ArmarioActivo != null)
        {
            AplicarVisibilidadModelo(false);

            camara.position = ArmarioEscondite.ArmarioActivo.PosicionCamaraInterna;
            float pitchInterno = controladorJugador != null
                ? Mathf.Clamp(controladorJugador.RotacionVertical, -22f, 22f)
                : 0f;
            camara.rotation = Quaternion.Euler(pitchInterno, transform.eulerAngles.y, 0f);
            return;
        }

        float objetivoFactor = terceraPersona ? 1f : 0f;
        factorTransicionTercera = Mathf.MoveTowards(
            factorTransicionTercera, objetivoFactor, Time.unscaledDeltaTime / 0.14f);

        Vector3 posPrimera = ObtenerPosicionPrimeraPersona();
        Quaternion rotPrimera = ObtenerRotacionPrimeraPersona();

        if (!terceraPersona && factorTransicionTercera <= 0.001f)
        {
            AplicarVisibilidadModelo(false);
            camara.position = posPrimera;
            camara.rotation = rotPrimera;
            return;
        }

        CalcularPoseTerceraPersona(out Vector3 posTercera, out Quaternion rotTercera);

        float tSuave = Mathf.SmoothStep(0f, 1f, factorTransicionTercera);
        Vector3 posCamaraFinal = Vector3.Lerp(posPrimera, posTercera, tSuave);
        Quaternion rotCamaraFinal = Quaternion.Slerp(rotPrimera, rotTercera, tSuave);

        // En 3ª persona todo el modelo (incluyendo cabeza, ojos y boca) se reactiva con ShadowCastingMode.On
        // y capa PlayerHead incluida, evitando ocultamiento salvo si la cámara está dentro de la cabeza (< 0.34m).
        float distACabeza = Vector3.Distance(posCamaraFinal, posPrimera);
        bool mostrarModelo = terceraPersona && factorTransicionTercera >= 0.12f && distACabeza >= 0.34f;
        AplicarVisibilidadModelo(mostrarModelo);

        camara.position = posCamaraFinal;
        camara.rotation = rotCamaraFinal;
    }

    private Vector3 ObtenerPosicionPrimeraPersona()
    {
        if (cabeza != null)
            return cabeza.position + transform.forward * 0.06f + Vector3.up * 0.04f;
        return transform.position + Vector3.up * 1.62f;
    }

    private Quaternion ObtenerRotacionPrimeraPersona()
    {
        if (controladorJugador == null)
            controladorJugador = GetComponent<PlayerController>();
        float pitch = controladorJugador != null ? controladorJugador.RotacionVertical : 0f;
        return Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
    }

    private void CalcularPoseTerceraPersona(out Vector3 posicionFinal, out Quaternion rotacionFinal)
    {
        if (controladorJugador == null)
            controladorJugador = GetComponent<PlayerController>();

        float pitch = controladorJugador != null
            ? Mathf.Clamp(controladorJugador.RotacionVertical, -45f, 62f)
            : 10f;
        float yaw = transform.eulerAngles.y;

        Quaternion rotacionOrbita = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 pivoteCentral = transform.position + Vector3.up * alturaPivoteTercera;
        Vector3 direccionDerecha = transform.right;
        float offsetLateral = desplazamientoHombro;

        if (offsetLateral > 0.01f)
        {
            RaycastHit[] golpesLaterales = Physics.SphereCastAll(
                pivoteCentral, 0.16f, direccionDerecha, offsetLateral, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < golpesLaterales.Length; i++)
            {
                Collider col = golpesLaterales[i].collider;
                if (col == null || col.transform.IsChildOf(transform)) continue;
                offsetLateral = Mathf.Min(offsetLateral, Mathf.Max(0f, golpesLaterales[i].distance - 0.10f));
            }
        }

        Vector3 pivote = pivoteCentral + direccionDerecha * offsetLateral;
        Vector3 direccionAtras = rotacionOrbita * Vector3.back;

        float distanciaPermitida = distanciaTercera;
        RaycastHit[] golpes = Physics.SphereCastAll(
            pivote, radioColisionCamara, direccionAtras, distanciaTercera, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < golpes.Length; i++)
        {
            Collider col = golpes[i].collider;
            if (col == null || col.transform.IsChildOf(transform)) continue;

            float distSegura = Mathf.Max(0.42f, golpes[i].distance - 0.18f);
            if (distSegura < distanciaPermitida)
                distanciaPermitida = distSegura;
        }

        float velocidadAjuste = distanciaPermitida < distanciaActualTercera ? 32f : 14f;
        distanciaActualTercera = Mathf.Lerp(
            distanciaActualTercera, distanciaPermitida, 1f - Mathf.Exp(-velocidadAjuste * Time.unscaledDeltaTime));

        posicionFinal = pivote + direccionAtras * distanciaActualTercera;
        rotacionFinal = rotacionOrbita;
    }

    private void PasarVelocidadAlAnimator()
    {
        if (animator == null || controlador == null) return;
        if (ArmarioEscondite.JugadorOculto)
        {
            animator.SetFloat("Velocidad", 0f);
            animator.SetBool("EnElAire", false);
            return;
        }
        Vector3 velocidadPlana = controlador.velocity;
        velocidadPlana.y = 0f;
        animator.SetFloat("Velocidad", velocidadPlana.magnitude);
        animator.SetBool("EnElAire", !controlador.isGrounded);
    }

    private void CambiarCamaraConTeclaV()
    {
        if (Time.timeScale == 0f || PanelTerminalZ1.Abierto || ArmarioEscondite.JugadorOculto) return;
        if (Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame) return;

        terceraPersona = !terceraPersona;
        if (terceraPersona)
        {
            distanciaActualTercera = distanciaTercera;
            AplicarVisibilidadModelo(true);
        }
        else
        {
            AplicarVisibilidadModelo(false);
        }
    }
}
