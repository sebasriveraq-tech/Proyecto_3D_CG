# Elementos transversales, Canvas y prefabs (versión final)

Esta guía documenta **cómo quedaron armados** el Canvas, el jugador y el recogible, y **qué se corrigió** durante la revisión del 26-oct. Sirve para tres cosas: reconstruir los prefabs si se pierden, entender cada valor para la sustentación y repetir la verificación antes de la entrega.

> Estado: `Canvas_Principal.prefab` importa **sin errores** en Unity 6000.3.14f1, el HUD se ve en Play, el prompt de la credencial aparece automáticamente y las 4 escenas usan las mismas instancias de Canvas, Player y GameManager. La cadena **Terminal → credencial → Puerta → Z2** está probada (sección 6) y el **guardado/carga en JSON** también (sección 7.1).

---

## 1. Qué evalúa el docente y con qué lo cumplimos

| Elemento transversal | Archivo | Dónde vive | Para qué sirve |
|----------------------|---------|------------|----------------|
| Estado global | `GAME/Scripts/Core/GameManager.cs` | Prefab `Core/GameManager` (1 instancia por escena) | Vida, inventario, códigos, puertas y objetivos entre escenas |
| Interfaz | `GAME/Scripts/Core/UIManager.cs` | Prefab `Canvas_Principal` | Mostrar paneles, vida, inventario, mensajes y prompt |
| Jugador | `PlayerController` + `PlayerHealth` + `InteractionController` | Prefab `Player` | Mover, mirar, recibir daño e interactuar igual en Z1, Z2 y Z3 |
| Objetos interactuables | `Interactable` + `Recogible` + `Terminal` + `Puerta` | Prefabs `Recogible_Credencial`, `Terminal_Registro`, `Puerta_Zona2` | Base común para puertas, terminales, botones y recogibles |
| Guardado JSON | `GAME/Scripts/Core/SaveManager.cs` | Clase estática; archivo en `persistentDataPath` | Persistir vida, inventario, códigos, puertas, objetivos y escena |

**Idea central para la sustentación:** el GameManager es el único que **guarda** estado; el UIManager solo **pinta**; el Player y los interactuables son **prefabs reutilizados** en todas las escenas. Nada se duplica ni se vuelve a programar por zona.

---

## 2. Qué hace cada clase nueva

| Clase | Carpeta | Responsabilidad | Lo que NO hace |
|-------|---------|-----------------|----------------|
| `UIManager` | `Scripts/Core` | Activar/ocultar paneles, actualizar barra y textos, listar ítems y códigos, mostrar prompt y mensajes | No guarda progreso ni cambia de escena |
| `PlayerController` | `Scripts/Player` | Movimiento y cámara con New Input System + CharacterController | No recibe daño ni sabe de ítems |
| `PlayerHealth` | `Scripts/Player` | Vida, cooldown de daño, HUD y derrota | No mueve al jugador |
| `InteractionController` | `Scripts/Player` | SphereCast desde la cámara, prompt y tecla E | No decide qué hace cada objeto |
| `Interactable` | `Scripts/Interactables` | Clase base abstracta: obliga a implementar `Interactuar()` | No tiene lógica propia |
| `Recogible` | `Scripts/Interactables` | Suma un ítem al GameManager, actualiza el HUD y desaparece | No sirve para puertas ni terminales |
| `Terminal` | `Scripts/Interactables` | Suma un código al GameManager y lo muestra en el HUD | No abre puertas ni cambia de escena |
| `Puerta` | `Scripts/Interactables` | Pide un ítem; si lo tiene, sube como compuerta y carga la siguiente zona | No recoge ítems ni da códigos |
| `ControlPaneles` | `Scripts/Core` | Teclas de los paneles: ENTER (partida nueva), C (continuar), ESC (pausa) e I (inventario) | No dibuja UI ni mueve al jugador |
| `SaveManager` | `Scripts/Core` | Convierte el GameManager a JSON (`Guardar`), lo lee (`Cargar`) y borra el archivo (`BorrarGuardado`) | No tiene estado propio ni está en la escena: es una clase estática |

**Flujo completo de una interacción (para contarlo de memoria):**

1. `InteractionController` lanza un `SphereCast` desde la cámara (4 m, radio 0.35).
2. Si el collider golpeado tiene un `Interactable` en su padre, guarda ese objeto como `objetivoActual`.
3. Al cambiar de objetivo, pide a `UIManager` que muestre u oculte el prompt.
4. Si se presiona **E** (`Keyboard.current.eKey.wasPressedThisFrame`) y hay objetivo, llama `Interactuar()`.
5. `Recogible.Interactuar()` agrega el `idItem` al GameManager, refresca el inventario, muestra "Recogiste: …", oculta el prompt y destruye el objeto.
6. `Terminal.Interactuar()` agrega el `idCodigo` y muestra "Código obtenido: …".
7. `Puerta.Interactuar()` revisa si el jugador tiene `idLlave`; si lo tiene, anima la compuerta y llama `CambiarEscena`.

Reglas del proyecto: un solo Canvas (prefab), un solo GameManager, un controlador por escena y **New Input System** (no usar `Input.GetAxis`).

---

## 3. Canvas_Principal (prefab final)

### 3.0 Importar TMP Essential Resources (solo una vez)

Los textos usan **TextMeshPro (TMP)**. El paquete viene con UGUI (`com.unity.ugui 2.0.0`), pero sus recursos se importan una vez:

1. Menú `Window > TextMeshPro > Import TMP Essential Resources`.
2. Si aparece la ventana **TMP Importer**, clic en **Import TMP Essentials**.
3. Esperar el reimport. Se crea `Assets/TextMesh Pro` con la fuente **LiberationSans SDF**.
4. No hace falta importar "TMP Examples & Extras".

> Sin este paso los `TextMeshProUGUI` aparecen sin fuente y el texto no se ve.

### 3.1 Canvas, escala y EventSystem

| Ajuste | Valor final |
|--------|-------------|
| Render Mode | **Screen Space - Overlay** |
| Canvas Scaler → UI Scale Mode | **Scale With Screen Size** |
| Reference Resolution | **1920 × 1080** |
| Screen Match Mode | Match Width Or Height, **Match = 0.5** |
| Escala del objeto raíz (Transform) | **1, 1, 1** |
| Capas | UI (Layer 5) |
| Componentes del raíz | `Canvas`, `Canvas Scaler`, `Graphic Raycaster`, `UIManager` |

**Importante:** si la escala del Canvas queda en `0,0,0` no se ve nada y el ratón no detecta clics. Fue uno de los errores corregidos.

