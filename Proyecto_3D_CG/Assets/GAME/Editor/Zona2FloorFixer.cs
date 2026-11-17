using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Zona2FloorFixer: deja el piso de la Zona 2 en un estado seguro y apoya al jugador
/// exactamente sobre la superficie al entrar a la escena.
///
/// El problema reportado era que el jugador se caia al recorrer el suelo de la Zona 2.
/// Las causas tipicas son: el jugador nace atravesando el collider del piso, o pisa
/// placas decorativas que tambien tienen collider. Este reparador:
///   1. Garantiza que Piso_Laboratorio_Zona2 tenga un BoxCollider solido y no-trigger.
///   2. Quita los colliders de las placas decorativas (Piso_Placa_*), que son solo visuales.
///   3. Apoya al Player 5 cm por encima del piso real usando un raycast.
public static class Zona2FloorFixer
{
    private const string RutaEscena = "Assets/GAME/Scenes/Zona2_Laboratorio.unity";
    private const float MargenSobreElSuelo = 0.05f;

    [MenuItem("GAME/Zonas/Reparar piso de la Zona 2")]
    public static void RepararMenu()
    {
        Reparar();
        EditorUtility.DisplayDialog("Zona 2", "Piso revisado y jugador apoyado sobre la superficie.", "Aceptar");
    }

    // Punto de entrada para -executeMethod (batch mode).
    public static void RepararBatch()
    {
        Reparar();
    }

    public static void Reparar()
    {
        Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        var informe = new StringBuilder();
        informe.AppendLine("[Zona2FloorFixer] Informe de reparacion");

        // ------------------------------------------------------------------
        // 1. Piso principal
        // ------------------------------------------------------------------
        GameObject piso = GameObject.Find("Piso_Laboratorio_Zona2");
        if (piso == null)
        {
            Debug.LogError("[Zona2FloorFixer] No se encontro Piso_Laboratorio_Zona2.");
            return;
        }

        var cajaPiso = piso.GetComponent<BoxCollider>();
        if (cajaPiso == null) cajaPiso = piso.AddComponent<BoxCollider>();

        cajaPiso.center = Vector3.zero;
        cajaPiso.size = Vector3.one;
        cajaPiso.isTrigger = false;
        cajaPiso.enabled = true;

        Physics.SyncTransforms();
        Bounds limitesPiso = cajaPiso.bounds;
        informe.AppendLine($"Piso: centro={limitesPiso.center} tamano={limitesPiso.size} " +
                           $"superficie y={limitesPiso.max.y:F3}");

        if (limitesPiso.size.y < 0.1f)
            informe.AppendLine("AVISO: el piso es muy delgado; un CharacterController rapido podria atravesarlo.");

        // ------------------------------------------------------------------
        // 2. Placas decorativas: se quedan sin collider (solo son visuales)
        // ------------------------------------------------------------------
        int placasLimpiadas = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("Piso_Placa")) continue;

            foreach (var collider in t.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled) continue;
                collider.enabled = false;
                placasLimpiadas++;
            }
        }
        informe.AppendLine($"Colliders desactivados en placas decorativas: {placasLimpiadas}");

        // ------------------------------------------------------------------
        // 3. Apoyar al jugador sobre el piso
        // ------------------------------------------------------------------
        GameObject jugador = GameObject.Find("Player");
        if (jugador == null)
        {
            informe.AppendLine("AVISO: no se encontro el Player en la escena.");
        }
        else
        {
            Vector3 posicion = jugador.transform.position;
            float alturaSuelo = SuperficieBajo(jugador, posicion);

            if (float.IsNaN(alturaSuelo))
            {
                alturaSuelo = limitesPiso.max.y;
                informe.AppendLine("Sin raycast valido: se usa la superficie del piso principal.");
            }

            Vector3 destino = new Vector3(posicion.x, alturaSuelo + MargenSobreElSuelo, posicion.z);
            jugador.transform.position = destino;
            Physics.SyncTransforms();

            informe.AppendLine($"Player: {posicion} -> {destino}");
            informe.AppendLine("Colliders que tocan al jugador en el spawn: " + CollidersEnSpawn(jugador, destino));
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        informe.AppendLine("Escena guardada: " + escena.path);

        Debug.Log(informe.ToString());
    }

    // Lanza un rayo desde arriba ignorando los colliders del propio jugador.
    private static float SuperficieBajo(GameObject jugador, Vector3 posicion)
    {
        var collidersJugador = jugador.GetComponentsInChildren<Collider>(true);
        var estados = new List<bool>(collidersJugador.Length);
        foreach (var collider in collidersJugador)
        {
            estados.Add(collider.enabled);
            collider.enabled = false;
        }

        Vector3 origen = new Vector3(posicion.x, posicion.y + 5f, posicion.z);
        float superficie = float.NaN;
        if (Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, 20f, ~0, QueryTriggerInteraction.Ignore))
            superficie = golpe.point.y;

        for (int i = 0; i < collidersJugador.Length; i++)
            collidersJugador[i].enabled = estados[i];

        return superficie;
    }

    private static string CollidersEnSpawn(GameObject jugador, Vector3 posicion)
    {
        var capsula = jugador.GetComponent<CharacterController>();
        Vector3 centro = posicion;
        float altura = 2f;
        float radio = 0.4f;

        if (capsula != null)
        {
            altura = capsula.height;
            radio = capsula.radius;
            centro = posicion + Vector3.up * (capsula.center.y);
        }

        Vector3 arriba = centro + Vector3.up * Mathf.Max(0f, altura * 0.5f - radio);
        Vector3 abajo = centro - Vector3.up * Mathf.Max(0f, altura * 0.5f - radio);

        var encontrados = new StringBuilder();
        foreach (var collider in Physics.OverlapCapsule(arriba, abajo, radio - 0.01f,
                     ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform.IsChildOf(jugador.transform)) continue;
            if (encontrados.Length > 0) encontrados.Append(", ");
            encontrados.Append(collider.name);
        }

        return encontrados.Length > 0 ? encontrados.ToString() : "ninguno";
    }
}
