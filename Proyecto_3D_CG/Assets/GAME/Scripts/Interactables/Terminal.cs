using UnityEngine;

/// Terminal: entrega un codigo. La terminal de recepcion primero se repara con una pila.
public class Terminal : Interactable
{
    [Header("Codigo que entrega")]
    public string idCodigo = "codigo_a";
    public string codigoVisible = "A-4721";
    public bool requiereEnergia;
    public LaboratorioController laboratorio;

    private const string objetivoReparacionZ1 = "terminal_reparada_z1";
    private readonly string[] modulosZ1 = { "A", "B", "C" };
    private PanelTerminalZ1 panelReparacion;
    private PantallaTerminalZ1 pantalla;

    private void Start()
    {
        if (!EsTerminalDeRecepcion()) return;

        // Estos objetos solo se crean para la terminal de la Zona 1.
        mensajePrompt = "Presiona E para usar la terminal";
        panelReparacion = gameObject.AddComponent<PanelTerminalZ1>();
        pantalla = gameObject.AddComponent<PantallaTerminalZ1>();
        pantalla.Configurar(this);
        ActualizarPantalla();
    }

    public override void Interactuar()
    {
        ReproducirSonidoInteraccion();
        if (requiereEnergia &&
            (laboratorio == null || !laboratorio.PuedeConsultarTerminal()))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("La terminal no tiene energía. Acciona el interruptor");
            return;
        }

        GameManager juego = GameManager.Instance;
        if (juego == null) return;

        if (EsTerminalDeRecepcion())
        {
            // Las partidas anteriores que ya tenian el codigo no repiten la reparacion.
            if (juego.ObjetivoCompletado(objetivoReparacionZ1))
                juego.AgregarCodigo(idCodigo);
            ActualizarPantalla();
            if (panelReparacion != null) panelReparacion.Abrir(this);
            return;
        }

        bool yaTeniaCodigo = juego.TieneCodigo(idCodigo);
        juego.AgregarCodigo(idCodigo);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ActualizarCodigos(juego.codigos);
            UIManager.Instance.MostrarMensaje((yaTeniaCodigo ? "Código consultado: " : "Código obtenido: ") + codigoVisible);
        }
    }

    private bool EsTerminalDeRecepcion()
    {
        return gameObject.scene.name == "Zona1_Recepcion" && idCodigo == "codigo_a";
    }

    public bool ReparacionTerminada()
    {
        GameManager juego = GameManager.Instance;
        return juego != null &&
            (juego.TieneCodigo(idCodigo) || juego.ObjetivoCompletado(objetivoReparacionZ1));
    }

    public string SiguienteModulo()
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || ReparacionTerminada()) return "";
        if (juego.cantidadModulosRetiradosZ1 < modulosZ1.Length)
            return modulosZ1[juego.cantidadModulosRetiradosZ1];
        if (juego.modulosTerminalZ1.Count > 0)
            return juego.modulosTerminalZ1.Peek();
        return "";
    }

    // La tecla equivocada no modifica la pila ni entrega el codigo.
    public bool ProcesarModulo(string letra)
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || ReparacionTerminada() || letra != SiguienteModulo()) return false;

        if (juego.cantidadModulosRetiradosZ1 < modulosZ1.Length)
        {
            juego.modulosTerminalZ1.Push(letra);
            juego.cantidadModulosRetiradosZ1++;
        }
        else
        {
            // El ultimo modulo retirado es el primero que se instala.
            juego.modulosTerminalZ1.Pop();
            if (juego.modulosTerminalZ1.Count == 0)
            {
                juego.CompletarObjetivo(objetivoReparacionZ1);
                juego.AgregarCodigo(idCodigo);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ActualizarCodigos(juego.codigos);
                    UIManager.Instance.MostrarMensaje("Panel reparado. Código obtenido: " + codigoVisible);
                }
                ActualizarPantalla();
            }
        }
        return true;
    }

    private void ActualizarPantalla()
    {
        if (pantalla == null) return;
        GameManager juego = GameManager.Instance;
        int pasosHechos = 0;
        if (juego != null)
            pasosHechos = juego.cantidadModulosRetiradosZ1 + (modulosZ1.Length - juego.modulosTerminalZ1.Count);
        pantalla.ActualizarEstado(ReparacionTerminada(), codigoVisible, pasosHechos, modulosZ1.Length * 2);
    }
}