Cada escena necesita un **EventSystem** con `Input System UI Input Module` (no `Standalone Input Module`, porque el proyecto usa New Input System). En Z1 ya existe.

### 3.2 Jerarquía final

```
Canvas_Principal
├─ Panel_Menu              (activo solo en Menu)
│  ├─ Franja_Menu
│  ├─ Titulo
│  ├─ Subtitulo
│  ├─ Linea_Menu
│  ├─ Descripcion_Menu
│  └─ Pie_Menu
├─ Panel_HUD               (activo en Z1, Z2 y Z3)
│  ├─ BarraVida_Fondo
│  │  └─ BarraVida
│  ├─ TextoVida
│  ├─ FondoMensaje
│  ├─ TextoMensaje
│  ├─ FondoPrompt
│  ├─ TextoPrompt
│  ├─ TextoInventario
│  ├─ Mira_H
│  └─ Mira_V
├─ Panel_Pausa
│  ├─ Card_Pausa
│  ├─ Titulo
│  └─ Sub_Pausa
├─ Panel_Inventario
│  ├─ Card_Inventario
│  ├─ Titulo
│  ├─ TextoInvPanel
│  ├─ PieInv
│  └─ TextoCodigos
├─ Panel_Victoria
│  ├─ Card_Victoria
│  ├─ Titulo
│  └─ Sub_Victoria
└─ Panel_Derrota
   ├─ Card_Derrota
   ├─ Titulo
   └─ Sub_Derrota
```

Los seis paneles ocupan toda la pantalla (anclas **stretch/stretch**, offsets 0). Como `UIManager.MostrarSolo()` deja **un solo panel activo**, el orden entre ellos no cambia el resultado.

### 3.3 Cómo leer las tablas de medidas

- **Ancla** = Anchor Min → Anchor Max. `top-left (0,1)` significa que el objeto se pega a la esquina superior izquierda.
- **Pos** = `Anchored Position` en píxeles de la resolución de referencia (1920 × 1080).
- **Tamaño** = `Width × Height` (`Size Delta`).
- **Pivote** = punto del objeto que se toma como referencia. `(0,1)` = esquina superior izquierda; `(0.5,0.5)` = centro; `(0.5,0)` = centro inferior.
- Todos los textos son `TextMeshProUGUI` con fuente **LiberationSans SDF**.

### 3.4 Panel_Menu

Fondo sólido azul muy oscuro `#0B1118` (0.043, 0.067, 0.094, alfa 1).

| Objeto | Ancla | Pos | Tamaño | Fuente | Texto y color |
|--------|-------|-----|--------|--------|---------------|
| `Franja_Menu` (Image) | top stretch (0,1)-(1,1) | (0, 0) | 0 × 6 | — | Turquesa `#35C4BE`; franja decorativa superior |
| `Titulo` (TMP) | centro | (0, 182) | 1400 × 110 | 64 | `LABORATORIO ABANDONADO`, turquesa, centrado |
| `Subtitulo` (TMP) | centro | (0, 104) | 1400 × 44 | 20 | `PROYECTO FINAL  -  COMPUTACIÓN GRÁFICA  -  GRUPO 51`, turquesa, centrado |
| `Linea_Menu` (Image) | centro | (0, 42) | 180 × 2 | — | Turquesa con alfa 0.85 |
| `Descripcion_Menu` (TMP) | centro | (0, -8) | 980 × 64 | 20 | `Explora las instalaciones, recupera las credenciales y abre la sala de control.` + salto de línea + `Presiona ENTER para comenzar.`, gris claro, centrado |
| `Pie_Menu` (TMP) | bottom-center | (0, 36) | 1200 × 28 | 15 | `Universidad Autónoma de Occidente  -  Computación Gráfica  -  Grupo 51`, gris oscuro, centrado |

La franja, la línea y el título turquesa son el "hilo visual" del proyecto; los paneles de zona usan el mismo turquesa en el prompt y en los títulos de victoria.

### 3.5 Panel_HUD

Imagen transparente (alfa 0) y **Raycast Target desactivado** para no bloquear la mirada del jugador.

| Objeto | Ancla | Pos | Tamaño | Fuente | Detalle |
|--------|-------|-----|--------|--------|---------|
| `BarraVida_Fondo` (Image) | top-left (0,1) | (32, -32) | 340 × 22 | — | Fondo `#10181F` (0.063, 0.094, 0.122) alfa 0.9, sprite *Background* tipo Sliced |
| `BarraVida` (Image) | stretch/stretch | (0, 0) | **-4 × -4** | — | **Filled / Horizontal / Left**, verde `#3DDC84` (0.239, 0.863, 0.518), Fill Amount 1 |
| `TextoVida` (TMP) | top-left | (32, -62) | 380 × 30 | 20 | `VIDA: 100 / 100`, alineado a la izquierda, color `#EAF2F5` |
| `FondoMensaje` (Image) | top-center | (0, -28) | 980 × 60 | — | Fondo `#0B1118` alfa 0.85, **inactivo al iniciar** |
| `TextoMensaje` (TMP) | top-center | (0, -28) | 940 × 60 | 30 | Vacío; lo llena `UIManager.MostrarMensaje()`, blanco, centrado |
| `FondoPrompt` (Image) | bottom-center | (0, 88) | 780 × 56 | — | Fondo `#0B1118` alfa 0.9, **inactivo al iniciar** |
| `TextoPrompt` (TMP) | bottom-center | (0, 88) | 740 × 56 | 22 | Vacío; lo llena `MostrarPrompt()`, color `#EAF2F5`, centrado |
| `TextoInventario` (TMP) | top-right (1,1) | (-32, -32) | 420 × 260 | 18 | `INVENTARIO` + lista, alineado a la derecha, color `#BFD1D9` |
| `Mira_H` (Image) | centro | (0, 0) | 18 × 2 | — | Blanco alfa 0.7 |
| `Mira_V` (Image) | centro | (0, 0) | 2 × 18 | — | Blanco alfa 0.7 |

**Por qué la barra mide `-4 × -4`:** `BarraVida` está anclada en stretch al fondo. Un tamaño negativo recorta 2 px por lado, así el fondo oscuro se ve como un borde de 2 px. Con tamaño `0 × 0` la barra tapaba el borde completo y parecía un bloque macizo.

**Por qué el inventario es `420 × 260`:** entra en dos líneas (`INVENTARIO` y `Sin objetos`) sin pisar el mensaje superior ni el prompt inferior.

### 3.6 Panel_Pausa

