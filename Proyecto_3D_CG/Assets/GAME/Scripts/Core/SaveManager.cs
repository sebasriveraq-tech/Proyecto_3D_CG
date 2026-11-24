using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// DatosGuardado: es el "molde" del archivo JSON. Solo guarda tipos que Unity sabe convertir
/// (string, int, float, List<string>, List<ParBool>). Las Dictionary NO se pueden guardar directo.
[System.Serializable]
public class DatosGuardado
{
    public string escenaActual;
    public int vidaJugador;
    public int muertes;
    public float tiempoJugadoSegundos;
    public List<string> inventario = new List<string>();
    public List<string> codigos = new List<string>();
    public List<string> modulosTerminalZ1 = new List<string>();
    public int cantidadModulosRetiradosZ1;
    public List<ParBool> puertas = new List<ParBool>();
    public List<ParBool> objetivos = new List<ParBool>();
}

/// ParBool: una pareja clave/valor para poder guardar las Dictionary como lista.
[System.Serializable]
public class ParBool
{
    public string clave;
    public bool valor;
}

[System.Serializable]
public class ResultadoPartida
{
    public string resultado = "Victoria";
    public int vidaRestante;
    public int vidaMaxima;
    public int muertes;
    public List<string> inventario;
}

/// SaveManager: convierte el estado del GameManager a JSON y lo vuelve a leer.
/// Es estatico (no es MonoBehaviour) porque no necesita estar en la escena:
/// solo recibe datos y escribe un archivo.
public static class SaveManager
{
    // El enunciado pide que el JSON se vea dentro del proyecto: va en StreamingAssets.
    // OJO: en un juego compilado esa carpeta queda de solo lectura; para la entrega
    // del curso el guardado se escribe igualmente ahi.
    private static string Ruta
    {
        get { return Path.Combine(Application.streamingAssetsPath, "guardado.json"); }
    }

    // Carpeta vieja donde se guardaba antes de mover el JSON a StreamingAssets.
    private static string RutaAntigua
    {
        get { return Path.Combine(Application.persistentDataPath, "guardado.json"); }
    }

    private static void AsegurarCarpeta()
    {
        string carpeta = Path.GetDirectoryName(Ruta);
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
    }

    // Si todavia no existe el JSON nuevo pero hay uno en la carpeta vieja,
    // se traslada una vez para no perder la partida en curso.
    private static void MigrarGuardadoAntiguo()
    {
        if (File.Exists(Ruta) || !File.Exists(RutaAntigua)) return;
        AsegurarCarpeta();
        File.Copy(RutaAntigua, Ruta);
    }

    public static bool ExisteGuardado()
    {
        MigrarGuardadoAntiguo();
        return File.Exists(Ruta);
    }

    public static DatosGuardado CapturarEstadoActual(string escenaOverride = null)
    {
        if (GameManager.Instance == null) return null;

        GameManager gm = GameManager.Instance;
        DatosGuardado datos = new DatosGuardado();
        datos.escenaActual = !string.IsNullOrEmpty(escenaOverride) ? escenaOverride : gm.escenaActual;
        datos.vidaJugador = Mathf.Max(1, gm.vidaJugador);
        datos.muertes = gm.muertes;
        datos.tiempoJugadoSegundos = gm.tiempoJugadoSegundos;
        datos.inventario = new List<string>(gm.inventario);
        datos.codigos = new List<string>(gm.codigos);

        // La pila se guarda como lista, empezando por el modulo de arriba.
        datos.modulosTerminalZ1 = new List<string>(gm.modulosTerminalZ1);
        datos.cantidadModulosRetiradosZ1 = gm.cantidadModulosRetiradosZ1;

        // Dictionary -> List<ParBool> (JsonUtility no serializa Dictionary).
        foreach (KeyValuePair<string, bool> par in gm.puertas)
            datos.puertas.Add(new ParBool { clave = par.Key, valor = par.Value });

        foreach (KeyValuePair<string, bool> par in gm.objetivos)
            datos.objetivos.Add(new ParBool { clave = par.Key, valor = par.Value });

        return datos;
    }

