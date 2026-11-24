using UnityEngine;

/// Recogible: almacena el item en GameManager, actualiza el inventario y se retira de la escena.
public class Recogible : Interactable
{
    [Header("Datos del item")]
    public string idItem = "item_sin_id";     // ID del contrato: credencial_z1, credencial_z2...
    public string nombreVisible = "Objeto";   // texto que ve el jugador

    private Light luzIndicadora;
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

        luzIndicadora = GetComponentInChildren<Light>(true);
        fasePulso = Random.Range(0f, Mathf.PI * 2f);
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
