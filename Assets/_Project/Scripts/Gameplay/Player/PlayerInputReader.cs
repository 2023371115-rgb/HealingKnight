using UnityEngine;
using UnityEngine.InputSystem;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Controles estilo Metal Slug (teclado y gamepad). Las acciones se crean aquí en código
    /// para que se vean todos los controles en un solo lugar.
    ///
    ///   Acción                         Teclado            Gamepad
    ///   Mover                          A / D, flechas     Stick izq. / cruceta
    ///   Apuntar arriba / diagonal      W  (+ A o D)       Arriba (+ lado)
    ///   Agacharse / apuntar abajo*     S  (+ A o D)       Abajo (+ lado)      *abajo solo en el aire
    ///   Disparar (mantener)            J                  X (oeste)
    ///   Saltar                         K / Espacio        A (sur)
    ///   Bomba de medicamento           L                  B (este)
    ///   Escudo burbuja (mantener)      Shift izq. / I     Gatillo izq.
    ///   Medicamento anterior/siguiente Q / E              LB / RB
    ///   Medicamento directo            1 a 5              —
    ///   Pausa                          Esc                Start
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap map;
        private InputAction move, jump, shoot, bomb, shield, prevMed, nextMed, pause;
        private InputAction[] directMed;

        /// <summary>Dirección (incluye arriba/abajo para apuntar y agacharse).</summary>
        public Vector2 Move { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool ShootPressed { get; private set; }
        public bool ShootHeld { get; private set; }
        public bool BombPressed { get; private set; }
        public bool ShieldHeld { get; private set; }
        public int MedicationStep { get; private set; }   // -1, 0 o 1
        public int DirectMedicationIndex { get; private set; } = -1;
        public bool PausePressed { get; private set; }

        /// <summary>Para cinemáticas, pausa o muerte: el personaje ignora los controles.</summary>
        public bool Blocked { get; set; }

        private void Awake()
        {
            map = new InputActionMap("Gameplay");

            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            shoot = map.AddAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/j");
            shoot.AddBinding("<Gamepad>/buttonWest");
            shoot.AddBinding("<Gamepad>/rightTrigger");

            jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/k");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            bomb = map.AddAction("Bomb", InputActionType.Button);
            bomb.AddBinding("<Keyboard>/l");
            bomb.AddBinding("<Gamepad>/buttonEast");

            shield = map.AddAction("Shield", InputActionType.Button);
            shield.AddBinding("<Keyboard>/leftShift");
            shield.AddBinding("<Keyboard>/i");
            shield.AddBinding("<Gamepad>/leftTrigger");

            prevMed = map.AddAction("PrevMedication", InputActionType.Button);
            prevMed.AddBinding("<Keyboard>/q");
            prevMed.AddBinding("<Gamepad>/leftShoulder");

            nextMed = map.AddAction("NextMedication", InputActionType.Button);
            nextMed.AddBinding("<Keyboard>/e");
            nextMed.AddBinding("<Gamepad>/rightShoulder");

            pause = map.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            directMed = new InputAction[5];
            for (int i = 0; i < directMed.Length; i++)
            {
                directMed[i] = map.AddAction("Medication" + (i + 1), InputActionType.Button);
                directMed[i].AddBinding("<Keyboard>/" + (i + 1));
            }
        }

        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();
        private void OnDestroy() => map?.Dispose();

        private void Update()
        {
            // La pausa se lee siempre, incluso con el personaje bloqueado.
            PausePressed = pause.WasPressedThisFrame();

            if (Blocked)
            {
                Move = Vector2.zero;
                JumpPressed = ShootPressed = ShootHeld = BombPressed = ShieldHeld = false;
                MedicationStep = 0;
                DirectMedicationIndex = -1;
                return;
            }

            Vector2 m = move.ReadValue<Vector2>();
            Move = m.sqrMagnitude > 1f ? m.normalized : m;
            JumpPressed = jump.WasPressedThisFrame();
            ShootPressed = shoot.WasPressedThisFrame();
            ShootHeld = shoot.IsPressed();
            BombPressed = bomb.WasPressedThisFrame();
            ShieldHeld = shield.IsPressed();

            MedicationStep = 0;
            if (prevMed.WasPressedThisFrame()) MedicationStep = -1;
            else if (nextMed.WasPressedThisFrame()) MedicationStep = 1;

            DirectMedicationIndex = -1;
            for (int i = 0; i < directMed.Length; i++)
            {
                if (directMed[i].WasPressedThisFrame())
                {
                    DirectMedicationIndex = i;
                    break;
                }
            }
        }
    }
}
