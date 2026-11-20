using UnityEngine;

/// Recogible: almacena el item en GameManager, actualiza el inventario y se retira de la escena.
public class Recogible : Interactable
{
    [Header("Datos del item")]
    public string idItem = "item_sin_id";     // ID del contrato: credencial_z1, credencial_z2...
    public string nombreVisible = "Objeto";   // texto que ve el jugador

    private Light luzIndicadora;
    private Renderer ledIndicador;
    private Color colorBaseLed;
    private float fasePulso;

    private void Start()
    {
        if (idItem == "credencial_z2" || idItem == "tarjeta_acceso_z2")
            ConfigurarVisualTarjetaAcceso();
    }

    private void Update()
    {
        if (luzIndicadora == null) return;

        float pulso = 0.78f + 0.22f * Mathf.Sin(Time.time * 2.6f + fasePulso);
        luzIndicadora.intensity = 0.38f * pulso;
    }

    private void ConfigurarVisualTarjetaAcceso()
    {
        // Ocultar únicamente las mallas primitivas antiguas (raíz, Cuerpo, Chip)
        // conservando activo el modelo 3D real de la credencial/tarjeta (Prop_KeyCard).
        MeshRenderer rendererRaiz = GetComponent<MeshRenderer>();
        if (rendererRaiz != null) rendererRaiz.enabled = false;

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r.gameObject == gameObject) continue;
            string n = r.gameObject.name;
            if (n == "Cuerpo" || n == "Chip" || n == "Manija")
                r.enabled = false;
            else
                r.enabled = true;
        }

        Transform modelo3D = null;
        foreach (Transform hijo in GetComponentsInChildren<Transform>(true))
        {
            if (hijo == transform) continue;
            if (hijo.name.Contains("3D") || hijo.name.Contains("KeyCard") || hijo.name.Contains("Credencial"))
            {
                modelo3D = hijo;
                break;
            }
        }

        if (modelo3D == null)
        {
            string nombreBuscado = idItem == "credencial_z2" ? "Credencial_Cientifica_3D" : "Evidencia_Tarjeta_3D";
            GameObject externo = GameObject.Find(nombreBuscado);
            if (externo != null)
            {
                modelo3D = externo.transform;
                modelo3D.SetParent(transform, false);
                modelo3D.localPosition = new Vector3(0f, 0.014f, 0f);
                modelo3D.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Vector3 esc = transform.lossyScale;
                modelo3D.localScale = new Vector3(
                    0.75f / Mathf.Max(Mathf.Abs(esc.x), 0.001f),
                    0.75f / Mathf.Max(Mathf.Abs(esc.y), 0.001f),
                    0.75f / Mathf.Max(Mathf.Abs(esc.z), 0.001f));
            }
        }

        if (modelo3D != null)
        {
            modelo3D.gameObject.SetActive(true);
            foreach (Renderer r in modelo3D.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }

        bool esCredencialPrincipal = idItem == "credencial_z2";
        colorBaseLed = esCredencialPrincipal
            ? new Color(0.08f, 0.88f, 1f)     // Cian de autorización de laboratorio
            : new Color(0.98f, 0.72f, 0.14f); // Ámbar de tarjeta electrónica de seguridad
        fasePulso = Random.Range(0f, Mathf.PI * 2f);

        Transform existente = transform.Find("Detalle_Credencial_RFID");
        if (existente != null) Destroy(existente.gameObject);

        GameObject contenedor = new GameObject("Detalle_Credencial_RFID");
        Transform raizDetalle = contenedor.transform;
        raizDetalle.SetParent(transform, false);

        Vector3 escalaPadre = transform.lossyScale;
        raizDetalle.localScale = new Vector3(
            1f / Mathf.Max(Mathf.Abs(escalaPadre.x), 0.001f),
            1f / Mathf.Max(Mathf.Abs(escalaPadre.y), 0.001f),
            1f / Mathf.Max(Mathf.Abs(escalaPadre.z), 0.001f));
        raizDetalle.localPosition = Vector3.zero;

        Material matPortatarjeta = CrearMaterialVisual(
            "MAT_PortaCredencial",
            esCredencialPrincipal ? new Color(0.11f, 0.15f, 0.20f) : new Color(0.16f, 0.17f, 0.19f),
            false, 0.65f, 0.72f);
        Material matMetalClip = CrearMaterialVisual(
            "MAT_ClipMetalico",
            new Color(0.68f, 0.73f, 0.78f),
            false, 0.85f, 0.82f);
        Material matChipRfid = CrearMaterialVisual(
            "MAT_ChipDoradoRFID",
            new Color(0.88f, 0.71f, 0.24f),
            false, 0.9f, 0.85f);
        Material matBanda = CrearMaterialVisual(
            "MAT_BandaSeguridad",
            esCredencialPrincipal ? new Color(0.05f, 0.36f, 0.52f) : new Color(0.48f, 0.26f, 0.06f),
            false, 0.25f, 0.6f);
        Material matLed = CrearMaterialVisual("MAT_LedEstadoRFID", colorBaseLed, true, 0.2f, 0.9f);

        // Base portacredencial delgada bajo la tarjeta 3D (o cuerpo completo si no hubiera FBX hijo)
        CrearPiezaVisual(
            PrimitiveType.Cube, "Marco_PortaCredencial", raizDetalle,
            new Vector3(0f, 0.006f, 0f), new Vector3(0.24f, 0.010f, 0.155f), matPortatarjeta);

        if (modelo3D == null)
        {
            Material matTarjeta = CrearMaterialVisual(
                "MAT_CuerpoTarjeta", new Color(0.88f, 0.91f, 0.95f), false, 0.15f, 0.75f);
            CrearPiezaVisual(
                PrimitiveType.Cube, "Lamina_Tarjeta", raizDetalle,
                new Vector3(0f, 0.013f, 0f), new Vector3(0.22f, 0.006f, 0.138f), matTarjeta);
        }

        // Banda identificadora superior/lateral según el tipo de credencial
        CrearPiezaVisual(
            PrimitiveType.Cube, "Franja_Nivel_Acceso", raizDetalle,
            new Vector3(0f, 0.017f, 0.052f), new Vector3(0.215f, 0.004f, 0.026f), matBanda);

        // Chip de contacto RFID dorado
        CrearPiezaVisual(
            PrimitiveType.Cube, "Chip_Contacto_RFID", raizDetalle,
            new Vector3(-0.062f, 0.018f, -0.012f), new Vector3(0.036f, 0.004f, 0.028f), matChipRfid);

        if (esCredencialPrincipal)
        {
            // Clip metálico de sujeción propio de una credencial de identificación de personal
            CrearPiezaVisual(
                PrimitiveType.Cube, "Clip_Sujecion_ID", raizDetalle,
                new Vector3(-0.125f, 0.011f, 0f), new Vector3(0.024f, 0.014f, 0.058f), matMetalClip);
        }
        else
        {
            // Lector / cabezal magnético codificado para la tarjeta de acceso secundaria
            CrearPiezaVisual(
                PrimitiveType.Cube, "Banda_Magnetica_Acceso", raizDetalle,
                new Vector3(0f, 0.017f, -0.052f), new Vector3(0.215f, 0.004f, 0.020f), matMetalClip);
        }

        // Micro-LED indicador de estado en la esquina de la tarjeta
        GameObject ledObj = CrearPiezaVisual(
            PrimitiveType.Cylinder, "LED_Estado_Tarjeta", raizDetalle,
            new Vector3(0.088f, 0.018f, -0.048f), new Vector3(0.016f, 0.004f, 0.016f), matLed);
        ledIndicador = ledObj.GetComponent<Renderer>();

        // Luz puntual sutil de corto alcance para integrarse con la superficie donde reposa
        GameObject luzObjeto = new GameObject("Luz_Indicadora_RFID");
        luzObjeto.transform.SetParent(raizDetalle, false);
        luzObjeto.transform.localPosition = new Vector3(0.05f, 0.08f, 0f);
        luzIndicadora = luzObjeto.AddComponent<Light>();
        luzIndicadora.type = LightType.Point;
        luzIndicadora.color = colorBaseLed;
        luzIndicadora.intensity = 0.38f;
        luzIndicadora.range = 1.15f;
        luzIndicadora.shadows = LightShadows.None;
    }

    private static GameObject CrearPiezaVisual(
        PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject pieza = GameObject.CreatePrimitive(tipo);
        pieza.name = nombre;
        pieza.transform.SetParent(padre, false);
        pieza.transform.localPosition = posicion;
        pieza.transform.localScale = escala;
        Collider colisionador = pieza.GetComponent<Collider>();
        if (colisionador != null) Destroy(colisionador);
        pieza.GetComponent<Renderer>().sharedMaterial = material;
        return pieza;
    }

    private static Material CrearMaterialVisual(
        string nombre, Color color, bool emisivo, float metalico = 0.3f, float suavidad = 0.6f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = nombre };
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metalico);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", suavidad);
        if (emisivo)
        {
            Color emision = color * 1.9f;
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emision);
        }
        return material;
    }

    public override void Interactuar()
    {
        ReproducirSonidoInteraccion();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AgregarItem(idItem);

            if (UIManager.Instance != null)
                UIManager.Instance.ActualizarInventario(GameManager.Instance.inventario);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarMensaje("Recogiste: " + nombreVisible);
            UIManager.Instance.OcultarPrompt();
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
