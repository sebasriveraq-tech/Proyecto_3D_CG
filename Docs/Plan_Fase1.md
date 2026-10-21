# PLAN FASE 1 - Laboratorio Abandonado (Computacion Grafica, Grupo 51)

Grupo de **3 integrantes** -> **3 escenas jugables** (se elimina la Zona 4).
Progresion: **Z1 -> Z2 -> Z3** (lineal).
Motor: Unity **6000.3.14f1** - URP - New Input System.
Rama de trabajo personal: cada integrante trabaja en su rama (`IvanDB`, `SamuelTR`, `Sebastian`) y se integra en `main`.

---

## 0. ESTADO REAL DEL PROYECTO (verificado en el repo)

Ya esta hecho (aunque el Gantt decia "falta"):

- [x] Unity 6000.3.14f1
- [x] Paquete **AI Navigation** 2.0.12 (`com.unity.ai.navigation`) instalado
- [x] Paquete **Input System** 1.19.0 instalado + `Active Input Handling = 1` (New Input System)
- [x] URP 17.3.0, UGUI 2.0.0, Timeline
- [x] Repositorio Git + remote GitHub + `.gitignore` de Unity
- [x] Git LFS 3.7.1 instalado en la maquina
- [x] 3 ramas personales (Ivan, Samuel, Sebastian)

Falta realmente:

- [ ] Activar Git LFS **dentro del repo** (`.gitattributes` + `git lfs install`)
- [ ] Estructura de carpetas en `Assets/` (hoy solo hay `Scenes`, `Settings`, `TutorialInfo`)
- [ ] Crear las escenas: `Menu`, `Zona1_Recepcion`, `Zona2_Laboratorio`, `Zona3_Control` (hoy solo `SampleScene`)
- [ ] `Active Input Handling` = **New Input System (1)** -> **confirmado por el docente** (ver seccion 6)
- [ ] Este plan: concepto narrativo, mapa de zonas, dependencias, contrato de datos, convenciones, esquema JSON

---

## 1. CONCEPTO NARRATIVO (que es y cual es el nuestro)

**Que significa:** es la historia corta que justifica por que el jugador esta ahi, que debe hacer y por que hay enemigos. No es un adorno: de ahi salen los nombres de las zonas, los objetos y las metas. Se escribe en 1 parrafo.

**El nuestro (propuesta, pueden editarla):**

> Durante un experimento de inteligencia artificial y automatizacion, el sistema central del laboratorio ejecuto un **protocolo de contencion**: bloqueo las puertas, corto la energia de varios modulos y dejo activos a los agentes de seguridad con orden de neutralizar cualquier intruso. El jugador despierta dentro de la instalacion en emergencia y debe **reconstruir lo que paso, recuperar credenciales y codigos, reparar modulos y habilitar la salida**, evitando a los agentes que aun patrullan.

De esta historia se desprenden directamente:
- Los **agentes** (drones/guardias/criatura) = los enemigos que atacan.
- Las **credenciales y codigos** = los objetos que se recogen y se usan entre zonas.
- La **salida** = la condicion de victoria en la Zona 3.
- La **vida en cero** = condicion de derrota (te neutralizan).

---

## 2. MAPA DE ZONAS (3 zonas, una por integrante)

| Zona | Nombre | Proposito | Mecanica principal | Agente | Que DA a la siguiente |
|------|--------|-----------|--------------------|--------|------------------------|
| **Z1** | Recepcion / Area de ingreso | Ensenar controles, interaccion y obtencion de la primera credencial | Terminal de registro + tutorial de interaccion | Dron de vigilancia danado (patrulla corta, ataque debil) | **Credencial Z1** + **Codigo A** |
| **Z2** | Laboratorio principal | Resolver un mecanismo y buscar evidencia | Secuencia de valvulas / reparacion de equipo | Guardia de seguridad (patrulla completa) | **Evidencia Z2** + **Codigo B** + abre acceso a Z3 |
| **Z3** | Sala de control / Contencion | Usar todo lo acumulado y escapar | Terminal que exige **Codigo A + Codigo B** + secuencia final de escape | Varios agentes se activan al restaurar la energia | **Condicion de VICTORIA** |

