# Explicación del GameManager (y para qué sirve cada clase)

Proyecto: **Laboratorio Abandonado** — Computación Gráfica, Grupo 51.
Este documento explica línea por línea qué hace el `GameManager.cs` y dónde
encaja dentro de la arquitectura completa del juego.

---

## 1. ¿Qué es el GameManager?

Es **el único objeto que recuerda el estado global del juego** mientras el
jugador se mueve entre escenas (Zona 1 → Zona 2 → Zona 3).

Piénsalo así: cada escena de Unity es una "habitación". Cuando pasas de una
habitación a otra, Unity **destruye todo lo de la habitación anterior**
(incluidos los scripts). Si el estado del juego (vida, inventario, puertas
abiertas) viviera dentro de una escena, se perdería al cambiar de zona. Por
eso el estado vive en un objeto especial que **no se destruye**: el
GameManager.

### Regla del enunciado que cumple

> "Un único GameManager (Singleton + DontDestroyOnLoad) y un controlador por
> escena. Está prohibido concentrar la lógica en un solo script."

El GameManager **solo guarda estado** (datos) y ofrece funciones para
leerlo/modificar. La lógica de cada cosa vive en su propio script:
la puerta abre la puerta, el agente persigue, la UI dibuja. El GameManager
es el "tablero de anuncios" donde todos leen y escriben, no el "jefe" que
hace todo.

---

## 2. Anatomía del script (línea por línea)

### 2.1 El Singleton

```csharp
public static GameManager Instance { get; private set; }
```

- `static`: la variable pertenece a la **clase**, no a un objeto concreto.
  Por eso cualquier script puede escribir `GameManager.Instance` desde
  cualquier escena, sin buscar el objeto en la escena.
- `{ get; private set; }`: cualquiera puede **leer** `Instance`, pero solo el
  propio GameManager puede **asignarlo**. Nadie externo puede reemplazarlo.

### 2.2 El estado global

| Variable | Tipo | Qué guarda |
|---|---|---|
| `vidaJugador` | `int` | Vida actual (empieza en 100). |
| `inventario` | `List<string>` | IDs de objetos recogidos (ej. `"llave_z1"`). |
| `codigos` | `List<string>` | Códigos descubiertos (ej. `"codigo_a"`). |
| `puertas` | `Dictionary<string, bool>` | ID de puerta → ¿abierta? |
| `objetivos` | `Dictionary<string, bool>` | ID de objetivo → ¿completado? |
| `escenaActual` | `string` | Nombre de la zona donde está el jugador. |
| `tiempoJugadoSegundos` | `float` | Tiempo total jugado (para el JSON final). |

**¿Por qué `string` como ID?** Cada objeto interactivo del juego tiene un
nombre único (`"puerta_z2"`, `"codigo_b"`). Guardar strings hace que el
guardado JSON sea legible y que las zonas no dependan entre sí.

### 2.3 `Awake()` — crear el Singleton y evitar duplicados

```csharp
if (Instance != null && Instance != this)
{
    Destroy(gameObject);
    return;
}
Instance = this;
DontDestroyOnLoad(gameObject);
```

`Awake` se ejecuta una sola vez al cargarse el objeto, antes que `Start`.

1. **¿Ya existe un GameManager?** (`Instance != null`) **¿y soy yo?**
   (`Instance != this`). Si ya existe OTRO, este objeto nuevo es un
   duplicado: se autodestruye. Esto es lo que permite recargar la escena del
   menú sin que aparezcan dos GameManagers.
2. Si no existe, me declaro el oficial (`Instance = this`).
3. `DontDestroyOnLoad(gameObject)`: le digo a Unity "cuando cargues otra
   escena, no destruyas este objeto". Gracias a esto el estado sobrevive a
   los cambios de zona.

### 2.4 `Start()` y `Update()`

- `Start`: anota el nombre de la escena actual al arrancar
  (`SceneManager.GetActiveScene().name`), por si se abre el juego directo en
  una zona (útil para probar sin pasar por el menú).
- `Update`: suma `Time.deltaTime` (segundos que duró el último frame) para
  llevar la cuenta del tiempo jugado. Se guardará en el JSON.

### 2.5 Las funciones de inventario, códigos, puertas y objetivos

Todas siguen el mismo patrón. Ejemplo con puertas:

```csharp
public void AbrirPuerta(string idPuerta) { puertas[idPuerta] = true; }

public bool PuertaAbierta(string idPuerta)
{
    return puertas.ContainsKey(idPuerta) && puertas[idPuerta];
}
```

- **Escribir**: `puertas[idPuerta] = true;` crea la clave si no existía, o la
  actualiza si ya estaba.
- **Leer**: primero pregunto si la clave existe (`ContainsKey`). Si no
  existe, devuelvo `false` sin error. Esto evita el clásico
  `KeyNotFoundException` cuando una escena pregunta por una puerta que el
  jugador nunca tocó.

**¿Cómo se usa desde otra clase?** Desde el script de una puerta:

