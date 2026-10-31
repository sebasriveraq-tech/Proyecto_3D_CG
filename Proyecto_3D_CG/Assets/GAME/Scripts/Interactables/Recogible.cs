using UnityEngine;


/// Recogible: almacena el item en GameManager, actualiza el inventario y se retira de la escena.

public class Recogible : Interactable
{
    [Header("Datos del item")]
    public string idItem = "item_sin_id";     // ID del contrato: credencial_z1, evidencia_z2...
    public string nombreVisible = "Objeto";   // texto que ve el jugador

    private Transform visualCredencial;
    private Vector3 posicionBaseVisual;
    private float faseVisual;

    private void Start()
    {
        if (idItem == "credencial_z2" || idItem == "tarjeta_acceso_z2")
            CrearVisualCredencialVisible();
    }

    private void Update()
    {
        if (visualCredencial == null) return;

        visualCredencial.localPosition = posicionBaseVisual + Vector3.up * Mathf.Sin(Time.time * 1.8f + faseVisual) * 0.045f;
        visualCredencial.Rotate(Vector3.up, 28f * Time.deltaTime, Space.Self);
    }

    private void CrearVisualCredencialVisible()
    {
        // Sustituye la tarjeta plana por un módulo de acceso luminoso que se reconoce desde lejos.
        foreach (Renderer visualAnterior in GetComponentsInChildren<Renderer>(true))
            visualAnterior.enabled = false;

        GameObject modulo = new GameObject("Visual_Modulo_Acceso");
        visualCredencial = modulo.transform;
        visualCredencial.SetParent(transform, false);

        Vector3 escalaRaiz = transform.lossyScale;
        visualCredencial.localScale = new Vector3(
            1f / Mathf.Max(Mathf.Abs(escalaRaiz.x), 0.001f),
            1f / Mathf.Max(Mathf.Abs(escalaRaiz.y), 0.001f),
            1f / Mathf.Max(Mathf.Abs(escalaRaiz.z), 0.001f));
        posicionBaseVisual = Vector3.up * 0.04f;
        faseVisual = Random.Range(0f, Mathf.PI * 2f);

        Material carcasa = CrearMaterialVisual("MAT_Modulo_Acceso", new Color(0.045f, 0.085f, 0.13f), false);
        Material metal = CrearMaterialVisual("MAT_Base_Acceso", new Color(0.18f, 0.25f, 0.29f), false);
        Material luzCian = CrearMaterialVisual("MAT_Luz_Acceso", new Color(0.04f, 0.85f, 1f), true);
        Material luzLima = CrearMaterialVisual("MAT_Indicador_Acceso", new Color(0.65f, 1f, 0.08f), true);

        CrearPiezaVisual(PrimitiveType.Cylinder, "Base", visualCredencial, new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.055f, 0.42f), metal);
        CrearPiezaVisual(PrimitiveType.Cylinder, "Aro_Luminoso", visualCredencial, new Vector3(0f, 0.15f, 0f), new Vector3(0.34f, 0.018f, 0.34f), luzCian);
        CrearPiezaVisual(PrimitiveType.Capsule, "Modulo_Central", visualCredencial, new Vector3(0f, 0.48f, 0f), new Vector3(0.2f, 0.31f, 0.2f), carcasa);
        CrearPiezaVisual(PrimitiveType.Cube, "Visor_Cian", visualCredencial, new Vector3(0f, 0.49f, -0.096f), new Vector3(0.105f, 0.34f, 0.025f), luzCian);
        CrearPiezaVisual(PrimitiveType.Cube, "Indicador_Lima", visualCredencial, new Vector3(0f, 0.4f, -0.113f), new Vector3(0.045f, 0.09f, 0.015f), luzLima);
        CrearPiezaVisual(PrimitiveType.Sphere, "Nucleo", visualCredencial, new Vector3(0f, 0.86f, 0f), new Vector3(0.23f, 0.23f, 0.23f), luzCian);

        GameObject luzObjeto = new GameObject("Luz_Indicadora");
        luzObjeto.transform.SetParent(visualCredencial, false);
        luzObjeto.transform.localPosition = new Vector3(0f, 0.76f, 0f);
        Light luz = luzObjeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.12f, 0.82f, 1f);
        luz.intensity = 1.2f;
        luz.range = 2.6f;
        luz.shadows = LightShadows.None;
    }

    private static void CrearPiezaVisual(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject pieza = GameObject.CreatePrimitive(tipo);
        pieza.name = nombre;
        pieza.transform.SetParent(padre, false);
        pieza.transform.localPosition = posicion;
        pieza.transform.localScale = escala;
        Collider colisionador = pieza.GetComponent<Collider>();
        if (colisionador != null) Destroy(colisionador);
        pieza.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material CrearMaterialVisual(string nombre, Color color, bool emisivo)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = nombre };
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (emisivo)
        {
            Color emision = color * 2.2f;
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