Fondo casi negro `#05080B` alfa 0.86. Tarjeta central `Card_Pausa` de 620 × 330, color `#121A22` alfa 0.98.

| Objeto | Pos | Tamaño | Fuente | Texto |
|--------|-----|--------|--------|-------|
| `Titulo` (TMP) | (0, 78) | 520 × 64 | 42 | `PAUSA`, blanco `#EAF2F5`, centrado |
| `Sub_Pausa` (TMP) | (0, -92) | 520 × 40 | 18 | `Presiona ESC para continuar`, gris `#8FA0AC`, centrado |

### 3.7 Panel_Inventario

Tarjeta `Card_Inventario` de 780 × 580.

| Objeto | Pos | Tamaño | Fuente | Texto |
|--------|-----|--------|--------|-------|
| `Titulo` (TMP) | (0, 238) | 660 × 50 | 34 | `INVENTARIO`, blanco, alineado a la izquierda |
| `TextoInvPanel` (TMP) | (0, 60) | 660 × 300 | 21 | Inicia en `Sin objetos`; lo llena `ActualizarInventario()`, alineado arriba-izquierda |
| `TextoCodigos` (TMP) | (0, -160) | 660 × 140 | 19 | `CÓDIGOS` + lista; lo llena `ActualizarCodigos()`, turquesa de marca, alineado arriba-izquierda |
| `PieInv` (TMP) | (0, -252) | 660 × 28 | 15 | `Presiona I para cerrar`, gris oscuro, centrado |

> El texto de ítems se redujo de 420 a 300 de alto y subió a `y = 60` para dejarle sitio a la sección de códigos. Con ítems de sobra, el texto crece hacia abajo desde el borde superior y no choca con `TextoCodigos`, que empieza en `y = -90`.

### 3.8 Panel_Victoria y Panel_Derrota

Ambos usan tarjeta central y el mismo esquema: título arriba y subtítulo abajo, **centrados verticalmente** respecto a la tarjeta.

| Panel | Tarjeta | Título (pos / tamaño / fuente) | Subtítulo (pos / tamaño / fuente) |
|-------|---------|-------------------------------|-----------------------------------|
| Victoria | `Card_Victoria` 760 × 380 | `VICTORIA` (0, 36) · 660 × 64 · 46 · **verde** `#3DDC84` | `Has recuperado todas las credenciales del laboratorio.` (0, -46) · 620 × 44 · 20 · gris |
| Derrota | `Card_Derrota` 760 × 360 | `TE NEUTRALIZARON` (0, 36) · 680 × 64 · 44 · **rojo** `#FF5C5C` | `Serás reiniciado en la zona de control.` (0, -46) · 620 × 44 · 19 · gris |

> Antes los títulos estaban en `y = 110` y `y = 100`, y los subtítulos en `y = 28` y `y = 20`. El bloque quedaba pegado arriba y con un hueco abajo. Con `36 / -46` el contenido queda equidistante del borde superior e inferior.

### 3.9 Paleta del proyecto

| Uso | Color (float) | Hex aproximado |
|-----|---------------|----------------|
| Turquesa de marca | 0.208, 0.769, 0.745 | `#35C4BE` |
| Fondo de menú | 0.043, 0.067, 0.094 | `#0B1118` |
| Velo de pausa/victoria/derrota | 0.020, 0.031, 0.043 (alfa 0.86) | `#05080B` |
| Tarjetas | 0.071, 0.102, 0.133 | `#121A22` |
| Texto principal | 0.918, 0.949, 0.961 | `#EAF2F5` |
| Texto secundario | 0.561, 0.627, 0.675 | `#8FA0AC` |
| Texto de pie | 0.306, 0.353, 0.400 | `#4E5A66` |
| Vida alta | 0.239, 0.863, 0.518 | `#3DDC84` |
| Vida baja | 1.000, 0.361, 0.361 | `#FF5C5C` |

Los tres colores de vida (`colorVidaAlta`, `colorVidaMedia`, `colorVidaBaja`) están serializados en el `UIManager` y se interpolan con `Color.Lerp` según el porcentaje.

### 3.10 Conectar el UIManager

El componente ya está conectado en el prefab. Referencias (no borrarlas):

- **Paneles:** `panelHUD`, `panelMenu`, `panelPausa`, `panelInventario`, `panelVictoria`, `panelDerrota`.
- **HUD:** `barraVida` (el Image `BarraVida`), `textoVida`, `textoMensaje`, `fondoMensaje`, `textoInventario`, `textoPrompt`, `fondoPrompt`.
- **Panel de inventario:** `textoInventarioPanel` y `textoCodigosPanel`.
- **Colores:** alta, media y baja.

`Awake()` guarda `Instance` y oculta `fondoMensaje` y `fondoPrompt`. `Start()` lee la vida, el inventario y los códigos del GameManager para que el HUD y el panel arranquen con el estado real.

### 3.11 Prefab y estados por escena

1. El prefab vive en `Assets/GAME/Prefabs/UI/Canvas_Principal.prefab`.
2. Las 4 escenas tienen una **instancia** del mismo prefab (no Canvas nuevos).
3. Estados:

| Escena | Panel_Menu | Panel_HUD | Otros 4 |
|--------|------------|-----------|---------|
| `Menu` | activo | inactivo | inactivos |
| `Zona1_Recepcion` | inactivo (override) | activo (override) | inactivos |
| `Zona2_Laboratorio` | inactivo (override) | activo (override) | inactivos |
| `Zona3_Control` | inactivo (override) | activo (override) | inactivos |

> Los overrides de cada zona solo cambian los checks de `m_IsActive`. Cualquier cambio de diseño se hace en el **prefab** y se aplica a las escenas con `Overrides > Apply All`.

### 3.12 Teclas de los paneles (`ControlPaneles`)

El componente va **en la raíz del prefab del Canvas**, junto a `UIManager`, así las cuatro escenas lo heredan sin tocar nada.

| Tecla | Qué hace | Cómo |
|-------|----------|------|
| **ENTER** | En el menú carga la Zona 1 | `SceneManager.LoadScene("Zona1_Recepcion")` |
| **ESC** | Pausa / reanuda | `Time.timeScale = 0` y `MostrarPausa()`; al volver, `1` y `MostrarHUD()` |
| **I** | Abre / cierra el inventario | Alterna `MostrarInventario()` y `MostrarHUD()` |

