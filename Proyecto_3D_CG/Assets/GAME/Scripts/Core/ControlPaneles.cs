using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// ControlPaneles: teclas que abren los paneles del Canvas (ENTER para empezar, ESC para pausar, I para el inventario).
public class ControlPaneles : MonoBehaviour
{
    private void Update()
    {
        // Sin teclado o sin Canvas no hay nada que hacer.
        if (Keyboard.current == null || UIManager.Instance == null) return;

        // ENTER: solo funciona en el menu (cuando el Panel_Menu esta visible). Carga la Zona 1.
        if (Keyboard.current.enterKey.wasPressedThisFrame && UIManager.Instance.panelMenu.activeSelf)
        {
            SceneManager.LoadScene("Zona1_Recepcion");
            return;
        }

        // ESC: si el juego esta congelado, lo reanuda; si el HUD esta visible, lo pausa.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Time.timeScale == 0f)
                Reanudar();
            else if (UIManager.Instance.panelHUD.activeSelf)
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
