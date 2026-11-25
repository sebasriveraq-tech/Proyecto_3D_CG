using UnityEngine;
using UnityEngine.AI;

public enum AgentState
{
    Patrulla,
    AlertaInvestigacion,
    Persecucion,
    Ataque,
    Retorno
}

[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    public float pausaEnPunto = 0.6f;

    [Header("Percepcion")]
    public float distanciaDeteccion = 10f;
    [Range(1f, 360f)] public float campoVision = 110f;
    public Transform puntoVision;
    public LayerMask capasVision = ~0;
    public float tiempoInvestigacion = 4f;

    [Header("Ataque")]
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

    [Header("Audio de pasos")]
    public AudioClip sonidoPasos;
    public float intervaloPasoPatrulla = 0.62f;
    public float intervaloPasoPersecucion = 0.36f;
    public float volumenPasos = 0.44f;

    public AgentState EstadoActual { get; private set; } = AgentState.Patrulla;

    private NavMeshAgent agente;
    private PlayerHealth saludJugador;
    private Vector3 puntoInicial;
    private Vector3 ultimaPosicionConocida;
    private int indicePatrulla;
    private float proximoDano;
    private float finInvestigacion;
    private float proximaActualizacionRuta;
    private float pausaHasta;
    private float temporizadorPasos;
    private bool patrullaEsperando;
    private MaterialPropertyBlock bloqueMaterial;
    private AgentAnimatorZ3 animacionesZ3;
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
        animacionesZ3 = GetComponentInChildren<AgentAnimatorZ3>();
        audioEstado = audioEstado != null ? audioEstado : GetComponent<AudioSource>();
        if (audioEstado == null) audioEstado = gameObject.AddComponent<AudioSource>();
        audioEstado.playOnAwake = false;
        audioEstado.loop = false;
        audioEstado.spatialBlend = 1f;
        audioEstado.minDistance = 2.2f;
        audioEstado.maxDistance = 16f;
        audioEstado.rolloffMode = AudioRolloffMode.Logarithmic;
        audioEstado.dopplerLevel = 0f;
        audioEstado.volume = volumenPasos;
        GameSettingsManager.GarantizarInstancia().RegistrarFuenteConVolumenBase(audioEstado, volumenPasos);

        if (indicadoresVisuales == null || indicadoresVisuales.Length == 0)
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
        if (GetComponentInChildren<LuzEstadoDronZ1>() != null)
            return;

        Transform trEstado = transform.Find("Luz_Estado_Agente_3D");
        if (trEstado == null)
        {
            GameObject goLuz = new GameObject("Luz_Estado_Agente_3D");
            goLuz.transform.SetParent(transform, false);
            goLuz.transform.localPosition = new Vector3(0f, 1.55f, 0.28f);
            luzEstado3D = goLuz.AddComponent<Light>();
            luzEstado3D.type = LightType.Point;
            luzEstado3D.range = 3.2f;
            luzEstado3D.intensity = 1.65f;
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
            luzRellenoModelo.range = 3.2f;
            luzRellenoModelo.intensity = 1.6f;
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
        saludJugador = FindFirstObjectByType<PlayerHealth>();
        agente.stoppingDistance = Mathf.Max(0.1f, distanciaAtaque * 0.8f);
        agente.autoBraking = true;
        ActualizarColor(colorPatrulla);
        if (!agente.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agente.Warp(hit.position);
        if (agente.isOnNavMesh)
            IrAlSiguientePunto();
        else
            Debug.LogError("El agente no encuentra una superficie NavMesh en Zona 2.", this);
    }

    private void Update()
    {
        if (Time.timeScale == 0f || agente == null || !agente.enabled || !agente.isOnNavMesh)
            return;

        if (saludJugador == null)
            saludJugador = FindFirstObjectByType<PlayerHealth>();
        if (saludJugador == null || saludJugador.vidaActual <= 0)
            return;

        Vector3 posicionJugador = saludJugador.transform.position;
        float distanciaJugador = Vector3.Distance(transform.position, posicionJugador);
        bool visible = PuedeVerJugador(posicionJugador);

        if (visible)
            ultimaPosicionConocida = posicionJugador;

        switch (EstadoActual)
        {
            case AgentState.Patrulla:
                ActualizarPatrulla(visible);
                break;
            case AgentState.AlertaInvestigacion:
                ActualizarInvestigacion(visible);
                break;
            case AgentState.Persecucion:
                ActualizarPersecucion(visible);
                break;
            case AgentState.Ataque:
                ActualizarAtaque(visible, distanciaJugador);
                break;
            case AgentState.Retorno:
                ActualizarRetorno(visible);
                break;
        }

        ActualizarPasosAgente();
    }

    private void ActualizarPasosAgente()
    {
        if (sonidoPasos == null || audioEstado == null || agente == null)
            return;

        if (agente.velocity.sqrMagnitude <= 0.12f)
        {
            temporizadorPasos = Mathf.Min(temporizadorPasos, 0.12f);
            return;
        }

        bool rapido = EstadoActual == AgentState.Persecucion || EstadoActual == AgentState.Ataque;
        temporizadorPasos -= Time.deltaTime;
        if (temporizadorPasos <= 0f)
        {
            float factor = GameSettingsManager.FactorEfectos;
            if (factor > 0.0001f)
            {
                audioEstado.mute = GameSettingsManager.Instance != null && GameSettingsManager.Instance.silenciado;
                audioEstado.pitch = Random.Range(0.92f, 1.06f);
                audioEstado.PlayOneShot(sonidoPasos, rapido ? 1f : 0.72f);
            }
            temporizadorPasos = Mathf.Max(0.20f, rapido ? intervaloPasoPersecucion : intervaloPasoPatrulla);
        }
    }

    private void ActualizarPatrulla(bool visible)
    {
        if (visible)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        if (agente.pathPending)
            return;

        bool llegoAlPunto = !agente.hasPath ||
            agente.remainingDistance <= Mathf.Max(0.25f, agente.stoppingDistance);
        if (!llegoAlPunto)
        {
            patrullaEsperando = false;
            return;
        }

        if (!patrullaEsperando)
        {
            patrullaEsperando = true;
            pausaHasta = Time.time + Mathf.Max(0f, pausaEnPunto);
        }

        if (Time.time < pausaHasta)
            return;

        patrullaEsperando = false;
        IrAlSiguientePunto();
    }

    private void ActualizarInvestigacion(bool visible)
    {
        if (visible)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        ActualizarDestino(ultimaPosicionConocida);
        bool llegoAlLugar = !agente.pathPending &&
            (!agente.hasPath || agente.remainingDistance <= Mathf.Max(0.35f, agente.stoppingDistance));
        if (Time.time >= finInvestigacion && llegoAlLugar)
            CambiarEstado(AgentState.Retorno);
    }

    private void ActualizarPersecucion(bool visible)
    {
        if (visible)
        {
            ActualizarDestino(saludJugador.transform.position);
            float distancia = Vector3.Distance(transform.position, saludJugador.transform.position);
            if (distancia <= distanciaAtaque)
                CambiarEstado(AgentState.Ataque);
            return;
        }

        finInvestigacion = Time.time + tiempoInvestigacion;
        CambiarEstado(AgentState.AlertaInvestigacion);
    }

    private void ActualizarAtaque(bool visible, float distancia)
    {
        if (!visible)
        {
            finInvestigacion = Time.time + tiempoInvestigacion;
            CambiarEstado(AgentState.AlertaInvestigacion);
            return;
        }

        if (distancia > distanciaAtaque * 1.2f)
        {
            CambiarEstado(AgentState.Persecucion);
            return;
        }

        if (agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.ResetPath();
        }

        MirarAlJugador();
        if (Time.time >= proximoDano)
        {
            proximoDano = Time.time + cooldownDano;
            if (animacionesZ3 != null)
                animacionesZ3.Atacar();
            saludJugador.RecibirDano(dano);
            LabAudioFeedback.ReproducirAtaqueAgente(transform.position);
            if (animacionesZ3 == null)
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

        Vector3 destino = ObtenerPuntoRetorno();
        ActualizarDestino(destino);
        if (!agente.pathPending &&
            (!agente.hasPath || agente.remainingDistance <= Mathf.Max(0.35f, agente.stoppingDistance)))
        {
            CambiarEstado(AgentState.Patrulla);
            IrAlSiguientePunto();
        }
    }

    private bool PuedeVerJugador(Vector3 posicionJugador)
    {
        Vector3 origen = puntoVision != null
            ? puntoVision.position
            : transform.position + Vector3.up * 1.5f;
        Vector3 destino = posicionJugador + Vector3.up;
        Vector3 direccion = destino - origen;
        float distancia = direccion.magnitude;
        if (distancia > distanciaDeteccion || distancia < 0.001f)
            return false;
        if (Vector3.Angle(transform.forward, direccion) > campoVision * 0.5f)
            return false;

        RaycastHit[] impactos = Physics.RaycastAll(
            origen, direccion.normalized, distancia, capasVision, QueryTriggerInteraction.Ignore);
        System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit impacto in impactos)
        {
            Transform golpeado = impacto.collider.transform;
            if (golpeado == transform || golpeado.IsChildOf(transform))
                continue;

            return golpeado == saludJugador.transform || golpeado.IsChildOf(saludJugador.transform);
        }
        return false;
    }

    public void Investigar(Vector3 posicion)
    {
        if (EstadoActual == AgentState.Persecucion || EstadoActual == AgentState.Ataque)
            return;

        ultimaPosicionConocida = posicion;
        finInvestigacion = Time.time + tiempoInvestigacion;
        CambiarEstado(AgentState.AlertaInvestigacion);
    }

    private void CambiarEstado(AgentState nuevoEstado)
    {
        if (EstadoActual == nuevoEstado)
            return;

        if (EstadoActual == AgentState.Ataque && agente != null && agente.enabled && agente.isOnNavMesh)
            agente.isStopped = false;

        EstadoActual = nuevoEstado;
        proximaActualizacionRuta = 0f;
        patrullaEsperando = false;
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
            if (renderizador == null || EsMallaConTextura(renderizador)) continue;
            renderizador.GetPropertyBlock(bloqueMaterial);
            bloqueMaterial.SetColor("_BaseColor", color);
            bloqueMaterial.SetColor("_Color", color);
            bloqueMaterial.SetColor("_EmissionColor", color * 0.35f);
            renderizador.SetPropertyBlock(bloqueMaterial);
        }
    }

    private void ActualizarDestino(Vector3 destino)
    {
        if (Time.time < proximaActualizacionRuta)
            return;
        agente.SetDestination(destino);
        proximaActualizacionRuta = Time.time + 0.2f;
    }

    private void IrAlSiguientePunto()
    {
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            int intentos = puntosPatrulla.Length;
            while (intentos-- > 0 && puntosPatrulla[indicePatrulla] == null)
                indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
            if (puntosPatrulla[indicePatrulla] != null)
                ActualizarDestino(puntosPatrulla[indicePatrulla].position);
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
        }
        else
        {
            Vector3 destino = puntoInicial + new Vector3(indicePatrulla % 2 == 0 ? 3f : -3f, 0f, 0f);
            ActualizarDestino(destino);
            indicePatrulla++;
            if (indicePatrulla > 3) indicePatrulla = 0;
        }
    }

    private Vector3 ObtenerPuntoRetorno()
    {
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            int indice = Mathf.Clamp(indicePatrulla - 1, 0, puntosPatrulla.Length - 1);
            if (puntosPatrulla[indice] != null)
                return puntosPatrulla[indice].position;
        }
        return puntoInicial;
    }

    private void MirarAlJugador()
    {
        Vector3 direccion = saludJugador.transform.position - transform.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(direccion), agente.angularSpeed * Time.deltaTime);
    }

    private System.Collections.IEnumerator PulsoAtaque()
    {
        Vector3 escala = transform.localScale;
        transform.localScale = escala * 1.08f;
        yield return new WaitForSeconds(0.12f);
        if (this != null) transform.localScale = escala;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origen = puntoVision != null
            ? puntoVision.position
            : transform.position + Vector3.up * 1.5f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origen, distanciaDeteccion);
        Gizmos.color = Color.cyan;
        Vector3 izquierda = Quaternion.Euler(0f, -campoVision * 0.5f, 0f) * transform.forward;
        Vector3 derecha = Quaternion.Euler(0f, campoVision * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(origen, izquierda * distanciaDeteccion);
        Gizmos.DrawRay(origen, derecha * distanciaDeteccion);
    }
}
