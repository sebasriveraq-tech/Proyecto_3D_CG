using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// Puerta: pide un item al GameManager. Si el jugador lo tiene, se abre y carga la siguiente zona.
public class Puerta : Interactable
{
    [Header("Condicion")]
    public string idLlave = "credencial_z1";   // item que debe tener el jugador
    public string idPuerta = "puerta_z2";      // id que queda guardado en el GameManager
    public string idCodigoRequerido;

    [Header("Destino")]
    public string escenaDestino = "Zona2_Laboratorio";
    public float duracionApertura = 0.8f;

    private bool abierta = false;

    public override void Interactuar()
    {
        if (abierta || GameManager.Instance == null) return;

        // Sin la credencial no pasa nada: solo se avisa.
        if (!GameManager.Instance.TieneItem(idLlave))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Necesitas: " + idLlave.Replace("_", " "));
            ReproducirSonidoInteraccion();
            return;
        }

        if (!string.IsNullOrEmpty(idCodigoRequerido) &&
            !GameManager.Instance.TieneCodigo(idCodigoRequerido))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.MostrarMensaje("Consulta la terminal para obtener el código de acceso");
            ReproducirSonidoInteraccion();
            return;
        }

        LabAudioFeedback.ReproducirAperturaPuerta(transform.position);
        LabAudioFeedback.ReproducirLiberacionVapor(transform.position);
        abierta = true;
        GameManager.Instance.AbrirPuerta(idPuerta);

        if (UIManager.Instance != null)
            UIManager.Instance.MostrarPrompt("Acceso concedido. Abriendo...");

        CrearHumoTransicion();
        StartCoroutine(AbrirYCambiar());
    }

    // La puerta sube como una compuerta y despues carga la siguiente zona.
    private IEnumerator AbrirYCambiar()
    {
        Vector3 inicio = transform.position;
        Vector3 fin = inicio + Vector3.up * 2.5f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionApertura;
            transform.position = Vector3.Lerp(inicio, fin, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.42f);

        GameManager.Instance.CambiarEscena(escenaDestino);
    }

    private void CrearHumoTransicion()
    {
        GameObject nube = new GameObject("Humo_Transicion_Puerta");
        nube.transform.position = transform.position;
        nube.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        DontDestroyOnLoad(nube);
        Destroy(nube, 3.7f);

        Texture2D textura;
        Material material = CrearMaterialHumo(out textura);
        if (material == null) return;

        CrearCapaHumo(nube.transform, "Chorro_Vapor", material, 1.35f, 2.15f, 1.8f, 3.2f,
            new Vector3(2.4f, 2.5f, 0.35f), 92, 126, 18f, new Color(0.92f, 0.95f, 0.96f, 0.78f), 0.85f);
        CrearCapaHumo(nube.transform, "Nube_Densa", material, 2.2f, 3.25f, 2.5f, 4.1f,
            new Vector3(2.8f, 2.6f, 0.48f), 58, 82, 9f, new Color(0.72f, 0.77f, 0.8f, 0.65f), 0.58f);

        Destroy(material, 3.8f);
        Destroy(textura, 3.8f);
    }

    private static void CrearCapaHumo(Transform padre, string nombre, Material material,
        float vidaMin, float vidaMax, float tamanoMin, float tamanoMax, Vector3 area,
        short estallidoMin, short estallidoMax, float emision, Color tinte, float empuje)
    {
        GameObject capa = new GameObject(nombre);
        capa.transform.SetParent(padre, false);
        capa.transform.localPosition = Vector3.up * 0.65f;

        ParticleSystem humo = capa.AddComponent<ParticleSystem>();
        humo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = humo.main;
        main.duration = 0.92f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoMin, tamanoMax);
        main.startColor = tinte;
        main.gravityModifier = -0.035f;
        main.maxParticles = 220;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emissionModule = humo.emission;
        emissionModule.rateOverTime = emision;
        emissionModule.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, estallidoMin, estallidoMax),
            new ParticleSystem.Burst(0.22f, (short)(estallidoMin / 3), (short)(estallidoMax / 3)),
            new ParticleSystem.Burst(0.58f, (short)(estallidoMin / 4), (short)(estallidoMax / 4))
        });

        ParticleSystem.ShapeModule shape = humo.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = area;
        shape.randomDirectionAmount = 0.55f;

        ParticleSystem.VelocityOverLifetimeModule velocity = humo.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.95f);
        velocity.z = new ParticleSystem.MinMaxCurve(emision > 12f ? 0.55f : 0.15f, empuje);

        ParticleSystem.ColorOverLifetimeModule color = humo.colorOverLifetime;
        color.enabled = true;
        Gradient desvanecer = new Gradient();
        desvanecer.SetKeys(
            new[] { new GradientColorKey(tinte, 0f), new GradientColorKey(new Color(0.46f, 0.51f, 0.54f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(tinte.a, 0.08f), new GradientAlphaKey(tinte.a * 0.72f, 0.5f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(desvanecer);

        ParticleSystem.SizeOverLifetimeModule size = humo.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.48f), new Keyframe(0.42f, 1.08f), new Keyframe(1f, 1.85f)));

        ParticleSystem.NoiseModule noise = humo.noise;
        noise.enabled = true;
        noise.strength = 0.48f;
        noise.frequency = 0.38f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.24f);
        noise.quality = ParticleSystemNoiseQuality.Medium;

        ParticleSystemRenderer renderer = humo.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.maxParticleSize = 0.8f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = material;
        humo.Play();
    }

    private static Material CrearMaterialHumo(out Texture2D textura)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            textura = null;
            return null;
        }

        const int lado = 128;
        textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        textura.name = "Textura_Humo_Transicion";
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < lado; y++)
        for (int x = 0; x < lado; x++)
        {
            float nx = (x + 0.5f) / lado * 2f - 1f;
            float ny = (y + 0.5f) / lado * 2f - 1f;
            float deformacionX = Mathf.PerlinNoise(nx * 2.7f + 13f, ny * 2.7f + 7f) - 0.5f;
            float deformacionY = Mathf.PerlinNoise(nx * 2.7f + 37f, ny * 2.7f + 19f) - 0.5f;
            float humoX = nx + deformacionX * 0.34f;
            float humoY = ny + deformacionY * 0.34f;
            float radio = Mathf.Sqrt(humoX * humoX + humoY * humoY);

            // Ruido en varias escalas: bordes irregulares y pliegues internos como vapor turbulento.
            float volumen = Mathf.PerlinNoise(humoX * 3.2f + 5f, humoY * 3.2f + 11f);
            float pliegues = Mathf.PerlinNoise(humoX * 8.5f + 23f, humoY * 8.5f + 4f);
            float vetas = Mathf.PerlinNoise(humoX * 15f + humoY * 2.2f, humoY * 5.5f + 29f);
            float densidad = volumen * 0.52f + pliegues * 0.31f + vetas * 0.17f;
            float bordeIrregular = 1.02f + (volumen - 0.5f) * 0.42f + (pliegues - 0.5f) * 0.18f - radio;
            float silueta = Mathf.SmoothStep(0f, 0.42f, bordeIrregular + 0.16f);
            float alpha = silueta * Mathf.Clamp01(0.46f + densidad * 0.72f);

            // Paleta fría azul acero con un leve brillo cian propio del laboratorio.
            Color tono = new Color(0.62f + vetas * 0.08f, 0.76f + pliegues * 0.1f, 0.86f + volumen * 0.1f, alpha);
            textura.SetPixel(x, y, tono);
        }
        textura.Apply();
        Material material = new Material(shader) { name = "MAT_Humo_Transicion" };
        material.SetTexture("_BaseMap", textura);
        material.mainTexture = textura;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }
}
