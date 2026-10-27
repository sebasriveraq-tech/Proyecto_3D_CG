using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


/// PlayerHealth: vida del jugador con daño por cooldown. 

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vidaActual = 100;

    [Header("Dano")]
    public float cooldownDano = 1f;           // segundos entre golpes
    public float esperaAntesDeReiniciar = 2f; // tiempo que se ve el panel de derrota

    private float proximoDanoPermitido = 0f;  // momento (Time.time) en que se puede volver a recibir dano
    private bool muerto = false;

    private void Start()
    {
        
        if (GameManager.Instance != null)
            vidaActual = GameManager.Instance.vidaJugador; // Si venimos de otra zona, el GameManager tiene la vida real.

        ActualizarHUD();
    }

    private void Update()
    {
        // Si el juego esta en pausa, la tecla K no hace dano.
        if (Time.timeScale == 0f) return;

        // TEMPORAL: tecla K para probar el HUD y el cooldown mientras no hay agentes.Se borra cuando el AgentController haga dano de verdad.
        
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            RecibirDano(10);
    }

   
    // RECIBIR DAÑO (con cooldown)

    public void RecibirDano(int cantidad)
    {
        if (muerto) return;

        
        if (Time.time < proximoDanoPermitido) return; // Si todavia no paso el cooldown, el golpe se ignora.

        proximoDanoPermitido = Time.time + cooldownDano;

        vidaActual -= cantidad;
        if (vidaActual < 0) vidaActual = 0;

       
        if (GameManager.Instance != null)
            GameManager.Instance.vidaJugador = vidaActual; // Guardar la vida en el estado global.

        ActualizarHUD();

        if (vidaActual == 0) StartCoroutine(Morir());
    }

    private void ActualizarHUD()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ActualizarVida(vidaActual, vidaMaxima);
    }

  
    // MUERTE: panel de derrota y recarga de la zona

    private IEnumerator Morir()
    {
        muerto = true;

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarDerrota();

        yield return new WaitForSeconds(esperaAntesDeReiniciar);

        if (GameManager.Instance != null)
        {
            // Checkpoint provisional: se revive con vida completa.Cuando exista el SaveManager, aqui se cargara el JSON.
            GameManager.Instance.vidaJugador = vidaMaxima;
            GameManager.Instance.ReiniciarDesdeCheckpoint();
        }
        else
        {
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Permite probar la zona directamente sin pasar por el menu.
        }
    }
}
