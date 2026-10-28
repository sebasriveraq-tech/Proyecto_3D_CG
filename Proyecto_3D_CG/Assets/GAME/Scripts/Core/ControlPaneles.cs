using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// ControlPaneles: teclas que abren los paneles del Canvas (ENTER para empezar, ESC para pausar, I para el inventario)
/// y metodos que usan los botones de los paneles de Victoria y Derrota.
public class ControlPaneles : MonoBehaviour
{
    private void Update()
    {
        // Sin teclado o sin Canvas no hay nada que hacer.
        if (Keyboard.current == null || UIManager.Instance == null) return;

        // ENTER: solo funciona en el menu (cuando el Panel_Menu esta visible). Empieza una partida nueva.
        if (Keyboard.current.enterKey.wasPressedThisFrame && UIManager.Instance.panelMenu.activeSelf)
        {
            IniciarPartidaNueva();
            return;
        }

        // C: en el menu continúa desde el ultimo guardado (si existe).
        if (Keyboard.current.cKey.wasPressedThisFrame && UIManager.Instance.panelMenu.activeSelf)
        {
            ContinuarPartida();
            return;
        }

        // ESC: solo reanuda si el panel de pausa esta abierto; si el HUD esta visible, lo pausa.
        // (Los paneles de Victoria y Derrota tienen botones y no se cierran con ESC.)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (UIManager.Instance.panelPausa.activeSelf)
                Reanudar();
            else if (Time.timeScale > 0f && UIManager.Instance.panelHUD.activeSelf)
                Pausar();
            return;
        }

        // I: abre o cierra el inventario (solo jugando, nunca pausado).
        if (Keyboard.current.iKey.wasPressedThisFrame && Time.timeScale > 0f)
        {
            if (UIManager.Instance.panelInventario.activeSelf)
                UIManager.Instance.MostrarHUD();
            else if (UIManager.Instance.panelHUD.activeSelf)
                UIManager.Instance.MostrarInventario();
        }
    }

    private void IniciarPartidaNueva()
    {
        // Sin GameManager (por ejemplo si se abre la Zona 1 directo) solo se carga la escena.
        if (GameManager.Instance == null)
        {
            SceneManager.LoadScene("Zona1_Recepcion");
            return;
        }

        GameManager.Instance.NuevaPartida();                   // limpia el estado y borra el JSON viejo
        GameManager.Instance.CambiarEscena("Zona1_Recepcion"); // guarda el checkpoint inicial y entra a Z1
    }

    private void ContinuarPartida()
    {
        // Si no hay archivo no se hace nada: en la primera partida todavia no existe.
        if (GameManager.Instance == null || !GameManager.Instance.HayPartidaGuardada()) return;

        GameManager.Instance.ContinuarPartida();
    }

    // ---------------------------------------------------------------
    // BOTONES DE LOS PANELES FINALES
    // ---------------------------------------------------------------

    // DERROTA: vuelve al ultimo checkpoint y recarga la zona con vida completa.
    public void Reintentar()
    {
        Time.timeScale = 1f;   // el juego se congelo al mostrar el panel; hay que devolver el tiempo

        if (GameManager.Instance != null)
            GameManager.Instance.ReiniciarDesdeCheckpoint();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // VICTORIA: empieza una partida limpia desde la Zona 1.
    public void JugarDeNuevo()
    {
        Time.timeScale = 1f;
        IniciarPartidaNueva();
    }

    // Los dos paneles: vuelve al menu principal.
    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    private void Pausar()
    {
        Time.timeScale = 0f;                 // congela el juego
        UIManager.Instance.MostrarPausa();
        Cursor.lockState = CursorLockMode.None;   // libera el mouse para poder usar el menu
        Cursor.visible = true;
    }

    private void Reanudar()
    {
        Time.timeScale = 1f;                 // devuelve el tiempo normal
        UIManager.Instance.MostrarHUD();
        Cursor.lockState = CursorLockMode.Locked; // vuelve a bloquear el mouse en el centro
        Cursor.visible = false;
    }
}
