using System.Collections;
using Oculus.Interaction.Locomotion;
using UnityEngine;

namespace VRClassroom
{
    [DisallowMultipleComponent]
    public sealed class ClassroomLocomotionControls : MonoBehaviour
    {
        [SerializeField] private MovingSetting movingSetting;
        [SerializeField] private TurningSetting turningSetting;

        private bool ready;

        private IEnumerator Start()
        {
            if (movingSetting == null || turningSetting == null)
            {
                Debug.LogError("Назначьте MovingSetting и TurningSetting из Meta rig.", this);
                enabled = false;
                yield break;
            }

            // Meta settings подписываются на изменения в Start своих компонентов.
            yield return null;
            turningSetting.ControllerTurn.Value = TurningSetting.RotationStyle.Snap;
            turningSetting.RotationSnapAngle.Value = 30f;
            movingSetting.ControllerMovement.Value = MovingSetting.MovementStyle.Teleport;
            ready = true;
        }

        private void Update()
        {
            if (!ready || !OVRInput.GetDown(OVRInput.RawButton.B))
                return;

            // Переключаемся при отпущенных стиках, чтобы новое движение не началось сразу.
            Vector2 leftStick = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
            Vector2 rightStick = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            if (leftStick.sqrMagnitude > 0.04f || rightStick.sqrMagnitude > 0.04f)
                return;

            if (movingSetting.ControllerMovement.Value == MovingSetting.MovementStyle.Teleport)
                UseSmoothMovement();
            else
                UseTeleport();
        }

        public void UseTeleport()
        {
            if (ready)
                movingSetting.ControllerMovement.Value = MovingSetting.MovementStyle.Teleport;
        }

        public void UseSmoothMovement()
        {
            if (ready)
                movingSetting.ControllerMovement.Value = MovingSetting.MovementStyle.Slide;
        }
    }
}
