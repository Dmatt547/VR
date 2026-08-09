using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

// goes on every element sphere prefab (Flame, Ice, Stone, and so on)
// handles the audio and haptic side of the element's representation:
//   - a looping ambient sound while it sits on the shelf
//   - a one-shot sound and a controller vibration when it's picked up
//
// same event handler pattern as the flight markers in week 3 - the grab
// interactable raises selectEntered/selectExited, this script just responds
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class ElementSphere : MonoBehaviour
{
    [Header("Element")]
    [SerializeField] private string elementName = "Flame";

    [Header("Audio")]
    [SerializeField] private AudioClip ambientLoop;
    [SerializeField] private AudioClip grabClip;
    [SerializeField] private float ambientVolume = 0.35f;
    [SerializeField] private float grabVolume = 0.8f;

    [Header("Haptics")]
    [SerializeField] private float grabAmplitude = 0.4f;
    [SerializeField] private float grabDuration = 0.15f;

    [Header("Physics")]
    // 0 = falls normally, 1 = floats. steam and smoke use values near 1 so they
    // drift upwards, stone stays at 0 so it drops like a rock
    [SerializeField] private float buoyancy = 0f;

    private XRGrabInteractable grabInteractable;
    private AudioSource elementAudio;
    private Rigidbody sphereRigidbody;

    // read by the combiner in stage 2, so it knows what this sphere is
    public string ElementName => elementName;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        elementAudio = GetComponent<AudioSource>();
        sphereRigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        PlayAmbientLoop();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    // set the audio source up in code so all 16 prefabs are guaranteed to be
    // configured the same way
    private void PlayAmbientLoop()
    {
        if (ambientLoop == null) return;

        elementAudio.clip = ambientLoop;
        elementAudio.loop = true;
        elementAudio.volume = ambientVolume;

        // spatial blend 1 means fully 3D, so the sound gets quieter as you walk
        // away from the shelf instead of playing at full volume everywhere
        elementAudio.spatialBlend = 1f;
        elementAudio.Play();
    }

    // applying an upwards force each physics step cancels out part of gravity.
    // done in FixedUpdate rather than Update because it's a physics change
    private void FixedUpdate()
    {
        if (buoyancy == 0f) return;

        sphereRigidbody.AddForce(-Physics.gravity * buoyancy * sphereRigidbody.mass);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (grabClip != null)
        {
            elementAudio.PlayOneShot(grabClip, grabVolume);
        }

        SendHaptics(args);
    }

    // the haptic player lives on the controller that grabbed us, not on this
    // sphere, so we look it up from the interactor in the event arguments
    private void SendHaptics(SelectEnterEventArgs args)
    {
        if (grabAmplitude <= 0f) return;

        HapticImpulsePlayer hapticPlayer = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();

        if (hapticPlayer == null) return;

        // frequency of 0 tells the device to use its own default
        hapticPlayer.SendHapticImpulse(grabAmplitude, grabDuration, 0f);
    }
}
