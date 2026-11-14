using UnityEngine;

/// ArmarioEscondite: armario con una puerta que gira sobre una bisagra.
/// Con E se abre o se cierra con una animacion suave.
/// Con la puerta cerrada, su collider corta la linea de vision del agente:
/// el jugador puede esconderse adentro sin tocar el FSM del agente.
public class ArmarioEscondite : Interactable
{
    [Header("Puerta")]
    public Transform bisagra;             // pivote donde gira la puerta
    public float anguloApertura = -105f;  // grados que gira al abrir
    public float duracion = 0.9f;         // segundos que tarda el movimiento

    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private float progreso;               // 0 = cerrada, 1 = abierta
    private float progresoObjetivo;       // a donde queremos llegar

    private void Start()
    {
        if (bisagra == null) return;

        duracion = Mathf.Max(0.05f, duracion);
        rotacionCerrada = bisagra.localRotation;
        rotacionAbierta = rotacionCerrada * Quaternion.Euler(0f, anguloApertura, 0f);
    }

    private void Update()
    {
        if (bisagra == null) return;

        // El progreso avanza parejo y SmoothStep le da arranque y frenado suaves.
        progreso = Mathf.MoveTowards(progreso, progresoObjetivo, Time.deltaTime / duracion);
        float suave = Mathf.SmoothStep(0f, 1f, progreso);
        bisagra.localRotation = Quaternion.Slerp(rotacionCerrada, rotacionAbierta, suave);
    }

    public override void Interactuar()
    {
        // Invierte el destino: si estaba abierta, se cierra; si estaba cerrada, se abre.
        progresoObjetivo = progresoObjetivo > 0.5f ? 0f : 1f;

        // Sin sonido a proposito: abrir o cerrar el armario no debe atraer al agente.
    }
}
