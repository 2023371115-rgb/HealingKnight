# Healing Knight

Beat 'em up 2.5D de un jugador (estilo Castle Crashers) hecho en Unity 6. Un nanobot médico recorre los sistemas del cuerpo del Paciente 001 y derrota enfermedades usando el medicamento correcto.

**Entrega de diciembre:** Sistema Digestivo (Boca y esófago, Estómago, Intestino).

---

## Primer uso (5 minutos)

1. Abre el proyecto en Unity (6000.6.3f1). La primera vez tarda en importar.
2. En la barra de menús: **Healing Knight → Crear proyecto base**.
   Crea los sprites de prueba, los datos (medicamentos, enemigos, sistemas), los prefabs y las escenas.
   Se puede volver a ejecutar: **nunca sobrescribe** lo que ya existe.
3. Abre `Assets/_Project/Scenes/MainMenu.unity` y dale **Play**.
4. Haz commit de todo lo que se generó, incluidos los archivos `.meta`.

Para probar mecánicas sin pasar por el menú, abre **Healing Knight → Abrir escena → Sandbox**.

## Controles

| Acción | Teclado | Gamepad |
|---|---|---|
| Mover (incluye profundidad) | WASD / flechas | Stick izq. / cruceta |
| Saltar | Espacio | A |
| Disparar (mantener) | J | X / gatillo der. |
| Escudo (mantener) | K | B / gatillo izq. |
| Golpe cercano | L | Y |
| Medicamento anterior / siguiente | Q / E | LB / RB |
| Medicamento directo | 1 a 5 | — |
| Pausa | Esc | Start |

Los controles se definen en `Scripts/Gameplay/Player/PlayerInputReader.cs`.

---

## Estructura de carpetas

```
Assets/
├── _Project/                 ← TODO lo del juego va aquí
│   ├── Art/                  Sprites (Placeholders se generan solos; reemplázalos con tu arte)
│   ├── Audio/
│   ├── Data/                 ScriptableObjects: balance y contenido, editables sin código
│   │   ├── Combat/           EffectivenessTable (tabla medicamento × amenaza)
│   │   ├── Enemies/          Parasito, ParasitoGrande, Bacteria_HPylori, Rotavirus
│   │   ├── Medications/      Antibiótico, Antiviral, ..., Antiparasitario, Mucolítico
│   │   ├── Player/           PlayerConfig (velocidades, salto, daño, escudo...)
│   │   ├── Resources/        GameCatalog (lista de sistemas del mapa)
│   │   └── Systems/          Un folder por sistema con su SystemData y sus LevelData
│   ├── Prefabs/              Player, Enemies, Projectiles, Hazards
│   ├── Scenes/               MainMenu, BodyMap, Levels/<Sistema>/..., Sandbox
│   ├── Scripts/
│   │   ├── Core/             Datos, efectividad, daño, progreso y guardado (sin dependencias)
│   │   ├── Gameplay/         Jugador, enemigos, arenas, peligros, cámara, nivel
│   │   ├── UI/               HUD, pausa, menú principal, mapa del cuerpo
│   │   └── Editor/           Herramienta "Crear proyecto base"
│   └── Tests/EditMode/       Pruebas automáticas (Window → General → Test Runner)
├── _Prototype/               El prototipo entregado para AU (solo referencia; se puede borrar)
├── Settings/                 URP y configuración de render
└── TextMesh Pro/
```

Cada carpeta de `Scripts` es un *assembly* (`.asmdef`): compila por separado y no puede depender de las de "arriba". La dirección es Core ← Gameplay ← UI ← Editor.

## Cómo funciona el 2.5D

- La **posición** de un personaje es el punto del piso donde están sus pies: **X** es izquierda/derecha y **Y** es profundidad (más arriba es más al fondo).
- La **altura del salto** se aplica solo al hijo `Visual`. La `Shadow` se queda en el piso.
- Un golpe o una cápsula solo acierta si el objetivo está en la **misma franja de profundidad** (`laneTolerance`).
- El orden de dibujado es por Y (*Custom Axis* en `Settings/Renderer2D`): lo que está más al frente se dibuja encima. Cada personaje tiene un `SortingGroup` para que sus piezas se ordenen juntas.
- No se usa la física de Unity para el combate. Las colisiones son matemática simple (`DepthMath`, `Hurtbox`), más predecible y fácil de ajustar.

## Cómo agregar contenido

| Quiero... | Pasos |
|---|---|
| **Cambiar balance** (daño, velocidad, vida) | Edita el asset en `Data/` (funciona incluso en Play Mode) |
| **Un enemigo nuevo** | Crea un `EnemyData` (clic derecho → Create → Healing Knight → Enemigo), duplica un prefab de `Prefabs/Enemies`, asígnale el dato y cambia el arte de `Visual` |
| **Un nivel nuevo** | Duplica una escena de `Scenes/Levels`, crea un `LevelData` con el nombre exacto de la escena, agrégalo a la lista del `SystemData` y a Build Settings |
| **Una arena** | GameObject con `ArenaZone`: `triggerX`, límites y oleadas (prefab + dato + cantidad) |
| **Un sistema nuevo** | Crea `SystemData` y sus `LevelData`, y agrégalo al `GameCatalog` |
| **Tu arte del personaje** | Importa el PSD, arma las piezas dentro de `Visual` del prefab `HealingKnight` y borra las figuras de prueba. Los scripts de la raíz no cambian |

## Reglas del repo

- `main` = estable. Trabaja en ramas `feature/nombre` y únelas con Pull Request.
- Una persona por escena a la vez.
- Mueve o renombra archivos **desde Unity** (para que su `.meta` viaje con ellos).
- Si usas archivos grandes (PSD, WAV), instala Git LFS una vez: `git lfs install`.
