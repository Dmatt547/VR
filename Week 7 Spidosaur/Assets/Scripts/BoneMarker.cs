using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on the Marker Template prefab. One grabbable handle per bone.
[RequireComponent(typeof(XRGrabInteractable))]
public class BoneMarker : MonoBehaviour
{
    [SerializeField] private float mass = 1f;

    // drag term, F = -c * v. without it explicit Euler gains energy and never settles
    [SerializeField] private float damping = 3f;

    private readonly List<BoneSpring> springs = new List<BoneSpring>();

    private Vector3 velocity = Vector3.zero;
    private Vector3 position;

    private XRGrabInteractable grabInteractable;
    private bool isHeld;
    private bool isAnchor;

    public bool IsHeld => isHeld;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        position = transform.position;

        // XRI cannot throw a kinematic Rigidbody and warns on every release
        grabInteractable.throwOnDetach = false;
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args) => isHeld = true;

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        velocity = Vector3.zero;
    }

    public void Initialise(bool anchored)
    {
        isAnchor = anchored;
        position = transform.position;
    }

    public void AddSpring(BoneSpring spring) => springs.Add(spring);

    public void DoDynamics(float deltaTime)
    {
        // held or pinned: the hand is the authority, not the springs
        if (isHeld || isAnchor)
        {
            position = transform.position;
            velocity = Vector3.zero;
            return;
        }

        Vector3 force = Vector3.zero;

        foreach (BoneSpring spring in springs)
        {
            force += spring.GetForce(transform);
        }

        force += -damping * velocity;

        Vector3 a = force / mass;

        velocity += deltaTime * a;
        position += deltaTime * velocity;

        transform.position = position;
    }
}
