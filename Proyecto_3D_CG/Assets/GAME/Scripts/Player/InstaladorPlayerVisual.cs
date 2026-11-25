using UnityEngine;
using UnityEngine.SceneManagement;

//al arrancar el juego y cada vez que se carga una escena, busca al objeto "Player" y le monta el modelo animado + PlayerVisual.


public static class InstaladorPlayerVisual
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Arrancar()
    {
        Debug.Log("[PlayerVisual] Instalador arrancando en escena: " + SceneManager.GetActiveScene().name);
        // sceneLoaded se dispara cada vez que se carga una escena (Menu -> Z1 -> Z2...).
        SceneManager.sceneLoaded += Instalar;
        Instalar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Instalar(Scene escena, LoadSceneMode modo)
    {
        GameObject jugador = GameObject.Find("Player");
        if (jugador == null)
        {
            Debug.Log("[PlayerVisual] No hay objeto 'Player' en " + escena.name + "; nada que montar.");
            return;
        }

        // Si ya tiene el componente, no se vuelve a montar (por si acaso).
        if (jugador.GetComponent<PlayerVisual>() != null)
        {
            Debug.Log("[PlayerVisual] El Player ya tiene PlayerVisual; se omite.");
            return;
        }

        PlayerVisual visual = jugador.AddComponent<PlayerVisual>();

        // El modelo animado se carga desde Resources; lo crearemos al importar Mixamo.
        GameObject prefabModelo = Resources.Load<GameObject>("PlayerMixamo");
        if (prefabModelo == null)
        {
            Debug.LogWarning("[PlayerVisual] Resources/PlayerMixamo NO se encontro. Revisa que el prefab exista en Assets/GAME/Art/Player/Resources.");
            return;
        }

        GameObject modelo = Object.Instantiate(prefabModelo, jugador.transform);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        modelo.name = "ModeloPlayer";

        visual.Configurar(jugador.GetComponent<CharacterController>(), modelo);
        Debug.Log("[PlayerVisual] Modelo '" + modelo.name + "' montado en el Player.");
    }
}
