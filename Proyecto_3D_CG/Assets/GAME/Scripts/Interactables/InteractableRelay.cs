using UnityEngine;

public class InteractableRelay : Interactable
{
    public Interactable destino;
    public Interactable destinoSecundario;

    private Transform ultimoOrigen;

    private void Start()
    {
        if (destino == null)
            destino = FindFirstObjectByType<CajonInteractuable>();
        if (destinoSecundario == null && destino is CajonInteractuable cajon)
        {
            Recogible recogible = cajon.GetComponentInChildren<Recogible>(true);
            if (recogible == null)
            {
                float mejorDist = 1.2f;
                foreach (Recogible r in FindObjectsByType<Recogible>(FindObjectsSortMode.None))
                {
                    if (r == null) continue;
                    float d = Vector3.Distance(cajon.transform.position, r.transform.position);
                    if (d < mejorDist)
                    {
                        mejorDist = d;
                        recogible = r;
                    }
                }
            }
            destinoSecundario = recogible;
        }
    }

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
