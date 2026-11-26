using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PanelMiniJuegoZ2 : MonoBehaviour
{
    public enum TipoMiniJuego
    {
        ReconexionCircuitos,
        RecuperacionDatos
    }

    [Serializable]
    public struct VistaCeldaCable
    {
        public Button boton;
        public Image fondo;
        public Image centro;
        public Image brazoNorte;
        public Image brazoEste;
        public Image brazoSur;
        public Image brazoOeste;
        public TextMeshProUGUI etiquetaTecla;
        public TextMeshProUGUI etiquetaGrados;
    }

    [Serializable]
    public struct VistaPasoPatron
    {
        public Image fondo;
        public Outline borde;
        public TextMeshProUGUI etiquetaPaso;
        public TextMeshProUGUI textoSimbolo;
        public TextMeshProUGUI etiquetaEstado;
    }

    [Serializable]
    public struct VistaBotonSimbolo
    {
        public Button boton;
        public Image fondo;
        public Outline borde;
        public TextMeshProUGUI textoSimbolo;
        public TextMeshProUGUI textoTecla;
    }

    [Header("Configuración General")]
    [SerializeField] private TipoMiniJuego tipo = TipoMiniJuego.ReconexionCircuitos;
    [SerializeField] private GameObject raizPanel;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoObjetivoDesbloqueo;
    [SerializeField] private TextMeshProUGUI textoInstruccionPrincipal;
    [SerializeField] private TextMeshProUGUI textoEstado;
    [SerializeField] private Image fondoBannerEstado;
    [SerializeField] private Button botonCerrar;

    [Header("Circuitos 3x3 (Terminal de Energía)")]
    [SerializeField] private VistaCeldaCable[] vistasCeldas = new VistaCeldaCable[9];
    [SerializeField] private Image indicadorSalidaFondo;
    [SerializeField] private TextMeshProUGUI indicadorSalidaTexto;

    [Header("Secuencia de Símbolos (Terminal de Servidores)")]
    [SerializeField] private TextMeshProUGUI textoProgresoPasos;
    [SerializeField] private Image fondoBadgeProgreso;
    [SerializeField] private VistaPasoPatron[] vistasPatron = new VistaPasoPatron[4];
    [SerializeField] private VistaBotonSimbolo[] vistasBotonesSimbolos = new VistaBotonSimbolo[6];

    private Action alCompletar;
    private Transform origenAudio;
    private string codigoResultado = "LAB-2381";
    private bool completado;
    private bool esteAbierto;
    private static PanelMiniJuegoZ2 instanciaAbierta;
    private bool botonesEnlazados;

    // Persistencia en memoria al cerrar y volver a abrir la terminal
    private bool estadoInicializado;
    private bool reabiertoConProgreso;

    // Direcciones de conexión de cada celda: 0 = Norte, 1 = Este, 2 = Sur, 3 = Oeste
    private const int DIR_NORTE = 0;
    private const int DIR_ESTE = 1;
    private const int DIR_SUR = 2;
    private const int DIR_OESTE = 3;

    private readonly bool[,,] puertosBase = new bool[3, 3, 4];
    private readonly int[,] rotacionInicial = new int[3, 3];
    private readonly int[,] rotacionCelda = new int[3, 3];
    private readonly bool[,] celdaEnergizada = new bool[3, 3];

    private static readonly string[] SimbolosDisponibles = { "A-1", "B-2", "C-3", "D-4", "E-6", "F-8" };
    private static readonly int[] PatronObjetivo = { 1, 2, 5, 0 };

    private int pasoSimbolosActual;
    private int ultimoBotonAcertado = -1;
    private float tiempoFlashAcierto;
    private int ultimoBotonError = -1;
    private float tiempoFlashError;

    public bool EstaCompletado => completado;

    private GameObject PanelObjetivo => raizPanel != null ? raizPanel : gameObject;

    private void Awake()
    {
        EnlazarBotones();
    }

    public void Configurar(TipoMiniJuego tipoJuego, string codigo, bool yaCompletado, Action callbackCompletado, Transform fuenteAudio = null)
    {
        tipo = tipoJuego;
        if (!string.IsNullOrEmpty(codigo))
            codigoResultado = codigo;
        completado = yaCompletado;
        alCompletar = callbackCompletado;
        origenAudio = fuenteAudio;

        if (yaCompletado && tipo == TipoMiniJuego.RecuperacionDatos)
            pasoSimbolosActual = 4;

        EnlazarBotones();
    }

    public void MarcarCompletadoExternamente(bool valor)
    {
        completado = valor;
        if (valor && tipo == TipoMiniJuego.RecuperacionDatos)
            pasoSimbolosActual = 4;
        if (PanelObjetivo.activeSelf)
            RefrescarVista();
    }

    public void Abrir()
    {
        if (Time.timeScale == 0f || EstaEnPantallaModal() || ArmarioEscondite.JugadorOculto) return;
        if (instanciaAbierta != null && instanciaAbierta != this)
            instanciaAbierta.Cerrar(false);

        EnlazarBotones();

        if (!estadoInicializado)
        {
            if (tipo == TipoMiniJuego.ReconexionCircuitos)
                InicializarCuadriculaCircuitos();
            else if (!completado)
                pasoSimbolosActual = 0;

            estadoInicializado = true;
            reabiertoConProgreso = false;
        }
        else
        {
            reabiertoConProgreso = !completado && TieneProgresoParcial();
        }

        if (completado && tipo == TipoMiniJuego.RecuperacionDatos)
            pasoSimbolosActual = 4;

        ultimoBotonAcertado = -1;
        ultimoBotonError = -1;
        tiempoFlashAcierto = 0f;
        tiempoFlashError = 0f;

        PanelObjetivo.SetActive(true);
        esteAbierto = true;
        instanciaAbierta = this;
        PanelTerminalZ1.AbiertoZ2 = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefrescarVista();
    }

    private bool TieneProgresoParcial()
    {
        if (tipo == TipoMiniJuego.RecuperacionDatos)
            return pasoSimbolosActual > 0 && pasoSimbolosActual < 4;

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (rotacionCelda[r, c] != rotacionInicial[r, c])
                    return true;
        return false;
    }

    public void Cerrar()
    {
        Cerrar(true);
    }

    public void Cerrar(bool restaurarCursorJuego)
    {
        bool reproducirCierre = esteAbierto && restaurarCursorJuego;
        if (esteAbierto)
        {
            esteAbierto = false;
            PanelTerminalZ1.AbiertoZ2 = false;
        }
        if (instanciaAbierta == this)
            instanciaAbierta = null;

        if (PanelObjetivo.activeSelf)
            PanelObjetivo.SetActive(false);

        if (reproducirCierre)
            LabAudioFeedback.ReproducirPanelUI(false);

        if (restaurarCursorJuego && !EstaEnPantallaModal() && Time.timeScale > 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public static void CerrarPanelActivoPorEventoExterno()
    {
        if (instanciaAbierta != null)
            instanciaAbierta.Cerrar(false);
        PanelTerminalZ1.AbiertoZ2 = false;
    }

    private static bool EstaEnPantallaModal()
    {
        UIManager ui = UIManager.Instance;
        if (ui == null) return false;
        return (ui.panelDerrota != null && ui.panelDerrota.activeSelf)
            || (ui.panelVictoria != null && ui.panelVictoria.activeSelf)
            || (ui.panelPausa != null && ui.panelPausa.activeSelf)
            || (ui.panelMenu != null && ui.panelMenu.activeSelf)
            || (ui.panelAjustes != null && ui.panelAjustes.activeSelf)
            || (ui.panelComoJugar != null && ui.panelComoJugar.activeSelf);
    }

    private void OnDisable()
    {
        if (esteAbierto)
        {
            esteAbierto = false;
            PanelTerminalZ1.AbiertoZ2 = false;
        }
        if (instanciaAbierta == this)
            instanciaAbierta = null;
    }

    private void EnlazarBotones()
    {
        if (botonesEnlazados) return;
        botonesEnlazados = true;

        if (botonCerrar != null)
        {
            botonCerrar.onClick.RemoveAllListeners();
            botonCerrar.onClick.AddListener(Cerrar);
        }

        if (vistasCeldas != null)
        {
            for (int i = 0; i < vistasCeldas.Length && i < 9; i++)
            {
                int r = i / 3;
                int c = i % 3;
                Button btn = vistasCeldas[i].boton;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => RotarCeldaCable(r, c));
                }
            }
        }

        if (vistasBotonesSimbolos != null)
        {
            for (int b = 0; b < vistasBotonesSimbolos.Length && b < 6; b++)
            {
                int idxSimbolo = b;
                Button btn = vistasBotonesSimbolos[b].boton;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ProcesarEntradaSimbolo(idxSimbolo, idxSimbolo));
                }
            }
        }
    }

    private Vector3 PosicionAudio()
    {
        if (origenAudio != null) return origenAudio.position;
        if (Camera.main != null) return Camera.main.transform.position;
        return Vector3.zero;
    }

    private void Update()
    {
        if (!esteAbierto || !PanelObjetivo.activeSelf) return;
        if (Time.timeScale == 0f || EstaEnPantallaModal() || ArmarioEscondite.JugadorOculto)
        {
            Cerrar(false);
            return;
        }

        bool refrescarPorTimer = false;
        if (tiempoFlashAcierto > 0f)
        {
            tiempoFlashAcierto -= Time.unscaledDeltaTime;
            if (tiempoFlashAcierto <= 0f)
            {
                tiempoFlashAcierto = 0f;
                ultimoBotonAcertado = -1;
                refrescarPorTimer = true;
            }
        }
        if (tiempoFlashError > 0f)
        {
            tiempoFlashError -= Time.unscaledDeltaTime;
            if (tiempoFlashError <= 0f)
            {
                tiempoFlashError = 0f;
                ultimoBotonError = -1;
                refrescarPorTimer = true;
            }
        }
        if (refrescarPorTimer)
            RefrescarVista();

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclado.escapeKey.wasPressedThisFrame)
        {
            Cerrar();
            return;
        }

        if (completado) return;

        if (tipo == TipoMiniJuego.ReconexionCircuitos)
        {
            if (teclado.digit1Key.wasPressedThisFrame || teclado.numpad1Key.wasPressedThisFrame) RotarCeldaCable(0, 0);
            else if (teclado.digit2Key.wasPressedThisFrame || teclado.numpad2Key.wasPressedThisFrame) RotarCeldaCable(0, 1);
            else if (teclado.digit3Key.wasPressedThisFrame || teclado.numpad3Key.wasPressedThisFrame) RotarCeldaCable(0, 2);
            else if (teclado.digit4Key.wasPressedThisFrame || teclado.numpad4Key.wasPressedThisFrame) RotarCeldaCable(1, 0);
            else if (teclado.digit5Key.wasPressedThisFrame || teclado.numpad5Key.wasPressedThisFrame) RotarCeldaCable(1, 1);
            else if (teclado.digit6Key.wasPressedThisFrame || teclado.numpad6Key.wasPressedThisFrame) RotarCeldaCable(1, 2);
            else if (teclado.digit7Key.wasPressedThisFrame || teclado.numpad7Key.wasPressedThisFrame) RotarCeldaCable(2, 0);
            else if (teclado.digit8Key.wasPressedThisFrame || teclado.numpad8Key.wasPressedThisFrame) RotarCeldaCable(2, 1);
            else if (teclado.digit9Key.wasPressedThisFrame || teclado.numpad9Key.wasPressedThisFrame) RotarCeldaCable(2, 2);
        }
        else
        {
            if (teclado.digit1Key.wasPressedThisFrame || teclado.numpad1Key.wasPressedThisFrame) ProcesarEntradaSimbolo(0, 0);
            else if (teclado.digit2Key.wasPressedThisFrame || teclado.numpad2Key.wasPressedThisFrame) ProcesarEntradaSimbolo(1, 1);
            else if (teclado.digit3Key.wasPressedThisFrame || teclado.numpad3Key.wasPressedThisFrame) ProcesarEntradaSimbolo(2, 2);
            else if (teclado.digit4Key.wasPressedThisFrame || teclado.numpad4Key.wasPressedThisFrame) ProcesarEntradaSimbolo(3, 3);
            else if (teclado.digit5Key.wasPressedThisFrame || teclado.numpad5Key.wasPressedThisFrame) ProcesarEntradaSimbolo(4, 4);
            else if (teclado.digit6Key.wasPressedThisFrame || teclado.numpad6Key.wasPressedThisFrame) ProcesarEntradaSimbolo(5, 5);
        }
    }

    // Minijuego 1: cuadrícula 3x3 de rotación de cables
    private void InicializarCuadriculaCircuitos()
    {
        ConfigurarPiezaBase(0, 0, false, true, true, true);
        ConfigurarPiezaBase(0, 1, false, true, true, true);
        ConfigurarPiezaBase(0, 2, false, false, true, true);

        ConfigurarPiezaBase(1, 0, true, false, true, false);
        ConfigurarPiezaBase(1, 1, true, false, true, false);
        ConfigurarPiezaBase(1, 2, true, false, true, false);

        ConfigurarPiezaBase(2, 0, true, true, false, false);
        ConfigurarPiezaBase(2, 1, true, true, false, true);
        ConfigurarPiezaBase(2, 2, true, true, false, true);

        int[,] inicio =
        {
            { 1, 2, 1 },
            { 1, 1, 1 },
            { 2, 2, 2 }
        };

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                rotacionInicial[r, c] = inicio[r, c];
                rotacionCelda[r, c] = completado ? 0 : inicio[r, c];
            }
        }

        PropagarCorrienteBFS();
    }

    private void ConfigurarPiezaBase(int fila, int col, bool norte, bool este, bool sur, bool oeste)
    {
        puertosBase[fila, col, DIR_NORTE] = norte;
        puertosBase[fila, col, DIR_ESTE] = este;
        puertosBase[fila, col, DIR_SUR] = sur;
        puertosBase[fila, col, DIR_OESTE] = oeste;
    }

    private bool TieneBrazoEnDireccion(int fila, int col, int direccion)
    {
        int giros = ((rotacionCelda[fila, col] % 4) + 4) % 4;
        int dirOriginal = (direccion - giros + 4) % 4;
        return puertosBase[fila, col, dirOriginal];
    }

    private void RotarCeldaCable(int fila, int col)
    {
        if (completado) return;

        reabiertoConProgreso = false;
        rotacionCelda[fila, col] = (rotacionCelda[fila, col] + 1) % 4;
        LabAudioFeedback.ReproducirTono(PosicionAudio(), 620f + (fila * 3 + col) * 35f, 0.07f, 0.30f);

        bool llegaASalida = PropagarCorrienteBFS();
        if (llegaASalida)
        {
            completado = true;
            LabAudioFeedback.ReproducirSistemaRestaurado(PosicionAudio());
            alCompletar?.Invoke();
        }

        RefrescarVista();
    }

    // Recorre las celdas conectadas usando Queue<Vector2Int> con búsqueda en anchura (BFS)
    private bool PropagarCorrienteBFS()
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                celdaEnergizada[r, c] = false;

        if (completado)
        {
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    celdaEnergizada[r, c] = true;
            return true;
        }

        if (!TieneBrazoEnDireccion(0, 0, DIR_OESTE))
            return false;

        Queue<Vector2Int> cola = new Queue<Vector2Int>();
        celdaEnergizada[0, 0] = true;
        cola.Enqueue(new Vector2Int(0, 0));

        while (cola.Count > 0)
        {
            Vector2Int actual = cola.Dequeue();
            int f = actual.x;
            int c = actual.y;

            if (f > 0 && !celdaEnergizada[f - 1, c] &&
                TieneBrazoEnDireccion(f, c, DIR_NORTE) &&
                TieneBrazoEnDireccion(f - 1, c, DIR_SUR))
            {
                celdaEnergizada[f - 1, c] = true;
                cola.Enqueue(new Vector2Int(f - 1, c));
            }
            if (f < 2 && !celdaEnergizada[f + 1, c] &&
                TieneBrazoEnDireccion(f, c, DIR_SUR) &&
                TieneBrazoEnDireccion(f + 1, c, DIR_NORTE))
            {
                celdaEnergizada[f + 1, c] = true;
                cola.Enqueue(new Vector2Int(f + 1, c));
            }
            if (c > 0 && !celdaEnergizada[f, c - 1] &&
                TieneBrazoEnDireccion(f, c, DIR_OESTE) &&
                TieneBrazoEnDireccion(f, c - 1, DIR_ESTE))
            {
                celdaEnergizada[f, c - 1] = true;
                cola.Enqueue(new Vector2Int(f, c - 1));
            }
            if (c < 2 && !celdaEnergizada[f, c + 1] &&
                TieneBrazoEnDireccion(f, c, DIR_ESTE) &&
                TieneBrazoEnDireccion(f, c + 1, DIR_OESTE))
            {
                celdaEnergizada[f, c + 1] = true;
                cola.Enqueue(new Vector2Int(f, c + 1));
            }
        }

        return celdaEnergizada[2, 2] && TieneBrazoEnDireccion(2, 2, DIR_ESTE);
    }

    // Minijuego 2: secuencia de 4 símbolos
    private void ProcesarEntradaSimbolo(int idxSimbolo, int idxBoton)
    {
        if (completado || pasoSimbolosActual < 0 || pasoSimbolosActual >= 4) return;

        reabiertoConProgreso = false;

        if (idxSimbolo == PatronObjetivo[pasoSimbolosActual])
        {
            tiempoFlashError = 0f;
            ultimoBotonError = -1;
            ultimoBotonAcertado = idxBoton;
            tiempoFlashAcierto = 0.42f;

            LabAudioFeedback.ReproducirTono(PosicionAudio(), 680f + pasoSimbolosActual * 110f, 0.09f, 0.36f);
            pasoSimbolosActual++;

            if (pasoSimbolosActual >= 4)
            {
                completado = true;
                LabAudioFeedback.ReproducirSistemaRestaurado(PosicionAudio());
                alCompletar?.Invoke();
            }
        }
        else
        {
            tiempoFlashAcierto = 0f;
            ultimoBotonAcertado = -1;
            ultimoBotonError = idxBoton;
            tiempoFlashError = 0.60f;
            LabAudioFeedback.ReproducirAccesoDenegado(PosicionAudio());
        }

        RefrescarVista();
    }

    // Actualización visual de los elementos del panel
    private void RefrescarVista()
    {
        if (!PanelObjetivo.activeSelf) return;

        if (tipo == TipoMiniJuego.ReconexionCircuitos)
        {
            if (textoTitulo != null)
                textoTitulo.text = "TERMINAL DE ENERGÍA — CUADRÍCULA DE CIRCUITOS";
            if (textoObjetivoDesbloqueo != null)
                textoObjetivoDesbloqueo.text = "DESBLOQUEA: Restaurar energía de la salida y activar los servidores";
            if (textoInstruccionPrincipal != null)
                textoInstruccionPrincipal.text = "ROTA LAS PIEZAS DE CABLE (CLIC O 1-9) PARA CONECTAR ENTRADA CON SALIDA";

            PropagarCorrienteBFS();

            Color colorEnergizado = completado
                ? new Color(0.20f, 0.94f, 0.52f, 1f)
                : new Color(0.18f, 0.88f, 1f, 1f);
            Color colorSinCorriente = new Color(0.36f, 0.44f, 0.52f, 1f);

            if (vistasCeldas != null && vistasCeldas.Length >= 9)
            {
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        VistaCeldaCable v = vistasCeldas[r * 3 + c];
                        bool activa = celdaEnergizada[r, c];

                        Color cCable = activa ? colorEnergizado : colorSinCorriente;
                        if (v.fondo != null)
                        {
                            v.fondo.color = activa
                                ? (completado ? new Color(0.08f, 0.28f, 0.18f, 1f) : new Color(0.08f, 0.24f, 0.34f, 1f))
                                : new Color(0.09f, 0.13f, 0.18f, 1f);
                        }

                        if (v.brazoNorte != null) { v.brazoNorte.gameObject.SetActive(TieneBrazoEnDireccion(r, c, DIR_NORTE)); v.brazoNorte.color = cCable; }
                        if (v.brazoEste != null) { v.brazoEste.gameObject.SetActive(TieneBrazoEnDireccion(r, c, DIR_ESTE)); v.brazoEste.color = cCable; }
                        if (v.brazoSur != null) { v.brazoSur.gameObject.SetActive(TieneBrazoEnDireccion(r, c, DIR_SUR)); v.brazoSur.color = cCable; }
                        if (v.brazoOeste != null) { v.brazoOeste.gameObject.SetActive(TieneBrazoEnDireccion(r, c, DIR_OESTE)); v.brazoOeste.color = cCable; }
                        if (v.centro != null) v.centro.color = activa ? Color.white : new Color(0.55f, 0.65f, 0.74f, 1f);
                        if (v.etiquetaGrados != null) v.etiquetaGrados.text = ((rotacionCelda[r, c] % 4) * 90) + "°";
                    }
                }
            }

            if (indicadorSalidaFondo != null && indicadorSalidaTexto != null)
            {
                indicadorSalidaFondo.color = completado
                    ? new Color(0.10f, 0.48f, 0.26f, 1f)
                    : new Color(0.14f, 0.20f, 0.26f, 0.98f);
                indicadorSalidaTexto.text = completado
                    ? "-> SALIDA\n<size=14>CORRIENTE 100%</size>"
                    : "-> SALIDA\n<size=14>SIN CORRIENTE</size>";
                indicadorSalidaTexto.color = completado
                    ? new Color(0.45f, 1f, 0.68f)
                    : new Color(0.75f, 0.82f, 0.88f);
            }

            if (fondoBannerEstado != null && textoEstado != null)
            {
                if (completado)
                {
                    fondoBannerEstado.color = new Color(0.08f, 0.40f, 0.22f, 0.98f);
                    textoEstado.text = "¡SISTEMA RESTAURADO! — ENERGÍA DE LA SALIDA Y SERVIDORES ACTIVA";
                    textoEstado.color = new Color(0.45f, 1f, 0.68f);
                }
                else if (reabiertoConProgreso)
                {
                    fondoBannerEstado.color = new Color(0.09f, 0.24f, 0.34f, 0.98f);
                    textoEstado.text = "PROGRESO CONSERVADO — Continúas desde el circuito que dejaste";
                    textoEstado.color = new Color(0.52f, 0.94f, 1f);
                }
                else
                {
                    fondoBannerEstado.color = new Color(0.08f, 0.16f, 0.22f, 0.98f);
                    textoEstado.text = "ESTADO: Conecta la corriente azul hasta SALIDA (tu avance se guarda al salir)";
                    textoEstado.color = new Color(1f, 0.82f, 0.34f);
                }
            }
        }
        else
        {
            if (textoTitulo != null)
                textoTitulo.text = "TERMINAL DE SERVIDORES — DESCIFRADO DE SECUENCIA";
            if (textoObjetivoDesbloqueo != null)
                textoObjetivoDesbloqueo.text = $"DESBLOQUEA: Compuerta de salida hacia el Sector 3 (Código {codigoResultado})";
            if (textoInstruccionPrincipal != null)
                textoInstruccionPrincipal.text = "INGRESA LA SECUENCIA DE 4 SÍMBOLOS SEGÚN EL PATRÓN";

            int pasoVisible = completado ? 4 : Mathf.Clamp(pasoSimbolosActual + 1, 1, 4);
            if (textoProgresoPasos != null && fondoBadgeProgreso != null)
            {
                if (completado)
                {
                    fondoBadgeProgreso.color = new Color(0.08f, 0.40f, 0.22f, 1f);
                    textoProgresoPasos.text = "COMPLETADO: PASO 4 / 4";
                    textoProgresoPasos.color = new Color(0.45f, 1f, 0.68f);
                }
                else
                {
                    fondoBadgeProgreso.color = new Color(0.08f, 0.24f, 0.36f, 1f);
                    textoProgresoPasos.text = $"PROGRESO: PASO {pasoVisible} / 4";
                    textoProgresoPasos.color = new Color(1f, 0.90f, 0.36f);
                }
            }

            if (vistasPatron != null && vistasPatron.Length >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    VistaPasoPatron vp = vistasPatron[i];
                    bool yaAcertado = completado || i < pasoSimbolosActual;
                    bool esActual = !completado && i == pasoSimbolosActual;
                    bool enError = esActual && tiempoFlashError > 0f;

                    if (yaAcertado)
                    {
                        if (vp.fondo != null) vp.fondo.color = new Color(0.08f, 0.42f, 0.22f, 1f);
                        if (vp.borde != null) vp.borde.effectColor = new Color(0.26f, 1f, 0.56f, 1f);
                        if (vp.textoSimbolo != null) vp.textoSimbolo.color = Color.white;
                        if (vp.etiquetaEstado != null) { vp.etiquetaEstado.text = "[OK] ACERTADO"; vp.etiquetaEstado.color = new Color(0.45f, 1f, 0.68f); }
                    }
                    else if (enError)
                    {
                        if (vp.fondo != null) vp.fondo.color = new Color(0.56f, 0.10f, 0.12f, 1f);
                        if (vp.borde != null) vp.borde.effectColor = new Color(1f, 0.30f, 0.30f, 1f);
                        if (vp.textoSimbolo != null) vp.textoSimbolo.color = Color.white;
                        if (vp.etiquetaEstado != null) { vp.etiquetaEstado.text = "[X] ERROR"; vp.etiquetaEstado.color = new Color(1f, 0.70f, 0.70f); }
                    }
                    else if (esActual)
                    {
                        if (vp.fondo != null) vp.fondo.color = new Color(0.07f, 0.24f, 0.38f, 1f);
                        if (vp.borde != null) vp.borde.effectColor = new Color(1f, 0.84f, 0.22f, 1f);
                        if (vp.textoSimbolo != null) vp.textoSimbolo.color = new Color(1f, 0.92f, 0.38f);
                        if (vp.etiquetaEstado != null) { vp.etiquetaEstado.text = ">> INGRESA ESTE"; vp.etiquetaEstado.color = new Color(1f, 0.88f, 0.36f); }
                    }
                    else
                    {
                        if (vp.fondo != null) vp.fondo.color = new Color(0.09f, 0.14f, 0.20f, 1f);
                        if (vp.borde != null) vp.borde.effectColor = new Color(0.32f, 0.46f, 0.60f, 0.7f);
                        if (vp.textoSimbolo != null) vp.textoSimbolo.color = new Color(0.86f, 0.92f, 0.98f);
                        if (vp.etiquetaEstado != null) { vp.etiquetaEstado.text = "PENDIENTE"; vp.etiquetaEstado.color = new Color(0.65f, 0.76f, 0.86f); }
                    }
                }
            }

            if (vistasBotonesSimbolos != null && vistasBotonesSimbolos.Length >= 6)
            {
                for (int b = 0; b < 6; b++)
                {
                    VistaBotonSimbolo vb = vistasBotonesSimbolos[b];
                    if (vb.boton != null) vb.boton.interactable = !completado;

                    if (completado)
                    {
                        if (vb.fondo != null) vb.fondo.color = new Color(0.08f, 0.32f, 0.18f, 1f);
                        if (vb.borde != null) vb.borde.effectColor = new Color(0.26f, 0.96f, 0.56f, 0.9f);
                        if (vb.textoSimbolo != null) vb.textoSimbolo.color = new Color(0.70f, 1f, 0.82f);
                    }
                    else if (b == ultimoBotonError && tiempoFlashError > 0f)
                    {
                        if (vb.fondo != null) vb.fondo.color = new Color(0.66f, 0.11f, 0.14f, 1f);
                        if (vb.borde != null) vb.borde.effectColor = new Color(1f, 0.34f, 0.34f, 1f);
                        if (vb.textoSimbolo != null) vb.textoSimbolo.color = Color.white;
                    }
                    else if (b == ultimoBotonAcertado && tiempoFlashAcierto > 0f)
                    {
                        if (vb.fondo != null) vb.fondo.color = new Color(0.10f, 0.52f, 0.26f, 1f);
                        if (vb.borde != null) vb.borde.effectColor = new Color(0.30f, 1f, 0.58f, 1f);
                        if (vb.textoSimbolo != null) vb.textoSimbolo.color = Color.white;
                    }
                    else
                    {
                        if (vb.fondo != null) vb.fondo.color = new Color(0.09f, 0.17f, 0.26f, 1f);
                        if (vb.borde != null) vb.borde.effectColor = new Color(0.38f, 0.78f, 0.96f, 0.85f);
                        if (vb.textoSimbolo != null) vb.textoSimbolo.color = Color.white;
                    }
                }
            }

            if (fondoBannerEstado != null && textoEstado != null)
            {
                if (completado)
                {
                    fondoBannerEstado.color = new Color(0.08f, 0.40f, 0.22f, 0.98f);
                    textoEstado.text = $"¡SISTEMA RESTAURADO! — CÓDIGO {codigoResultado} OBTENIDO (COMPUERTA DESBLOQUEADA)";
                    textoEstado.color = new Color(0.45f, 1f, 0.68f);
                }
                else if (tiempoFlashError > 0f)
                {
                    fondoBannerEstado.color = new Color(0.54f, 0.10f, 0.12f, 0.98f);
                    textoEstado.text = $"[X] SÍMBOLO INCORRECTO — Sigue el patrón marcado arriba (Paso {pasoVisible} / 4)";
                    textoEstado.color = Color.white;
                }
                else if (tiempoFlashAcierto > 0f)
                {
                    fondoBannerEstado.color = new Color(0.08f, 0.38f, 0.22f, 0.98f);
                    textoEstado.text = $"[OK] SÍMBOLO CORRECTO — Continúa con el Paso {pasoVisible} / 4";
                    textoEstado.color = new Color(0.45f, 1f, 0.68f);
                }
                else if (reabiertoConProgreso)
                {
                    fondoBannerEstado.color = new Color(0.09f, 0.24f, 0.34f, 0.98f);
                    textoEstado.text = $"PROGRESO CONSERVADO — Retomas exactamente en el Paso {pasoVisible} / 4";
                    textoEstado.color = new Color(0.52f, 0.94f, 1f);
                }
                else
                {
                    fondoBannerEstado.color = new Color(0.08f, 0.16f, 0.22f, 0.98f);
                    string objetivoTxt = SimbolosDisponibles[PatronObjetivo[pasoSimbolosActual]];
                    textoEstado.text = $"Ingresa el símbolo del Paso {pasoVisible} / 4 ({objetivoTxt}) — Tu progreso se guarda al salir";
                    textoEstado.color = new Color(0.88f, 0.96f, 1f);
                }
            }
        }
    }
}