Orden real de juego: **Menu -> Z1 -> Z2 -> Z3 -> Victoria**.
(Se quito la Zona 4; antes Z4 daba acceso a Z3, ahora **Z2 abre Z3 directamente**.)

Asignacion sugerida (pongan sus nombres):

| Integrante | Zona | Rama Git |
|------------|------|----------|
| Integrante 1: ________ | Z1 Recepcion | |
| Integrante 2: ________ | Z2 Laboratorio | |
| Integrante 3: ________ | Z3 Control | |

---

## 3. MATRIZ DE DEPENDENCIAS (que produce y que consume cada zona)

Esto responde "que necesita mi zona antes de poder funcionar". Se lee asi: una zona **produce** datos y **consume** datos de zonas anteriores. Todo viaja por el `GameManager` (no por escenas).

| Dato (ID) | Tipo | Produce | Consume | Para que sirve |
|-----------|------|---------|---------|----------------|
| `credencial_z1` | item | Z1 | Z2 | Abrir la puerta del laboratorio |
| `codigo_a` | code | Z1 | Z3 | Terminal final de escape |
| `evidencia_z2` | item | Z2 | Z3 | Parte del objetivo / puntaje |
| `codigo_b` | code | Z2 | Z3 | Terminal final de escape |
| `puerta_z2` | door | Z1 (al tener credencial) | Z2 | Acceso fisico a Z2 |
| `puerta_z3` | door | Z2 (al reparar) | Z3 | Acceso fisico a Z3 |
| `energia_z3` | objective | Z3 | Z3 | Al restaurarla se activan los agentes y habilita la terminal de escape |

**Regla de oro:** ninguna zona puede asumir que el jugador "ya tiene" algo sin preguntarselo al `GameManager`. Cada zona, al arrancar, consulta al `GameManager` que ya se consiguio en las zonas previas.

---

## 4. CONTRATO DE TRASPASO DE DATOS VIA GameManager (como se hace)

**Que significa:** es el "acuerdo" de que metodos publicos expone el `GameManager` para que las 3 escenas guarden y lean el progreso compartido. Se define ANTES de programar las escenas, para que los 3 trabajen en paralelo contra la misma API. El `GameManager` es **unico** (Singleton, `DontDestroyOnLoad`).

