using UnityEngine;

public class Computadora : Interactable
{
    [Header("Escena")]
    public Scene3Controller controlador;
    [Tooltip("0 = energía de la sala, 1 = energía de las puertas, 2 = salida final")]
    public int etapa = 0;
    [Tooltip("Cantidad de dígitos del código (11221 = 5)")]
    public int longitudCodigo = 5;

    [Header("Bloquear al jugador mientras usa la computadora")]
    [Tooltip("Arrastra aquí el PlayerController y el script de cámara/mouse look")]
    public MonoBehaviour[] scriptsJugador;

    private bool usada;
    private bool abierta;
    private CursorLockMode lockPrevio;
    private bool cursorVisiblePrevio;

    public override void Interactuar()
    {
        if (ArmarioEscondite.JugadorOculto || usada || abierta) return;

        if (UIManager.Instance == null || UIManager.Instance.panelTeclado == null || controlador == null)
        {
            Debug.LogWarning("Computadora: falta el panelTeclado en el UIManager o el Scene3Controller.");
            return;
        }

        ReproducirSonidoInteraccion();
        UIManager.Instance.OcultarPrompt();

        abierta = true;
        BloquearJugador(true);
        UIManager.Instance.AbrirTeclado(longitudCodigo, ValidarCodigo, AlCerrarPanel);
    }

    private bool ValidarCodigo(string codigo)
    {
        bool aceptado = controlador.IntentarCodigo(etapa, codigo);
        if (aceptado) usada = true;
        return aceptado;
    }

    private void AlCerrarPanel()
    {
        abierta = false;
        BloquearJugador(false);
    }

    private void BloquearJugador(bool bloquear)
    {
        if (bloquear)
        {
            lockPrevio = Cursor.lockState;
            cursorVisiblePrevio = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        foreach (MonoBehaviour s in scriptsJugador)
            if (s != null) s.enabled = !bloquear;

        if (!bloquear)
        {
            Cursor.lockState = lockPrevio;
            Cursor.visible = cursorVisiblePrevio;
        }
    }
}