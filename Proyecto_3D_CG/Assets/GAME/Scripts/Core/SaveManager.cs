using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    public List<string> bitacoraCola = new List<string>();
    public List<ParBool> puertas = new List<ParBool>();
    public List<ParBool> objetivos = new List<ParBool>();
}

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

public static class SaveManager
{
    private static string Ruta
    {
        get { return Path.Combine(Application.streamingAssetsPath, "guardado.json"); }
    }

    private static string RutaAntigua
    {
        get { return Path.Combine(Application.persistentDataPath, "guardado.json"); }
    }

    private static void AsegurarCarpeta()
    {
        string carpeta = Path.GetDirectoryName(Ruta);
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
    }

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

    // Extrae los elementos de una Stack usando estrictamente Peek, Pop y Push (LIFO)
    // dejando la pila original intacta y en el mismo orden.
    public static List<string> ExtraerContenidoPila(Stack<string> pila)
    {
        List<string> plano = new List<string>();
        if (pila == null || pila.Count == 0) return plano;

        Stack<string> auxiliar = new Stack<string>();
        while (pila.Count > 0)
        {
            string tope = pila.Peek();
            pila.Pop();
            plano.Add(tope);
            auxiliar.Push(tope);
        }

        while (auxiliar.Count > 0)
        {
            string topeAux = auxiliar.Peek();
            auxiliar.Pop();
            pila.Push(topeAux);
        }

        return plano;
    }

    // Extrae los elementos de una Queue usando estrictamente Peek, Dequeue y Enqueue (FIFO)
    // dejando la cola original intacta y en el mismo orden.
    public static List<string> ExtraerContenidoCola(Queue<string> cola)
    {
        List<string> plano = new List<string>();
        if (cola == null || cola.Count == 0) return plano;

        int cantidad = cola.Count;
        for (int i = 0; i < cantidad; i++)
        {
            string frente = cola.Peek();
            cola.Dequeue();
            plano.Add(frente);
            cola.Enqueue(frente);
        }

        return plano;
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

        // Stack -> contenido plano extraído con Peek/Pop/Push empezando por el tope.
        datos.modulosTerminalZ1 = ExtraerContenidoPila(gm.modulosTerminalZ1);
        datos.cantidadModulosRetiradosZ1 = gm.cantidadModulosRetiradosZ1;

        // Queue -> contenido plano extraído con Peek/Dequeue/Enqueue respetando orden FIFO.
        datos.bitacoraCola = ExtraerContenidoCola(gm.colaBitacora);

        // Dictionary -> List<ParBool>.
        foreach (KeyValuePair<string, bool> par in gm.puertas)
            datos.puertas.Add(new ParBool { clave = par.Key, valor = par.Value });

        foreach (KeyValuePair<string, bool> par in gm.objetivos)
            datos.objetivos.Add(new ParBool { clave = par.Key, valor = par.Value });

        return datos;
    }

    public static void GuardarDatos(DatosGuardado datos)
    {
        if (datos == null) return;

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
        gm.muertes = conservarMuertes ? Mathf.Max(gm.muertes, datos.muertes) : datos.muertes;
        gm.tiempoJugadoSegundos = datos.tiempoJugadoSegundos;
        gm.escenaActual = datos.escenaActual;

        gm.inventario = datos.inventario != null ? new List<string>(datos.inventario) : new List<string>();
        gm.codigos = datos.codigos != null ? new List<string>(datos.codigos) : new List<string>();

        // Reconstruir Stack: se apila desde el fondo hacia el tope con Push() para conservar el mismo Peek().
        gm.modulosTerminalZ1.Clear();
        if (datos.modulosTerminalZ1 != null)
        {
            for (int i = datos.modulosTerminalZ1.Count - 1; i >= 0; i--)
                gm.modulosTerminalZ1.Push(datos.modulosTerminalZ1[i]);
        }
        gm.cantidadModulosRetiradosZ1 = datos.cantidadModulosRetiradosZ1;

        // Reconstruir Queue: se encola en orden FIFO con Enqueue().
        gm.colaBitacora.Clear();
        if (datos.bitacoraCola != null)
        {
            for (int i = 0; i < datos.bitacoraCola.Count; i++)
                gm.colaBitacora.Enqueue(datos.bitacoraCola[i]);
        }

        // Reconstruir Dictionary a partir de las parejas clave/valor.
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
        if (ExisteGuardado()) File.Delete(Ruta);
        if (File.Exists(RutaAntigua)) File.Delete(RutaAntigua);
    }
}
