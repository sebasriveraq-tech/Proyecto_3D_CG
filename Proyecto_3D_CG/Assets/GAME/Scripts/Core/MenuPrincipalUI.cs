using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// MenuPrincipalUI: estado de los botones del menu principal.
/// "Continuar" solo se puede pulsar cuando existe una partida guardada.
public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Botones")]
    public Button botonContinuar;

    [Header("Textos")]
    public TextMeshProUGUI textoEstado;   // aviso bajo los botones

    private void OnEnable()
    {
        ActualizarEstado();
    }

    private void Start()
    {
        // Start corre despues de todos los Awake: el GameManager ya existe aqui.
        ActualizarEstado();
    }

    public void ActualizarEstado()
    {
        bool hayPartida = GameManager.Instance != null && GameManager.Instance.HayPartidaGuardada();

        if (botonContinuar != null)
            botonContinuar.interactable = hayPartida;

        if (textoEstado != null)
        {
            textoEstado.text = hayPartida
                ? "Hay una partida guardada. CONTINUAR retoma la ultima zona."
                : "No hay partidas guardadas: empieza con NUEVA PARTIDA.";
        }
    }
}