- Al pausar se libera el mouse (`Cursor.lockState = None`) y al reanudar se vuelve a bloquear.
- `PlayerController`, `InteractionController` y `PlayerHealth` empiezan su `Update()` con `if (Time.timeScale == 0f) return;`: con el juego congelado no se mueve la cámara, no se interactúa y la K no hace daño.
- ESC solo pausa si el HUD está visible (en el menú no hace nada). ENTER solo actúa si el Panel_Menu está visible.
- Las 4 escenas están en `File > Build Settings` con `Menu` en el índice 0; sin eso `LoadScene` falla en el build.

---

## 4. Player prefab

Prefab: `Assets/GAME/Prefabs/Player/Player.prefab`, instanciado en Z1, Z2 y Z3.

| Elemento | Valor |
|----------|-------|
| Raíz | `Player`, posición `(0, 0, 0)`, escala 1 |
| Colisiones | `Character Controller`: Center `(0, 1, 0)`, Height 2, Radius 0.4; **sin Capsule Collider** |
| Cámara hija | `Camera`, posición local `(0, 1.6, 0)`, FOV 60, **Tag = MainCamera** |
| `PlayerController` | `camara` → Camera hija; velocidad 4, correr 7, salto 5, gravedad -20, sensibilidad 0.12 |
| `PlayerHealth` | vida 100, cooldown 1 s, espera de derrota 2 s |
| `InteractionController` | `camara` → Camera hija; distancia 4, `grosorRayo` 0.35 |

- El mouse se bloquea en `Start()` (`CursorLockMode.Locked`) y se libera al salir de Play.
- En cada escena debe existir **una sola** cámara con Tag `MainCamera` y un solo `AudioListener`.
- El Player *no* es `DontDestroyOnLoad`: cada escena instancia el prefab.

**Controles:** WASD mover, mouse mirar, Shift correr, Espacio saltar. **K** es temporal para probar daño.

---

## 5. Recogible_Credencial y por qué ahora sí funciona

Prefab: `Assets/GAME/Prefabs/Interactables/Recogible_Credencial.prefab`.

| Ajuste | Valor |
|--------|-------|
| Forma | Cube, escala `0.4` |
| Collider | `Box Collider` 1 × 1 × 1, **Is Trigger = false** |
| Script | `Recogible` (que hereda de `Interactable`) |
| `mensajePrompt` | `Presiona E para recoger` |
| `idItem` | `credencial_z1` |
| `nombreVisible` | `Credencial Z1` |

En `Zona1_Recepcion` la instancia está en **`(0, 1.2, 2.5)`**: de frente al jugador (que aparece en el origen) y a 1.2 m de altura.

### 5.1 El problema del prompt y la solución

Antes el jugador miraba el cubo a menos de 3 m y **no aparecía nada**. La causa era geométrica, no lógica:

- La cámara está a **1.6 m** de altura y el rayo salía horizontal.
- El cubo, con escala 0.4 y centro en `y = 1.0`, terminaba en `y = 1.2`; el rayo pasaba **0.4 m por encima**.
- Un `Raycast` no tiene grosor: si no toca, no detecta.

Se corrigió en dos pasos:

1. `InteractionController` usa un **`SphereCast`** con `grosorRayo = 0.35`, es decir, un rayo "grueso" que tolera apuntar cerca del objeto.
2. La credencial se subió a **`y = 1.2`** (parte superior en 1.4 m). La diferencia con el rayo queda en 0.2 m, menor que el radio 0.35 → detección segura.

**Por qué el collider no es Trigger:** el `SphereCast` usa `QueryTriggerInteraction.Ignore`; si el collider fuera trigger, se ignoraría. Al ser sólido, el rayo lo detecta.

### 5.2 Flujo esperado

Mirar el cubo a menos de 3 m → aparece el prompt con fondo → **E** → mensaje "Recogiste: Credencial Z1" durante 2 s, el inventario del HUD pasa de `Sin objetos` a `- Credencial Z1`, el prompt se oculta y el cubo desaparece.

### 5.3 Duplicar para Z2 y Z3

1. Arrastrar el prefab a la escena.
2. Cambiar `idItem` (`evidencia_z2`, `llave_z3`, …) y `nombreVisible`.
3. Ajustar `mensajePrompt` si la acción no es "recoger" ("Presiona E para abrir", …).
4. Dejar el collider sin trigger.

---

## 6. Terminal y Puerta de Z1 (la cadena que lleva a Z2)

Dos prefabs nuevos en `Assets/GAME/Prefabs/Interactables/`, ambos con el mismo cubo base y un `Box Collider` sólido (sin trigger, para que el `SphereCast` los detecte igual que la credencial).

### 6.1 `Terminal_Registro.prefab`

| Ajuste | Valor |
|--------|-------|
| Forma | Cube, escala `(0.9, 1.4, 0.5)` |
| Collider | `Box Collider` 1 × 1 × 1, **Is Trigger = false** |
| Script | `Terminal` (hereda de `Interactable`) |
| `mensajePrompt` | `Presiona E para usar la terminal` |
| `idCodigo` | `codigo_a` (ID del contrato con Z3) |
| `codigoVisible` | `A-4721` (lo que ve el jugador) |
| Posición en Z1 | `(-2.5, 0.7, 3)` |

Qué hace: pide a `GameManager.AgregarCodigo(idCodigo)`, refresca la sección `CÓDIGOS` del panel de inventario con `UIManager.ActualizarCodigos()` y muestra en el HUD **"Código obtenido: A-4721"**. No se destruye, así que se puede volver a usar sin efecto (el GameManager ya no duplica códigos) y sirve para releer el código visible. No abre puertas: el código es información para la sala de control.

### 6.2 `Puerta_Zona2.prefab`

| Ajuste | Valor |
|--------|-------|
| Forma | Cube, escala `(1.6, 2.6, 0.2)` (una compuerta alta) |
| Collider | `Box Collider` 1 × 1 × 1, **Is Trigger = false** |
| Script | `Puerta` (hereda de `Interactable`) |
| `mensajePrompt` | `Presiona E para abrir la puerta` |
| `idLlave` | `credencial_z1` |
| `idPuerta` | `puerta_z2` |
| `escenaDestino` | `Zona2_Laboratorio` |
| `duracionApertura` | `0.8` s |
| Posición en Z1 | `(2.5, 1.3, 5)` |

Qué hace, paso a paso:

1. Si ya se abrió, ignora la tecla (evita dos cambios de escena).
2. Pregunta `GameManager.TieneItem(idLlave)`. Si no lo tiene, muestra **"Necesitas la credencial Z1"** y no se mueve.
3. Si lo tiene, marca `puerta_z2` como abierta en el GameManager y muestra "Acceso concedido. Abriendo…".
4. Una corrutina (`IEnumerator`) sube la puerta 2.5 m con `Vector3.Lerp` en 0.8 s, usando `Time.deltaTime`.
5. Al terminar llama `GameManager.CambiarEscena("Zona2_Laboratorio")`; el inventario y los códigos sobreviven porque el GameManager es `DontDestroyOnLoad`.