Esqueleto simple (cada linea se puede explicar en la sustentacion):

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // --- Singleton: una sola instancia que sobrevive entre escenas ---
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; } // anti-duplicado
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- ESTADO GLOBAL (esto es lo que se guarda en el JSON) ---
    public int vidaJugador = 100;
    public List<string> inventario = new List<string>();          // List<T>  (items)
    public List<string> codigos = new List<string>();             // List<T>  (codigos)
    public Dictionary<string, bool> puertas = new Dictionary<string, bool>();     // Dictionary  (id -> abierta)
    public Dictionary<string, bool> objetivos = new Dictionary<string, bool>();   // Dictionary  (id -> completo)
    public string escenaActual = "Menu";

    // --- API para las escenas (el "contrato") ---
    public void AgregarItem(string id)   { if (!inventario.Contains(id)) inventario.Add(id); }
    public bool TieneItem(string id)     { return inventario.Contains(id); }

    public void AgregarCodigo(string c)  { if (!codigos.Contains(c)) codigos.Add(c); }
    public bool TieneCodigo(string c)    { return codigos.Contains(c); }

    public void AbrirPuerta(string id)   { puertas[id] = true; }
    public bool PuertaAbierta(string id) { return puertas.ContainsKey(id) && puertas[id]; }

    public void CompletarObjetivo(string id) { objetivos[id] = true; }
    public bool ObjetivoCompletado(string id){ return objetivos.ContainsKey(id) && objetivos[id]; }

    // --- Cambio de escena (lo hace SIEMPRE el GameManager) ---
    public void CambiarEscena(string nombre)
    {
        escenaActual = nombre;
        SaveManager.Instance.Guardar();          // checkpoint al cambiar de escena
        SceneManager.LoadScene(nombre);
    }

    // --- Derrota: reinicia desde el ultimo guardado ---
    public void ReiniciarDesdeCheckpoint()
    {
        SaveManager.Instance.Cargar();
        SceneManager.LoadScene(escenaActual);
    }
}
```

**Como lo usan las zonas (ejemplos concretos):**

- Z1, al recoger la credencial: `GameManager.Instance.AgregarItem("credencial_z1");`
- Z2, al iniciar, para saber si puede abrir: `if (GameManager.Instance.TieneItem("credencial_z1")) { ... }`
- Z3, terminal final: `if (GameManager.Instance.TieneCodigo("codigo_a") && GameManager.Instance.TieneCodigo("codigo_b")) { victoria }`

Ese "acuerdo" de nombres (`credencial_z1`, `codigo_a`, `puerta_z2`, etc.) ES el contrato. Mientras los 3 respeten esos strings y esos metodos, cada uno programa su zona por separado sin pisarse.

---

## 5. ESQUEMA JSON DE GUARDADO (contenido minimo)

Se guarda en `Application.streamingAssetsPath` (carpeta `StreamingAssets`). Se guarda el **contenido**, no las estructuras; al cargar se reconstruyen las `List`/`Dictionary`.

Ejemplo de archivo `guardado.json`:

```json
{
  "escenaActual": "Zona2_Laboratorio",
  "vidaJugador": 80,
  "inventario": ["credencial_z1", "evidencia_z2"],
  "codigos": ["codigo_a"],
  "puertas": { "puerta_z2": true, "puerta_z3": false },
  "objetivos": { "tutorial_z1": true, "reparar_z2": false },
  "tiempoJugadoSegundos": 342
}
```

Clase C# que refleja ese JSON (con `JsonUtility`, lo mas simple):

```csharp
[System.Serializable]
public class DatosGuardado
{
    public string escenaActual;
    public int vidaJugador;
    public List<string> inventario = new List<string>();
    public List<string> codigos = new List<string>();
    public List<ParBool> puertas = new List<ParBool>();     // JsonUtility no guarda Dictionary: se usa lista de pares
    public List<ParBool> objetivos = new List<ParBool>();
    public int tiempoJugadoSegundos;
}

[System.Serializable]
public class ParBool { public string clave; public bool valor; }
```

> Nota importante: `JsonUtility` **no** serializa `Dictionary`. Por eso el Dictionary se convierte a una `List<ParBool>` al guardar y se reconstruye a Dictionary al cargar. Eso ademas cumple el requisito de "reconstruir las estructuras al cargar".

**Puntos de guardado (checkpoints):** al cambiar de escena y al usar una terminal de guardado. **No** se guarda en cada frame ni la posicion de los agentes.

---

## 6. CONVENCIONES DE CODIGO Y REGLAS DEL EQUIPO

**Nomenclatura C#:**
- Clases y metodos: `PascalCase` (`GameManager`, `AbrirPuerta`).
- Variables y campos privados: `camelCase` (`vidaJugador`, `inventario`).
- Constantes: `MAYUSCULAS_CON_GUION_BAJO`.
- Un archivo `.cs` por clase, con el mismo nombre (`GameManager.cs`).

**IDs de juego (el contrato):** siempre en `minusculas_con_guion_bajo` y prefijados por zona: `credencial_z1`, `codigo_a`, `puerta_z2`, `objetivo_z3`. Nada de espacios ni acentos en los IDs.

**Nomenclatura de assets:** `Prefabs/Puerta_Z1`, `Audio/SFX_Alerta`, `Materiales/Metal_Emisivo`. Prefijos por tipo.

**Reglas de integracion:**
1. Nadie hace push directo a `main`. Se trabaja en la rama personal y se abre **Pull Request**.
2. Antes de integrar: la escena debe abrir sin errores en consola.
3. **Un solo Canvas (prefab)** en todo el proyecto. Si tu zona necesita UI, es un **panel** dentro de ese prefab, activado por el `UIManager`. **Prohibido** crear Canvas nuevos en la escena.
4. **Un solo GameManager**. Ninguna escena crea el suyo.
5. Los cambios al prefab del Canvas se coordinan (afectan a todos).
6. Mensajes de commit claros: `feat(z2): valvula de presion funcional`.

**Active Input Handling (DECIDIDO por el docente):** se usa **New Input System** -> dejar `Active Input Handling = 1` (solo New). **Confirmado.** Consecuencias a tener en cuenta:
- El codigo viejo `Input.GetAxis(...)` / `Input.GetKeyDown(...)` **NO funciona** con este valor; si lo usan, Unity lanza error. No mezclar.
- Se trabaja con el asset `InputSystem_Actions.inputactions` (ya existe en `Assets/`) + el componente **`PlayerInput`**, o leyendo `Keyboard.current` / `Mouse.current`.
- Tarea 1.6 del Gantt: el paquete AI Navigation **ya esta instalado** y el Input Handling **ya esta en 1**, asi que esta tarea queda **completada**; solo falta que el equipo aprenda a leer el New Input System.
- Recomendacion: mover al jugador con un `PlayerInput` + mapa de acciones (`Move`, `Interact`, `Pause`) en vez de teclas sueltas. Es mas ordenado y se explica bien en la sustentacion.

---

## 7. ESTRUCTURA DE CARPETAS EN Assets/ (crear dentro de Unity)

Crear estas carpetas **desde el Unity Editor** (clic derecho en `Assets > Create > Folder`) para que se generen bien los `.meta`:

```
Assets/
  _Game/
    Scripts/
      Core/         (GameManager, SaveManager, AudioManager, UIManager)
      Player/       (PlayerController, PlayerHealth, InteractionController)
      Agents/       (AgentController y derivados)
      Interactables/(Interactable, Door, Terminal, Collectable, Boton, Interruptor)
    Prefabs/
      UI/           (el UNICO Canvas prefab)
      Interactables/
      Agents/
      Player/
    Scenes/         (Menu, Zona1_Recepcion, Zona2_Laboratorio, Zona3_Control)
    Materials/
    Animations/     (Animator Controllers)
  Art/              (modelos 3D, texturas)
  Audio/
    Music/
    SFX/
    Ambience/
  Particles/
  StreamingAssets/  (aqui vive guardado.json)
