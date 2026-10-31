# Créditos de assets externos

Este archivo registra **todo** modelo, textura, sonido o partícula que no haya creado el equipo.
El docente descarga el proyecto y aquí puede comprobar de dónde salió cada recurso y con qué licencia.
Se actualiza **en el mismo commit** en el que se agrega el asset.

## Reglas

- Usar solo licencias **CC0** o **CC-BY**. Evitar **CC-BY-NC**, **ND** y **SA**.
- Registrar la **URL de la página original** del asset (no la del archivo descargado).
- Una fila por **pack**, no por pieza. En "Piezas usadas" se listan los modelos que se tomaron.
- Si el asset se modificó (escala, material, recorte), anotarlo en "Notas".
- Los nombres de archivo dentro del proyecto deben ser claros y en minúsculas: `terminal_consola.fbx`, `puerta_corrediza.fbx`.

## Assets integrados

| # | Asset / pack | Tipo | Autor | Fuente (URL) | Licencia | Piezas usadas | Dónde se usa | Notas |
|---|--------------|------|-------|--------------|----------|---------------|--------------|-------|
| 1 | Furniture Kit | 3D | Kenney | https://kenney.nl/assets/furniture-kit | CC0 | mostrador, silla_oficina, archivador, monitor, teclado, raton, luminaria_techo, caja_carton, caja_abierta, planta_maceta, planta_pequena, basurero | Z1 (`Art/Z1_Recepcion/Models`) | Convertidos de GLB a OBJ con MTL; escala ajustada a tamaño real; sin texturas, colores planos por material |
| 2 | Space Station Kit | 3D | Kenney | https://kenney.nl/assets/space-station-kit | CC0 | terminal_consola, puerta_corrediza | Compartidos (`Art/Compartidos/Models`) | Convertidos de GLB a OBJ; atlas `colormap.png` incluido; terminal 0,72 m de alto, puerta 1,68 m |
| 3 | Pickup Key Card | 3D | Quaternius (publicado en Poly Pizza) | https://poly.pizza/m/EDvCEBvs8k | CC0 | credencial | Compartidos (`Art/Compartidos/Models`) | Convertido de GLB a OBJ; atlas incluido; escala 10,4 cm de ancho |
| 4 | Wall Clock | 3D | PierreB3D (Poly Haven) | https://polyhaven.com/a/wall_clock | CC0 | reloj_pared | Z1 (`Art/Z1_Recepcion/Models`) | Convertido de glTF a OBJ; textura difusa 1K; se omitió el vidrio; pivote centrado, 32 × 32 cm |
| 5 | Korean Fire Extinguisher 01 | 3D | UM JOORIN (Poly Haven) | https://polyhaven.com/a/korean_fire_extinguisher_01 | CC0 | extintor | Z1 (`Art/Z1_Recepcion/Models`) | Convertido de glTF a OBJ; texturas 1K; se omitió el vidrio del manómetro; base en el piso, 66 cm de alto |

> Ejemplo de fila completa:
> `| 1 | Sci-Fi Essentials Kit | 3D | Quaternius | https://quaternius.com/ | CC0 | consola, pantalla | Terminal_Registro (Z1) | escalado a 1.4 m de alto |`

## Assets propios (sin licencia externa)

| Asset | Cómo se hizo | Archivos |
|-------|--------------|----------|
| Audio de Z1 | Síntesis de audio procedural, WAV 44.1 kHz / 16 bits estéreo (música) y mono (SFX), sin samples de terceros | 16 SFX en `Audio/SFX/`, `Audio/Music/mus_z1_ambiente.wav` (48 s) y `Audio/Music/mus_persecucion.wav` (15 s) |
| Partículas de Z1 | Particle Systems de Unity armados por el equipo; texturas PNG de 64×64 y 128×128 generadas por código (sin imágenes de terceros) | `Particles/Z1_Recepcion/Textures/` (4 PNG), `Materials/` (4 materiales), `Prefabs/` (4 prefabs) |

> Los sonidos y la música son una **versión provisional** hecha por el equipo. La lista de pendientes de abajo sirve para reemplazarlos por grabaciones o packs si se quiere más realismo; mientras tanto, el proyecto ya tiene audio propio que se puede demostrar.

## Pendientes de descarga

### Modelos compartidos (`Art/Compartidos/Models`)

- [x] Credencial / tarjeta de acceso → `credencial.obj` (+ `.mtl` y atlas)
- [x] Terminal / consola con pantalla → `terminal_consola.obj` (+ atlas)
- [x] Puerta corrediza industrial (una sola hoja) → `puerta_corrediza.obj` (0,96 × 1,68 m)

### Modelos de Z1 (`Art/Z1_Recepcion/Models`)

