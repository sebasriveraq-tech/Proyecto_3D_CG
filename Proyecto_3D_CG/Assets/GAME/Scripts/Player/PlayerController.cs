using UnityEngine;
using UnityEngine.InputSystem;


/// PlayerController: movimiento del jugador en primera persona usando el New Input System (Keyboard.current y Mouse.current). 

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 4f;
    public float velocidadCorriendo = 7f;
    public float fuerzaSalto = 5f;
    public float gravedad = -20f;

    [Header("Camara")]
    public Transform camara;              // Camera hija del Player
    public float sensibilidadMouse = 0.12f;

    [Header("Seguridad contra caidas")]
    public float margenSobreElSuelo = 0.05f;  // altura a la que se apoya al jugador al entrar a la zona
    public float caidaMaxima = 6f;            // metros por debajo del suelo antes de devolverlo a tierra

    [Header("Audio de pasos")]
    public AudioClip[] pasosConcreto;
    public float intervaloPasoCaminar = 0.50f;
    public float intervaloPasoCorrer = 0.32f;
    public float volumenPasos = 0.34f;

    private CharacterController controlador;
    private float velocidadVertical;     // subida/bajada por gravedad y salto
    private float rotacionX;              // mirar arriba/abajo
    private float alturaSuelo;            // altura del piso de la zona actual
    private Vector3 posicionSegura;       // ultimo punto con el jugador apoyado
    private AudioSource fuentePasos;
    private float temporizadorPasos;
    private int ultimoIndicePaso = -1;
    private bool tienePasosExternos;
    private bool estabaEnSuelo = true;
    private float velocidadVerticalPrevia;

    public float RotacionVertical => rotacionX;

    private void Start()
    {
        controlador = GetComponent<CharacterController>();
        ConfigurarAudioPasos();

        if (camara != null)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Zona3_Control")
            {
                gameObject.AddComponent<AgitacionPorAgenteEtapa1>().Inicializar(camara);
            }
            else
            {
                gameObject.AddComponent<AgitacionPorAgente>().Inicializar(camara);
            }
        }

        if (GameSettingsManager.Instance != null)
            sensibilidadMouse = GameSettingsManager.Instance.sensibilidadMouse;

        // Apoyar al jugador exactamente sobre el piso de la zona: evita nacer
        // atravesando el suelo o quedar flotando cuando el spawn no es exacto.
        AjustarAlSuelo();
        posicionSegura = transform.position;

        // Bloquear el mouse en el centro de la ventana.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Con el juego pausado o el panel de reparacion abierto el jugador queda quieto.
        if (Time.timeScale == 0f || PanelTerminalZ1.Abierto) return;

        if (ArmarioEscondite.JugadorOculto)
        {
            MirarDentroDeArmario();
            return;
        }

        MirarConMouse();
        MoverConTeclado();
    }

    private void MirarDentroDeArmario()
    {
        if (Mouse.current == null || camara == null) return;
        Vector2 movimientoMouse = Mouse.current.delta.ReadValue();
        rotacionX -= movimientoMouse.y * sensibilidadMouse;
        rotacionX = Mathf.Clamp(rotacionX, -22f, 22f);
        camara.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
    }

   
    // MIRAR: el mouse gira el cuerpo (izquierda/derecha) y la camara (arriba/abajo)
   
    private void MirarConMouse()
    {
        if (Mouse.current == null || camara == null) return;

        Vector2 movimientoMouse = Mouse.current.delta.ReadValue();

       
        transform.Rotate(0f, movimientoMouse.x * sensibilidadMouse, 0f); // Girar el cuerpo del jugador sobre el eje Y.

        
        rotacionX -= movimientoMouse.y * sensibilidadMouse; // Girar solo la camara sobre el eje X (con limite para no dar la vuelta).
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);
        camara.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
    }

  
    // MOVER: WASD, Shift para correr, Espacio para saltar
  
    private void MoverConTeclado()
    {
        if (Keyboard.current == null) return;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        
        Vector3 direccion = transform.right * x + transform.forward * z; // Direccion relativa a hacia donde mira el jugador.

        if (direccion.magnitude > 1f) direccion.Normalize(); // Evitar que en diagonal se mueva mas rapido.

        bool corriendo = Keyboard.current.leftShiftKey.isPressed;
        float velocidadActual = corriendo ? velocidadCorriendo : velocidad;

        if (controlador.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f; // Gravedad: si esta en el piso, se mantiene pegado; si salta, sube.

        if (Keyboard.current.spaceKey.wasPressedThisFrame && controlador.isGrounded)
            velocidadVertical = fuerzaSalto;

        velocidadVertical += gravedad * Time.deltaTime;

        Vector3 movimiento = direccion * velocidadActual + Vector3.up * velocidadVertical;
        controlador.Move(movimiento * Time.deltaTime);

        ControlarPasos(x, z, corriendo);

        // Guardar el ultimo punto donde el jugador estaba realmente apoyado.
        if (controlador.isGrounded) posicionSegura = transform.position;

        // Si por cualquier motivo atraviesa el suelo, se devuelve a la superficie.
        if (transform.position.y < alturaSuelo - Mathf.Max(3f, Mathf.Abs(caidaMaxima)))
            RecuperarDeCaida();
    }

    private void ConfigurarAudioPasos()
    {
        tienePasosExternos = GetComponent<EfectosZona3>() != null;
        if (tienePasosExternos || pasosConcreto == null || pasosConcreto.Length == 0)
            return;

        fuentePasos = gameObject.AddComponent<AudioSource>();
        fuentePasos.playOnAwake = false;
        fuentePasos.loop = false;
        fuentePasos.spatialBlend = 0f;
        fuentePasos.dopplerLevel = 0f;
        fuentePasos.volume = volumenPasos;
        GameSettingsManager.GarantizarInstancia().RegistrarFuenteConVolumenBase(fuentePasos, volumenPasos);
    }

    private void ControlarPasos(float entradaX, float entradaZ, bool corriendo)
    {
        if (tienePasosExternos || fuentePasos == null || pasosConcreto == null || pasosConcreto.Length == 0)
            return;

        if (!estabaEnSuelo && controlador.isGrounded && velocidadVerticalPrevia < -3.5f)
        {
            ReproducirPasoConcreto(0.90f);
            temporizadorPasos = 0.25f;
        }
        else if (controlador.isGrounded)
        {
            Vector3 velPlana = controlador.velocity;
            velPlana.y = 0f;
            bool enMovimiento = (Mathf.Abs(entradaX) > 0.01f || Mathf.Abs(entradaZ) > 0.01f) && velPlana.magnitude > 0.35f;

            if (enMovimiento)
            {
                temporizadorPasos -= Time.deltaTime;
                if (temporizadorPasos <= 0f)
                {
                    ReproducirPasoConcreto(corriendo ? 1f : 0.78f);
                    temporizadorPasos = Mathf.Max(0.15f, corriendo ? intervaloPasoCorrer : intervaloPasoCaminar);
                }
            }
            else
            {
                temporizadorPasos = Mathf.Min(temporizadorPasos, 0.08f);
            }
        }

        estabaEnSuelo = controlador.isGrounded;
        velocidadVerticalPrevia = velocidadVertical;
    }

    private void ReproducirPasoConcreto(float escala)
    {
        if (fuentePasos == null || pasosConcreto == null || pasosConcreto.Length == 0) return;
        if (GameSettingsManager.FactorEfectos <= 0.0001f) return;

        int indice = 0;
        if (pasosConcreto.Length > 1)
        {
            indice = Random.Range(0, pasosConcreto.Length);
            if (indice == ultimoIndicePaso)
                indice = (indice + 1) % pasosConcreto.Length;
        }
        ultimoIndicePaso = indice;

        AudioClip clip = pasosConcreto[indice];
        if (clip == null) return;

        fuentePasos.mute = GameSettingsManager.Instance != null && GameSettingsManager.Instance.silenciado;
        fuentePasos.pitch = Random.Range(0.93f, 1.07f);
        fuentePasos.PlayOneShot(clip, escala);
    }

    // Coloca al jugador justo encima del piso que hay debajo del punto de aparicion.
    private void AjustarAlSuelo()
    {
        Vector3 origen = transform.position + Vector3.up * 3f;
        RaycastHit[] golpes = new RaycastHit[16];
        int total = Physics.RaycastNonAlloc(origen, Vector3.down, golpes, 12f, ~0, QueryTriggerInteraction.Ignore);

        float alturaPiso = float.NegativeInfinity;
        for (int i = 0; i < total; i++)
        {
            Collider otro = golpes[i].collider;
            if (otro == null || otro.transform.IsChildOf(transform) || otro is CharacterController) continue;
            if (golpes[i].point.y > alturaPiso) alturaPiso = golpes[i].point.y;
        }

        if (float.IsNegativeInfinity(alturaPiso)) return; // sin piso detectado: no se toca el spawn

        alturaSuelo = alturaPiso;
        float objetivo = alturaPiso + margenSobreElSuelo;
        if (Mathf.Abs(transform.position.y - objetivo) < 0.01f) return;

        controlador.enabled = false;
        transform.position = new Vector3(transform.position.x, objetivo, transform.position.z);
        controlador.enabled = true;
    }

    private void RecuperarDeCaida()
    {
        controlador.enabled = false;
        transform.position = posicionSegura + Vector3.up * 0.15f;
        controlador.enabled = true;
        velocidadVertical = 0f;
    }
}