### 6.3 Flujo esperado en Z1

1. El jugador recoge la **credencial** (`credencial_z1`) del cubo del centro.
2. Usa la **terminal** y obtiene el código **A-4721** (`codigo_a`).
3. Sin credencial, la puerta responde "Necesitas la credencial Z1".
4. Con credencial, la puerta sube y se carga **Zona2_Laboratorio** con el HUD mostrando `- Credencial Z1`.

> La puerta pide la **credencial**, no el código. El código A es narrativo: se usará en la terminal de Z3. Así lo dice el contrato de datos del `Plan_Fase1.md`.

**Para que la terminal responda hay que acercarse y apuntarle.** Desde el punto de aparición `(0, 0, 0)` la terminal queda a 3.9 m y unos 40° a la izquierda; además la credencial está justo al frente, así que al iniciar el prompt que se ve es el de la credencial. En la práctica: caminar con **W/A** hacia la izquierda hasta ~1.5 m de la terminal y mirarla; el prompt cambia a `Presiona E para usar la terminal` y **E** entrega el código. La distancia de detección se subió de 3 a 4 m para que ese recorrido sea corto.

**Por qué el GameManager ya no duplica:** `AgregarItem` y `AgregarCodigo` comprueban `Contains` antes de agregar. Si el jugador presiona E dos veces sobre la terminal, el inventario y la lista de códigos no crecen.

---

## 7. GameManager en las escenas (prefab compartido)

Ahora el GameManager sigue la misma regla que el Canvas y el Player: **un prefab, una instancia en cada escena**.

- Prefab: `Assets/GAME/Prefabs/Core/GameManager.prefab` (GameObject vacío + script `GameManager`).
- **Las 4 escenas** (`Menu`, `Z1`, `Z2`, `Z3`) tienen una instancia del mismo prefab.
- En `Awake()`, la primera instancia se queda como `Instance` y llama `DontDestroyOnLoad`; las demás detectan que ya existe una y **se destruyen solas**.
- Por eso, al pasar de Z1 a Z2 el GameManager que sobrevive es el mismo: en la prueba automática se contó **1 solo** GameManager en la escena Z2.
- Ya no existe `GameManagerProvisional`: ese objeto se reemplazó por la instancia del prefab.

**Ventaja de tenerlo en las 4 escenas:** Iván y Samuel pueden abrir Z2 o Z3 directamente en el editor y el HUD arranca con vida 100, inventario vacío y sin errores de referencia; no necesitan entrar desde el menú.

### 7.1 Guardado y carga (`SaveManager`)

`SaveManager` es una clase **estática** (no es MonoBehaviour, no va en la escena). Recibe el estado del GameManager, lo escribe en un JSON y lo vuelve a leer.

El archivo se guarda en:

```
Application.persistentDataPath/guardado.json
```

En Windows, por ejemplo: `C:/Users/User/AppData/LocalLow/DefaultCompany/Proyecto_3D_CG/guardado.json`.

**Por qué `persistentDataPath` y no `StreamingAssets`:** `StreamingAssets` se copia al build pero queda de **solo lectura**; en el editor se puede escribir, pero al compilar el juego `File.WriteAllText` fallaría. `persistentDataPath` es la carpeta del usuario donde sí se puede escribir siempre. El plan inicial decía `StreamingAssets`; el cambio se hizo al probar la escritura real.

**Qué se guarda (clase `DatosGuardado`):**

| Campo | Tipo | Por qué |
|-------|------|---------|
| `escenaActual` | `string` | para saber dónde continuar |
| `vidaJugador` | `int` | el HUD arranca con la vida real |
| `tiempoJugadoSegundos` | `float` | estadística de la partida |
| `inventario` | `List<string>` | ítems recogidos |
| `codigos` | `List<string>` | códigos descubiertos |
| `puertas` | `List<ParBool>` | qué puertas ya se abrieron |
| `objetivos` | `List<ParBool>` | qué objetivos están completos |

**Por qué `List<ParBool>` y no `Dictionary`:** `JsonUtility` no serializa `Dictionary`. Al guardar, cada pareja clave/valor del `Dictionary` se copia a un `ParBool`; al cargar, se reconstruye el `Dictionary` con un `foreach`. Eso también cumple el requisito de "reconstruir las estructuras al cargar".

**Cuándo se guarda y cuándo se carga:**

| Momento | Método | Qué hace |
|---------|--------|----------|
| Al cambiar de zona (puerta) | `GameManager.CambiarEscena` | `SaveManager.Guardar()` **antes** de `LoadScene` |
| Al morir | `GameManager.ReiniciarDesdeCheckpoint` | `SaveManager.Cargar()`, vida 100 y recarga la zona donde murió |
| ENTER en el menú | `ControlPaneles.IniciarPartidaNueva` | `NuevaPartida()` (limpia memoria y borra el JSON) y entra a Z1 |
| C en el menú | `ControlPaneles.ContinuarPartida` | `ContinuarPartida()`: carga el JSON y abre `escenaActual` |

**El menú ahora tiene dos opciones:** `Presiona ENTER para una partida nueva o C para continuar la guardada.` Si todavía no existe `guardado.json`, la tecla C no hace nada (es la primera partida).

---

## 8. Verificación realizada en el editor

