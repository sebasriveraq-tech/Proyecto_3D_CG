using UnityEngine;

public class EfectosZona3 : MonoBehaviour
{
    [Header("Sonidos de pasos")]
    [SerializeField] private AudioClip sonidoCaminar;
    [SerializeField] private AudioClip sonidoCorrer;

    [Header("Configuracion de pasos")]
    [SerializeField] private float velocidadParaCorrer = 4f;
    [SerializeField] private float intervaloCaminar = 0.55f;
    [SerializeField] private float intervaloCorrer = 0.35f;

    [Header("Luces de emergencia")]
    [SerializeField] private Light[] lucesEmergencia;
    [SerializeField] private float brilloMinimo = 0.3f;
    [SerializeField] private float brilloMaximo = 5f;
    [SerializeField] private float velocidadSirena = 2f;

    private CharacterController controlador;
    private AudioSource audioPasos;
    private float temporizadorPasos;

    private void Start()
    {
        controlador = GetComponent<CharacterController>();

        audioPasos = gameObject.AddComponent<AudioSource>();
        audioPasos.playOnAwake = false;
        audioPasos.spatialBlend = 1f;
        audioPasos.volume = 1f;

        foreach (Light luz in lucesEmergencia)
        {
            if (luz != null)
                luz.color = Color.red;
        }
    }

    private void Update()
    {
        ControlarPasos();
        ControlarLuces();
    }

    private void ControlarPasos()
    {
        if (controlador == null || !controlador.isGrounded)
            return;

        Vector3 velocidad = controlador.velocity;
        velocidad.y = 0f;

        if (velocidad.magnitude < 0.2f)
        {
            temporizadorPasos = 0f;
            return;
        }

        bool corriendo = velocidad.magnitude >= velocidadParaCorrer;

        float intervalo = corriendo
            ? intervaloCorrer
            : intervaloCaminar;

        temporizadorPasos -= Time.deltaTime;

        if (temporizadorPasos <= 0f)
        {
            AudioClip sonido = corriendo
                ? sonidoCorrer
                : sonidoCaminar;

            if (sonido != null)
                audioPasos.PlayOneShot(sonido);

            temporizadorPasos = intervalo;
        }
    }

    private void ControlarLuces()
    {
        float brillo = Mathf.Lerp(
            brilloMinimo,
            brilloMaximo,
            (Mathf.Sin(Time.time * velocidadSirena) + 1f) / 2f
        );

        foreach (Light luz in lucesEmergencia)
        {
            if (luz != null)
                luz.intensity = brillo;
        }
    }
}
