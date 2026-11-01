using UnityEngine;


/// Interactable: clase base de todo objeto con el que el jugador puede interactuar (recogibles, puertas, terminales, botones, interruptores).
/// Obliga a cada objeto concreto a escribir su propio Interactuar().
public abstract class Interactable : MonoBehaviour
{
    [Header("Interaccion")]
    public string mensajePrompt = "Presiona E para interactuar";

    protected void ReproducirSonidoInteraccion(AudioSource fuente = null)
    {
        if (fuente == null) fuente = GetComponent<AudioSource>();
        if (fuente != null && fuente.clip != null)
        {
            fuente.Play();
            return;
        }
        LabAudioFeedback.ReproducirInteraccion(transform.position, gameObject.name);
    }

    // Cada objeto dice que pasa cuando el jugador presiona E.
    public abstract void Interactuar();
}
