using UnityEngine;

public class Recogible : Interactable
{
    [Header("Datos del item")]
    public string idItem = "item_sin_id";     // id del item (ej. credencial_z1, credencial_z2)
    public string nombreVisible = "Objeto";   // texto que ve el jugador

    public override void Interactuar()
    {
        ReproducirSonidoInteraccion();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AgregarItem(idItem);

            if (UIManager.Instance != null)
                UIManager.Instance.ActualizarInventario(GameManager.Instance.inventario);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Recogiste: " + nombreVisible);
            UIManager.Instance.OcultarPrompt();
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
