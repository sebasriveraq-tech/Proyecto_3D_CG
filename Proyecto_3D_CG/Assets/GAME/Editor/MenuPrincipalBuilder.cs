using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// MenuPrincipalBuilder: arma el menu principal dentro de Canvas_Principal.prefab.
/// El menu queda desplegado con la introduccion, las mecanicas y los cuatro botones
/// (Nueva partida, Continuar, Como jugar, Salir), ademas del panel de ayuda.
public static class MenuPrincipalBuilder
{
    private const string RutaPrefab = "Assets/GAME/Prefabs/UI/Canvas_Principal.prefab";

    private static readonly Color Cian = new Color(0.208f, 0.769f, 0.745f, 1f);
    private static readonly Color TextoClaro = new Color(0.918f, 0.949f, 0.961f, 1f);
    private static readonly Color TextoSuave = new Color(0.647f, 0.714f, 0.761f, 1f);
    private static readonly Color FondoTarjeta = new Color(0.055f, 0.086f, 0.118f, 0.94f);
    private static readonly Color BotonNeutro = new Color(0.125f, 0.180f, 0.227f, 1f);

    private const string TextoMision =
        "Estas atrapado en el laboratorio abandonado de la universidad. Recupera las credenciales de acceso, " +
        "descifra los codigos de seguridad y abre las puertas que comunican la recepcion, el laboratorio y la sala de control.\n" +
        "Cada zona esconde objetos y pistas: explora con calma, guarda lo que encuentres y conserva la integridad.";

    private const string TextoControles =
        "WASD  Mover        RATON  Mirar        SHIFT  Correr        ESPACIO  Saltar\n" +
        "E  Interactuar        I  Inventario        V  Camara        ESC  Pausa";

    private const string TextoMovimiento =
        "W A S D\nDesplazarse por la zona.\n\n" +
        "RATON\nGirar la vista y apuntar.\n\n" +
        "SHIFT\nCorrer para recorrer distancias largas.\n\n" +
        "ESPACIO\nSaltar obstaculos bajos.\n\n" +
        "V\nAlternar camara en primera y tercera persona.";

    private const string TextoInteraccion =
        "E\nInteractuar con puertas, terminales, cajones e interruptores.\n\n" +
        "I\nAbrir el inventario y revisar los codigos descubiertos.\n\n" +
        "ESC\nPausar la partida y liberar el cursor.\n\n" +
        "ENTER / C\nEmpezar una partida nueva o continuar la guardada.";

    private const string TextoObjetivos =
        "1. Recorre las tres zonas del laboratorio.\n\n" +
        "2. Recoge credenciales y codigos: quedan guardados en el inventario.\n\n" +
        "3. Usa los codigos en las terminales para abrir las puertas de salida.\n\n" +
        "4. Llega a la sala de control y activa el sistema final.";

    [MenuItem("GAME/UI/Construir menu principal")]
    public static void ConstruirMenu()
    {
        Construir();
        EditorUtility.DisplayDialog("Menu principal", "Menu y panel de ayuda actualizados.", "Aceptar");
    }

    // Punto de entrada para -executeMethod (batch mode).
    public static void ConstruirBatch()
    {
        Construir();
    }

