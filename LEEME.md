# Healing Knight

*Run and gun* 2D de un jugador (estilo **Metal Slug**) hecho en Unity 6. Un nanobot médico recorre los sistemas del cuerpo del Paciente 001 y derrota enfermedades usando el medicamento correcto.

**Entrega de diciembre:** Sistema Digestivo: Estómago → Intestino delgado (la lombriz aparece y huye) → Intestino grueso (pelea final con la lombriz). La **Boca** es un nivel secreto: se desbloquea al tocar un pasaje escondido en el Estómago.

---

## Primer uso (5 minutos)

1. Abre el proyecto en Unity (6000.6.3f1). La primera vez tarda en importar.
2. En la barra de menús: **Healing Knight → Crear proyecto base**.
   Crea los sprites de prueba, los datos (medicamentos, enemigos, sistemas), los prefabs y las escenas.
   Se puede volver a ejecutar: **nunca sobrescribe** lo que ya existe.
3. Abre `Assets/_Project/Scenes/MainMenu.unity` y dale **Play**.
4. Haz commit de todo lo que se generó, incluidos los archivos `.meta`.

Para probar mecánicas sin pasar por el menú, abre **Healing Knight → Abrir escena → Sandbox** (tiene todos los enemigos y al jefe).

Si cambia la estructura del proyecto base (nuevos enemigos, niveles, etc.) y todavía no has personalizado los assets generados, usa **Healing Knight → Regenerar proyecto base**: borra `Data`, `Prefabs`, `Scenes` y `Art/Placeholders` de `_Project` y los vuelve a crear.

## Controles (copiados de Metal Slug)

| Acción | Teclado | Gamepad |
|---|---|---|
| Moverse | A / D | Stick izq. / cruceta |
| Apuntar arriba · **diagonal** (dos teclas a la vez) | W · W + A/D | Arriba · arriba + lado |
| Agacharse (en el suelo) · apuntar abajo o diagonal abajo (en el aire) | S · S (+ A/D) | Abajo |
| Disparar (mantener = ráfaga). Si un enemigo está pegado, **cuchillazo** automático | J | X |
| Saltar (altura fija) · **doble salto** en el aire | K / Espacio | A |
| **Bomba de medicamento** (granada, cantidad limitada por nivel) | L | B |
| Escudo burbuja (mantener) — levántalo justo antes del golpe para hacer **parry** | Shift izq. / I | Gatillo izq. |
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
│   │   ├── Enemies/          H. pylori, Giardia, Rotavirus, Entamoeba, C. difficile, Oxiuro, S. mutans, Jefe_Lombriz
│   │   ├── Medications/      Antibiótico, Antiviral, ..., Antiparasitario, Mucolítico
│   │   ├── Player/           PlayerConfig (velocidades, salto, daño, escudo...)
│   │   ├── Resources/        GameCatalog (lista de sistemas del mapa)
│   │   └── Systems/          Un folder por sistema con su SystemData y sus LevelData
│   ├── Prefabs/              Player, Enemies, Bosses, Projectiles, Hazards, Level
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

## Cómo funciona (vista lateral)

- La **posición** de un personaje son sus pies. El hijo `Visual` se voltea al mirar a la izquierda.
- El jugador y los enemigos que caminan usan `Rigidbody2D` + `BoxCollider2D` + `CharacterMotor2D` (gravedad propia, salto de altura fija, detección de suelo). Los voladores (virus) y el jefe se mueven por script.
- Capas de física: **Ground** (piso y plataformas), **Player**, **Enemy**, **PlayerProjectile**. Los personajes solo chocan con el piso: entre ellos se atraviesan, como en Metal Slug (`PhysicsLayers.cs`).
- Las **plataformas** son de un solo sentido (`PlatformEffector2D`): se atraviesan saltando desde abajo.
- Caer a un hueco cuesta vida y te regresa al último suelo seguro. Los enemigos que caen se eliminan.
- El combate **no** usa colisiones de física: disparos, cuchillo y bombas buscan `Hurtbox` (rectángulos) con matemática simple (`HitMath`). Por eso agacharse esquiva disparos altos y salta por encima de la embestida de la lombriz.
- Apuntado en 8 direcciones: `AimMath` (probado en `Tests/EditMode/Combat2DTests.cs`).

## Cómo agregar contenido

| Quiero... | Pasos |
|---|---|
| **Cambiar balance** (daño, velocidad, vida) | Edita el asset en `Data/` (funciona incluso en Play Mode) |
| **Un enemigo nuevo** | Crea un `EnemyData` (clic derecho → Create → Healing Knight → Enemigo), duplica un prefab de `Prefabs/Enemies`, asígnale el dato y cambia el arte de `Visual` |
| **Un nivel nuevo** | Duplica una escena de `Scenes/Levels`, crea un `LevelData` con el nombre exacto de la escena, agrégalo a la lista del `SystemData` y a Build Settings |
| **Una arena** | GameObject con `ArenaZone`: `triggerX`, límites, `floorY` y oleadas (prefab + dato + cantidad). La pantalla se cierra hasta acabar las oleadas |
| **Plataformas y huecos** | Plataforma: objeto en capa **Ground** con `BoxCollider2D` (*Used By Effector*) + `PlatformEffector2D`. Hueco: deja un espacio entre dos tramos de piso |
| **Un nivel secreto** | En su `LevelData` marca `hidden` y escribe un `secretId`; coloca un prefab `SecretPassage` con ese mismo id en algún nivel |
| **Un sistema nuevo** | Crea `SystemData` y sus `LevelData`, y agrégalo al `GameCatalog` |
| **Tu arte del personaje** | Importa el PSD, arma las piezas dentro de `Visual` del prefab `HealingKnight` y borra las figuras de prueba. Los scripts de la raíz no cambian |

## Reglas del repo

- `main` = estable. Trabaja en ramas `feature/nombre` y únelas con Pull Request.
- Una persona por escena a la vez.
- Mueve o renombra archivos **desde Unity** (para que su `.meta` viaje con ellos).
- Si usas archivos grandes (PSD, WAV), instala Git LFS una vez: `git lfs install`.
