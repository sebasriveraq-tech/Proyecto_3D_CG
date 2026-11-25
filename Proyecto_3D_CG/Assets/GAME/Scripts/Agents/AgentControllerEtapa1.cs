using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentControllerEtapa1 : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;
    public AgentAnimatorZ3 animaciones;
    public PlayerHealth saludJugador;

    [Header("Movimiento y Percepcion")]
    public float velocidadPersecucion = 5f;
    public float distanciaDeteccion = 32f;
    [Range(1f, 360f)] public float campoVision = 175f;
    public LayerMask capasVision = ~0;
    public float tiempoInvestigacion = 4.5f;

    [Header("Deteccion y ataque")]
    public float distanciaAtaque = 1.8f;
    public int dano = 10;
    public float cooldownDano = 1.5f;

    [Header("Reaccion")]
    public Renderer[] indicadoresVisuales;
    public AudioSource audioEstado;

    public Color colorPatrulla = new Color(0.25f, 0.72f, 0.78f);
    public Color colorAlerta = new Color(1f, 0.72f, 0.12f);
    public Color colorPersecucion = new Color(1f, 0.12f, 0.12f);
    public Color colorAtaque = new Color(1f, 0.28f, 0.04f);
    public Color colorRetorno = new Color(0.38f, 0.55f, 0.9f);

    public AgentState EstadoActual { get; private set; } = AgentState.Patrulla;

    private NavMeshAgent agente;
    private bool activado = false;
    public bool EstaActivado => activado;

    private Vector3 puntoInicial;
    private Vector3 ultimaPosicionConocida;
    private float proximoDano;
    private float finInvestigacion;
    private int indicePatrullaLocal;
    private MaterialPropertyBlock bloqueMaterial;
    private Light luzEstado3D;
    private Light luzRellenoModelo;

    private static bool EsMallaConTextura(Renderer renderizador)
    {
        if (renderizador == null) return false;
        if (renderizador is SkinnedMeshRenderer) return true;

        Material mat = renderizador.sharedMaterial;
        if (mat == null) return false;
        if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) return true;
        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null) return true;
        return false;
    }

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (animaciones == null)
            animaciones = GetComponentInChildren<AgentAnimatorZ3>();

        audioEstado = audioEstado != null
            ? audioEstado
            : GetComponent<AudioSource>();

        if (audioEstado == null)
            audioEstado = gameObject.AddComponent<AudioSource>();

        audioEstado.playOnAwake = false;
        audioEstado.spatialBlend = 1f;
        audioEstado.volume = 0.45f;

        if (indicadoresVisuales == null ||
            indicadoresVisuales.Length == 0)
        {
            Renderer[] todos = GetComponentsInChildren<Renderer>();
            var filtrados = new System.Collections.Generic.List<Renderer>(todos.Length);
            for (int i = 0; i < todos.Length; i++)
            {
                if (!EsMallaConTextura(todos[i]))
                    filtrados.Add(todos[i]);
            }
            indicadoresVisuales = filtrados.ToArray();
        }

        bloqueMaterial = new MaterialPropertyBlock();
        ConfigurarLuces3DAgente();
    }

    private void ConfigurarLuces3DAgente()
    {
        Transform trEstado = transform.Find("Luz_Estado_Agente_3D");
        if (trEstado == null)
        {
            GameObject goLuz = new GameObject("Luz_Estado_Agente_3D");
            goLuz.transform.SetParent(transform, false);
            goLuz.transform.localPosition = new Vector3(0f, 1.55f, 0.28f);
            luzEstado3D = goLuz.AddComponent<Light>();
            luzEstado3D.type = LightType.Point;
            luzEstado3D.range = 3.4f;
            luzEstado3D.intensity = 1.75f;
            luzEstado3D.shadows = LightShadows.None;
        }
        else
        {
            luzEstado3D = trEstado.GetComponent<Light>();
        }

        Transform trRelleno = transform.Find("Luz_Relleno_Modelo_3D");
        if (trRelleno == null)
        {
            GameObject goRelleno = new GameObject("Luz_Relleno_Modelo_3D");
            goRelleno.transform.SetParent(transform, false);
            goRelleno.transform.localPosition = new Vector3(0f, 1.35f, 0.58f);
            luzRellenoModelo = goRelleno.AddComponent<Light>();
            luzRellenoModelo.type = LightType.Point;
            luzRellenoModelo.color = new Color(0.95f, 0.96f, 1f);
            luzRellenoModelo.range = 3.4f;
            luzRellenoModelo.intensity = 1.75f;
            luzRellenoModelo.shadows = LightShadows.None;
        }
        else
        {
            luzRellenoModelo = trRelleno.GetComponent<Light>();
        }
    }

    private void Start()
    {
        puntoInicial = transform.position;
        ultimaPosicionConocida = puntoInicial;

        if (jugador == null)
        {
            GameObject objetoJugador =
                GameObject.FindGameObjectWithTag("Player");

            if (objetoJugador != null)
                jugador = objetoJugador.transform;
        }

        if (jugador != null && saludJugador == null)
        {
            saludJugador = jugador.GetComponent<PlayerHealth>();

            if (saludJugador == null)
                saludJugador = jugador.GetComponentInParent<PlayerHealth>();
        }

        agente.speed = velocidadPersecucion;
        agente.stoppingDistance = Mathf.Max(0.1f, distanciaAtaque * 0.8f);
        agente.isStopped = true;

        EstadoActual = AgentState.Patrulla;
        ActualizarColor(colorPatrulla);

        if (!agente.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
            {
                agente.Warp(hit.position);
            }
        }
    }

    private void Update()
    {
        if (!activado ||
            jugador == null ||
            saludJugador == null ||
            saludJugador.vidaActual <= 0)
            return;

        if (!agente.enabled || !agente.isOnNavMesh)
            return;

        Vector3 posJugador = jugador.position;
        float distancia = Vector3.Distance(transform.position, posJugador);
        bool visible = PuedeVerJugador(posJugador);

        if (visible)
            ultimaPosicionConocida = posJugador;

        switch (EstadoActual)
        {
            case AgentState.Patrulla:
                ActualizarPatrulla(visible);
                break;
            case AgentState.AlertaInvestigacion:
                ActualizarInvestigacion(visible);
                break;
            case AgentState.Persecucion:
                ActualizarPersecucion(visible, distancia);
                break;
            case AgentState.Ataque:
                ActualizarAtaque(visible, distancia);
                break;
            case AgentState.Retorno:
                ActualizarRetorno(visible);
                break;
        }
    }

    private void ActualizarPatrulla(bool visible)
    {
        if (visible)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        if (!agente.pathPending && (!agente.hasPath || agente.remainingDistance <= Mathf.Max(0.3f, agente.stoppingDistance)))
        {
            Vector3 offset = (indicePatrullaLocal % 2 == 0) ? transform.right * 2.5f : -transform.right * 2.5f;
            indicePatrullaLocal++;
            agente.isStopped = false;
            agente.SetDestination(puntoInicial + offset);
        }
    }

    private void ActualizarInvestigacion(bool visible)
    {
        if (visible)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        agente.isStopped = false;
        agente.SetDestination(ultimaPosicionConocida);
        bool llego = !agente.pathPending && (!agente.hasPath || agente.remainingDistance <= Mathf.Max(0.35f, agente.stoppingDistance));
        if (Time.time >= finInvestigacion && llego)
            CambiarEstado(AgentState.Retorno);
    }

    private void ActualizarPersecucion(bool visible, float distancia)
    {
        if (!visible && ArmarioEscondite.JugadorOculto)
        {
            finInvestigacion = Time.time + tiempoInvestigacion;
            CambiarEstado(AgentState.AlertaInvestigacion);
            return;
        }

        if (distancia <= distanciaAtaque)
        {
            CambiarEstado(AgentState.Ataque);
            return;
        }

        agente.isStopped = false;
        agente.SetDestination(jugador.position);
    }

    private void ActualizarAtaque(bool visible, float distancia)
    {
        if (ArmarioEscondite.JugadorOculto)
        {
            finInvestigacion = Time.time + tiempoInvestigacion;
            CambiarEstado(AgentState.AlertaInvestigacion);
            return;
        }

        if (distancia > distanciaAtaque * 1.15f)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        agente.isStopped = true;
        agente.ResetPath();
        MirarJugador();

        if (Time.time >= proximoDano)
        {
            proximoDano = Time.time + cooldownDano;

            if (animaciones != null)
                animaciones.Atacar();

            saludJugador.RecibirDano(dano);

            LabAudioFeedback.ReproducirAtaqueAgente(
                transform.position
            );

            if (animaciones == null)
                StartCoroutine(PulsoAtaque());
        }
    }

    private void ActualizarRetorno(bool visible)
    {
        if (visible)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        agente.isStopped = false;
        agente.SetDestination(puntoInicial);
        if (!agente.pathPending && (!agente.hasPath || agente.remainingDistance <= Mathf.Max(0.35f, agente.stoppingDistance)))
            CambiarEstado(AgentState.Patrulla);
    }

    private bool PuedeVerJugador(Vector3 posicionJugador)
    {
        if (ArmarioEscondite.JugadorOculto)
            return false;

        Vector3 origen = transform.position + Vector3.up * 1.45f;
        Vector3 destino = posicionJugador + Vector3.up * 1.0f;
        Vector3 direccion = destino - origen;
        float distancia = direccion.magnitude;
        if (distancia < 0.001f)
            return true;
        if (distancia > distanciaDeteccion)
            return false;

        if (Vector3.Angle(transform.forward, direccion) <= campoVision * 0.5f)
        {
            RaycastHit[] impactos = Physics.RaycastAll(
                origen, direccion.normalized, distancia, capasVision, QueryTriggerInteraction.Ignore);
            System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit impacto in impactos)
            {
                Transform golpeado = impacto.collider.transform;
                if (golpeado == transform || golpeado.IsChildOf(transform))
                    continue;
                if (golpeado == jugador || golpeado.IsChildOf(jugador))
                    return true;
                break;
            }
        }

        // En alerta de zona 3, si el jugador no está oculto en armario, mantiene rastreo activo dentro del sector.
        return !ArmarioEscondite.JugadorOculto && distancia <= distanciaDeteccion;
    }

    public void Activar()
    {
        if (activado)
            return;

        activado = true;

        if (agente.isOnNavMesh)
            agente.isStopped = false;

        if (jugador != null)
            ultimaPosicionConocida = jugador.position;

        CambiarEstado(AgentState.AlertaInvestigacion);
        StartCoroutine(TransicionPersecucion());
    }

    private IEnumerator TransicionPersecucion()
    {
        yield return new WaitForSeconds(0.4f);

        if (activado && EstadoActual != AgentState.Ataque)
            CambiarEstado(AgentState.Persecucion);
    }

    private void CambiarEstado(AgentState nuevoEstado)
    {
        if (EstadoActual == nuevoEstado)
            return;

        EstadoActual = nuevoEstado;

        if (nuevoEstado == AgentState.Persecucion || nuevoEstado == AgentState.AlertaInvestigacion)
            LabAudioFeedback.ReproducirAlertaAgente(transform.position);
        else if (nuevoEstado == AgentState.Retorno)
            LabAudioFeedback.ReproducirTono(transform.position, 270f, 0.16f, 0.25f);

        switch (nuevoEstado)
        {
            case AgentState.Patrulla: ActualizarColor(colorPatrulla); break;
            case AgentState.AlertaInvestigacion: ActualizarColor(colorAlerta); break;
            case AgentState.Persecucion: ActualizarColor(colorPersecucion); break;
            case AgentState.Ataque: ActualizarColor(colorAtaque); break;
            case AgentState.Retorno: ActualizarColor(colorRetorno); break;
        }
    }

    private void ActualizarColor(Color color)
    {
        if (luzEstado3D != null)
            luzEstado3D.color = color;

        if (indicadoresVisuales == null)
            return;

        foreach (Renderer renderizador in indicadoresVisuales)
        {
            if (renderizador == null || EsMallaConTextura(renderizador))
                continue;

            renderizador.GetPropertyBlock(bloqueMaterial);

            bloqueMaterial.SetColor("_BaseColor", color);
            bloqueMaterial.SetColor("_Color", color);
            bloqueMaterial.SetColor("_EmissionColor", color * 0.35f);

            renderizador.SetPropertyBlock(bloqueMaterial);
        }
    }

    private void MirarJugador()
    {
        Vector3 direccion = jugador.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direccion),
                agente.angularSpeed * Time.deltaTime
            );
        }
    }

    private IEnumerator PulsoAtaque()
    {
        Vector3 escalaOriginal = transform.localScale;

        transform.localScale = escalaOriginal * 1.08f;

        yield return new WaitForSeconds(0.12f);

        if (this != null)
            transform.localScale = escalaOriginal;
    }
}
