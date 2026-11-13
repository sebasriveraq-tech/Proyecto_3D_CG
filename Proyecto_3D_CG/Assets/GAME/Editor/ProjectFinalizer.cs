using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// ProjectFinalizer: ejecuta en una sola pasada el armado del menu, la reparacion
/// del piso de la Zona 2 y la verificacion del flujo de escenas.
///
/// Uso en batch mode:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod ProjectFinalizer.EjecutarTodo
public static class ProjectFinalizer
{
    private static readonly string[] Escenas =
    {
        "Assets/GAME/Scenes/Menu.unity",
        "Assets/GAME/Scenes/Zona1_Recepcion.unity",
        "Assets/GAME/Scenes/Zona2_Laboratorio.unity",
        "Assets/GAME/Scenes/Zona3_Control.unity"
    };

    public static void EjecutarTodo()
    {
        MenuPrincipalBuilder.Construir();
        Zona2FloorFixer.Reparar();
        VerificarEscenas();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectFinalizer] Proceso terminado.");
    }

    // Abre cada escena y confirma que tenga jugador, canvas, GameManager y EventSystem.
    public static void VerificarEscenas()
    {
        var informe = new StringBuilder("[ProjectFinalizer] Verificacion de escenas\n");

        foreach (string ruta in Escenas)
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

            GameObject jugador = BuscarEnEscena("Player");
            GameObject canvas = BuscarEnEscena("Canvas_Principal");
            GameObject gameManager = BuscarEnEscena("GameManager");
            GameObject eventSystem = BuscarEnEscena("EventSystem");

            bool esMenu = escena.name == "Menu";
            bool tieneControlador = jugador != null && jugador.GetComponent<PlayerController>() != null;
            bool tieneCamara = jugador != null && jugador.GetComponentInChildren<Camera>(true) != null;

            string estadoJugador = esMenu
                ? "n/a (menu)"
                : $"Player={(jugador ? "OK" : "FALTA")} " +
                  $"Controlador={(tieneControlador ? "OK" : "FALTA")} " +
                  $"Camara={(tieneCamara ? "OK" : "FALTA")}";

            informe.AppendLine(
                $"{escena.name,-20} {estadoJugador} " +
                $"Canvas={(canvas ? "OK" : "FALTA")} " +
                $"GameManager={(gameManager ? "OK" : "FALTA")} " +
                $"EventSystem={(eventSystem ? "OK" : "FALTA")}");
        }

        var modelo = Resources.Load<GameObject>("PlayerMixamo");
        informe.AppendLine("Resources/PlayerMixamo: " + (modelo != null ? "OK (modelo animado disponible en todas las zonas)" : "FALTA"));

        Debug.Log(informe.ToString());
    }

    private static GameObject BuscarEnEscena(string nombre)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == nombre) return go;
        }
        return null;
    }
}
