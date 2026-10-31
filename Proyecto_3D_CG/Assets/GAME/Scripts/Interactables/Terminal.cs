using UnityEngine;

/// Terminal: al interactuar entrega un codigo al GameManager (por ejemplo el Codigo A de la Zona 1).
/// No abre puertas ni cambia de escena: solo suma el codigo y avisa por el HUD.
public class Terminal : Interactable
{
    [Header("Codigo que entrega")]
    public string idCodigo = "codigo_a";        // ID interno del contrato
    public string codigoVisible = "A-4721";     // lo que ve el jugador
    public bool requiereEnergia;
    public LaboratorioController laboratorio;

    public override void Interactuar()
    {
        ReproducirSonidoInteraccion();
        if (requiereEnergia &&
            (laboratorio == null || !laboratorio.PuedeConsultarTerminal()))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("La terminal no tiene energía. Acciona el interruptor");
            return;
        }

        if (GameManager.Instance != null)
        {
            bool yaTeniaCodigo = GameManager.Instance.TieneCodigo(idCodigo);
            GameManager.Instance.AgregarCodigo(idCodigo);

            // El panel de inventario muestra la lista de codigos descubiertos.
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ActualizarCodigos(GameManager.Instance.codigos);
                UIManager.Instance.MostrarMensaje(
                    yaTeniaCodigo ? "Código consultado: " + codigoVisible :
                    "Código obtenido: " + codigoVisible);
            }
        }
    }
}