```csharp
// Al interactuar con la puerta:
GameManager.Instance.AbrirPuerta("puerta_z2");

// Al acercarse, para saber si debe verse abierta o cerrada:
if (GameManager.Instance.PuertaAbierta("puerta_z2")) { /* animar abierta */ }
```

### 2.6 `CambiarEscena()` — la única puerta de salida

```csharp
public void CambiarEscena(string nombreEscena)
{
    escenaActual = nombreEscena;
    // TODO: aquí irá SaveManager.Instance.Guardar();
    SceneManager.LoadScene(nombreEscena);
}
```

Toda transición de zona pasa por aquí. Esto garantiza que `escenaActual`
siempre esté actualizada **antes** de cambiar (clave para guardar y para el
checkpoint). El `TODO` se reemplaza por la llamada al `SaveManager` cuando
lo hagamos.

### 2.7 `ReiniciarDesdeCheckpoint()` y `NuevaPartida()`

- `ReiniciarDesdeCheckpoint`: recarga la escena actual. Después le añadiremos
  la carga del JSON para restaurar vida e inventario del punto guardado.
- `NuevaPartida`: vacía todo el estado en memoria. El borrado del archivo
  JSON en disco lo hará el menú (con el SaveManager), no el GameManager.

---

## 3. ¿Qué NO hace el GameManager?

Para no romper la arquitectura del enunciado, el GameManager **nunca**:

- Mueve al jugador → eso es `PlayerController`.
- Persigue o ataca → eso es `AgentController`.
- Dibuja botones o vida en pantalla → eso es el `UIManager` (Canvas único).
- Decide la lógica de una zona → eso es el `ZoneController` de esa escena.

## 4. Mapa de clases del proyecto (para qué sirve cada una)

| Clase | Carpeta | Para qué sirve |
|---|---|---|
| `GameManager` | `Scripts/Core` | Estado global + Singleton. **Este script.** |
| `SaveManager` | `Scripts/Core` | Escribe/lee el JSON de guardado en `StreamingAssets`. Llega en la siguiente iteración. |
| `UIManager` | `Scripts/Core` | Controla el Canvas único (prefab compartido por todas las escenas): vida, inventario, menús. |
| `PlayerController` | `Scripts/Player` | Movimiento del jugador con el **New Input System** y cámara. |
| `PlayerHealth` | `Scripts/Player` | Recibe daño con cooldown, avisa al GameManager y a la UI. |
| `AgentController` | `Scripts/Agents` | Máquina de 5 estados del agente (Patrulla, Alerta, Persecución, Ataque, Retorno) con NavMesh. |
| `InteractionController` | `Scripts/Interactables` | Detecta qué objeto interactivo está mirando el jugador y activa su efecto. |
| `ZoneController` | `Scripts/Core` (uno por escena) | Orquesta su zona: qué pasa al entrar, cuándo se completa, cuándo se abre la salida. |

Regla rápida para la sustentación: **si es "del juego entero", va en Core;
si es "de una escena", va en un controlador de escena; si es "de un objeto",
va en ese objeto.**

---

## 5. Cómo probar el GameManager ahora mismo

1. Abre Unity (versión **6000.3.14f1**) en el proyecto.
2. En la escena actual crea un objeto vacío: *GameObject → Create Empty* y
   llámalo `GameManager`.
3. Arrastra el script `Assets/_Game/Scripts/Core/GameManager.cs` sobre ese
   objeto (o con el objeto seleccionado, *Add Component → Game Manager*).
4. Dale a Play. Selecciona el objeto y en el Inspector verás vida = 100 y las
   listas vacías.
5. Prueba de fuego del Singleton: en el menú, guarda una **prefab** del
   GameManager (`Arrastrar del Hierarchy al Project`). En otra escena,
   coloca también la prefab. Al pasar de una escena a otra verás en la
   consola que el duplicado se destruye solo (se puede añadir un
   `Debug.Log` temporal en el `if` del `Awake` para demostrarlo).

> Nota: Unity generará automáticamente el archivo `GameManager.cs.meta`
> (con su GUID) al abrir el proyecto. No hay que crearlo a mano.

---

## 6. Preguntas típicas de sustentación (y sus respuestas)

**¿Por qué Singleton y no una variable pública estática suelta?**
Porque el Singleton garantiza una sola instancia real y controla quién puede
asignarla (`private set`). Una variable estática suelta no impediría duplicados.

**¿Qué pasa si olvidas DontDestroyOnLoad?**
El objeto se destruye al cambiar de escena y todo el estado (vida, inventario)
vuelve a los valores iniciales: el guardado entre zonas se rompe.

**¿Por qué `ContainsKey` antes de leer el Dictionary?**
Porque leer una clave inexistente lanza `KeyNotFoundException`. Así devolvemos
`false` de forma segura para puertas/objetivos que aún no existen.

**¿Dónde está el JSON?**
Aún no. El JSON llega con el `SaveManager`: este script solo mantiene el
estado en memoria; el SaveManager lo vuelca a `StreamingAssets/guardado.json`.
