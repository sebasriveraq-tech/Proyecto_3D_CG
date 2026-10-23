using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager: el UNICO script que guarda el estado global del juego.
/// Vive en la escena del menu y sobrevive a los cambios de escena.
/// No contiene logica de escenas, jugador, agentes ni UI: solo ESTADO
/// y las funciones para leerlo/modificarlo desde cualquier otro script.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // SINGLETON
    // ------------------------------------------------------------------
    // "static" = pertenece a la clase, no a un objeto. Asi cualquier
    // script puede escribir GameManager.Instance desde cualquier parte.
    // "private set" = solo esta clase puede asignarlo (nadie mas puede
    // reemplazar el GameManager desde fuera).
    public static GameManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // ESTADO GLOBAL (esto es lo que despues se guarda en el JSON)
    // ------------------------------------------------------------------
    public int vidaJugador = 100;

    public List<string> inventario = new List<string>();        // items recogidos
    public List<string> codigos = new List<string>();           // codigos descubiertos

    public Dictionary<string, bool> puertas = new Dictionary<string, bool>();     // idPuerta -> abierta
    public Dictionary<string, bool> objetivos = new Dictionary<string, bool>();   // idObjetivo -> completado

    public string escenaActual;                // zona donde esta el jugador
    public float tiempoJugadoSegundos;         // tiempo total de partida

    // ------------------------------------------------------------------
    // AWAKE: se ejecuta UNA vez, antes de Start, al cargar la escena.
    // Aqui se configura el Singleton y se evita el duplicado.
    // ------------------------------------------------------------------
    private void Awake()
    {
        // Si ya existe un GameManager (por ejemplo al recargar la escena
        // del menu), este objeto nuevo es un DUPLICADO: se destruye.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Este es el GameManager oficial:
        Instance = this;
        DontDestroyOnLoad(gameObject); // no morir al cambiar de escena
    }

    private void Start()
    {
        // Anotar en que escena arranco el juego (por si se abre una zona directo).
        escenaActual = SceneManager.GetActiveScene().name;
    }

    private void Update()
    {
        // Sumar el tiempo real de juego (Time.deltaTime = segundos del ultimo frame).
        tiempoJugadoSegundos += Time.deltaTime;
    }

    // ------------------------------------------------------------------
    // INVENTARIO
    // ------------------------------------------------------------------
    public void AgregarItem(string id) { inventario.Add(id); }
    public bool TieneItem(string id) { return inventario.Contains(id); }

    // ------------------------------------------------------------------
    // CODIGOS
    // ------------------------------------------------------------------
    public void AgregarCodigo(string codigo) { codigos.Add(codigo); }
    public bool TieneCodigo(string codigo) { return codigos.Contains(codigo); }

    // ------------------------------------------------------------------
    // PUERTAS
    // ------------------------------------------------------------------
    public void AbrirPuerta(string idPuerta) { puertas[idPuerta] = true; }

    public bool PuertaAbierta(string idPuerta)
    {
        // Si la clave no existe, devuelve false en vez de dar error.
        return puertas.ContainsKey(idPuerta) && puertas[idPuerta];
    }

    // ------------------------------------------------------------------
    // OBJETIVOS
    // ------------------------------------------------------------------
    public void CompletarObjetivo(string idObjetivo) { objetivos[idObjetivo] = true; }

    public bool ObjetivoCompletado(string idObjetivo)
    {
        return objetivos.ContainsKey(idObjetivo) && objetivos[idObjetivo];
    }

    // ------------------------------------------------------------------
    // CAMBIO DE ESCENA: la UNICA forma oficial de pasar de zona.
    // Guarda la partida antes de cambiar (SaveManager viene despues).
    // ------------------------------------------------------------------
    public void CambiarEscena(string nombreEscena)
    {
        escenaActual = nombreEscena;
        // TODO Fase 2: SaveManager.Instance.Guardar();  (se agrega cuando exista)
        SceneManager.LoadScene(nombreEscena);
    }

    // ------------------------------------------------------------------
    // REINICIO: volver a la ultima escena guardada tras morir.
    // ------------------------------------------------------------------
    public void ReiniciarDesdeCheckpoint()
    {
        // TODO Fase 2: SaveManager.Instance.Cargar(); para restaurar vida/estado.
        SceneManager.LoadScene(escenaActual);
    }

    // ------------------------------------------------------------------
    // NUEVA PARTIDA: limpiar el estado en memoria (el borrado del
    // archivo JSON lo hara el menu con el SaveManager).
    // ------------------------------------------------------------------
    public void NuevaPartida()
    {
        vidaJugador = 100;
        inventario.Clear();
        codigos.Clear();
        puertas.Clear();
        objetivos.Clear();
        tiempoJugadoSegundos = 0f;
    }
}
