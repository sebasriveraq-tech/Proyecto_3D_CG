using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Script de un solo uso: construye la recepcion de Zona 1 (sala amplia,
/// mobiliario, armarios escondite, luces y puntos de patrulla) alrededor de
/// los objetos jugables ya aprobados. Es idempotente: borra lo que creo en
/// la ejecucion anterior y lo vuelve a crear.
/// Menu: LAB > Z1 Construir Recepcion (Bloque 1+2)
/// </summary>
// Fuerza una recarga de dominio para que RevisarBandera se suscriba de nuevo.
[InitializeOnLoad]
public static class BootstrapEscenarioZ1
{
    const string NombreRaiz = "ESCENARIO_Z1";
    const string DirModelos = "Assets/GAME/Art/Z1_Recepcion/Models/";
    const string DirCompartidos = "Assets/GAME/Art/Z1_Recepcion/Compartidos/Models/";
    const string DirMateriales = "Assets/GAME/Art/Z1_Recepcion/Materials/";
    const string Bandera = "Temp/bootstrap_z1.txt";
    const string RutaEscenaZ1 = "Assets/GAME/Scenes/Zona1_Recepcion.unity";

    // Limites de la sala grande (30 x 18 metros).
    const float LimiteOeste = -14f;
    const float LimiteEste = 16f;
    const float LimiteSur = -8f;
    const float LimiteNorte = 10f;
    const float AlturaPared = 3.8f;

    // Disparador alternativo: crear el archivo Temp/bootstrap_z1.txt ejecuta el constructor
    static BootstrapEscenarioZ1()
    {
        EditorApplication.update += RevisarBandera;
    }

    static void RevisarBandera()
    {
        if (!File.Exists(Bandera)) return;
        File.Delete(Bandera);
        EditorApplication.update -= RevisarBandera;
        try
        {
            Construir();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[BootstrapZ1] Fallo: " + e);
        }
    }

    [MenuItem("LAB/Z1 Construir Recepcion (Bloque 1+2) %#z")]
    public static void Construir()
    {
        // ---------- 0. Abrir la escena correcta ----------
        var escenaActiva = EditorSceneManager.GetActiveScene();
        if (escenaActiva.path != RutaEscenaZ1)
        {
            if (escenaActiva.isDirty)
            {
                Debug.LogWarning("[BootstrapZ1] La escena activa tiene cambios sin guardar. Guardala y reintenta.");
                return;
            }
            EditorSceneManager.OpenScene(RutaEscenaZ1);
        }

        // Diagnostico: raices de la escena (para ubicar objetos jugables y particulas).
        foreach (GameObject raizEscena in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Debug.Log("[BootstrapZ1] Raiz escena: " + raizEscena.name);

        // ---------- 0b. Preparar raices (idempotente) ----------
        GameObject previo = GameObject.Find(NombreRaiz);
        if (previo != null) Object.DestroyImmediate(previo);

        GameObject raiz = new GameObject(NombreRaiz);
        GameObject paredes = new GameObject("Paredes");
        GameObject mobiliario = new GameObject("Mobiliario");
        GameObject iluminacion = new GameObject("Iluminacion");
        GameObject patrulla = new GameObject("PuntosPatrulla");
        paredes.transform.SetParent(raiz.transform, false);
        mobiliario.transform.SetParent(raiz.transform, false);
        iluminacion.transform.SetParent(raiz.transform, false);
        patrulla.transform.SetParent(raiz.transform, false);

        // ---------- 1. Materiales ----------
        if (!AssetDatabase.IsValidFolder(DirMateriales.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/GAME/Art/Z1_Recepcion", "Materials");
        Material pared = CrearMaterial("M_Pared_Z1", new Color(0.72f, 0.70f, 0.65f));
        Material piso = CrearMaterial("M_Piso_Z1", new Color(0.29f, 0.29f, 0.31f));
        Material techo = CrearMaterial("M_Techo_Z1", new Color(0.85f, 0.85f, 0.85f));
        Material madera = CrearMaterial("M_Madera_Z1", new Color(0.54f, 0.42f, 0.29f));
        Material metal = CrearMaterial("M_Metal_Z1", new Color(0.60f, 0.63f, 0.65f));
        Material plastico = CrearMaterial("M_Plastico_Z1", new Color(0.24f, 0.25f, 0.27f));
        Material extintorMat = CrearMaterialConTextura("M_Extintor_Z1", DirModelos + "extintor_atlas.jpg");
        Material relojMat = CrearMaterialConTextura("M_Reloj_Z1", DirModelos + "reloj_pared_atlas.jpg");

        // Materiales propios de los armarios escondite.
        Material armarioCuerpo = CrearMaterial("M_Armario_Z1", new Color(0.30f, 0.34f, 0.33f));
        Material armarioPuerta = CrearMaterial("M_ArmarioPuerta_Z1", new Color(0.40f, 0.44f, 0.42f));
        Material armarioDetalle = CrearMaterial("M_ArmarioDetalle_Z1", new Color(0.10f, 0.11f, 0.11f));
        Material luzArmario = CrearMaterialEmisivo("M_LuzArmario_Z1", new Color(0.16f, 1f, 0.45f));

        // Piso: el Plane ya existe; solo le ponemos material
        GameObject plane = GameObject.Find("Plane");
        if (plane != null) plane.GetComponent<Renderer>().sharedMaterial = piso;

        // ---------- 2. Sala grande: paredes, techo y particion con la puerta ----------
        float ancho = LimiteEste - LimiteOeste;
        float fondo = LimiteNorte - LimiteSur;
        float centroX = (LimiteOeste + LimiteEste) * 0.5f;
        float centroZ = (LimiteSur + LimiteNorte) * 0.5f;

        CrearCuba("ParedSur", paredes.transform, new Vector3(centroX, AlturaPared * 0.5f, LimiteSur), new Vector3(ancho, AlturaPared, 0.3f), pared);
        CrearCuba("ParedNorte", paredes.transform, new Vector3(centroX, AlturaPared * 0.5f, LimiteNorte), new Vector3(ancho, AlturaPared, 0.3f), pared);
        CrearCuba("ParedOeste", paredes.transform, new Vector3(LimiteOeste, AlturaPared * 0.5f, centroZ), new Vector3(0.3f, AlturaPared, fondo), pared);
        CrearCuba("ParedEste", paredes.transform, new Vector3(LimiteEste, AlturaPared * 0.5f, centroZ), new Vector3(0.3f, AlturaPared, fondo), pared);
        CrearCuba("Techo", paredes.transform, new Vector3(centroX, AlturaPared + 0.15f, centroZ), new Vector3(ancho + 0.6f, 0.3f, fondo + 0.6f), techo);

        // Columnas: cortan la linea de vision en el centro de la sala.
        CrearCuba("ColumnaOeste", paredes.transform, new Vector3(-4.5f, AlturaPared * 0.5f, -2f), new Vector3(0.7f, AlturaPared, 0.7f), pared);
        CrearCuba("ColumnaEste", paredes.transform, new Vector3(4.5f, AlturaPared * 0.5f, 2f), new Vector3(0.7f, AlturaPared, 0.7f), pared);

        // Particion detras de la Puerta_Zona2 (la puerta queda en x 1.5..3.5)
        CrearCuba("ParticionOeste", paredes.transform, new Vector3(-6.25f, AlturaPared * 0.5f, 5.3f), new Vector3(15.5f, AlturaPared, 0.3f), pared);
        CrearCuba("ParticionEste", paredes.transform, new Vector3(9.75f, AlturaPared * 0.5f, 5.3f), new Vector3(12.5f, AlturaPared, 0.3f), pared);
        CrearCuba("DintelPuerta", paredes.transform, new Vector3(2.5f, 3.2f, 5.3f), new Vector3(2f, 1.2f, 0.3f), pared);

        // Pasillo corto detras de la puerta (de z 5.3 a 10, entre x 1.65 y 3.35)
        CrearCuba("PasilloOeste", paredes.transform, new Vector3(1.65f, AlturaPared * 0.5f, 7.65f), new Vector3(0.3f, AlturaPared, 4.7f), pared);
        CrearCuba("PasilloEste", paredes.transform, new Vector3(3.35f, AlturaPared * 0.5f, 7.65f), new Vector3(0.3f, AlturaPared, 4.7f), pared);

        // ---------- 3. Mobiliario ----------
        // Recepcion central-oeste (cerca del spawn)
        GameObject mostradorRecepcion = ColocarModelo("mostrador", new Vector3(-8.5f, 0f, 1.8f), 0f, madera, mobiliario.transform);
        float alturaRecepcion = mostradorRecepcion != null ? CalcularBounds(mostradorRecepcion).max.y : 0.77f;
        ColocarModelo("monitor", new Vector3(-8.8f, alturaRecepcion, 1.55f), 15f, plastico, mobiliario.transform);
        ColocarModelo("teclado", new Vector3(-8.5f, alturaRecepcion, 2.05f), 0f, plastico, mobiliario.transform);
        ColocarModelo("raton", new Vector3(-8.05f, alturaRecepcion, 2.05f), 0f, plastico, mobiliario.transform);
        ColocarModelo("silla_oficina", new Vector3(-8.5f, 0f, 2.9f), 180f, metal, mobiliario.transform);

        // Escritorio de la terminal (rincon oeste-suroeste)
        GameObject mostradorTerminal = ColocarModelo("mostrador", new Vector3(-12.9f, 0f, -2.2f), 90f, madera, mobiliario.transform);
        float alturaTerminal = mostradorTerminal != null ? CalcularBounds(mostradorTerminal).max.y : 0.77f;
        ColocarModelo("silla_oficina", new Vector3(-11.9f, 0f, -3.0f), 135f, metal, mobiliario.transform);
        ColocarModelo("basurero", new Vector3(-12.0f, 0f, -0.8f), 0f, metal, mobiliario.transform);

        // Escritorio de la credencial (este, dentro de la ronda del agente)
        GameObject mostradorClave = ColocarModelo("mostrador", new Vector3(12.2f, 0f, 1.0f), 90f, madera, mobiliario.transform);
        float alturaClave = mostradorClave != null ? CalcularBounds(mostradorClave).max.y : 0.77f;
        ColocarModelo("monitor", new Vector3(12.2f, alturaClave, 0.55f), 270f, plastico, mobiliario.transform);
        ColocarModelo("teclado", new Vector3(11.95f, alturaClave, 0.55f), 270f, plastico, mobiliario.transform);
        ColocarModelo("silla_oficina", new Vector3(11.4f, 0f, 0.1f), 250f, metal, mobiliario.transform);

        // Cobertura tactica: fila de archivadores al oeste de la credencial
        for (int i = 0; i < 3; i++)
            ColocarModelo("archivador", new Vector3(6.8f, 0f, 0.9f + i * 0.55f), 90f, metal, mobiliario.transform);

        // Archivadores del rincon noroeste (detras del spawn)
        ColocarModelo("archivador", new Vector3(-13.15f, 0f, 4.1f), 90f, metal, mobiliario.transform);
        ColocarModelo("archivador", new Vector3(-13.15f, 0f, 3.55f), 90f, metal, mobiliario.transform);

        // Sillas de espera cerca de la pared sur
        ColocarModelo("silla_oficina", new Vector3(-5.6f, 0f, -6.2f), 15f, metal, mobiliario.transform);
        ColocarModelo("silla_oficina", new Vector3(-6.8f, 0f, -5.9f), 345f, metal, mobiliario.transform);

        // Cajas: rincon noroeste y rincon sureste
        ColocarModelo("caja_carton", new Vector3(-12.9f, 0f, -6.9f), 20f, madera, mobiliario.transform);
        ColocarModelo("caja_carton", new Vector3(-12.2f, 0f, -7.25f), 340f, madera, mobiliario.transform);
        ColocarModelo("caja_abierta", new Vector3(-13.4f, 0f, -7.1f), 300f, madera, mobiliario.transform);
        ColocarModelo("caja_carton", new Vector3(15.0f, 0f, -6.9f), 20f, madera, mobiliario.transform);
        ColocarModelo("caja_carton", new Vector3(15.5f, 0f, -7.3f), 340f, madera, mobiliario.transform);
        ColocarModelo("caja_abierta", new Vector3(14.4f, 0f, -7.2f), 300f, madera, mobiliario.transform);

        // Plantas, extintores y reloj
        ColocarModelo("planta_maceta", new Vector3(-13.3f, 0f, -4.2f), 0f, null, mobiliario.transform);
        ColocarModelo("planta_maceta", new Vector3(15.2f, 0f, -5.6f), 45f, null, mobiliario.transform);
        ColocarModelo("planta_maceta", new Vector3(-0.9f, 0f, 4.5f), 20f, null, mobiliario.transform);
        ColocarModelo("extintor", new Vector3(0.7f, 0f, 4.7f), 180f, extintorMat, mobiliario.transform);
        ColocarModelo("extintor", new Vector3(-13.6f, 0f, -0.3f), 90f, extintorMat, mobiliario.transform);
        ColocarModelo("reloj_pared", new Vector3(-2f, 2.7f, -7.8f), 0f, relojMat, mobiliario.transform, false);

        // ---------- 3b. Armarios escondite (puerta abatible) ----------
        CrearArmario("Armario_1", new Vector3(-9f, 0f, -7.3f), armarioCuerpo, armarioPuerta, armarioDetalle, luzArmario, mobiliario.transform);
        CrearArmario("Armario_2", new Vector3(0f, 0f, -7.3f), armarioCuerpo, armarioPuerta, armarioDetalle, luzArmario, mobiliario.transform);
        CrearArmario("Armario_3", new Vector3(9f, 0f, -7.3f), armarioCuerpo, armarioPuerta, armarioDetalle, luzArmario, mobiliario.transform);

        // ---------- 4. Reubicar y vestir los objetos jugables ----------
        GameObject credencial = GameObject.Find("Recogible_Credencial");
        if (credencial != null)
        {
            credencial.transform.position = new Vector3(12.2f, alturaClave + 0.22f, 1.5f);
            credencial.transform.rotation = Quaternion.identity;
            Debug.Log("[BootstrapZ1] Credencial movida a " + credencial.transform.position);
        }

        GameObject terminal = GameObject.Find("Terminal_Registro");
        if (terminal != null)
        {
            terminal.transform.position = new Vector3(-12.75f, alturaTerminal + 0.7f, -2.2f);
            terminal.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Debug.Log("[BootstrapZ1] Terminal movida a " + terminal.transform.position);
        }

        // La puerta ahora exige tambien el codigo de la terminal.
        GameObject puertaZ2 = GameObject.Find("Puerta_Zona2");
        if (puertaZ2 != null)
        {
            Puerta puertaScript = puertaZ2.GetComponent<Puerta>();
            if (puertaScript != null)
            {
                Debug.Log("[BootstrapZ1] idCodigoRequerido antes: '" + puertaScript.idCodigoRequerido + "'");
                puertaScript.idCodigoRequerido = "codigo_a";
                EditorUtility.SetDirty(puertaScript);
            }
        }

        Material credencialMat = CrearMaterialConTextura("M_Credencial_Z1", DirCompartidos + "credencial_atlas.png");
        Material terminalMat = CrearMaterialConTextura("M_TerminalConsola_Z1", DirCompartidos + "terminal_consola_atlas.png");
        Material puertaMat = CrearMaterialConTextura("M_PuertaCorrediza_Z1", DirCompartidos + "puerta_corrediza_atlas.png");
        Vestir("Recogible_Credencial", "credencial", credencialMat);
        Vestir("Terminal_Registro", "terminal_consola", terminalMat);
        Vestir("Puerta_Zona2", "puerta_corrediza", puertaMat);

        // ---------- 5. Luces colgantes y sus luminarias ----------
        Vector3[] posicionesLuz =
        {
            new Vector3(-9f, 3.3f, 2.5f),
            new Vector3(-9f, 3.3f, -4.5f),
            new Vector3(2f, 3.3f, 2.5f),
            new Vector3(2f, 3.3f, -4.5f),
            new Vector3(10f, 3.3f, 2.5f),
            new Vector3(10f, 3.3f, -4.5f),
            new Vector3(2.5f, 3.3f, 7.6f)
        };
        foreach (Vector3 pos in posicionesLuz)
        {
            CrearLuz(iluminacion.transform, pos);
            ColocarModelo("luminaria_techo", pos + Vector3.up * 0.06f, 0f, metal, iluminacion.transform, false);
        }

        // ---------- 6. Spawn del jugador ----------
        GameObject jugador = GameObject.Find("Player");
        if (jugador != null)
        {
            jugador.transform.position = new Vector3(-11f, 0f, 3.8f);
            jugador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // ---------- 7. Patrulla: una ronda alrededor de la credencial ----------
        Vector3[] posiciones =
        {
            new Vector3(8f, 0f, -1.6f),
            new Vector3(14.4f, 0f, -1.6f),
            new Vector3(14.4f, 0f, 4.4f),
            new Vector3(8f, 0f, 4.4f)
        };
        Transform[] puntos = new Transform[posiciones.Length];
        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject punto = new GameObject("Punto_" + (i + 1));
            punto.transform.SetParent(patrulla.transform, false);
            punto.transform.localPosition = posiciones[i];
            puntos[i] = punto.transform;
        }

        GameObject agente = GameObject.Find("Agente");
        if (agente != null)
        {
            agente.transform.position = new Vector3(8f, 0f, -1.6f);
            AgentController control = agente.GetComponent<AgentController>();
            if (control != null)
            {
                control.puntosPatrulla = puntos;
                control.pausaEnPunto = 1.2f;
            }
        }

        // ---------- 8. Efectos ambientales acomodados a la sala nueva ----------
        MoverObjeto("PS_Vapor_Tuberia", new Vector3(-13.6f, 2.4f, -5.2f));
        MoverObjeto("PS_Chispas_Luminaria", new Vector3(10f, 3.02f, 2.5f));

        // El polvo se genera en una caja de 12 x 12 m (la sala vieja). La sala nueva
        // mide 30 x 18 m, asi que se agranda la caja y se sube la tasa para que la
        // densidad de motas sea la misma que antes.
        GameObject polvo = GameObject.Find("PS_Polvo_Z1");
        if (polvo != null)
        {
            ParticleSystem sistema = polvo.GetComponent<ParticleSystem>();
            if (sistema != null)
            {
                ParticleSystem.ShapeModule forma = sistema.shape;
                ParticleSystem.EmissionModule emision = sistema.emission;
                ParticleSystem.MainModule principal = sistema.main;

                Vector3 cajaAntes = forma.scale;
                float tasaAntes = emision.rateOverTime.constant;
                int maximoAntes = principal.maxParticles;

                forma.shapeType = ParticleSystemShapeType.Box;
                forma.scale = new Vector3(28f, 3.2f, 17f);
                polvo.transform.position = new Vector3(1f, 2.2f, 1f);
                emision.rateOverTime = 130f;
                principal.maxParticles = 2200;

                Debug.Log("[BootstrapZ1] PS_Polvo_Z1: caja " + cajaAntes + " -> " + forma.scale +
                          ", tasa " + tasaAntes + " -> 130, max " + maximoAntes + " -> 2200, posicion " + polvo.transform.position);
            }
        }

        GameObject particulas = GameObject.Find("Particulas_Z1");
        if (particulas != null)
            Debug.Log("[BootstrapZ1] Particulas_Z1 esta en " + particulas.transform.position + " con " + particulas.transform.childCount + " hijos");

        // ---------- 9. Rehornear el NavMesh con los nuevos colliders ----------
        NavMeshSurface superficie = Object.FindFirstObjectByType<NavMeshSurface>();
        if (superficie != null)
        {
            superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            superficie.BuildNavMesh();
        }

        // ---------- 10. Guardar ----------
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[BootstrapZ1] Recepcion ampliada construida. Entra en Play para probar.");
        Selection.activeGameObject = raiz;
    }

    // Crea un armario escondite con cuerpo de cajas y puerta sobre bisagra.
    static void CrearArmario(string nombre, Vector3 posicion, Material cuerpo, Material puerta,
        Material detalle, Material luz, Transform padre)
    {
        GameObject armario = new GameObject(nombre);
        armario.transform.SetParent(padre, false);
        armario.transform.localPosition = posicion;

        // Cuerpo: fondo, laterales, tapa y base (cada caja trae su BoxCollider).
        CrearCuba("Fondo", armario.transform, new Vector3(0f, 1.2f, -0.49f), new Vector3(1.5f, 2.4f, 0.12f), cuerpo);
        CrearCuba("LateralOeste", armario.transform, new Vector3(-0.69f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 1.1f), cuerpo);
        CrearCuba("LateralEste", armario.transform, new Vector3(0.69f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 1.1f), cuerpo);
        CrearCuba("Tapa", armario.transform, new Vector3(0f, 2.34f, 0f), new Vector3(1.5f, 0.12f, 1.1f), cuerpo);
        CrearCuba("Base", armario.transform, new Vector3(0f, 0.06f, 0f), new Vector3(1.5f, 0.12f, 1.1f), cuerpo);

        // Bisagra en el borde oeste de la boca; la puerta gira hacia afuera.
        GameObject bisagra = new GameObject("Bisagra");
        bisagra.transform.SetParent(armario.transform, false);
        bisagra.transform.localPosition = new Vector3(-0.63f, 0f, 0.49f);

        // La puerta es una raiz sin escala: asi las piezas hijas usan metros reales
        // y las rendijas quedan a la altura de los ojos (de 1,35 a 1,85 m del piso).
        GameObject puertaCubo = new GameObject("Puerta");
        puertaCubo.transform.SetParent(bisagra.transform, false);
        puertaCubo.transform.localPosition = new Vector3(0.63f, 1.2f, 0f);

        CrearCubaSinCollider("PanelInferior", puertaCubo.transform, new Vector3(0f, -0.48f, 0f), new Vector3(1.26f, 1.2f, 0.05f), puerta);
        CrearCubaSinCollider("PanelSuperior", puertaCubo.transform, new Vector3(0f, 0.87f, 0f), new Vector3(1.26f, 0.42f, 0.05f), puerta);
        CrearCubaSinCollider("LargueroOeste", puertaCubo.transform, new Vector3(-0.59f, 0.39f, 0f), new Vector3(0.08f, 0.54f, 0.05f), puerta);
        CrearCubaSinCollider("LargueroEste", puertaCubo.transform, new Vector3(0.59f, 0.39f, 0f), new Vector3(0.08f, 0.54f, 0.05f), puerta);
        // La ventana va de 1,32 a 1,86 m; los listones dejan libre la franja de 1,44 a 1,72 m,
        // justo a la altura de los ojos (1,60 m), para ver la sala desde adentro.
        CrearCubaSinCollider("Liston_1", puertaCubo.transform, new Vector3(0f, 0.20f, 0f), new Vector3(1.1f, 0.08f, 0.06f), detalle);
        CrearCubaSinCollider("Liston_2", puertaCubo.transform, new Vector3(0f, 0.565f, 0f), new Vector3(1.1f, 0.08f, 0.06f), detalle);
        CrearCubaSinCollider("Manija", puertaCubo.transform, new Vector3(0.45f, -0.2f, 0.055f), new Vector3(0.06f, 0.28f, 0.06f), detalle);

        // Un solo collider macizo cubre la puerta: se ve la sala por las rendijas,
        // pero el rayo de vision del agente choca aqui y no detecta al jugador.
        BoxCollider bloqueo = puertaCubo.AddComponent<BoxCollider>();
        bloqueo.center = Vector3.zero;
        bloqueo.size = new Vector3(1.26f, 2.16f, 0.05f);

        // Panel interior: sirve para volver a cerrar la puerta desde adentro.
        CrearCuba("PanelInterior", armario.transform, new Vector3(0f, 1.5f, -0.41f), new Vector3(0.55f, 0.4f, 0.05f), detalle);
        CrearCuba("LuzInterior", armario.transform, new Vector3(0f, 1.62f, -0.37f), new Vector3(0.05f, 0.05f, 0.02f), luz);

        ArmarioEscondite escondite = armario.AddComponent<ArmarioEscondite>();
        escondite.bisagra = bisagra.transform;
        escondite.anguloApertura = -105f;
        escondite.duracion = 0.9f;
        escondite.mensajePrompt = "Presiona E para abrir o cerrar el armario";
    }

    // Crea un cubo con material y collider (paredes y piezas simples)
    static GameObject CrearCuba(string nombre, Transform padre, Vector3 localPos, Vector3 tamano, Material material)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = localPos;
        cubo.transform.localScale = tamano;
        cubo.GetComponent<Renderer>().sharedMaterial = material;
        return cubo;
    }

    // Igual que CrearCuba, pero sin collider (piezas decorativas que no deben estorbar)
    static GameObject CrearCubaSinCollider(string nombre, Transform padre, Vector3 localPos, Vector3 tamano, Material material)
    {
        GameObject cubo = CrearCuba(nombre, padre, localPos, tamano, material);
        Object.DestroyImmediate(cubo.GetComponent<Collider>());
        return cubo;
    }

    // Instancia un modelo OBJ de la carpeta de Z1, con material y MeshCollider en cada malla hija
    static GameObject ColocarModelo(string nombre, Vector3 posicion, float rotacionY, Material material, Transform padre, bool conCollider = true)
    {
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(DirModelos + nombre + ".obj");
        if (original == null)
            original = AssetDatabase.LoadAssetAtPath<GameObject>(DirCompartidos + nombre + ".obj");
        if (original == null)
        {
            Debug.LogWarning("[BootstrapZ1] No existe el modelo: " + nombre);
            return null;
        }
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(original);
        instancia.name = nombre;
        instancia.transform.SetParent(padre, false);
        instancia.transform.localPosition = posicion;
        instancia.transform.rotation = Quaternion.Euler(0f, rotacionY, 0f);
        if (material != null)
            foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = material;

        if (conCollider)
        {
            // Los OBJ traen la malla en hijos: el MeshCollider va en cada malla,
            // no en la raiz (que no tiene malla propia).
            int agregados = 0;
            foreach (MeshFilter malla in instancia.GetComponentsInChildren<MeshFilter>())
            {
                if (malla.GetComponent<Collider>() == null)
                {
                    malla.gameObject.AddComponent<MeshCollider>();
                    agregados++;
                }
            }
            if (agregados == 0 && instancia.GetComponentInChildren<Collider>() == null)
                Debug.LogWarning("[BootstrapZ1] " + nombre + " quedo sin collider");
        }

        Bounds b = CalcularBounds(instancia);
        Debug.Log("[BootstrapZ1] " + nombre + " tamano=" + b.size + " alturaMin=" + b.min.y);
        return instancia;
    }

    // Pone el modelo 3D como "piel" del objeto jugable (cubo) y oculta el cubo
    static void Vestir(string nombreObjeto, string nombreModelo, Material material = null)
    {
        GameObject objetivo = GameObject.Find(nombreObjeto);
        if (objetivo == null)
        {
            Debug.LogWarning("[BootstrapZ1] No se encontro en escena: " + nombreObjeto);
            return;
        }
        Transform viejo = objetivo.transform.Find("Modelo3D");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        Renderer cubo = objetivo.GetComponent<Renderer>();
        Bounds destino = cubo != null ? cubo.bounds : new Bounds(objetivo.transform.position, Vector3.one);

        GameObject modelo = ColocarModelo(nombreModelo, Vector3.zero, 0f, material, objetivo.transform, false);
        if (modelo == null) return;
        modelo.name = "Modelo3D";
        modelo.transform.localPosition = Vector3.zero;

        // Escala uniforme para que quepa dentro del cubo y pie sobre su base
        Bounds original = CalcularBounds(modelo);
        float factor = Mathf.Min(
            destino.size.x / Mathf.Max(0.01f, original.size.x),
            destino.size.y / Mathf.Max(0.01f, original.size.y),
            destino.size.z / Mathf.Max(0.01f, original.size.z));
        modelo.transform.localScale = Vector3.one * factor;
        Bounds ajustado = CalcularBounds(modelo);
        modelo.transform.position += new Vector3(
            destino.center.x - ajustado.center.x,
            destino.min.y - ajustado.min.y,
            destino.center.z - ajustado.center.z);

        if (cubo != null) cubo.enabled = false; // el cubo ya no se ve; el collider sigue
    }

    static void MoverObjeto(string nombre, Vector3 posicion)
    {
        GameObject objeto = GameObject.Find(nombre);
        if (objeto != null)
        {
            objeto.transform.position = posicion;
            Debug.Log("[BootstrapZ1] " + nombre + " movido a " + posicion);
        }
    }

    static void CrearLuz(Transform padre, Vector3 posicion)
    {
        GameObject luz = new GameObject("Luz_Colgante");
        luz.transform.SetParent(padre, false);
        luz.transform.localPosition = posicion;
        Light punto = luz.AddComponent<Light>();
        punto.type = LightType.Point;
        punto.color = new Color(1f, 0.85f, 0.63f);
        punto.intensity = 1.2f;
        punto.range = 10f;
        punto.shadows = LightShadows.None;
    }

    static Bounds CalcularBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    static Material CrearMaterial(string nombre, Color color)
    {
        string ruta = DirMateriales + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CrearMaterialConTextura(string nombre, string rutaTextura)
    {
        string ruta = DirMateriales + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura);
        if (textura != null) material.SetTexture("_BaseMap", textura);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CrearMaterialEmisivo(string nombre, Color color)
    {
        Material material = CrearMaterial(nombre, color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 1.6f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
