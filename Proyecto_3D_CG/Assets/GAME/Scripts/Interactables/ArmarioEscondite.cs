using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArmarioEscondite : Interactable
{
    private enum EstadoArmario
    {
        CerradoLibre,
        Entrando,
        Oculto,
        Saliendo
    }

    public static bool JugadorOculto { get; private set; }
    public static ArmarioEscondite ArmarioActivo { get; private set; }
    public static bool FijarCamaraInterna { get; private set; }
    private static readonly HashSet<ArmarioEscondite> armariosActivos = new HashSet<ArmarioEscondite>();

    [Header("Puerta y Referencias")]
    public Transform bisagra;             // pivote donde gira la puerta
    public Transform puntoInterno;        // punto de referencia dentro del armario
    public Transform puntoExterno;        // punto de salida frente al armario
    public float anguloApertura = -105f;  // grados que gira al abrir
    public float duracion = 0.45f;        // segundos que tarda el giro de la puerta

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoAbrir;
    public AudioClip sonidoCerrar;

    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private float progreso;               // 0 = cerrada, 1 = abierta
    private float progresoObjetivo;

    private EstadoArmario estado = EstadoArmario.CerradoLibre;
    private Coroutine rutinaActiva;
    private bool solicitudSalidaPendiente;
    private float tiempoBloqueoReentrada;
    private int frameUltimaInteraccion = -1;

    private PlayerController jugadorActual;
    private CharacterController characterControllerJugador;
    private Collider[] collidersPuerta;
    private float yawBaseArmario;
    private float offsetYawMirada;

    private bool EnTransicion => estado == EstadoArmario.Entrando || estado == EstadoArmario.Saliendo;
    private float DuracionPuertaEfectiva => Mathf.Clamp(duracion, 0.15f, 0.35f);

    public Vector3 PosicionCamaraInterna =>
        puntoInterno != null
            ? puntoInterno.position + Vector3.up * 1.52f + transform.forward * 0.08f
            : transform.TransformPoint(new Vector3(0f, 1.57f, 0.02f));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstadoEstatico()
    {
        JugadorOculto = false;
        ArmarioActivo = null;
        FijarCamaraInterna = false;
        armariosActivos.Clear();
    }

    private void Awake()
    {
        if (JugadorOculto && ArmarioActivo == null)
        {
            JugadorOculto = false;
            FijarCamaraInterna = false;
        }
    }

    private void OnEnable()
    {
        armariosActivos.Add(this);
    }

    private bool EstaPosicionDentroDeEsteArmario(Vector3 posMundo)
    {
        Vector3 local = transform.InverseTransformPoint(posMundo);
        return Mathf.Abs(local.x) < 0.72f
            && local.z > -0.58f
            && local.z < 0.58f
            && local.y >= -0.25f
            && local.y < 2.45f;
    }

    public static bool EstaJugadorDentroDeArmario(Transform jugador)
    {
        if (JugadorOculto) return true;
        if (jugador == null) return false;
        foreach (ArmarioEscondite armario in armariosActivos)
        {
            if (armario != null && armario.EstaPosicionDentroDeEsteArmario(jugador.position))
                return true;
        }
        return false;
    }

    private void Start()
    {
        if (bisagra == null)
            bisagra = transform.Find("Bisagra");

        if (bisagra != null)
        {
            duracion = Mathf.Max(0.15f, duracion);
            rotacionCerrada = bisagra.localRotation;
            rotacionAbierta = rotacionCerrada * Quaternion.Euler(0f, anguloApertura, 0f);
            collidersPuerta = bisagra.GetComponentsInChildren<Collider>(true);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1.8f;
        audioSource.maxDistance = 16f;
        audioSource.dopplerLevel = 0f;

        if (puntoInterno == null)
        {
            Transform existente = transform.Find("Punto_Referencia_Interno");
            if (existente != null)
            {
                puntoInterno = existente;
            }
            else
            {
                GameObject goInt = new GameObject("Punto_Referencia_Interno");
                goInt.transform.SetParent(transform, false);
                goInt.transform.localPosition = new Vector3(0f, 0.05f, -0.05f);
                goInt.transform.localRotation = Quaternion.identity;
                puntoInterno = goInt.transform;
            }
        }

        if (puntoExterno == null)
        {
            Transform existenteExt = transform.Find("Punto_Referencia_Externo");
            if (existenteExt != null)
            {
                puntoExterno = existenteExt;
            }
            else
            {
                GameObject goExt = new GameObject("Punto_Referencia_Externo");
                goExt.transform.SetParent(transform, false);
                goExt.transform.localPosition = new Vector3(0f, 0.05f, 1.45f);
                goExt.transform.localRotation = Quaternion.identity;
                puntoExterno = goExt.transform;
            }
        }

        // Limpiar colliders residuales internos (PanelInterior / LuzInterior) que no deben bloquear ni ensuciar Gizmos
        Transform panelInt = transform.Find("PanelInterior");
        if (panelInt != null)
        {
            foreach (Collider c in panelInt.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
        Transform luzInt = transform.Find("LuzInterior");
        if (luzInt != null)
        {
            foreach (Collider c in luzInt.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        // Trigger dedicado únicamente frente a la puerta del armario (+Z local) para delimitar acceso frontal
        if (transform.Find("Zona_Acceso_Frontal") == null)
        {
            GameObject zonaFrente = new GameObject("Zona_Acceso_Frontal");
            zonaFrente.transform.SetParent(transform, false);
            BoxCollider trig = zonaFrente.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 1.15f, 1.05f);
            trig.size = new Vector3(1.35f, 2.25f, 1.35f);
        }

        mensajePrompt = "Presiona E para esconderte en el armario";
    }

    public override bool PuedeInteractuar(Transform origenJugador)
    {
        if (estado == EstadoArmario.Oculto)
            return true;

        if (JugadorOculto || EnTransicion || Time.unscaledTime < tiempoBloqueoReentrada || origenJugador == null)
            return false;

        Vector3 localPos = transform.InverseTransformPoint(origenJugador.position);
        if (localPos.z < 0.40f || localPos.z > 2.95f || Mathf.Abs(localPos.x) > 0.95f)
            return false;

        Vector3 haciaJugador = origenJugador.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude < 0.001f)
            return false;

        float dotPosicion = Vector3.Dot(transform.forward, haciaJugador.normalized);
        if (dotPosicion < 0.45f)
            return false;

        Vector3 miradaPlana = origenJugador.forward;
        miradaPlana.y = 0f;
        if (miradaPlana.sqrMagnitude > 0.001f)
        {
            float dotMirada = Vector3.Dot(miradaPlana.normalized, -transform.forward);
            if (dotMirada < 0.05f)
                return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        // Zona frontal válida (verde) frente a la puerta (+Z)
        Gizmos.color = new Color(0.15f, 0.95f, 0.45f, 0.35f);
        Gizmos.DrawWireCube(new Vector3(0f, 1.15f, 1.15f), new Vector3(1.35f, 2.25f, 1.35f));
        // Zona trasera bloqueada (rojo) detrás del fondo (-Z)
        Gizmos.color = new Color(0.95f, 0.20f, 0.20f, 0.45f);
        Gizmos.DrawWireCube(new Vector3(0f, 1.15f, -0.80f), new Vector3(1.40f, 2.25f, 0.50f));
    }

    private void Update()
    {
        if (bisagra != null)
        {
            progreso = Mathf.MoveTowards(progreso, progresoObjetivo, Time.deltaTime / DuracionPuertaEfectiva);
            float suave = Mathf.SmoothStep(0f, 1f, progreso);
            bisagra.localRotation = Quaternion.Slerp(rotacionCerrada, rotacionAbierta, suave);
        }

        if (Time.timeScale == 0f || PanelTerminalZ1.Abierto || PanelTeclado.Abierto)
            return;

        // Si el jugador pulsa E rápido mientras termina la entrada, encolar la salida automática
        if (estado == EstadoArmario.Entrando)
        {
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame &&
                Time.frameCount != frameUltimaInteraccion)
            {
                frameUltimaInteraccion = Time.frameCount;
                solicitudSalidaPendiente = true;
            }
            return;
        }

        if (estado == EstadoArmario.Oculto)
        {
            if (!JugadorOculto) JugadorOculto = true;
            if (ArmarioActivo != this) ArmarioActivo = this;

            // Mantener al jugador fijo en el punto interno con rotación horizontal limitada para no atravesar paredes
            if (jugadorActual != null && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                offsetYawMirada = Mathf.Clamp(offsetYawMirada + delta.x * jugadorActual.sensibilidadMouse, -32f, 32f);
                jugadorActual.transform.position = puntoInterno.position;
                jugadorActual.transform.rotation = Quaternion.Euler(0f, yawBaseArmario + offsetYawMirada, 0f);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.MostrarPrompt("Oculto en el armario — Presiona E para salir");

            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame &&
                Time.frameCount != frameUltimaInteraccion)
            {
                frameUltimaInteraccion = Time.frameCount;
                IniciarSalida();
            }
            return;
        }

        // Failsafe anti-atrapamiento: si por cualquier colisión el jugador quedó dentro del armario en estado libre,
        // ofrecer y permitir la salida inmediata al presionar E.
        if (estado == EstadoArmario.CerradoLibre && !JugadorOculto && ArmarioActivo == null)
        {
            PlayerController jugador = ObtenerJugadorActual();
            if (jugador != null && EstaPosicionDentroDeEsteArmario(jugador.transform.position))
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.MostrarPrompt("Presiona E para salir del armario");

                if (Keyboard.current != null &&
                    Keyboard.current.eKey.wasPressedThisFrame &&
                    Time.frameCount != frameUltimaInteraccion)
                {
                    frameUltimaInteraccion = Time.frameCount;
                    jugadorActual = jugador;
                    characterControllerJugador = jugador.GetComponent<CharacterController>();
                    yawBaseArmario = transform.eulerAngles.y;
                    IniciarSalida();
                }
            }
        }
    }

    public override void Interactuar()
    {
        if (Time.frameCount == frameUltimaInteraccion || PanelTerminalZ1.Abierto || PanelTeclado.Abierto)
            return;

        frameUltimaInteraccion = Time.frameCount;

        if (estado == EstadoArmario.Entrando)
        {
            solicitudSalidaPendiente = true;
            return;
        }

        if (estado == EstadoArmario.Saliendo)
            return;

        if (estado == EstadoArmario.Oculto)
        {
            IniciarSalida();
            return;
        }

        if (JugadorOculto || Time.unscaledTime < tiempoBloqueoReentrada)
            return;

        PlayerController jugador = ObtenerJugadorActual();
        if (jugador == null)
        {
            progresoObjetivo = progresoObjetivo > 0.5f ? 0f : 1f;
            ReproducirSonidoPuerta(progresoObjetivo > 0.5f);
            return;
        }

        if (EstaPosicionDentroDeEsteArmario(jugador.transform.position))
        {
            jugadorActual = jugador;
            characterControllerJugador = jugador.GetComponent<CharacterController>();
            yawBaseArmario = transform.eulerAngles.y;
            IniciarSalida();
            return;
        }

        if (!PuedeInteractuar(jugador.transform))
            return;

        if (rutinaActiva == null)
            rutinaActiva = StartCoroutine(RutinaEntrarEscondite(jugador));
    }

    private PlayerController ObtenerJugadorActual()
    {
        if (jugadorActual != null && jugadorActual.gameObject.activeInHierarchy)
            return jugadorActual;

        jugadorActual = FindFirstObjectByType<PlayerController>();
        return jugadorActual;
    }

    private void IniciarSalida()
    {
        if (rutinaActiva != null || estado == EstadoArmario.Saliendo)
            return;

        solicitudSalidaPendiente = false;
        rutinaActiva = StartCoroutine(RutinaSalirEscondite());
    }

    private void EstablecerColisionPuertaConJugador(Collider colJugador, bool ignorar)
    {
        if (colJugador == null || !colJugador.enabled || collidersPuerta == null)
            return;

        for (int i = 0; i < collidersPuerta.Length; i++)
        {
            Collider colPuerta = collidersPuerta[i];
            if (colPuerta != null && colPuerta.enabled)
                Physics.IgnoreCollision(colPuerta, colJugador, ignorar);
        }
    }

    private void ReproducirSonidoPuerta(bool abrir)
    {
        LabAudioFeedback.ReproducirPuertaArmario(
            transform.position,
            abrir,
            audioSource,
            abrir ? sonidoAbrir : sonidoCerrar);
    }

    private IEnumerator RutinaEntrarEscondite(PlayerController jugador)
    {
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        PanelTerminalZ1.CerrarPanelActivoPorEventoExterno();
        PanelTeclado.CerrarPanelActivoPorEventoExterno();

        estado = EstadoArmario.Entrando;
        solicitudSalidaPendiente = false;
        JugadorOculto = true;
        ArmarioActivo = this;
        FijarCamaraInterna = false;

        jugadorActual = jugador;
        characterControllerJugador = jugador.GetComponent<CharacterController>();
        EstablecerColisionPuertaConJugador(characterControllerJugador, true);
        if (characterControllerJugador != null)
            characterControllerJugador.enabled = false;

        if (UIManager.Instance != null)
            UIManager.Instance.OcultarPrompt();

        yawBaseArmario = transform.eulerAngles.y;
        offsetYawMirada = 0f;

        // 1. Abrir la puerta del armario con su sonido metálico
        ReproducirSonidoPuerta(true);
        progresoObjetivo = 1f;
        float limiteEspera = DuracionPuertaEfectiva + 0.25f;
        float esperaApertura = 0f;
        while (progreso < 0.88f && esperaApertura < limiteEspera)
        {
            esperaApertura += Time.deltaTime;
            yield return null;
        }

        // 2. Desplazar suavemente al jugador al punto de referencia interno mirando hacia las rendijas
        Vector3 posInicio = jugador.transform.position;
        Quaternion rotInicio = jugador.transform.rotation;
        Vector3 posDestino = puntoInterno.position;
        Quaternion rotDestino = Quaternion.Euler(0f, yawBaseArmario, 0f);

        float t = 0f;
        const float tiempoEntrada = 0.24f;
        while (t < 1f)
        {
            t += Time.deltaTime / tiempoEntrada;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            jugador.transform.position = Vector3.Lerp(posInicio, posDestino, k);
            jugador.transform.rotation = Quaternion.Slerp(rotInicio, rotDestino, k);
            yield return null;
        }

        jugador.transform.position = posDestino;
        jugador.transform.rotation = rotDestino;
        FijarCamaraInterna = true;

        // 3. Cerrar la puerta para cortar la línea de visión del guardia
        ReproducirSonidoPuerta(false);
        progresoObjetivo = 0f;
        float esperaCierre = 0f;
        while (progreso > 0.05f && esperaCierre < limiteEspera)
        {
            esperaCierre += Time.deltaTime;
            yield return null;
        }

        estado = EstadoArmario.Oculto;
        rutinaActiva = null;

        if (solicitudSalidaPendiente)
        {
            solicitudSalidaPendiente = false;
            IniciarSalida();
            yield break;
        }

        mensajePrompt = "Presiona E para salir del armario";
        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Te has escondido en el armario");
            UIManager.Instance.MostrarPrompt("Oculto en el armario — Presiona E para salir");
        }
    }

    private IEnumerator RutinaSalirEscondite()
    {
        estado = EstadoArmario.Saliendo;
        solicitudSalidaPendiente = false;
        JugadorOculto = true;
        ArmarioActivo = this;

        if (UIManager.Instance != null)
            UIManager.Instance.OcultarPrompt();

        // 1. Abrir la puerta del armario antes de mover al jugador
        ReproducirSonidoPuerta(true);
        progresoObjetivo = 1f;
        float limiteEspera = DuracionPuertaEfectiva + 0.25f;
        float esperaApertura = 0f;
        while (progreso < 0.88f && esperaApertura < limiteEspera)
        {
            esperaApertura += Time.deltaTime;
            yield return null;
        }

        // 2. El jugador sale mientras la puerta permanece abierta
        FijarCamaraInterna = false;
        if (jugadorActual != null)
        {
            Vector3 posInicio = jugadorActual.transform.position;
            Vector3 posSalida = puntoExterno.position;
            if (puntoExterno.parent == transform && puntoExterno.localPosition.z < 1.45f)
            {
                Vector3 locExt = puntoExterno.localPosition;
                locExt.z = 1.45f;
                posSalida = transform.TransformPoint(locExt);
            }
            Quaternion rotInicio = jugadorActual.transform.rotation;
            Quaternion rotSalida = Quaternion.Euler(0f, yawBaseArmario, 0f);

            float t = 0f;
            const float tiempoSalida = 0.24f;
            while (t < 1f)
            {
                t += Time.deltaTime / tiempoSalida;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                jugadorActual.transform.position = Vector3.Lerp(posInicio, posSalida, k);
                jugadorActual.transform.rotation = Quaternion.Slerp(rotInicio, rotSalida, k);
                yield return null;
            }

            jugadorActual.transform.position = posSalida;
            jugadorActual.transform.rotation = rotSalida;
        }

        // 3. Liberar al jugador afuera ignorando la puerta mientras cierra para evitar empujones de vuelta
        if (characterControllerJugador != null)
        {
            characterControllerJugador.enabled = true;
            EstablecerColisionPuertaConJugador(characterControllerJugador, true);
        }

        JugadorOculto = false;
        FijarCamaraInterna = false;
        if (ArmarioActivo == this)
            ArmarioActivo = null;

        mensajePrompt = "Presiona E para esconderte en el armario";
        tiempoBloqueoReentrada = Time.unscaledTime + 0.35f;

        ReproducirSonidoPuerta(false);
        progresoObjetivo = 0f;
        float esperaCierre = 0f;
        while (progreso > 0.05f && esperaCierre < limiteEspera)
        {
            esperaCierre += Time.deltaTime;
            yield return null;
        }

        EstablecerColisionPuertaConJugador(characterControllerJugador, false);
        estado = EstadoArmario.CerradoLibre;
        rutinaActiva = null;
    }

    private void OnDisable()
    {
        armariosActivos.Remove(this);
        if (rutinaActiva != null)
        {
            StopCoroutine(rutinaActiva);
            rutinaActiva = null;
        }

        if (characterControllerJugador != null)
        {
            characterControllerJugador.enabled = true;
            EstablecerColisionPuertaConJugador(characterControllerJugador, false);
        }

        solicitudSalidaPendiente = false;
        if (estado == EstadoArmario.Oculto || estado == EstadoArmario.Entrando || estado == EstadoArmario.Saliendo)
        {
            JugadorOculto = false;
            FijarCamaraInterna = false;
            if (ArmarioActivo == this)
                ArmarioActivo = null;
        }
        else if (ArmarioActivo == this)
        {
            FijarCamaraInterna = false;
            ArmarioActivo = null;
        }
        estado = EstadoArmario.CerradoLibre;
    }
}
