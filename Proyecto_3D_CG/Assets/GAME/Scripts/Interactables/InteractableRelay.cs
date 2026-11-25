using UnityEngine;

/// Redirige la interacción y el aviso de un modelo 3D con collider sólido hacia su componente interactuable lógico.
public class InteractableRelay : Interactable
{
    public Interactable destino;
    public Interactable destinoSecundario;

    private Transform ultimoOrigen;

    private Interactable ResolverObjetivo(Transform origenJugador)
    {
        if (destinoSecundario != null && destinoSecundario.gameObject.activeInHierarchy)
        {
            Collider colSec = destinoSecundario.GetComponent<Collider>();
            if ((colSec == null || colSec.enabled) && destinoSecundario.PuedeInteractuar(origenJugador))
                return destinoSecundario;
        }

        if (destino != null && destino.gameObject.activeInHierarchy && destino.PuedeInteractuar(origenJugador))
            return destino;

        return null;
    }

    public override bool PuedeInteractuar(Transform origenJugador)
    {
        ultimoOrigen = origenJugador;
        Interactable activo = ResolverObjetivo(origenJugador);
        if (activo == null)
            return false;

        mensajePrompt = activo.mensajePrompt;
        return true;
    }

    public override void Interactuar()
    {
        Interactable activo = ResolverObjetivo(ultimoOrigen);
        if (activo != null)
            activo.Interactuar();
    }
}
