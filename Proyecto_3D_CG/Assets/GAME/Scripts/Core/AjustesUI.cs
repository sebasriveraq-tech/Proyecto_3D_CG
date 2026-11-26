using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AjustesUI : MonoBehaviour
{
    public static AjustesUI Instance { get; private set; }

    [Header("Controles de Ajustes")]
    public Slider sliderVolumenGeneral;
    public Slider sliderVolumenMusica;
    public Slider sliderVolumenEfectos;
    public Slider sliderSensibilidad;
    public Toggle toggleSilencio;

    public TextMeshProUGUI valorVolumenGeneral;
    public TextMeshProUGUI valorVolumenMusica;
    public TextMeshProUGUI valorVolumenEfectos;
    public TextMeshProUGUI valorSensibilidad;

    public Button botonRestablecer;
    public Button botonVolver;

    private bool sincronizando;
    private bool abiertoDesdePausa;
    private bool eventosEnlazados;

    private void Awake()
    {
        Instance = this;
        Inicializar();
    }

    public void Inicializar()
    {
        Instance = this;
        GameSettingsManager.GarantizarInstancia();
        EnlazarEventos();
        RefrescarControlesDesdeSettings();
    }

    public void AbrirAjustes(bool desdePausa)
    {
        abiertoDesdePausa = desdePausa;
        EnlazarEventos();
        RefrescarControlesDesdeSettings();

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarAjustes();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarAjustes()
    {
        if (GameSettingsManager.Instance != null)
            GameSettingsManager.Instance.ReproducirFeedbackUI();

        if (UIManager.Instance == null) return;

        if (abiertoDesdePausa)
            UIManager.Instance.MostrarPausa();
        else
            UIManager.Instance.MostrarMenu();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RefrescarControlesDesdeSettings()
    {
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        sincronizando = true;

        if (sliderVolumenGeneral != null) sliderVolumenGeneral.value = cfg.volumenGeneral;
        if (sliderVolumenMusica != null) sliderVolumenMusica.value = cfg.volumenMusica;
        if (sliderVolumenEfectos != null) sliderVolumenEfectos.value = cfg.volumenEfectos;
        if (sliderSensibilidad != null) sliderSensibilidad.value = cfg.sensibilidadMouse;
        if (toggleSilencio != null) toggleSilencio.isOn = cfg.silenciado;

        ActualizarEtiquetasValores(cfg);
        sincronizando = false;
    }

    private void EnlazarEventos()
    {
        if (eventosEnlazados) return;
        eventosEnlazados = true;

        if (sliderVolumenGeneral != null)
        {
            sliderVolumenGeneral.onValueChanged.RemoveListener(AlCambiarVolumenGeneral);
            sliderVolumenGeneral.onValueChanged.AddListener(AlCambiarVolumenGeneral);
        }
        if (sliderVolumenMusica != null)
        {
            sliderVolumenMusica.onValueChanged.RemoveListener(AlCambiarVolumenMusica);
            sliderVolumenMusica.onValueChanged.AddListener(AlCambiarVolumenMusica);
        }
        if (sliderVolumenEfectos != null)
        {
            sliderVolumenEfectos.onValueChanged.RemoveListener(AlCambiarVolumenEfectos);
            sliderVolumenEfectos.onValueChanged.AddListener(AlCambiarVolumenEfectos);
        }
        if (toggleSilencio != null)
        {
            toggleSilencio.onValueChanged.RemoveListener(AlCambiarSilencio);
            toggleSilencio.onValueChanged.AddListener(AlCambiarSilencio);
        }
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.onValueChanged.RemoveListener(AlCambiarSensibilidad);
            sliderSensibilidad.onValueChanged.AddListener(AlCambiarSensibilidad);
        }
        if (botonRestablecer != null)
        {
            botonRestablecer.onClick.RemoveListener(AlPulsarRestablecer);
            botonRestablecer.onClick.AddListener(AlPulsarRestablecer);
        }
        if (botonVolver != null)
        {
            botonVolver.onClick.RemoveListener(CerrarAjustes);
            botonVolver.onClick.AddListener(CerrarAjustes);
        }
    }

    private void ActualizarEtiquetasValores(GameSettingsManager cfg)
    {
        if (valorVolumenGeneral != null)
            valorVolumenGeneral.text = cfg.silenciado ? "SILENCIADO" : Mathf.RoundToInt(cfg.volumenGeneral * 100f) + "%";
        if (valorVolumenMusica != null)
            valorVolumenMusica.text = Mathf.RoundToInt(cfg.volumenMusica * 100f) + "%";
        if (valorVolumenEfectos != null)
            valorVolumenEfectos.text = Mathf.RoundToInt(cfg.volumenEfectos * 100f) + "%";
        if (valorSensibilidad != null)
            valorSensibilidad.text = cfg.sensibilidadMouse.ToString("0.00");

        if (toggleSilencio != null)
        {
            TextMeshProUGUI etiquetaSilencio = toggleSilencio.GetComponentInChildren<TextMeshProUGUI>(true);
            if (etiquetaSilencio != null)
            {
                etiquetaSilencio.text = cfg.silenciado ? "SILENCIADO" : "ACTIVO";
                etiquetaSilencio.color = cfg.silenciado
                    ? new Color(0.04f, 0.06f, 0.08f, 1f)
                    : new Color(0.62f, 0.72f, 0.79f, 1f);
            }
        }
    }

    private void AlCambiarVolumenGeneral(float valor)
    {
        if (sincronizando) return;
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        cfg.EstablecerVolumenGeneral(valor);
        ActualizarEtiquetasValores(cfg);
    }

    private void AlCambiarVolumenMusica(float valor)
    {
        if (sincronizando) return;
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        cfg.EstablecerVolumenMusica(valor);
        ActualizarEtiquetasValores(cfg);
    }

    private void AlCambiarVolumenEfectos(float valor)
    {
        if (sincronizando) return;
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        cfg.EstablecerVolumenEfectos(valor);
        ActualizarEtiquetasValores(cfg);
    }

    private void AlCambiarSilencio(bool estado)
    {
        if (sincronizando) return;
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        cfg.EstablecerSilencio(estado);
        ActualizarEtiquetasValores(cfg);
    }

    private void AlCambiarSensibilidad(float valor)
    {
        if (sincronizando) return;
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        cfg.EstablecerSensibilidadMouse(valor);
        ActualizarEtiquetasValores(cfg);
    }

    private void AlPulsarRestablecer()
    {
        GameSettingsManager cfg = GameSettingsManager.GarantizarInstancia();
        cfg.RestablecerPredeterminados();
        RefrescarControlesDesdeSettings();
    }
}
