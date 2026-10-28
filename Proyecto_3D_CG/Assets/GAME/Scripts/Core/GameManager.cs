using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    // SINGLETON

    public static GameManager Instance { get; private set; }


    // ESTADO GLOBAL 

    public int vidaJugador = 100;

    public List<string> inventario = new List<string>();        // items recogidos
    public List<string> codigos = new List<string>();           // codigos descubiertos

    public Dictionary<string, bool> puertas = new Dictionary<string, bool>();     // idPuerta -> abierta
    public Dictionary<string, bool> objetivos = new Dictionary<string, bool>();   // idObjetivo -> completado

    public string escenaActual;                // zona donde esta el jugador
    public float tiempoJugadoSegundos;         // tiempo total de partida

   
    // AWAKE: se ejecuta UNA vez, antes de Start, al cargar la escena. Aqui se configura el Singleton y se evita el duplicado.
    
 
    private void Awake()
    {
       
        if (Instance != null && Instance != this) // Si ya existe un GameManager (por ejemplo al recargar la escena del menu), este objeto nuevo es un DUPLICADO: se destruye.
        {
            Destroy(gameObject);
            return;
        }

   
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
       
        escenaActual = SceneManager.GetActiveScene().name;
    }

    private void Update()
    {
        
        tiempoJugadoSegundos += Time.deltaTime; // Sumar el tiempo real de juego 
    }

    // INVENTARIO
    public void AgregarItem(string id) { if (!inventario.Contains(id)) inventario.Add(id); }
    public bool TieneItem(string id) { return inventario.Contains(id); }


    // CODIGOS
  
    public void AgregarCodigo(string codigo) { if (!codigos.Contains(codigo)) codigos.Add(codigo); }
    public bool TieneCodigo(string codigo) { return codigos.Contains(codigo); }

  
    // PUERTAS
   
    public void AbrirPuerta(string idPuerta) { puertas[idPuerta] = true; }

    public bool PuertaAbierta(string idPuerta)
    {
        
        return puertas.ContainsKey(idPuerta) && puertas[idPuerta]; // Si la clave no existe, devuelve false en vez de dar error.
    }

    
    // OBJETIVOS
   
    public void CompletarObjetivo(string idObjetivo) { objetivos[idObjetivo] = true; }

    public bool ObjetivoCompletado(string idObjetivo)
    {
        return objetivos.ContainsKey(idObjetivo) && objetivos[idObjetivo];
    }

    // CAMBIO DE ESCENA: la UNICA forma oficial de pasar de zona. Guarda la partida antes de cambiar.

    public void CambiarEscena(string nombreEscena)
    {
        escenaActual = nombreEscena;
        SaveManager.Guardar();            // checkpoint: se guarda justo antes de salir de la zona
        SceneManager.LoadScene(nombreEscena);
    }

    // VICTORIA: la llama el objeto final de la Zona 3. La partida termina:
    // se borra el guardado (ya no hay nada que continuar) y se muestra el panel de victoria.

    public void GanarPartida()
    {
        SaveManager.BorrarGuardado();

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarVictoria();
    }

  
    // REINICIO: al morir se recupera el progreso del ultimo checkpoint y se recarga la zona actual.

    public void ReiniciarDesdeCheckpoint()
    {
        SaveManager.Cargar();             // inventario, codigos, puertas y vida vuelven al checkpoint
        vidaJugador = 100;                // se revive con vida completa
        escenaActual = SceneManager.GetActiveScene().name; // se recarga la zona donde murio
        SceneManager.LoadScene(escenaActual);
    }

    // CONTINUAR: el menu lee el JSON y abre la zona donde se quedo el jugador.

    public bool HayPartidaGuardada()
    {
        return SaveManager.ExisteGuardado();
    }

    public void ContinuarPartida()
    {
        if (!SaveManager.ExisteGuardado()) return;

        SaveManager.Cargar();             // restaura el estado y la escena guardada
        SceneManager.LoadScene(escenaActual);
    }

    // NUEVA PARTIDA: limpiar el estado en memoria y borrar el archivo viejo.

    public void NuevaPartida()
    {
        vidaJugador = 100;
        inventario.Clear();
        codigos.Clear();
        puertas.Clear();
        objetivos.Clear();
        tiempoJugadoSegundos = 0f;
        SaveManager.BorrarGuardado();
    }
}
