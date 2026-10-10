using UnityEngine;
using UnityEngine.InputSystem;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Lee los controles con el Input System (teclado y gamepad).
    /// Las acciones se crean aquí en código para que se vean todos los controles en un solo lugar.
    ///
    ///   Acción              Teclado              Gamepad
    ///   Mover               WASD / flechas        Stick izq. / cruceta
    ///   Saltar              Espacio               A (sur)
    ///   Disparar (mantener) J                     X (oeste)
    ///   Golpe cercano       L                     Y (norte)
    ///   Escudo (mantener)   K                     B (este) / gatillo izq.
    ///   Medicamento ant/sig Q / E                 LB / RB
    ///   Medicamento directo 1 a 5                 —
    ///   Pausa               Esc                   Start
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap map;
        private InputAction move, jump, shoot, melee, shield, prevMed, nextMed, pause;
        private InputAction[] directMed;

        public Vector2 Move { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool ShootHeld { get; private set; }
        public bool MeleePressed { get; private set; }
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

            jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            shoot = map.AddAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/j");
            shoot.AddBinding("<Gamepad>/buttonWest");
            shoot.AddBinding("<Gamepad>/rightTrigger");

            melee = map.AddAction("Melee", InputActionType.Button);
            melee.AddBinding("<Keyboard>/l");
            melee.AddBinding("<Gamepad>/buttonNorth");

            shield = map.AddAction("Shield", InputActionType.Button);
            shield.AddBinding("<Keyboard>/k");
            shield.AddBinding("<Gamepad>/buttonEast");
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
                JumpPressed = ShootHeld = MeleePressed = ShieldHeld = false;
                MedicationStep = 0;
                DirectMedicationIndex = -1;
                return;
            }

            Vector2 m = move.ReadValue<Vector2>();
            Move = m.sqrMagnitude > 1f ? m.normalized : m;
            JumpPressed = jump.WasPressedThisFrame();
            ShootHeld = shoot.IsPressed();
            MeleePressed = melee.WasPressedThisFrame();
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
