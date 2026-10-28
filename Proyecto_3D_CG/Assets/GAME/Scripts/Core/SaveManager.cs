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
    // Application.persistentDataPath es una carpeta del usuario donde SI se puede escribir
    // (StreamingAssets queda de solo lectura cuando se compila el juego).
    private static string Ruta
    {
        get { return Path.Combine(Application.persistentDataPath, "guardado.json"); }
    }

    public static bool ExisteGuardado()
    {
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

        // Dictionary -> List<ParBool> (JsonUtility no serializa Dictionary).
        foreach (KeyValuePair<string, bool> par in GameManager.Instance.puertas)
            datos.puertas.Add(new ParBool { clave = par.Key, valor = par.Value });

        foreach (KeyValuePair<string, bool> par in GameManager.Instance.objetivos)
            datos.objetivos.Add(new ParBool { clave = par.Key, valor = par.Value });

        // true = JSON con saltos de linea, para poder leerlo con el Bloc de notas.
        string json = JsonUtility.ToJson(datos, true);
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
        if (ExisteGuardado()) File.Delete(Ruta);
    }
}