```

> `StreamingAssets` se crea con `Assets > Create > Folder` y se llama exactamente `StreamingAssets` (Unity lo reconoce por nombre). Ahi se lee/escribe el JSON con `Application.streamingAssetsPath`.

---

## 8. GIT LFS (activar en el repo)

Git LFS ya esta instalado en la maquina, pero el repo aun no lo usa. Falta el archivo `.gitattributes` y `git lfs install`. Ver archivo `.gitattributes` propuesto en la raiz del repo. Comandos (una sola vez, quien administre el repo):

```bash
cd "D:/PROYECTOS CG UAO/REPOSITORIOS/Proyecto_3D_CG"
git lfs install
git add .gitattributes
git commit -m "chore: activa Git LFS para assets binarios"
```

Despues, cada integrante hace `git pull` para heredar la configuracion.

---

## 9. TABLERO Y CRONOGRAMA

- Cronograma: ya existe el Gantt (`Laboratorio_Abandonado_Gantt.xml`) -> hay que **actualizarlo a 3 zonas** (quitar Zona 4). Pendiente aparte.
- Tablero de tareas: usar **GitHub Projects** (ya tienen GitHub) o **Trello**. Columnas sugeridas: `Por hacer | En progreso | En revision (PR) | Hecho`. Cada tarea del Gantt se vuelve una tarjeta asignada a un integrante.

---

## CHECKLIST FINAL DE FASE 1

- [ ] Concepto narrativo redactado (seccion 1)
- [ ] Mapa de 3 zonas y asignacion de nombres (seccion 2)
- [ ] Matriz de dependencias acordada (seccion 3)
- [ ] Contrato del GameManager definido (seccion 4)
- [ ] Esquema JSON acordado (seccion 5)
- [ ] Convenciones de codigo leidas y aceptadas por los 3 (seccion 6)
- [ ] Decision de Active Input Handling tomada (seccion 6)
- [ ] Carpetas de Assets creadas (seccion 7)
- [ ] Git LFS activado (seccion 8)
- [ ] Tablero creado (seccion 9)
- [ ] **HITO: plan, alcance, reparto y arquitectura base definidos**
