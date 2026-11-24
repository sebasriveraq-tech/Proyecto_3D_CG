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

        // Con el panel de ajustes abierto, ESC vuelve al menu anterior (Menu Principal o Pausa).
        if (UIManager.Instance.panelAjustes != null && UIManager.Instance.panelAjustes.activeSelf)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) CerrarAjustes();
            return;
        }

        // Con la ayuda abierta, ESC vuelve al menu principal.
        if (UIManager.Instance.panelComoJugar != null && UIManager.Instance.panelComoJugar.activeSelf)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) CerrarComoJugar();
            return;
        }

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

        // Con el panel de reparacion abierto no se usa ninguna otra tecla de panel:
        // el propio panel procesa ESC y las letras.
        if (PanelTerminalZ1.Abierto) return;

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

        // I: abre o cierra el inventario (solo jugando y sin el panel de reparacion abierto).
        if (Keyboard.current.iKey.wasPressedThisFrame && Time.timeScale > 0f && !PanelTerminalZ1.Abierto)
        {
            if (UIManager.Instance.panelInventario.activeSelf)
            {
                UIManager.Instance.ReproducirClicUI(0.92f);
                UIManager.Instance.MostrarHUD();
            }
            else if (UIManager.Instance.panelHUD.activeSelf)
            {
                UIManager.Instance.ReproducirClicUI(1.08f);
                UIManager.Instance.MostrarInventario();
            }
        }
    }

    private void IniciarPartidaNueva()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(1.08f);

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

        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(1.08f);
        GameManager.Instance.ContinuarPartida();
    }

    // ---------------------------------------------------------------
    // BOTONES DEL MENU PRINCIPAL
    // ---------------------------------------------------------------

    // Los botones del menu llaman a estos metodos publicos (los mismos que ENTER y C).
    public void BotonNuevaPartida() { IniciarPartidaNueva(); }
    public void BotonContinuar() { ContinuarPartida(); }

    public void BotonComoJugar()
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.ReproducirFeedbackUI();
        if (UIManager.Instance != null) UIManager.Instance.MostrarComoJugar();
    }

    public void CerrarComoJugar()
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.ReproducirFeedbackUI();
        if (UIManager.Instance != null) UIManager.Instance.MostrarMenu();
    }

    public void BotonAjustesMenu()
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.ReproducirFeedbackUI();
        AjustesUI ajustes = AjustesUI.Instance != null ? AjustesUI.Instance : GetComponent<AjustesUI>();
        if (ajustes != null)
            ajustes.AbrirAjustes(false);
    }

    public void BotonAjustesPausa()
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.ReproducirFeedbackUI();
        AjustesUI ajustes = AjustesUI.Instance != null ? AjustesUI.Instance : GetComponent<AjustesUI>();
        if (ajustes != null)
            ajustes.AbrirAjustes(true);
    }

    public void CerrarAjustes()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(0.92f);
        if (AjustesUI.Instance != null)
            AjustesUI.Instance.CerrarAjustes();
        else if (UIManager.Instance != null)
            UIManager.Instance.MostrarMenu();
    }

    public void BotonReanudar()
    {
        Reanudar();
    }

    public void BotonSalir()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // en el editor detiene el Play Mode
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------
    // BOTONES DE LOS PANELES FINALES
    // ---------------------------------------------------------------

    // DERROTA: vuelve al ultimo checkpoint y recarga la zona con vida completa.
    public void Reintentar()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(1.06f);
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        Time.timeScale = 1f;   // el juego se congelo al mostrar el panel; hay que devolver el tiempo

        if (GameManager.Instance != null)
            GameManager.Instance.ReiniciarDesdeCheckpoint();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // VICTORIA: empieza una partida limpia desde la Zona 1.
    public void JugarDeNuevo()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(1.06f);
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        Time.timeScale = 1f;
        IniciarPartidaNueva();
    }

    // Los dos paneles: vuelve al menu principal.
    public void VolverAlMenu()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(0.92f);
        PanelMiniJuegoZ2.CerrarPanelActivoPorEventoExterno();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;   // el menu principal usa el mouse
        Cursor.visible = true;
        SceneManager.LoadScene("Menu");
    }

    private void Pausar()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(1f);
        Time.timeScale = 0f;                 // congela el juego
        UIManager.Instance.MostrarPausa();
        Cursor.lockState = CursorLockMode.None;   // libera el mouse para poder usar el menu
        Cursor.visible = true;
    }

    private void Reanudar()
    {
        if (UIManager.Instance != null) UIManager.Instance.ReproducirClicUI(0.90f);
        Time.timeScale = 1f;                 // devuelve el tiempo normal
        UIManager.Instance.MostrarHUD();
        Cursor.lockState = CursorLockMode.Locked; // vuelve a bloquear el mouse en el centro
        Cursor.visible = false;
    }
}
