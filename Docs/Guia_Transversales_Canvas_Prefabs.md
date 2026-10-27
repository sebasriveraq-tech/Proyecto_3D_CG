# Elementos transversales, Canvas y prefabs (versión final)

Esta guía documenta **cómo quedaron armados** el Canvas, el jugador y el recogible, y **qué se corrigió** durante la revisión del 26-oct. Sirve para tres cosas: reconstruir los prefabs si se pierden, entender cada valor para la sustentación y repetir la verificación antes de la entrega.

> Estado: `Canvas_Principal.prefab` importa **sin errores** en Unity 6000.3.14f1, el HUD se ve en Play, el prompt de la credencial aparece automáticamente y las 4 escenas usan las mismas instancias de Canvas y Player.

---

## 1. Qué evalúa el docente y con qué lo cumplimos

| Elemento transversal | Archivo | Dónde vive | Para qué sirve |
|----------------------|---------|------------|----------------|
| Estado global | `GAME/Scripts/Core/GameManager.cs` | 1 objeto en Menu (y `GameManagerProvisional` en Z1 para pruebas) | Vida, inventario, códigos, puertas y objetivos entre escenas |
| Interfaz | `GAME/Scripts/Core/UIManager.cs` | Prefab `Canvas_Principal` | Mostrar paneles, vida, inventario, mensajes y prompt |
| Jugador | `PlayerController` + `PlayerHealth` + `InteractionController` | Prefab `Player` | Mover, mirar, recibir daño e interactuar igual en Z1, Z2 y Z3 |
| Objetos interactuables | `Interactable` + `Recogible` | Prefab `Recogible_Credencial` | Base común para puertas, terminales, botones y recogibles |
| Guardado JSON | `SaveManager` (siguiente bloque) | StreamingAssets | Persistencia (itinerario 29-30 oct) |

**Idea central para la sustentación:** el GameManager es el único que **guarda** estado; el UIManager solo **pinta**; el Player y los interactuables son **prefabs reutilizados** en todas las escenas. Nada se duplica ni se vuelve a programar por zona.

---

## 2. Qué hace cada clase nueva

| Clase | Carpeta | Responsabilidad | Lo que NO hace |
|-------|---------|-----------------|----------------|
| `UIManager` | `Scripts/Core` | Activar/ocultar paneles, actualizar barra y textos, mostrar prompt y mensajes | No guarda progreso ni cambia de escena |
| `PlayerController` | `Scripts/Player` | Movimiento y cámara con New Input System + CharacterController | No recibe daño ni sabe de ítems |
| `PlayerHealth` | `Scripts/Player` | Vida, cooldown de daño, HUD y derrota | No mueve al jugador |
| `InteractionController` | `Scripts/Player` | SphereCast desde la cámara, prompt y tecla E | No decide qué hace cada objeto |
| `Interactable` | `Scripts/Interactables` | Clase base abstracta: obliga a implementar `Interactuar()` | No tiene lógica propia |
| `Recogible` | `Scripts/Interactables` | Suma un ítem al GameManager, actualiza el HUD y desaparece | No sirve para puertas ni terminales |

**Flujo completo de una interacción (para contarlo de memoria):**

1. `InteractionController` lanza un `SphereCast` desde la cámara (3 m, radio 0.35).
2. Si el collider golpeado tiene un `Interactable` en su padre, guarda ese objeto como `objetivoActual`.
3. Al cambiar de objetivo, pide a `UIManager` que muestre u oculte el prompt.
4. Si se presiona **E** (`Keyboard.current.eKey.wasPressedThisFrame`) y hay objetivo, llama `Interactuar()`.
5. `Recogible.Interactuar()` agrega el `idItem` al GameManager, refresca el inventario, muestra "Recogiste: …", oculta el prompt y destruye el objeto.

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
│  └─ PieInv
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
| `Descripcion_Menu` (TMP) | centro | (0, -8) | 980 × 64 | 20 | `Explora las instalaciones, recupera las credenciales y abre la sala de control.`, gris claro, centrado |
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
| `TextoInvPanel` (TMP) | (0, -6) | 660 × 420 | 21 | Inicia en `Sin objetos`; lo llena `ActualizarInventario()`, alineado arriba-izquierda |
| `PieInv` (TMP) | (0, -252) | 660 × 28 | 15 | `Presiona I para cerrar`, gris oscuro, centrado |

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
- **Panel de inventario:** `textoInventarioPanel`.
- **Colores:** alta, media y baja.

`Awake()` guarda `Instance` y oculta `fondoMensaje` y `fondoPrompt`. `Start()` lee la vida y el inventario del GameManager para que el HUD arranque con el estado real.

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
| `InteractionController` | `camara` → Camera hija; distancia 3, `grosorRayo` 0.35 |

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

## 6. GameManager en las escenas

- **Menu** tiene el GameManager definitivo; sobrevive con `DontDestroyOnLoad`.
- **Z1** tiene `GameManagerProvisional` (el que se agregó): un objeto vacío con el script `GameManager`, vida 100 e inventario vacío. **Está correcto** y sirve para probar la zona sin pasar por Menu.
- Si se entra desde Menu, el `GameManagerProvisional` de Z1 detecta que ya existe `Instance` en `Awake()` y **se destruye solo**. No hay estado duplicado.
- Z2 y Z3 no lo tienen: si se reproducen directamente, el jugador funciona con vida 100 y el inventario no persiste (el `Recogible` solo suma cuando `GameManager.Instance` existe). Si se necesita probarlas sueltas, se puede agregar un provisional igual.

**Regla:** el objeto global se llama GameManager; el de prueba se llama `GameManagerProvisional` para que en la jerarquía se distinga cuál es el definitivo.

---

## 7. Verificación realizada en el editor

