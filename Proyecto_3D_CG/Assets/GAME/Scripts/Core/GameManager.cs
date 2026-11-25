using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    // SINGLETON

    public static GameManager Instance { get; private set; }


    // ESTADO GLOBAL 

    public int vidaJugador = 100;
    public int muertes;

    public List<string> inventario = new List<string>();        // items recogidos
    public List<string> codigos = new List<string>();           // codigos descubiertos

    // Modulos retirados de la terminal de recepcion: el ultimo sale primero (Stack LIFO).
    public Stack<string> modulosTerminalZ1 = new Stack<string>();
    public int cantidadModulosRetiradosZ1 = 0;

    // Bitacora FIFO de eventos y objetivos de la partida (Queue: Enqueue, Peek, Dequeue).
    public Queue<string> colaBitacora = new Queue<string>();

    public Dictionary<string, bool> puertas = new Dictionary<string, bool>();     // idPuerta -> abierta
    public Dictionary<string, bool> objetivos = new Dictionary<string, bool>();   // idObjetivo -> completado

    public string escenaActual;                // zona donde esta el jugador
    public float tiempoJugadoSegundos;         // tiempo total de partida

    private DatosGuardado checkpointZonaActual;
    private bool notificarCheckpointAlEntrar;
    private Coroutine rutinaNotificacionCheckpoint;

   
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
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        Scene escena = SceneManager.GetActiveScene();
        escenaActual = escena.name;
        if (EsEscenaJugable(escena.name) && checkpointZonaActual == null)
            InicializarCheckpointZona(escena.name);
    }

    private static bool EsEscenaJugable(string nombre)
    {
        return nombre == "Zona1_Recepcion"
            || nombre == "Zona2_Laboratorio"
            || nombre == "Zona3_Control";
    }

    private bool cambiandoEscena;

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        cambiandoEscena = false;
        if (!EsEscenaJugable(escena.name)) return;

        escenaActual = escena.name;
        InicializarCheckpointZona(escena.name);
    }

    private void InicializarCheckpointZona(string nombreEscena)
    {
        if (checkpointZonaActual == null || checkpointZonaActual.escenaActual != nombreEscena)
        {
            DatosGuardado disco = SaveManager.LeerDatos();
            if (disco != null && disco.escenaActual == nombreEscena)
            {
                checkpointZonaActual = disco;
            }
            else
            {
                checkpointZonaActual = SaveManager.CapturarEstadoActual(nombreEscena);
                SaveManager.GuardarDatos(checkpointZonaActual);
                notificarCheckpointAlEntrar = true;
            }
        }

        if (notificarCheckpointAlEntrar)
        {
            notificarCheckpointAlEntrar = false;
            if (rutinaNotificacionCheckpoint != null) StopCoroutine(rutinaNotificacionCheckpoint);
            rutinaNotificacionCheckpoint = StartCoroutine(MostrarNotificacionCheckpoint());
        }
    }

    private IEnumerator MostrarNotificacionCheckpoint()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarToastCheckpoint("Checkpoint guardado");
        rutinaNotificacionCheckpoint = null;
    }

    private void CargarEscenaConFade(string nombreEscena, float duracionFade = 0.75f)
    {
        if (cambiandoEscena) return;
        StartCoroutine(RutinaCargarEscenaConFade(nombreEscena, duracionFade));
    }

    private IEnumerator RutinaCargarEscenaConFade(string nombreEscena, float duracionFade)
    {
        cambiandoEscena = true;
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        if (cfg != null && cfg.MultiplicadorTransicionMusica > 0.02f)
            yield return cfg.FundirMusicaA(0f, duracionFade);

        cambiandoEscena = false;
        SceneManager.LoadScene(nombreEscena);
    }

    private void Update()
    {
        
        tiempoJugadoSegundos += Time.deltaTime; // Sumar el tiempo real de juego 
    }

    // BITACORA FIFO (Queue: Enqueue, Peek, Dequeue)
    public void RegistrarEventoBitacora(string evento)
    {
        if (string.IsNullOrEmpty(evento)) return;
        colaBitacora.Enqueue(evento);
        while (colaBitacora.Count > 10)
        {
            string antiguo = colaBitacora.Peek();
            colaBitacora.Dequeue();
        }
    }

    // INVENTARIO
    public void AgregarItem(string id)
    {
        if (!inventario.Contains(id))
        {
            inventario.Add(id);
            RegistrarEventoBitacora("Item:" + id);
        }
    }
    public bool TieneItem(string id) { return inventario.Contains(id); }


    // CODIGOS
  
    public void AgregarCodigo(string codigo)
    {
        if (!codigos.Contains(codigo))
        {
            codigos.Add(codigo);
            RegistrarEventoBitacora("Codigo:" + codigo);
        }
    }
    public bool TieneCodigo(string codigo) { return codigos.Contains(codigo); }

  
    // PUERTAS
   
    public void AbrirPuerta(string idPuerta)
    {
        puertas[idPuerta] = true;
        RegistrarEventoBitacora("Puerta:" + idPuerta);
    }

    public bool PuertaAbierta(string idPuerta)
    {
        
        return puertas.ContainsKey(idPuerta) && puertas[idPuerta]; // Si la clave no existe, devuelve false en vez de dar error.
    }

    
    // OBJETIVOS
   
    public void CompletarObjetivo(string idObjetivo)
    {
        objetivos[idObjetivo] = true;
        RegistrarEventoBitacora("Objetivo:" + idObjetivo);
    }

    public bool ObjetivoCompletado(string idObjetivo)
    {
        return objetivos.ContainsKey(idObjetivo) && objetivos[idObjetivo];
    }

    // CAMBIO DE ESCENA: la UNICA forma oficial de pasar de zona. Guarda la partida antes de cambiar.

    public void CambiarEscena(string nombreEscena)
    {
        if (cambiandoEscena) return;

        escenaActual = nombreEscena;
        if (EsEscenaJugable(nombreEscena))
        {
            checkpointZonaActual = SaveManager.CapturarEstadoActual(nombreEscena);
            SaveManager.GuardarDatos(checkpointZonaActual);
            notificarCheckpointAlEntrar = true;
        }
        CargarEscenaConFade(nombreEscena, 0.75f);
    }

    // VICTORIA: la llama el objeto final de la Zona 3. La partida termina:
    // se borra el guardado (ya no hay nada que continuar) y se muestra el panel de victoria.

    public void GanarPartida()
    {
        SaveManager.BorrarGuardado();

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarVictoria();
    }

  
    // REINICIO: al morir se recupera el progreso previo al inicio de la zona y se recarga la zona actual.

    public void ReiniciarDesdeCheckpoint()
    {
        if (cambiandoEscena) return;

        string zonaActual = SceneManager.GetActiveScene().name;
        DatosGuardado snap = (checkpointZonaActual != null && checkpointZonaActual.escenaActual == zonaActual)
            ? checkpointZonaActual
            : null;

        if (snap == null)
        {
            DatosGuardado disco = SaveManager.LeerDatos();
            if (disco != null && disco.escenaActual == zonaActual)
                snap = disco;
        }

        if (snap != null)
            SaveManager.AplicarDatos(snap, true);

        vidaJugador = 100;                // se revive con vida completa
        escenaActual = zonaActual;        // se recarga la zona donde murio
        notificarCheckpointAlEntrar = false;
        Time.timeScale = 1f;
        CargarEscenaConFade(zonaActual, 0.45f);
    }

    // CONTINUAR: el menu lee el JSON y abre la zona donde se quedo el jugador.

    public bool HayPartidaGuardada()
    {
        return SaveManager.ExisteGuardado();
    }

    public void ContinuarPartida()
    {
        if (cambiandoEscena) return;

        DatosGuardado datos = SaveManager.LeerDatos();
        if (datos == null) return;

        checkpointZonaActual = datos;
        SaveManager.AplicarDatos(datos, false);
        notificarCheckpointAlEntrar = false;
        CargarEscenaConFade(escenaActual, 0.45f);
    }

    public void IrAlMenuConFade()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CargarEscenaConFade("Menu", 0.55f);
    }

    // NUEVA PARTIDA: limpiar el estado en memoria y borrar el archivo viejo.

    public void NuevaPartida()
    {
        vidaJugador = 100;
        inventario.Clear();
        codigos.Clear();
        modulosTerminalZ1.Clear();
        cantidadModulosRetiradosZ1 = 0;
        colaBitacora.Clear();
        puertas.Clear();
        objetivos.Clear();
        tiempoJugadoSegundos = 0f;
        muertes = 0;
        checkpointZonaActual = null;
        notificarCheckpointAlEntrar = false;
        SaveManager.BorrarGuardado();
    }
}