- [x] Mostrador de recepción → `mostrador.obj` (1,47 × 0,77 m)
- [x] Silla de oficina → `silla_oficina.obj`
- [x] Casillero o archivador metálico → `archivador.obj` (1,70 m de alto)
- [x] Monitor y teclado → `monitor.obj`, `teclado.obj`, `raton.obj`
- [x] Luminaria de techo → `luminaria_techo.obj`
- [x] Cajas de cartón o canastas → `caja_carton.obj`, `caja_abierta.obj`
- [ ] Letrero de pared ("ZONA 1 / RECEPCIÓN") → se hará con TextMeshPro, no requiere descarga
- [x] Extintor → `extintor.obj` (Poly Haven, 66 cm de alto)
- [x] Reloj de pared → `reloj_pared.obj` (Poly Haven, 32 × 32 cm)
- [ ] Cámara de seguridad → pendiente; en Poly Pizza solo aparece con CC-BY, se puede usar acreditando al autor
- [x] Planta o maceta volcada → `planta_maceta.obj`, `planta_pequena.obj`

### Sonidos (`Assets/GAME/Audio`)

- [x] ~~Ambiente en loop: zumbido eléctrico + viento (`Audio/Ambience`)~~ → **sustituido por música de ambiente** (el zumbido se percibía como estática de TV)
- [x] Música de ambiente tétrica y lenta, en loop (`Audio/Music/mus_z1_ambiente.wav`) — provisional del equipo
- [x] Música de persecución, en loop (`Audio/Music/mus_persecucion.wav`) — provisional del equipo
- [x] Pasos sobre concreto (`Audio/SFX/sfx_pasos_concreto_01…05.wav`)
- [x] Puerta corrediza + servo (`Audio/SFX/sfx_puerta_servo.wav`)
- [x] Beep de terminal / tecleo (`Audio/SFX/sfx_terminal_beep.wav`, `sfx_terminal_tecleo.wav`)
- [x] Recoger ítem (`Audio/SFX/sfx_recoger.wav`)
- [x] Golpe del agente (`Audio/SFX/sfx_agente_golpe.wav`, `sfx_agente_pasos.wav`)
- [x] Alarma lejana (`Audio/SFX/sfx_alarma_lejana.wav`) — con desvanecido final de 1,2 s
- [x] Clic de UI y stingers de victoria/derrota (`Audio/SFX/ui_clic.wav`, `stinger_victoria.wav`, `stinger_derrota.wav`)
- [x] Música de fondo (`Audio/Music/mus_z1_ambiente.wav`, `mus_persecucion.wav`)

### Partículas (`Assets/GAME/Particles/Z1_Recepcion`)

- [x] Polvo flotando en la luz → `Prefabs/PS_Polvo_Z1.prefab`
- [x] Chispas de la luminaria rota → `Prefabs/PS_Chispas_Luminaria.prefab`
- [x] Vapor de tubería → `Prefabs/PS_Vapor_Tuberia.prefab`
- [x] Brillo del recogible → `Prefabs/PS_Brillo_Credencial.prefab`
- [x] Texturas de partículas (humo, chispa, polvo, brillo) → `Textures/tex_particula_*.png` (propias)

### Texturas (`Art/Z1_Recepcion/Textures`)

- [ ] Concreto
- [ ] Metal pintado
- [ ] Óxido / desgaste

## Licencias: guía rápida

| Licencia | Qué obliga | ¿Se puede usar? |
|----------|------------|-----------------|
| **CC0** | Nada (se acredita igual por cortesía) | Sí, es la preferida |
| **CC-BY** | Dar crédito al autor con enlace | Sí, registrando autor y URL |
| **CC-BY-SA** | Crédito y compartir igual | Evitar |
| **CC-BY-ND** | Crédito y no modificar | Evitar (hay que escalar y cambiar materiales) |
| **CC-BY-NC** | Solo uso no comercial | Evitar para no discutirlo en la sustentación |
| **Asset Store / uso libre** | Según el pack | Sí, anotar el pack y su licencia |

## Registro de descargas

| Fecha | Qué se agregó | Commit / tarea |
|-------|---------------|----------------|
| 2026-10-31 | 15 modelos 3D: 12 de Z1 y 3 compartidos (Kenney + Poly Pizza), convertidos a OBJ | Tarea "Elementos de escena Z1" (sin commit) |
| 2026-10-31 | Reloj de pared y extintor (Poly Haven, 1K), convertidos a OBJ | Tarea "Elementos de escena Z1" (sin commit) |
| 2026-10-31 | Partículas de Z1: 4 prefabs, 4 materiales y 4 texturas generadas por código (sin assets externos) | Tarea "Partículas Z1" (sin commit) |