| Prueba | Resultado |
|--------|-----------|
| Importación del prefab | **0 errores** en Console después de limpiar y refrescar (`Ctrl+R`) |
| Valores internos del prefab | Auditados uno por uno: anclas, posiciones, tamaños, pivotes, fuentes y textos coinciden con las tablas de esta guía |
| Canvas en Game view | HUD visible: barra, `VIDA: 100 / 100`, `INVENTARIO / Sin objetos`, mira |
| Play mode en Z1 | Al iniciar aparece **"Presiona E para recoger"** sin mover el mouse (campos `camara`, `SphereCast`, `Interactable` y `MostrarPrompt` funcionando) |
| Escena Z1 | Player en el origen, credencial a 2.5 m al frente, `GameManagerProvisional` presente, Canvas con override HUD activo / Menu inactivo |
| Paneles Pausa, Inventario, Victoria y Derrota | No se pudieron fotografiar de forma confiable desde el editor; se verificaron por jerarquía y valores numéricos |
| Recolección con **E** | Falta la pulsación manual final: el entorno de automatización no entrega teclas sintéticas al Play mode. Es la única prueba que debe hacerse a mano |

> Aviso del editor: al entrar a Play pueden aparecer 5 errores `NoSubscription` de `generators.ai.unity.com`. Provienen del paquete **Unity AI Assistant** del editor, no del proyecto. No afectan la compilación ni la ejecución y no hay que "arreglarlos" en el código.

**Cómo probar los paneles sin código:** en Play mode, seleccionar `Canvas_Principal` en la jerarquía y activar/desactivar los checks de `Panel_Pausa`, `Panel_Inventario`, `Panel_Victoria` y `Panel_Derrota`. El HUD se oculta solo cuando `UIManager.MostrarSolo()` muestra otro panel; para verlos con el HUD encima, activarlos manualmente sin llamar al método.

---

## 8. Errores encontrados y corregidos

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

---

## 9. Checklist de prueba (lo que se muestra al docente)

- [ ] TMP Essentials importado (existe `Assets/TextMesh Pro` y los textos tienen fuente).
- [ ] Las 4 escenas tienen una **instancia del mismo** `Canvas_Principal` (no Canvas nuevos).
- [ ] Z1, Z2 y Z3 tienen una **instancia del mismo** `Player.prefab`.
- [ ] El HUD muestra `VIDA: 100 / 100` y la barra llena al iniciar.
- [ ] Tecla **K** (temporal) baja 10 de vida y actualiza barra y texto; el **cooldown de 1 s** impide bajar más de un golpe por segundo.
- [ ] Al llegar a 0 aparece **TE NEUTRALIZARON** y a los 2 s se recarga la zona con vida completa.
- [ ] Mirar la credencial muestra el prompt con fondo; **E** la recoge, actualiza el inventario y desaparece.
- [ ] Los paneles Pausa, Inventario y Victoria se ven centrados al activarlos en la jerarquía.
- [ ] Abrir Z2/Z3 y confirmar que el Canvas y el Player se ven igual (prefabs compartidos).
- [ ] Console sin errores al abrir cada escena (ignorar los `NoSubscription` del Unity AI Assistant).

---

## 10. Preguntas típicas de sustentación

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
En `Keyboard.current` y `Mouse.current` de `PlayerController`, `InteractionController` y la tecla de prueba de `PlayerHealth`. No se usa `Input.GetAxis` en ningún script.

**¿Por qué `Interactable` es abstracta?**
Porque define el contrato (`Interactuar()`) y obliga a que cada objeto concreto escriba su comportamiento: recoger, abrir, activar, etc. El `InteractionController` solo conoce la base.

**¿Por qué SphereCast y no Raycast?**
Porque un Raycast no tiene grosor y exigía puntería perfecta. El SphereCast es un rayo con radio 0.35: si la cámara apunta cerca del objeto, igual lo detecta. Es la diferencia entre "tocar el píxel exacto" y "apuntar al objeto".

**¿Por qué la credencial está a 1.2 m?**
Porque la cámara está a 1.6 m y el cubo mide 0.4 m. A 1.0 m el rayo pasaba por encima; a 1.2 m la parte superior queda a 1.4 m y entra en el radio del SphereCast.

**¿Por qué el collider del recogible no es trigger?**
Porque el SphereCast usa `QueryTriggerInteraction.Ignore`. Si fuera trigger, el rayo lo ignoraría y no habría prompt. El collider sólido es el que detecta la mirada.

**¿Por qué la barra tiene tamaño negativo?**
Porque está en stretch y un `-4` recorta 2 px por lado, dejando ver el fondo como borde. Es una técnica de UGUI: el tamaño negativo reduce el área del hijo respecto al padre.

**¿Por qué el Canvas usa 1920 × 1080 y Match 0.5?**
Porque es la resolución de referencia del proyecto y `Match 0.5` reparte el escalado entre ancho y alto, así el HUD no se deforma en pantallas 16:9 ni en ventanas más cuadradas.

**¿Dónde está el guardado JSON?**
En el siguiente bloque del itinerario (29-30 oct): `DatosGuardado` + `SaveManager` en `StreamingAssets`, con `List`/`Dictionary` reconstruidos al cargar.

---

## 11. Siguiente bloque del itinerario

| Fecha | Tarea |
|-------|-------|
| 27-28 oct | Cambio de escenas + victoria/derrota; `AgentController` base + NavMesh; AudioManager; botones del menú (pausa con ESC, inventario con I) |
| 29-30 oct | `SaveManager`/`DatosGuardado` JSON en StreamingAssets; FSM del agente; flujo Menu → nueva partida / continuar |
| 2 nov | Checkpoints, reinicio desde JSON y **HITO: núcleo transversal funcional** |