| Prueba | Resultado |
|--------|-----------|
| Importación del prefab | **0 errores** en Console después de limpiar y refrescar (`Ctrl+R`) |
| Valores internos del prefab | Auditados uno por uno: anclas, posiciones, tamaños, pivotes, fuentes y textos coinciden con las tablas de esta guía |
| Canvas en Game view | HUD visible: barra, `VIDA: 100 / 100`, `INVENTARIO / Sin objetos`, mira |
| Play mode en Z1 | Al iniciar aparece **"Presiona E para recoger"** sin mover el mouse (campos `camara`, `SphereCast`, `Interactable` y `MostrarPrompt` funcionando) |
| Escena Z1 | Player en el origen, credencial a 2.5 m al frente, `GameManagerProvisional` presente, Canvas con override HUD activo / Menu inactivo |
| Paneles Pausa, Inventario, Victoria y Derrota | No se pudieron fotografiar de forma confiable desde el editor; se verificaron por jerarquía y valores numéricos |
| Recolección con **E** | Falta la pulsación manual final: el entorno de automatización no entrega teclas sintéticas al Play mode. Es la única prueba que debe hacerse a mano |
| `ControlPaneles` | El componente quedó en la raíz del prefab (visible en el Inspector junto a `UIManager`) y el Play mode compila y corre sin excepciones. Las teclas ENTER/ESC/I se prueban a mano, por la misma limitación de teclas sintéticas |
| `Terminal_Registro` y `Puerta_Zona2` | Los dos prefabs importan sin errores; las instancias quedaron en Z1 en `(-2.5, 0.7, 3)` y `(2.5, 1.3, 5)` y los componentes se auditaron en el Inspector (código, llave, escena destino y duración) |
| Cadena Terminal → credencial → puerta | **Verificada con una prueba automática temporal** (`PruebaAuto` + `RuntimeInitializeOnLoadMethod`, borrada al terminar): sin credencial la puerta no se abre; la terminal entrega `codigo_a`; la credencial suma `credencial_z1`; con la credencial la puerta se abre, carga `Zona2_Laboratorio` y el inventario y los códigos sobreviven al cambio |
| Escena Z2 al entrar desde la puerta | HUD con `- Credencial Z1`, `GameManager` en `DontDestroyOnLoad`, sin excepciones. Sale el aviso de **2 AudioListeners** porque la Z2 tiene su `Main Camera` activa (ver error 15) |
| Guardado y carga JSON (ida y vuelta) | Prueba temporal (borrada al terminar): con vida 70, `credencial_z1`, `codigo_a`, `puerta_z2` y `tutorial_z1` se llamó `Guardar()`; se limpió la memoria a mano y `Cargar()` devolvió **todo** igual. El JSON quedó legible (indentado) y `BorrarGuardado()` eliminó el archivo |
| Cambio de escena Z1 → Z2 (checkpoint) | El script llamó `CambiarEscena("Zona2_Laboratorio")`: el JSON quedó con `escenaActual: Zona2_Laboratorio`, en Z2 apareció **1 solo** GameManager, `textoVida = "VIDA: 70 / 100"`, `textoInventario = "INVENTARIO / - Credencial z1"` y `textoCodigosPanel = "CÓDIGOS / - CÓDIGO A"` |
| GameManager como prefab en las 4 escenas | Cada escena tiene una instancia de `Prefabs/Core/GameManager.prefab`; en Play desde Z2 y al volver a `Menu` se contó **1** GameManager y el panel del menú quedó visible con el HUD oculto |
| Sección `CÓDIGOS` del panel de inventario | Verificada en Play con un script temporal (borrado al terminar): el panel muestra `INVENTARIO / - Credencial z1` arriba y `CÓDIGOS / - CÓDIGO A` en turquesa abajo, sin solaparse con el pie |
| Estado real del HUD en Play | Prueba temporal (borrada) que lee los valores: `panelHUD` activo, `textoVida = "VIDA: 100 / 100"`, `barraVida.fillAmount = 1`, `textoInventario = "INVENTARIO / Sin objetos"`, `textoCodigosPanel = "CÓDIGOS / Sin códigos"`. El HUD no estaba roto: se veía recortado por el **zoom del Game view** (ver error 16) |
| Terminal desde la posición real del jugador | El jugador se colocó en `(-2.5, 0, 1)` mirando en +Z (a 1.5 m de la terminal): el `SphereCast` devuelve `Terminal_Registro a 1.54 m`, el prompt pasa a `Presiona E para usar la terminal`, `Interactuar()` entrega `codigo_a` y en pantalla sale `Código obtenido: A-4721` |

> Aviso del editor: al entrar a Play pueden aparecer 5 errores `NoSubscription` de `generators.ai.unity.com`. Provienen del paquete **Unity AI Assistant** del editor, no del proyecto. No afectan la compilación ni la ejecución y no hay que "arreglarlos" en el código.

**Cómo probar los paneles sin código:** en Play mode, seleccionar `Canvas_Principal` en la jerarquía y activar/desactivar los checks de `Panel_Pausa`, `Panel_Inventario`, `Panel_Victoria` y `Panel_Derrota`. El HUD se oculta solo cuando `UIManager.MostrarSolo()` muestra otro panel; para verlos con el HUD encima, activarlos manualmente sin llamar al método.

---

## 9. Errores encontrados y corregidos

| # | Síntoma | Causa | Corrección |
|---|---------|-------|------------|
| 1 | El Canvas no se veía y no respondía al ratón | Escala del objeto raíz en `0,0,0` | Escala `1,1,1` |
| 2 | El prefab no importaba / referencias rotas | Espacio al final en el nombre `Panel_HUD ` y referencias a objetos inexistentes | Nombre exacto `Panel_HUD` y recableado del `UIManager` |
| 3 | La barra de vida no bajaba visualmente | `Image Type = Simple` | `Image Type = Filled`, `Horizontal`, `Fill Origin = Left`, `Fill Amount = 1` |
| 4 | La barra parecía un bloque sin borde | `BarraVida` ocupaba todo el fondo | `Size Delta = -4 × -4` (borde de 2 px) |
| 5 | El texto de vida no coincidía con el formato del script | Placeholder distinto | `VIDA: 100 / 100` (el script escribe `"VIDA: " + actual + " / " + maxima`) |
| 6 | El cubo no tenía lógica | El prefab se armó sin el componente `Recogible` | Se agregó `Recogible` con id, nombre y prompt |
| 7 | El prompt nunca aparecía | `Raycast` horizontal a 1.6 m pasaba 0.4 m por encima del cubo de 0.4 m | `SphereCast` con radio 0.35 + credencial a `y = 1.2` |
| 8 | La cámara no era la principal | Camera sin Tag y sin referencia en los scripts | Tag `MainCamera`, FOV 60 y referencias asignadas |
| 9 | No había EventSystem / módulo incorrecto | Faltaba o tenía `Standalone Input Module` | `EventSystem` + `Input System UI Input Module` |
| 10 | Títulos de Victoria/Derrota desbalanceados | `y = 110 / 100` y subtítulos en `28 / 20` | Título en `36`, subtítulo en `-46` |
| 11 | Avisos de "prefab modificado externamente" | El prefab se editó en disco mientras Unity lo tenía abierto | Se salió de Prefab Mode, se refrescó y se confirmó que el archivo conservó los valores |
| 12 | Errores `NoSubscription` en Console | Paquete Unity AI Assistant del editor | No es código del proyecto; se documenta y se ignora |
| 13 | La Zona 1 mostraba el menú encima del HUD | En esa escena el override de `Panel_Menu` estaba en `1` | Override puesto en `0` (igual que Z2 y Z3) |
| 14 | `LoadScene` fallaría en el build | Solo Z1 estaba en `Build Settings` | Se agregaron las 4 escenas, con `Menu` en índice 0 |
| 15 | Aviso "There are 2 audio listeners in the scene" al entrar a Z2 | La Z2 tiene su `Main Camera` activa con `AudioListener` y el Player trae otro | **Pendiente de Z2 (Iván):** desactivar el `AudioListener` de la `Main Camera` de Z2, como ya está en Z1. No rompe la lógica; es un aviso de audio |
| 16 | "El HUD no aparece" en Play mode | El **zoom del Game view** estaba en `1.5x`: el canvas se dibuja más grande que el área visible y se recortan los bordes, donde viven la barra, `VIDA` y el inventario. El prompt del centro seguía viéndose | Bajar el `Scale` del Game view (barra del encabezado) o maximizar con **Shift + Espacio**. Los valores leídos por script confirmaron que el HUD nunca dejó de estar activo |
| 17 | Z2 y Z3 abrían sin GameManager y el HUD arrancaba sin estado | Solo Menu y Z1 tenían el objeto; Z2/Z3 no | Se creó `Prefabs/Core/GameManager.prefab` y se puso una instancia en las 4 escenas. Se eliminó `GameManagerProvisional` |
| 18 | El guardado fallaría en el build | El plan decía guardar en `StreamingAssets`, que en un build es de solo lectura | El JSON vive en `Application.persistentDataPath` (carpeta escribible del usuario) |
| 19 | `JsonUtility` no guardaba las puertas ni los objetivos | `Dictionary` no es serializable por `JsonUtility` | Se convierte a `List<ParBool>` al guardar y se reconstruye el `Dictionary` al cargar |
| 20 | Aviso "scene modified externally" al editar los `.unity` con Unity abierto | Los cambios se hicieron desde el editor de texto | En el diálogo se elige **Reload**; la escena conserva los valores del archivo |

