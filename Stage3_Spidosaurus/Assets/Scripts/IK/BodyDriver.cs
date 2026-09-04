using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal user-driven body motion. Speed and direction change at any instant, which is what
/// forces the gait planner and the solver to cope with a moving hip (constraint 3 of the
/// Spidosaurus challenge).
///
/// Arrow keys, not WASD: the XR Device Simulator already owns WASD for moving the headset,
/// so sharing those keys would drive the camera and the creature at the same time.
///
/// This project has Active Input Handling set to "Input System Package (New)", so the legacy
/// UnityEngine.Input class is unavailable and Keyboard.current is read directly.
/// </summary>
public class BodyDriver : MonoBehaviour
{
    public float moveSpeed = 0.6f;
    public float turnSpeed = 60f;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float forward = (kb.upArrowKey.isPressed   ? 1f : 0f)
                      - (kb.downArrowKey.isPressed ? 1f : 0f);

        float turn    = (kb.rightArrowKey.isPressed ? 1f : 0f)
                      - (kb.leftArrowKey.isPressed  ? 1f : 0f);

        transform.Translate(Vector3.forward * forward * moveSpeed * Time.deltaTime, Space.Self);

        // Space.World, not the default Space.Self. Turning about the creature's OWN up axis
        // accumulates roll and pitch as soon as the body is not perfectly level, and the
        // creature slowly corkscrews. Yaw belongs to the world, not to the body.
        transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.World);
    }
}
