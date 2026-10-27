using UnityEngine;


/// Recogible: interactuable de prueba (credencial, evidencia, tarjeta...).Al presionar E guarda el item en el GameManager, actualiza el inventario del HUD y desaparece de la escena.

public class Recogible : Interactable
{
    [Header("Datos del item")]
    public string idItem = "item_sin_id";     // ID del contrato: credencial_z1, evidencia_z2...
    public string nombreVisible = "Objeto";   // texto que ve el jugador

    public override void Interactuar()
    {
        
        if (GameManager.Instance != null) // 1. Guardar el item en el estado global.
        {
            GameManager.Instance.AgregarItem(idItem);

            
            if (UIManager.Instance != null)// 2. Actualizar la lista del inventario en el HUD.
                UIManager.Instance.ActualizarInventario(GameManager.Instance.inventario);
        }

        
        if (UIManager.Instance != null)// 3. Mensaje de retroalimentacion.
        {
            UIManager.Instance.MostrarMensaje("Recogiste: " + nombreVisible);
            UIManager.Instance.OcultarPrompt();
        }

        
        Destroy(gameObject);// 4. El objeto ya no esta en la escena.
    }
}
