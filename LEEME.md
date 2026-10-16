# Healing Knight

*Run and gun* 2D de un jugador (estilo **Metal Slug**) hecho en Unity 6. Un nanobot médico recorre los sistemas del cuerpo del Paciente 001 y derrota enfermedades usando el medicamento correcto.

**Entrega de diciembre:** Sistema Digestivo: Estómago → Intestino delgado (la lombriz aparece y huye) → Intestino grueso (pelea final con la lombriz). La **Boca** es un nivel secreto: se desbloquea al tocar un pasaje escondido en el Estómago.

**Siguiente (prototipo listo):** Sistema Respiratorio: Nariz (rinovirus, polen, S. aureus) → Tráquea (tos ferina) → Pulmones (neumonía, influenza y el jefe volador: la **colmena de tuberculosis**; da la fórmula 2 y cierra con el aviso de que la infección va al cerebro). Mecánica del GDD: el paso por los bronquiolos está bloqueado por **mucosidad** (Mucolítico; te frena si la tocas) e **inflamación** (Antiinflamatorio; se contrae al dañarla). El **polen** es un alérgeno: Antihistamínico.

**Prototipo del Sistema Nervioso:** Médula espinal (poliovirus, meningococo, rabia; aprendes las **tormentas eléctricas**: un destello avisa 1 s dónde cae el rayo de 2 u y el escudo no protege) → Cerebro (herpes y los demás entre **neuronas sanas**: tus cápsulas las atraviesan, pero los disparos enemigos las dañan (−3 de **salud del cerebro**) y las infectan. Las neuronas rojas sueltan descargas y desgastan el cerebro hasta que las calmas. Si la salud del cerebro llega a 0, desenlace negativo y se reinicia el nivel) → **Hipocampo** (jefe: el **virus de la rabia** como el ciempiés de Centipede: una cadena de viriones que baja de riel en cada orilla, se parte en dos si rompes el medio, invoca rayos, infecta neuronas y en furia corre a ras de piso; da la fórmula 3). El **Anticonvulsivo** es la cápsula regional: calma neuronas rojas (+5 cerebro) y su bomba deja un campo neuroprotector.

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

## Controles

Apuntas **libre con el mouse** (o el stick derecho del control). Si no usas el mouse, apuntas en 8 direcciones como en Metal Slug: W arriba, W + A/D en diagonal, S abajo en el aire. Mover el mouse cambia al apuntado con mouse; presionar W regresa al de teclas.

| Acción | Teclado / mouse | Gamepad |
|---|---|---|
| Moverse | A / D | Stick izq. / cruceta |
| Apuntar libre (miras hacia donde apuntas) | Mouse | Stick der. |
| Apuntar en 8 direcciones | W, W + A/D, S en el aire | Stick izq. |
| Agacharse | S | Abajo |
| Disparar (mantener = ráfaga). Si un enemigo está pegado, **cuchillazo** automático. Disparar hacia abajo en el aire frena la caída | J / clic izquierdo | X / gatillo der. |
| Escudo burbuja (mantener) — levántalo justo antes del golpe para hacer **parry** | K / clic derecho | Gatillo izq. |
| Saltar (altura fija) · **doble salto** en el aire | Espacio | A / LB |
| **Caída lenta**: mantén saltar mientras caes (1.5 s, se recarga al pisar el suelo) | mantener Espacio | mantener A / LB |
| **Bomba de medicamento** (granada). Empiezas con 5; la barra bajo el contador se llena con daño efectivo, cuchillazos y parry, y al llenarse ganas otra (máx. 10) | L / F | B / RB |
| **Fijar** (mantener): te quedas quieto para apuntar con calma | U / Shift izq. | Clic del stick der. |
| Medicamento anterior / siguiente | Rueda del mouse / Q / E | — / Y |
| Medicamento directo | 1 a 5 | — |
| Pausa | Esc | Start |

Los controles se definen en `Scripts/Gameplay/Player/PlayerInputReader.cs`.

---

## Sonido (experimento)

Cada medicamento tiene su sonido de **disparo** y de **impacto** (campos `fireSound` y `hitSound` de su MedicationData); si la cápsula es ineficaz suena el **rebote** general (`Data/Resources/CombatAudio`, donde también están los volúmenes). Los sonidos de `Audio/Resources/SFX` son de prueba, sintetizados: para cambiar uno, arrastra tu archivo al campo del medicamento. Si el campo está vacío, el juego busca solo en esa carpeta `Disparo_<medicamento>`, `Impacto_<medicamento>` y `Rebote_Ineficaz` (con el nombre del archivo del medicamento, p. ej. `Disparo_Antibiotico`), así que basta con reemplazar el archivo con el mismo nombre. Usa sonidos cortos (menos de 0.2 s), en mono y en `.ogg` (los `.wav` van por Git LFS).

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
- Apuntado: libre con mouse o stick derecho (`AimMath.Toward`) o en 8 direcciones con teclas (`AimMath.Resolve`). Probado en `Tests/EditMode/Combat2DTests.cs`.

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
