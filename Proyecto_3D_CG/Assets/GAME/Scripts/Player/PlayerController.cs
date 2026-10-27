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

    private CharacterController controlador;
    private float velocidadVertical;     // subida/bajada por gravedad y salto
    private float rotacionX;              // mirar arriba/abajo

    private void Start()
    {
        controlador = GetComponent<CharacterController>();

        // Bloquear el mouse en el centro de la ventana.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        MirarConMouse();
        MoverConTeclado();
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

        float velocidadActual = velocidad;
        if (Keyboard.current.leftShiftKey.isPressed) velocidadActual = velocidadCorriendo;

        
        if (controlador.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f; // Gravedad: si esta en el piso, se mantiene pegado; si salta, sube.

        if (Keyboard.current.spaceKey.wasPressedThisFrame && controlador.isGrounded)
            velocidadVertical = fuerzaSalto;

        velocidadVertical += gravedad * Time.deltaTime;

        Vector3 movimiento = direccion * velocidadActual + Vector3.up * velocidadVertical;
        controlador.Move(movimiento * Time.deltaTime);
    }
}
