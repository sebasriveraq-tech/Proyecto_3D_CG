using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// SerializationModeFixer: deja el proyecto en serializacion de texto y convierte
/// las escenas que todavia estaban guardadas en binario.
///
/// Dos requisitos para que una escena se guarde en texto:
/// 1. ProjectSettings/EditorSettings.asset con m_SerializationMode = 2 (Force Text).
/// 2. Ningun objeto nativo binario incrustado en la escena. En este proyecto el
///    culpable es el NavMeshData horneado dentro de las zonas: Unity no puede
///    escribirlo en YAML y cae a binario. Se elimina el dato incrustado y, si una
///    NavMeshSurface lo estaba usando, se externaliza primero a un .asset.
public static class SerializationModeFixer
{
    private const string EscenaAuditoria = "Assets/GAME/Scenes/__Zona2_Laboratorio_AuditCopy.unity";

    [MenuItem("GAME/Proyecto/Convertir escenas y prefabs a texto")]
    public static void ConvertirMenu()
    {
        Convertir();
        EditorUtility.DisplayDialog("Serializacion",
            "Proyecto en modo texto. Revisa la consola para ver el informe.", "Aceptar");
    }

    // Punto de entrada para -executeMethod (batch mode).
    public static void ConvertirBatch()
    {
        Convertir();
    }

    public static void Convertir()
    {
        var informe = new StringBuilder("[SerializationModeFixer] Informe\n");

        // 1. Modo de serializacion del proyecto: texto.
        EditorSettings.serializationMode = SerializationMode.ForceText;
        AssetDatabase.SaveAssets();
        informe.AppendLine("Modo de serializacion: " + EditorSettings.serializationMode);

        // 2. La copia de auditoria de la Zona 2 no forma parte del proyecto.
        if (File.Exists(EscenaAuditoria) || File.Exists(EscenaAuditoria + ".meta"))
            AssetDatabase.DeleteAsset(EscenaAuditoria);

        // 3. Escenas en binario: limpiar datos nativos incrustados y re-guardar.
        foreach (string ruta in Buscar("t:Scene"))
        {
            if (!EsBinario(ruta)) continue;
            ConvertirEscena(ruta, informe);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Prefabs u otros assets que sigan en binario.
        var binarios = new List<string>();
        foreach (string ruta in Buscar("t:Prefab"))
            if (EsBinario(ruta)) binarios.Add(ruta);

        if (binarios.Count > 0)
        {
            informe.AppendLine("Prefabs en binario: " + binarios.Count);
            AssetDatabase.ForceReserializeAssets(binarios,
                ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 5. Informe final.
        foreach (string ruta in Buscar("t:Scene"))
            informe.AppendLine($"Escena   {(EsBinario(ruta) ? "BINARIA" : "texto  ")}  {ruta}");
        foreach (string ruta in Buscar("t:Prefab"))
        {
            if (EsBinario(ruta))
                informe.AppendLine("Prefab   BINARIO  " + ruta);
        }

        Debug.Log(informe.ToString());
    }

    private static void ConvertirEscena(string ruta, StringBuilder informe)
    {
        informe.AppendLine("Escena binaria: " + ruta);
        Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

        // NavMeshData incrustado en la escena (sin ruta de asset): se elimina.
        var incrustados = new List<NavMeshData>();
        foreach (NavMeshData datos in Resources.FindObjectsOfTypeAll<NavMeshData>())
        {
            if (datos == null || EditorUtility.IsPersistent(datos)) continue;
            if (AssetDatabase.GetAssetPath(datos) != "") continue;
            incrustados.Add(datos);
        }

        if (incrustados.Count == 0)
        {
            informe.AppendLine("  Sin NavMeshData incrustado.");
        }
        else
        {
            // Si una superficie usa el dato incrustado, se lleva a un .asset propio.
            foreach (NavMeshSurface superficie in Object.FindObjectsByType<NavMeshSurface>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                NavMeshData datos = superficie.navMeshData;
                if (datos == null) continue;
                if (AssetDatabase.GetAssetPath(datos) != "") continue;

                string destino = RutaAssetNavMesh(ruta, datos.name);
                NavMeshData copia = Object.Instantiate(datos);
                copia.name = Path.GetFileNameWithoutExtension(destino);
                AssetDatabase.CreateAsset(copia, destino);
                superficie.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(destino);
                informe.AppendLine("  NavMeshSurface usa dato incrustado -> " + destino);
            }

            foreach (NavMeshData datos in incrustados)
            {
                if (datos == null) continue;
                string nombre = datos.name;
                Object.DestroyImmediate(datos, true);
                informe.AppendLine("  NavMeshData incrustado eliminado: " + nombre);
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        bool guardada = EditorSceneManager.SaveScene(escena, ruta);
        EditorSceneManager.CloseScene(escena, true);
        AssetDatabase.ImportAsset(ruta);

        informe.AppendLine($"  SaveScene={guardada} bytes={new FileInfo(ruta).Length} " +
                           $"resultado={(EsBinario(ruta) ? "BINARIO" : "texto")}");
    }

    private static string RutaAssetNavMesh(string rutaEscena, string nombreDatos)
    {
        string carpeta = Path.GetDirectoryName(rutaEscena).Replace('\\', '/');
        string baseNombre = "NavMesh_" + Path.GetFileNameWithoutExtension(rutaEscena);
        string candidato = carpeta + "/" + baseNombre + ".asset";
        int n = 1;
        while (File.Exists(candidato))
            candidato = carpeta + "/" + baseNombre + "_" + (n++) + ".asset";
        return candidato;
    }

    private static List<string> Buscar(string filtro)
    {
        var rutas = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets(filtro, new[] { "Assets" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(ruta) || Directory.Exists(ruta)) continue;
            rutas.Add(ruta);
        }
        rutas.Sort();
        return rutas;
    }

    private static bool EsBinario(string ruta)
    {
        try
        {
            using (var stream = File.OpenRead(ruta))
            {
                var buffer = new byte[16];
                int leidos = stream.Read(buffer, 0, buffer.Length);
                if (leidos < 5) return false;

                // Los archivos de texto de Unity empiezan con "%YAML".
                return !(buffer[0] == (byte)'%' && buffer[1] == (byte)'Y' &&
                         buffer[2] == (byte)'A' && buffer[3] == (byte)'M' &&
                         buffer[4] == (byte)'L');
            }
        }
        catch
        {
            return false;
        }
    }
}
