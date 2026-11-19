using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// Pantalla visual de la terminal de Zona 1. No modifica la reparación ni el guardado.
public class PantallaTerminalZ1 : MonoBehaviour
{
    private GameObject pantallaRaiz;
    private Material materialPantalla;
    private Material materialMarco;
    private TextMeshPro texto;
    private Light luz;

    // Estado visible: sirve para revisar la pantalla desde pruebas.
    public string TextoPantalla => texto != null ? texto.text : "";
    public Color ColorLuz => luz != null ? luz.color : Color.black;
    public Color ColorPantalla => materialPantalla != null && materialPantalla.HasProperty("_BaseColor")
        ? materialPantalla.GetColor("_BaseColor") : Color.black;

    public void Configurar(Terminal terminal)
    {
        if (terminal == null || pantallaRaiz != null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogWarning("No se encontró un shader Unlit para la pantalla de la terminal.");
            return;
        }

        materialMarco = new Material(shader);
        materialPantalla = new Material(shader);
        PonerColor(materialMarco, new Color(0.045f, 0.055f, 0.06f));

        Transform baseTerminal = terminal.transform;
        pantallaRaiz = new GameObject("Pantalla_Terminal_Z1");
        Transform raiz = pantallaRaiz.transform;
        raiz.SetParent(baseTerminal, false);

        // El modelo tiene su frente en +Z propio; Modelo3D gira -90° respecto
        // a la terminal, por lo que el frente corresponde a -X de la terminal.
        // Su parte superior está cerca de y=0.056 en coordenadas de la terminal.
        raiz.localPosition = new Vector3(-0.028f, 0.08f, 0f);
        raiz.localRotation = Quaternion.identity;

        // Compensa la escala no uniforme del prefab antes de girar el panel.
        // Así las medidas siguientes están expresadas aproximadamente en metros.
        Vector3 escala = baseTerminal.lossyScale;
        raiz.localScale = new Vector3(1f / Mathf.Max(Mathf.Abs(escala.x), 0.001f),
                                      1f / Mathf.Max(Mathf.Abs(escala.y), 0.001f),
                                      1f / Mathf.Max(Mathf.Abs(escala.z), 0.001f));

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(raiz, false);
        panel.transform.localPosition = new Vector3(0f, 0.19f, 0f);
        // La cara visible de los cubos y del texto mira hacia -Z del panel.
        panel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        CrearCaja("Marco", panel.transform, Vector3.zero,
                  new Vector3(0.58f, 0.38f, 0.06f), materialMarco);
        CrearCaja("Pantalla", panel.transform, new Vector3(0f, 0f, -0.034f),
                  new Vector3(0.52f, 0.32f, 0.01f), materialPantalla);

        GameObject objetoTexto = new GameObject("Texto");
        objetoTexto.transform.SetParent(panel.transform, false);
        objetoTexto.transform.localPosition = new Vector3(0f, 0f, -0.041f);
        texto = objetoTexto.AddComponent<TextMeshPro>();
        texto.rectTransform.sizeDelta = new Vector2(0.48f, 0.28f);
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = 2.5f;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 0.5f;
        texto.fontSizeMax = 2.5f;
        texto.color = Color.white;
        texto.textWrappingMode = TextWrappingModes.NoWrap;

        GameObject objetoLuz = new GameObject("Luz_Pantalla");
        objetoLuz.transform.SetParent(panel.transform, false);
        objetoLuz.transform.localPosition = new Vector3(0f, 0.02f, -0.16f);
        luz = objetoLuz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 2f;
        luz.intensity = 1.2f;
        luz.shadows = LightShadows.None;

        ActualizarEstado(false, null);
    }

    public void ActualizarEstado(bool reparada, string codigo, int pasosHechos = 0, int pasosTotales = 6)
    {
        if (pantallaRaiz == null) return;

        Color colorPantalla = reparada ? new Color(0.015f, 0.32f, 0.09f)
                                      : new Color(0.38f, 0.18f, 0.015f);
        PonerColor(materialPantalla, colorPantalla);
        if (reparada)
            texto.text = "TERMINAL ACTIVA\n" + (string.IsNullOrEmpty(codigo) ? "A-4721" : codigo);
        else if (pasosHechos > 0)
            texto.text = "TERMINAL\nREPARANDO  " + pasosHechos + "/" + pasosTotales;
        else
            texto.text = "TERMINAL\nREPARACION PENDIENTE";
        luz.color = reparada ? new Color(0.25f, 1f, 0.4f) : new Color(1f, 0.6f, 0.18f);
    }

    private static void CrearCaja(string nombre, Transform padre, Vector3 posicion,
                                  Vector3 escala, Material material)
    {
        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caja.name = nombre;
        caja.transform.SetParent(padre, false);
        caja.transform.localPosition = posicion;
        caja.transform.localScale = escala;
        // Se desactiva de inmediato: Destroy se aplica al final del frame.
        Collider colision = caja.GetComponent<Collider>();
        colision.enabled = false;
        Destroy(colision);
        MeshRenderer renderer = caja.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void PonerColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
    }

    private void OnDestroy()
    {
        if (pantallaRaiz != null) Destroy(pantallaRaiz);
        if (materialPantalla != null) Destroy(materialPantalla);
        if (materialMarco != null) Destroy(materialMarco);
    }
}