    public static void Construir()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        try
        {
            Transform canvas = raiz.transform;
            GameObject panelMenu = BuscarHijo(canvas, "Panel_Menu");
            GameObject panelDerrota = BuscarHijo(canvas, "Panel_Derrota");

            if (panelMenu == null || panelDerrota == null)
            {
                Debug.LogError("[MenuPrincipalBuilder] No se encontro Panel_Menu o Panel_Derrota.");
                return;
            }

            GameObject plantillaTexto = BuscarHijo(panelMenu.transform, "Descripcion_Menu");
            GameObject plantillaBoton = BuscarHijo(panelDerrota.transform, "BtnReintentar");
            if (plantillaTexto == null || plantillaBoton == null)
            {
                Debug.LogError("[MenuPrincipalBuilder] Faltan las plantillas de texto o de boton.");
                return;
            }

            // 1. Quitar lo que el constructor genero en una corrida anterior.
            LimpiarGenerados(canvas, panelMenu.transform);

            // 2. Encabezado del menu.
            RectTransform rtTitulo = Rect(panelMenu.transform, "Titulo");
            if (rtTitulo != null)
            {
                Colocar(rtTitulo, new Vector2(0f, 352f), new Vector2(1500f, 100f));
                EstiloTexto(rtTitulo.GetComponent<TextMeshProUGUI>(), 64, TextoClaro, TextAlignmentOptions.Center, true);
            }

            RectTransform rtSubtitulo = Rect(panelMenu.transform, "Subtitulo");
            if (rtSubtitulo != null)
            {
                Colocar(rtSubtitulo, new Vector2(0f, 286f), new Vector2(1500f, 40f));
                var subtitulo = rtSubtitulo.GetComponent<TextMeshProUGUI>();
                EstiloTexto(subtitulo, 20, Cian, TextAlignmentOptions.Center, false);
                if (subtitulo != null) subtitulo.characterSpacing = 3f;
            }

            RectTransform rtLinea = Rect(panelMenu.transform, "Linea_Menu");
            if (rtLinea != null)
            {
                Colocar(rtLinea, new Vector2(0f, 238f), new Vector2(460f, 2f));
                var imagen = rtLinea.GetComponent<Image>();
                if (imagen != null) imagen.color = new Color(Cian.r, Cian.g, Cian.b, 0.6f);
            }

            // 3. Tarjeta de la mision (introduccion del juego).
            GameObject cardMision = CrearTarjeta(panelMenu.transform, "Card_Mision",
                new Vector2(0f, 88f), new Vector2(1320f, 250f));
            CrearAcento(cardMision.transform, 6f);

            CrearTexto(plantillaTexto, cardMision.transform, "Encabezado_Mision", "LA MISION",
                24, Cian, TextAlignmentOptions.Left, new Vector2(0f, 96f), new Vector2(1240f, 34f), true);

            // El texto original del menu pasa a ser el cuerpo de la mision.
            RectTransform rtDescripcion = Rect(panelMenu.transform, "Descripcion_Menu");
            if (rtDescripcion != null)
            {
                rtDescripcion.SetParent(cardMision.transform, false);
                Colocar(rtDescripcion, new Vector2(0f, -16f), new Vector2(1240f, 180f));
                var descripcion = rtDescripcion.GetComponent<TextMeshProUGUI>();
                EstiloTexto(descripcion, 21, TextoSuave, TextAlignmentOptions.TopLeft, false);
                if (descripcion != null)
                {
                    descripcion.text = TextoMision;
                    descripcion.lineSpacing = 6f;
                }
            }

            // 4. Tarjeta de mecanicas y controles.
            GameObject cardControles = CrearTarjeta(panelMenu.transform, "Card_Controles",
                new Vector2(0f, -142f), new Vector2(1320f, 160f));
            CrearAcento(cardControles.transform, 6f);

            CrearTexto(plantillaTexto, cardControles.transform, "Encabezado_Controles", "MECANICAS Y CONTROLES",
                24, Cian, TextAlignmentOptions.Left, new Vector2(0f, 52f), new Vector2(1240f, 32f), true);

            CrearTexto(plantillaTexto, cardControles.transform, "Texto_Controles", TextoControles,
                19, TextoClaro, TextAlignmentOptions.Left, new Vector2(0f, -10f), new Vector2(1240f, 90f), false);

            // 5. Botones del menu.
            GameObject fila = CrearContenedor(panelMenu.transform, "Fila_Botones",
                new Vector2(0f, -302f), new Vector2(1320f, 64f));

            ControlPaneles control = raiz.GetComponent<ControlPaneles>();
            if (control == null) control = raiz.AddComponent<ControlPaneles>();

            Button btnNueva = CrearBoton(plantillaBoton, fila.transform, "BtnNuevaPartida", "NUEVA PARTIDA",
                new Vector2(-495f, 0f), new Vector2(310f, 60f), Cian,
                new UnityAction(control.BotonNuevaPartida));

            Button btnContinuar = CrearBoton(plantillaBoton, fila.transform, "BtnContinuar", "CONTINUAR",
                new Vector2(-165f, 0f), new Vector2(310f, 60f), BotonNeutro,
                new UnityAction(control.BotonContinuar));

            CrearBoton(plantillaBoton, fila.transform, "BtnComoJugar", "COMO JUGAR",
                new Vector2(165f, 0f), new Vector2(310f, 60f), BotonNeutro,
                new UnityAction(control.BotonComoJugar));

            CrearBoton(plantillaBoton, fila.transform, "BtnSalir", "SALIR",
                new Vector2(495f, 0f), new Vector2(310f, 60f), BotonNeutro,
                new UnityAction(control.BotonSalir));

            TextMeshProUGUI textoEstado = CrearTexto(plantillaTexto, panelMenu.transform, "Texto_Estado", "",
                17, TextoSuave, TextAlignmentOptions.Center, new Vector2(0f, -366f), new Vector2(1320f, 30f), false);

            // 6. Pie del menu.
            RectTransform rtPie = Rect(panelMenu.transform, "Pie_Menu");
            if (rtPie != null)
            {
                Colocar(rtPie, new Vector2(0f, -470f), new Vector2(1400f, 40f));
                EstiloTexto(rtPie.GetComponent<TextMeshProUGUI>(), 16, TextoSuave, TextAlignmentOptions.Center, false);
            }

            // 7. Panel de ayuda "Como jugar".
            GameObject panelAyuda = CrearPanelComoJugar(canvas, plantillaTexto, plantillaBoton, control);

            // 8. Conectar el Canvas con los paneles nuevos.
            UIManager ui = raiz.GetComponent<UIManager>();
            if (ui != null) ui.panelComoJugar = panelAyuda;

            MenuPrincipalUI menuUI = panelMenu.GetComponent<MenuPrincipalUI>();
            if (menuUI == null) menuUI = panelMenu.AddComponent<MenuPrincipalUI>();
            menuUI.botonContinuar = btnContinuar;
            menuUI.textoEstado = textoEstado;

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Debug.Log("[MenuPrincipalBuilder] Menu principal construido y guardado.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        AssetDatabase.SaveAssets();
    }

    private static GameObject CrearPanelComoJugar(Transform canvas, GameObject plantillaTexto,
        GameObject plantillaBoton, ControlPaneles control)
    {
        GameObject anterior = BuscarHijo(canvas, "Panel_ComoJugar");
        if (anterior != null) Object.DestroyImmediate(anterior);

        GameObject panel = new GameObject("Panel_ComoJugar", typeof(RectTransform));
        panel.layer = 5;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        var fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.02f, 0.035f, 0.055f, 1f);
        fondo.raycastTarget = true;

        CrearTexto(plantillaTexto, panel.transform, "Titulo_ComoJugar", "COMO JUGAR",
            56, TextoClaro, TextAlignmentOptions.Center, new Vector2(0f, 440f), new Vector2(1500f, 90f), true);

        CrearTexto(plantillaTexto, panel.transform, "Subtitulo_ComoJugar",
            "MECANICAS DEL JUGADOR E INTERACCION CON LAS ESCENAS",
            20, Cian, TextAlignmentOptions.Center, new Vector2(0f, 378f), new Vector2(1500f, 40f), false);

        CrearTarjetaConTexto(plantillaTexto, panel.transform, "Card_Movimiento", "MOVIMIENTO", TextoMovimiento,
            new Vector2(-460f, 100f), new Vector2(410f, 440f));
        CrearTarjetaConTexto(plantillaTexto, panel.transform, "Card_Interaccion", "INTERACCION", TextoInteraccion,
            new Vector2(0f, 100f), new Vector2(410f, 440f));
        CrearTarjetaConTexto(plantillaTexto, panel.transform, "Card_Objetivos", "OBJETIVOS", TextoObjetivos,
            new Vector2(460f, 100f), new Vector2(410f, 440f));

        CrearBoton(plantillaBoton, panel.transform, "BtnVolverComoJugar", "VOLVER AL MENU",
            new Vector2(0f, -320f), new Vector2(360f, 64f), Cian,
            new UnityAction(control.CerrarComoJugar));

        CrearTexto(plantillaTexto, panel.transform, "Pie_ComoJugar",
            "ESC tambien vuelve al menu principal",
            16, TextoSuave, TextAlignmentOptions.Center, new Vector2(0f, -430f), new Vector2(1400f, 30f), false);

        panel.SetActive(false);
        return panel;
    }

