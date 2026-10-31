# Prefabs interactuables de Zona 2

La librería compartida está en `Assets/GAME/Prefabs/Interactables`. Usa el mismo
`Interactable` base y el `SphereCast` del `InteractionController`, así que el
jugador activa todos los objetos con la tecla E y el prompt del Canvas existente.

## Prefabs

| Prefab | Componente | Comportamiento |
| --- | --- | --- |
| `Puerta_Zona2` | `Puerta` | Requiere la credencial Z1 y carga la escena configurada. |
| `Terminal_Registro` | `Terminal` | Entrega un código al GameManager. Configura `idCodigo` y `codigoVisible`. |
| `Recogible_Credencial` | `Recogible` | Agrega un item al inventario. Cambia el ID y el nombre en el Inspector. |
| `Boton` | `BotonInteractuable` | Dispara un UnityEvent, actualiza su indicador y puede activarse una vez. |
| `Interruptor` | `InterruptorInteractuable` | Alterna encendido/apagado, mueve la palanca y dispara eventos por estado. |
| `Cajon` | `CajonInteractuable` | Abre y cierra el frente animado. |
| `Tarjeta` | `Recogible` | Tarjeta de acceso reutilizable; configura `idItem` por instancia. |

Botones, interruptores y cajones exponen `AudioSource` para asignar los SFX de la
biblioteca compartida cuando esté integrada. Los UnityEvents permiten conectar
acciones de escena desde el Inspector sin acoplar los prefabs a un controlador.

## Percepción del agente

`AgentController` combina `distanciaDeteccion`, `campoVision` y un raycast desde
`puntoVision`. Si no se asigna `puntoVision`, usa la posición del agente más 1.5 m.
El primer collider visible debe pertenecer al jugador; paredes y otros objetos
bloquean la detección. El agente solo persigue y ataca mientras ve al jugador.
Seleccionar el agente en la escena muestra el radio y los límites del campo visual.
