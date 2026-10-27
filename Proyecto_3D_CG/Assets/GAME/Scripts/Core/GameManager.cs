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
    public void AgregarItem(string id) { inventario.Add(id); }
    public bool TieneItem(string id) { return inventario.Contains(id); }


    // CODIGOS
  
    public void AgregarCodigo(string codigo) { codigos.Add(codigo); }
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

    // CAMBIO DE ESCENA: la UNICA forma oficial de pasar de zona.Guarda la partida antes de cambiar.
    

    public void CambiarEscena(string nombreEscena)
    {
        escenaActual = nombreEscena;
     
        SceneManager.LoadScene(nombreEscena);
    }

  
    // REINICIO: volver a la ultima escena guardada tras morir.

    public void ReiniciarDesdeCheckpoint()
    {

        SceneManager.LoadScene(escenaActual);
    }

    // NUEVA PARTIDA: limpiar el estado en memoria (el borrado del archivo JSON lo hara el menu con el SaveManager). 

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