    private static void CrearTarjetaConTexto(GameObject plantillaTexto, Transform padre, string nombre,
        string encabezado, string cuerpo, Vector2 posicion, Vector2 tamano)
    {
        GameObject tarjeta = CrearTarjeta(padre, nombre, posicion, tamano);
        CrearAcento(tarjeta.transform, 5f);

        float mitad = tamano.y * 0.5f;
        CrearTexto(plantillaTexto, tarjeta.transform, "Encabezado", encabezado,
            24, Cian, TextAlignmentOptions.Left, new Vector2(0f, mitad - 44f), new Vector2(tamano.x - 70f, 32f), true);

        CrearTexto(plantillaTexto, tarjeta.transform, "Cuerpo", cuerpo,
            18, TextoSuave, TextAlignmentOptions.TopLeft, new Vector2(0f, -46f), new Vector2(tamano.x - 70f, tamano.y - 110f), false);
    }

    private static GameObject CrearTarjeta(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
    {
        GameObject tarjeta = new GameObject(nombre, typeof(RectTransform));
        tarjeta.layer = 5;
        RectTransform rt = tarjeta.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        var imagen = tarjeta.AddComponent<Image>();
        imagen.color = FondoTarjeta;
        imagen.raycastTarget = false;
        return tarjeta;
    }

    // Barra vertical fina en el borde izquierdo de una tarjeta.
    private static void CrearAcento(Transform tarjeta, float ancho)
    {
        GameObject acento = new GameObject("Acento", typeof(RectTransform));
        acento.layer = 5;
        RectTransform rt = acento.GetComponent<RectTransform>();
        rt.SetParent(tarjeta, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(0f, 8f);
        rt.offsetMax = new Vector2(ancho, -8f);
        rt.anchoredPosition = new Vector2(0f, 0f);

        var imagen = acento.AddComponent<Image>();
        imagen.color = Cian;
        imagen.raycastTarget = false;
    }

    private static GameObject CrearContenedor(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
    {
        GameObject contenedor = new GameObject(nombre, typeof(RectTransform));
        contenedor.layer = 5;
        RectTransform rt = contenedor.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        return contenedor;
    }

    private static TextMeshProUGUI CrearTexto(GameObject plantilla, Transform padre, string nombre,
        string contenido, float tamano, Color color, TextAlignmentOptions alineacion,
        Vector2 posicion, Vector2 tamanoRect, bool negrita)
    {
        GameObject copia = Object.Instantiate(plantilla, padre);
        copia.name = nombre;

        RectTransform rt = copia.GetComponent<RectTransform>();
        Colocar(rt, posicion, tamanoRect);

        TextMeshProUGUI texto = copia.GetComponent<TextMeshProUGUI>();
        if (texto != null)
        {
            texto.text = contenido;
            EstiloTexto(texto, tamano, color, alineacion, negrita);
            texto.raycastTarget = false;
        }
        return texto;
    }

    private static void EstiloTexto(TextMeshProUGUI texto, float tamano, Color color,
        TextAlignmentOptions alineacion, bool negrita)
    {
        if (texto == null) return;
        texto.fontSize = tamano;
        texto.color = color;
        texto.alignment = alineacion;
        texto.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        texto.enableAutoSizing = false;
        texto.raycastTarget = false;
    }

    private static Button CrearBoton(GameObject plantilla, Transform padre, string nombre, string etiqueta,
        Vector2 posicion, Vector2 tamano, Color color, UnityAction accion)
    {
        GameObject copia = Object.Instantiate(plantilla, padre);
        copia.name = nombre;

        RectTransform rt = copia.GetComponent<RectTransform>();
        Colocar(rt, posicion, tamano);

        var imagen = copia.GetComponent<Image>();
        if (imagen != null) imagen.color = color;

        var boton = copia.GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick = new Button.ButtonClickedEvent();
            if (accion != null) UnityEventTools.AddVoidPersistentListener(boton.onClick, accion);
        }

        TextMeshProUGUI texto = copia.GetComponentInChildren<TextMeshProUGUI>(true);
        if (texto != null)
        {
            texto.text = etiqueta;
            texto.fontSize = 21;
            texto.fontStyle = FontStyles.Bold;
            texto.color = Color.white;
            texto.characterSpacing = 2f;
            texto.alignment = TextAlignmentOptions.Center;
        }

        return boton;
    }

    private static void Colocar(RectTransform rt, Vector2 posicion, Vector2 tamano)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        rt.localScale = Vector3.one;
    }

    private static void LimpiarGenerados(Transform canvas, Transform panelMenu)
    {
        string[] enMenu =
        {
            "Card_Mision", "Card_Controles", "Fila_Botones", "Texto_Estado",
            "Encabezado_Mision", "Encabezado_Controles", "Texto_Controles"
        };

        for (int i = panelMenu.childCount - 1; i >= 0; i--)
        {
            Transform hijo = panelMenu.GetChild(i);
            if (System.Array.IndexOf(enMenu, hijo.name) >= 0)
                Object.DestroyImmediate(hijo.gameObject);
        }

        GameObject panelAyuda = BuscarHijo(canvas, "Panel_ComoJugar");
        if (panelAyuda != null) Object.DestroyImmediate(panelAyuda);
    }

    private static GameObject BuscarHijo(Transform padre, string nombre)
    {
        for (int i = 0; i < padre.childCount; i++)
        {
            Transform hijo = padre.GetChild(i);
            if (hijo.name == nombre) return hijo.gameObject;
        }
        return null;
    }

    private static RectTransform Rect(Transform padre, string nombre)
    {
        GameObject hijo = BuscarHijo(padre, nombre);
        return hijo != null ? hijo.GetComponent<RectTransform>() : null;
    }
}
