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

    public static void Guardar()
    {
        if (GameManager.Instance == null) return;

        DatosGuardado datos = new DatosGuardado();
        datos.escenaActual = GameManager.Instance.escenaActual;
        datos.vidaJugador = GameManager.Instance.vidaJugador;
        datos.tiempoJugadoSegundos = GameManager.Instance.tiempoJugadoSegundos;
        datos.inventario = new List<string>(GameManager.Instance.inventario);
        datos.codigos = new List<string>(GameManager.Instance.codigos);

        // La pila se guarda como lista, empezando por el modulo de arriba.
        datos.modulosTerminalZ1 = new List<string>(GameManager.Instance.modulosTerminalZ1);
        datos.cantidadModulosRetiradosZ1 = GameManager.Instance.cantidadModulosRetiradosZ1;

        // Dictionary -> List<ParBool> (JsonUtility no serializa Dictionary).
        foreach (KeyValuePair<string, bool> par in GameManager.Instance.puertas)
            datos.puertas.Add(new ParBool { clave = par.Key, valor = par.Value });

        foreach (KeyValuePair<string, bool> par in GameManager.Instance.objetivos)
            datos.objetivos.Add(new ParBool { clave = par.Key, valor = par.Value });

        // true = JSON con saltos de linea, para poder leerlo con el Bloc de notas.
        string json = JsonUtility.ToJson(datos, true);
        AsegurarCarpeta();
        File.WriteAllText(Ruta, json);
        Debug.Log("Partida guardada en: " + Ruta);
    }

    public static void Cargar()
    {
        if (GameManager.Instance == null) return;
        if (!ExisteGuardado()) return;

        string json = File.ReadAllText(Ruta);
        DatosGuardado datos = JsonUtility.FromJson<DatosGuardado>(json);
        if (datos == null) return;

        GameManager.Instance.vidaJugador = datos.vidaJugador;
        GameManager.Instance.tiempoJugadoSegundos = datos.tiempoJugadoSegundos;
        GameManager.Instance.escenaActual = datos.escenaActual;

        // Se reconstruyen las List (si el JSON venia sin ese campo, queda vacia).
        GameManager.Instance.inventario = datos.inventario ?? new List<string>();
        GameManager.Instance.codigos = datos.codigos ?? new List<string>();

        // Se agregan desde el fondo para conservar el mismo modulo arriba.
        GameManager.Instance.modulosTerminalZ1.Clear();
        if (datos.modulosTerminalZ1 != null)
            for (int i = datos.modulosTerminalZ1.Count - 1; i >= 0; i--)
                GameManager.Instance.modulosTerminalZ1.Push(datos.modulosTerminalZ1[i]);
        GameManager.Instance.cantidadModulosRetiradosZ1 = datos.cantidadModulosRetiradosZ1;

        // Se reconstruyen las Dictionary a partir de la lista de parejas.
        GameManager.Instance.puertas.Clear();
        if (datos.puertas != null)
            foreach (ParBool par in datos.puertas)
                GameManager.Instance.puertas[par.clave] = par.valor;

        GameManager.Instance.objetivos.Clear();
        if (datos.objetivos != null)
            foreach (ParBool par in datos.objetivos)
                GameManager.Instance.objetivos[par.clave] = par.valor;

        Debug.Log("Partida cargada desde: " + Ruta);
    }

    public static void BorrarGuardado()
    {
        // Se borran las dos rutas: si quedara la vieja, volveria a migrarse al continuar.
        if (ExisteGuardado()) File.Delete(Ruta);
        if (File.Exists(RutaAntigua)) File.Delete(RutaAntigua);
    }
}