    public static void GuardarDatos(DatosGuardado datos)
    {
        if (datos == null) return;

        // true = JSON con saltos de linea, para poder leerlo con el Bloc de notas.
        string json = JsonUtility.ToJson(datos, true);
        AsegurarCarpeta();
        File.WriteAllText(Ruta, json);
        Debug.Log("Partida guardada en: " + Ruta);
    }

    public static void Guardar()
    {
        DatosGuardado datos = CapturarEstadoActual();
        GuardarDatos(datos);
    }

    public static DatosGuardado LeerDatos()
    {
        if (!ExisteGuardado()) return null;

        string json = File.ReadAllText(Ruta);
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonUtility.FromJson<DatosGuardado>(json);
    }

    public static void AplicarDatos(DatosGuardado datos, bool conservarMuertes = true)
    {
        if (GameManager.Instance == null || datos == null) return;

        GameManager gm = GameManager.Instance;
        gm.vidaJugador = datos.vidaJugador;
        // Reintentar un checkpoint conserva las muertes acumuladas si corresponde.
        gm.muertes = conservarMuertes ? Mathf.Max(gm.muertes, datos.muertes) : datos.muertes;
        gm.tiempoJugadoSegundos = datos.tiempoJugadoSegundos;
        gm.escenaActual = datos.escenaActual;

        // Se reconstruyen las List nuevas para no compartir referencia con el snapshot.
        gm.inventario = datos.inventario != null ? new List<string>(datos.inventario) : new List<string>();
        gm.codigos = datos.codigos != null ? new List<string>(datos.codigos) : new List<string>();

        // Se agregan desde el fondo para conservar el mismo modulo arriba.
        gm.modulosTerminalZ1.Clear();
        if (datos.modulosTerminalZ1 != null)
            for (int i = datos.modulosTerminalZ1.Count - 1; i >= 0; i--)
                gm.modulosTerminalZ1.Push(datos.modulosTerminalZ1[i]);
        gm.cantidadModulosRetiradosZ1 = datos.cantidadModulosRetiradosZ1;

        // Se reconstruyen las Dictionary a partir de la lista de parejas.
        gm.puertas.Clear();
        if (datos.puertas != null)
            foreach (ParBool par in datos.puertas)
                gm.puertas[par.clave] = par.valor;

        gm.objetivos.Clear();
        if (datos.objetivos != null)
            foreach (ParBool par in datos.objetivos)
                gm.objetivos[par.clave] = par.valor;

        Debug.Log("Partida cargada desde checkpoint: " + datos.escenaActual);
    }

    public static void Cargar()
    {
        DatosGuardado datos = LeerDatos();
        if (datos == null) return;
        AplicarDatos(datos, true);
    }

    public static bool GuardarPuntuacion(ResultadoPartida resultado, out string ruta)
    {
        ruta = Path.Combine(Application.streamingAssetsPath, "puntuacion.json");
        string temporal = ruta + ".tmp";
        try
        {
            AsegurarCarpeta();
            File.WriteAllText(temporal, JsonUtility.ToJson(resultado, true));
            if (File.Exists(ruta))
                File.Replace(temporal, ruta, null);
            else
                File.Move(temporal, ruta);
            return true;
        }
        catch (System.Exception error) when (error is IOException || error is System.UnauthorizedAccessException)
        {
            Debug.LogWarning("No se pudo guardar la puntuación: " + error.Message);
            return false;
        }
    }

    public static void BorrarGuardado()
    {
        // Se borran las dos rutas: si quedara la vieja, volveria a migrarse al continuar.
        if (ExisteGuardado()) File.Delete(Ruta);
        if (File.Exists(RutaAntigua)) File.Delete(RutaAntigua);
    }
}