---

## 10. Checklist de prueba (lo que se muestra al docente)

- [ ] TMP Essentials importado (existe `Assets/TextMesh Pro` y los textos tienen fuente).
- [ ] Las 4 escenas tienen una **instancia del mismo** `Canvas_Principal` (no Canvas nuevos).
- [ ] Z1, Z2 y Z3 tienen una **instancia del mismo** `Player.prefab`.
- [ ] Las 4 escenas tienen una **instancia del mismo** `GameManager.prefab` (`Prefabs/Core`).
- [ ] El HUD muestra `VIDA: 100 / 100` y la barra llena al iniciar.
- [ ] Tecla **K** (temporal) baja 10 de vida y actualiza barra y texto; el **cooldown de 1 s** impide bajar más de un golpe por segundo.
- [ ] Al llegar a 0 aparece **TE NEUTRALIZARON** y a los 2 s se recarga la zona con vida completa.
- [ ] Mirar la credencial muestra el prompt con fondo; **E** la recoge, actualiza el inventario y desaparece.
- [ ] En `Menu`, **ENTER** empieza una partida nueva y carga `Zona1_Recepcion` con vida 100 e inventario vacío.
- [ ] Después de cruzar la puerta a Z2, existe `guardado.json` en `C:/Users/<usuario>/AppData/LocalLow/DefaultCompany/Proyecto_3D_CG` y contiene `"escenaActual": "Zona2_Laboratorio"`.
- [ ] Al volver al `Menu` con una partida guardada, **C** abre la última zona y el HUD conserva el inventario y los códigos.
- [ ] Al morir, la zona se recarga con vida completa y el progreso del último checkpoint (inventario, códigos, puertas).
- [ ] En zona, **ESC** abre `PAUSA` y congela al jugador; **ESC** otra vez devuelve el HUD y bloquea el mouse.
- [ ] En zona, **I** abre el `INVENTARIO` con la lista; **I** otra vez vuelve al HUD.
- [ ] En Z1 no se ve el menú: solo el HUD (el override de `Panel_Menu` está en `0`).
- [ ] La **terminal** responde con **E** (acercándose y mirándola) y muestra `Código obtenido: A-4721`.
- [ ] Sin la credencial, la **puerta** muestra `Necesitas la credencial Z1` y no se mueve.
- [ ] Con la credencial, la puerta sube y carga **Z2**; el HUD conserva `- Credencial Z1`.
- [ ] En el panel de inventario (**I**) aparece la sección `CÓDIGOS` con los códigos obtenidos (por ejemplo `- CÓDIGO A`).
- [ ] Los paneles Pausa, Inventario y Victoria se ven centrados al activarlos en la jerarquía.
- [ ] Abrir Z2/Z3 y confirmar que el Canvas y el Player se ven igual (prefabs compartidos).
- [ ] Console sin errores al abrir cada escena (ignorar los `NoSubscription` del Unity AI Assistant).

---

## 11. Preguntas típicas de sustentación

**¿Por qué el Canvas es un prefab y no se crea en cada escena?**
Porque la interfaz debe ser idéntica en las 4 escenas y un solo cambio se propaga a todas. Además cumple la regla de un único Canvas con paneles internos.

**¿Por qué UIManager no es DontDestroyOnLoad como el GameManager?**
Porque el GameManager guarda **estado** y debe sobrevivir; la interfaz es **presentación** y cada escena instancia el mismo prefab. Así no quedan Canvas duplicados ni referencias viejas.

**¿Por qué TextMeshPro y no el Text antiguo?**
Porque es el sistema de texto actual de Unity integrado con UGUI: mejor nitidez, texto escalable y una sola fuente (`LiberationSans SDF`) para todo el proyecto. Los campos del `UIManager` son `TextMeshProUGUI`.

**¿Por qué PlayerHealth usa un cooldown?**
Para que el agente no quite vida en cada frame. Se compara `Time.time` con el momento en que se permite el siguiente golpe; mientras no pase, el daño se ignora.

**¿Por qué CharacterController y no Rigidbody?**
Porque para un FPS simple da movimiento directo, gravedad y colisiones sin configurar física; además se explica en una sola clase.

**¿Dónde se usa el New Input System?**
En `Keyboard.current` y `Mouse.current` de `PlayerController`, `InteractionController`, `PlayerHealth` (tecla K de prueba) y `ControlPaneles` (ENTER, C, ESC e I). No se usa `Input.GetAxis` en ningún script.

**¿Por qué `Interactable` es abstracta?**
Porque define el contrato (`Interactuar()`) y obliga a que cada objeto concreto escriba su comportamiento: recoger, abrir, activar, etc. El `InteractionController` solo conoce la base.

