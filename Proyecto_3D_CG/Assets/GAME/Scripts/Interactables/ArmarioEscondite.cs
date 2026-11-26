using System.Collections;
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

    private void Awake()
    {
        if (JugadorOculto && ArmarioActivo == null)
        {
            JugadorOculto = false;
            FijarCamaraInterna = false;
        }
    }

    public static bool EstaJugadorDentroDeArmario(Transform jugador)
    {
        if (JugadorOculto) return true;
        if (jugador == null || ArmarioActivo == null) return false;
        return Vector3.Distance(jugador.position, ArmarioActivo.transform.position) < 1.0f;
    }

    private void Start()
    {
        if (bisagra != null)
        {
            duracion = Mathf.Max(0.15f, duracion);
            rotacionCerrada = bisagra.localRotation;
            rotacionAbierta = rotacionCerrada * Quaternion.Euler(0f, anguloApertura, 0f);
            collidersPuerta = bisagra.GetComponentsInChildren<Collider>(true);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        mensajePrompt = "Presiona E para esconderte en el armario";
    }

    // Valida con producto punto (Vector3.Dot) que el jugador esté frente a la puerta y mirando al armario
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
            JugadorOculto = true;
            ArmarioActivo = this;

            if (jugadorActual != null && puntoInterno != null && Mouse.current != null)
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

        if (jugadorActual == null || !jugadorActual.gameObject.activeInHierarchy)
            jugadorActual = FindFirstObjectByType<PlayerController>();

        if (jugadorActual == null || !PuedeInteractuar(jugadorActual.transform))
            return;

        if (rutinaActiva == null)
            rutinaActiva = StartCoroutine(RutinaEntrarEscondite(jugadorActual));
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

        ReproducirSonidoPuerta(true);
        progresoObjetivo = 1f;
        float limiteEspera = DuracionPuertaEfectiva + 0.25f;
        float esperaApertura = 0f;
        while (progreso < 0.88f && esperaApertura < limiteEspera)
        {
            esperaApertura += Time.deltaTime;
            yield return null;
        }

        Vector3 posInicio = jugador.transform.position;
        Quaternion rotInicio = jugador.transform.rotation;
        Vector3 posDestino = puntoInterno != null ? puntoInterno.position : transform.position;
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

        ReproducirSonidoPuerta(true);
        progresoObjetivo = 1f;
        float limiteEspera = DuracionPuertaEfectiva + 0.25f;
        float esperaApertura = 0f;
        while (progreso < 0.88f && esperaApertura < limiteEspera)
        {
            esperaApertura += Time.deltaTime;
            yield return null;
        }

        FijarCamaraInterna = false;
        if (jugadorActual != null)
        {
            Vector3 posInicio = jugadorActual.transform.position;
            Vector3 posSalida = puntoExterno != null
                ? puntoExterno.position
                : transform.TransformPoint(new Vector3(0f, 0.05f, 1.45f));
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
        if (ArmarioActivo == this)
        {
            JugadorOculto = false;
            FijarCamaraInterna = false;
            ArmarioActivo = null;
        }
        estado = EstadoArmario.CerradoLibre;
    }
}