**¿Por qué SphereCast y no Raycast?**
Porque un Raycast no tiene grosor y exigía puntería perfecta. El SphereCast es un rayo con radio 0.35: si la cámara apunta cerca del objeto, igual lo detecta. Es la diferencia entre "tocar el píxel exacto" y "apuntar al objeto".

**¿Por qué la terminal no responde si el jugador apenas aparece?**
Porque está a 3.9 m del punto de aparición y el `SphereCast` solo alcanza 4 m **en la dirección en que mira la cámara**. Al iniciar, la cámara apunta a la credencial, que está al frente; la terminal queda ~40° a la izquierda y fuera de la mirada. El jugador debe girar y caminar hacia ella: ahí nace el prompt. No es un error de lógica, es la regla de "mira y acércate" de cualquier FPS.

**¿Por qué la credencial está a 1.2 m?**
Porque la cámara está a 1.6 m y el cubo mide 0.4 m. A 1.0 m el rayo pasaba por encima; a 1.2 m la parte superior queda a 1.4 m y entra en el radio del SphereCast.

**¿Por qué el collider del recogible no es trigger?**
Porque el SphereCast usa `QueryTriggerInteraction.Ignore`. Si fuera trigger, el rayo lo ignoraría y no habría prompt. El collider sólido es el que detecta la mirada.

**¿Por qué la barra tiene tamaño negativo?**
Porque está en stretch y un `-4` recorta 2 px por lado, dejando ver el fondo como borde. Es una técnica de UGUI: el tamaño negativo reduce el área del hijo respecto al padre.

**¿Por qué el Canvas usa 1920 × 1080 y Match 0.5?**
Porque es la resolución de referencia del proyecto y `Match 0.5` reparte el escalado entre ancho y alto, así el HUD no se deforma en pantallas 16:9 ni en ventanas más cuadradas.

**¿Por qué la puerta pide la credencial y no el código A?**
Porque el contrato de datos separa dos cosas: la credencial es la **llave física** que abre la puerta de Z1, y el código A es **información** que se usará en la terminal de Z3. Si la puerta pidiera el código, la terminal quedaría sin función narrativa y la cadena de tres zonas se rompería.

**¿Por qué la puerta se mueve con una corrutina?**
Porque `Vector3.Lerp` dentro de un `while` con `yield return null` reparte el movimiento en varios frames sin bloquear el juego. Así el jugador ve la compuerta subir 0.8 s antes del cambio de escena, en lugar de desaparecer de golpe.

**¿Por qué el GameManager no duplica ítems ni códigos?**
Porque `AgregarItem` y `AgregarCodigo` comprueban `Contains` antes de agregar. Sin eso, usar dos veces la terminal dejaría `codigo_a` repetido y el inventario mostraría la credencial varias veces.

**¿Por qué el inventario muestra "CÓDIGO A" y no "A-4721"?**
Porque el GameManager guarda el **ID del contrato** (`codigo_a`), que es lo que Z3 consulta con `TieneCodigo`. El texto legible (`A-4721`) se muestra en el mensaje de la terminal, y como la terminal no se destruye, se puede volver a usar para releerlo. Si más adelante Z3 pide teclear el código, se guardará también el texto visible.

**¿Dónde está el guardado JSON?**
En `Application.persistentDataPath/guardado.json` (en Windows: `C:/Users/<usuario>/AppData/LocalLow/DefaultCompany/Proyecto_3D_CG/guardado.json`). Se escribe con `JsonUtility.ToJson(DatosGuardado, true)` y se lee con `JsonUtility.FromJson<DatosGuardado>`.

**¿Por qué no se guarda en `StreamingAssets` como decía el plan?**
Porque `StreamingAssets` queda de solo lectura al compilar el juego: `File.WriteAllText` funcionaría en el editor y fallaría en el `.exe`. `persistentDataPath` es la carpeta del usuario donde la aplicación siempre puede escribir. Se corrigió al hacer la primera prueba real de escritura.

**¿Por qué `SaveManager` no es un MonoBehaviour / Singleton?**
Porque no tiene estado ni necesita estar en la escena: solo transforma datos. Al ser `static`, cualquier script lo llama con `SaveManager.Guardar()` sin arrastrar referencias en el Inspector. El que sí tiene estado y sobrevive es el GameManager.

**¿Por qué `JsonUtility` y no otro serializador?**
Porque viene con Unity, no requiere paquetes y es suficiente para listas de `string`, enteros y pares clave/valor. La única limitación es que no serializa `Dictionary`, y se resuelve con `List<ParBool>`.

**¿Cuándo se guarda la partida?**
En el **checkpoint**: justo antes de cambiar de zona (`CambiarEscena`) y al empezar una partida nueva. No se guarda cada frame ni la posición de los agentes: el JSON se escribe pocas veces y la partida se puede recuperar sin congelar el juego.

**¿Qué pasa exactamente cuando el jugador muere?**
`PlayerHealth` muestra `TE NEUTRALIZARON`, espera 2 s y llama `ReiniciarDesdeCheckpoint()`: `SaveManager.Cargar()` devuelve el progreso del último guardado, la vida vuelve a 100 y se recarga la zona donde murió. Lo que se hubiera recogido **después** del checkpoint se pierde, que es la definición de checkpoint.

**¿Cómo se reconstruyen las estructuras al cargar?**
`DatosGuardado` trae `List<string>` y `List<ParBool>`; al cargar, las listas se asignan al GameManager y cada `ParBool` se vuelve a meter en el `Dictionary` con `puertas[clave] = valor`. Así el JSON guarda contenido plano y el juego recupera sus `Dictionary`.

---

## 12. Siguiente bloque del itinerario

| Fecha | Tarea |
|-------|-------|
| 27-28 oct | **Hecho:** ESC (pausa), I (inventario), las 4 escenas en Build Settings, `Terminal` + `Puerta` en Z1 con cambio a Z2, sección `CÓDIGOS` del inventario y `GameManager` como prefab en las 4 escenas. **Falta:** victoria/derrota con botones, `AgentController` base + NavMesh, AudioManager |
| 29-30 oct | **Hecho:** `SaveManager`/`DatosGuardado` con JSON en `persistentDataPath`, guardado al cambiar de escena, carga al morir (checkpoint), ENTER = partida nueva y C = continuar (verificado con prueba automática). **Falta:** FSM del agente |
| 2 nov | Victoria/derrota con botones (reintentar / menú) y **HITO: núcleo transversal funcional** |
