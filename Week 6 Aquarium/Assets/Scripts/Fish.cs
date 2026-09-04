using UnityEngine;

// Goes on the fish prefab. One fish, built with the state machine pattern:
// SetState is the only place the state changes, each event handler switches on
// the current state, and FixedUpdate runs the actions for the active state.
[RequireComponent(typeof(Rigidbody))]
public class Fish : MonoBehaviour
{
    public enum FishState { NotInit, LookingForFood, ChasingFood, Eating, Fleeing, BeingDigested }

    private const float WallAvoidDistance = 1.2f;   // how close to the glass before steering away
    private const float WallAvoidStrength = 1.5f;   // how hard that steering competes with the target
    private const float EscapeWallBias = 1.5f;      // how hard an escape point bends off the glass
    private const float StuckSpeed = 0.15f;         // below this speed for StuckTime counts as stuck
    private const float StuckTime = 1f;

    [Header("Swimming")]
    [SerializeField] private float swimForce = 2.5f;
    [SerializeField] private float maxSpeed = 0.5f;         // real limit also scales with size
    [SerializeField] private float chaseMultiplier = 1.3f;
    [SerializeField] private float fleeMultiplier = 1.4f;
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float arriveDistance = 0.4f;

    [Header("Senses")]
    [SerializeField] private float senseRadius = 3f;
    [SerializeField] private float scanInterval = 0.25f;
    // how much bigger a fish must be to count as a predator, so two fish of the
    // same size don't each decide they can eat the other
    [SerializeField] private float sizeAdvantage = 1.25f;

    [Header("Feeding")]
    // multiplied by the two sizes added together. the root colliders touch at
    // 0.5 x that, so anything under 0.5 here can never fire
    [SerializeField] private float biteDistance = 0.7f;
    [SerializeField] private float eatingDuration = 2f;
    [SerializeField] private float digestionDuration = 2.5f;

    [Header("Timers")]
    [SerializeField] private float wanderTimeout = 6f;
    [SerializeField] private float fleeTimeout = 4f;
    [SerializeField] private float fleeDistance = 4f;

    private Aquarium aquarium;
    private Rigidbody fishRigidbody;
    private Renderer fishRenderer;
    private Collider fishCollider;

    private FishState state = FishState.NotInit;
    private Vector3 targetPosition;
    private Fish targetFish;                // prey being chased, or predator being fled
    private float nextScanTime;
    private float stuckTimer;
    private float digestStartSize;

    // bumped on every state change, so a timer started in a state the fish has
    // already left cannot fire a transition
    private int stateToken;

    // the scan picks up tank walls as well as fish, so this is sized well above
    // the number of fish in the tank
    private readonly Collider[] neighbours = new Collider[32];

    // size is just the scale, and size decides who eats whom
    public float Size => transform.localScale.x;

    public bool IsActiveSwimmer => state != FishState.NotInit && state != FishState.BeingDigested;

    private void Awake()
    {
        fishRigidbody = GetComponent<Rigidbody>();
        fishRenderer = GetComponentInChildren<Renderer>();
        fishCollider = GetComponent<Collider>();
    }

    // called by FishManager straight after it spawns this fish
    public void Initialise(Aquarium tank, float startingSize)
    {
        aquarium = tank;
        transform.localScale = Vector3.one * startingSize;
    }

    private void Start()
    {
        fishRigidbody.useGravity = false;
        fishRigidbody.linearDamping = 2f;
        fishRigidbody.freezeRotation = true;
        // Unity parks a near-motionless rigidbody, which showed up as a fish
        // wedged on the glass freezing for a second before twitching on again
        fishRigidbody.sleepThreshold = 0f;

        SetState(FishState.LookingForFood);
    }

    // the only place the state changes
    private void SetState(FishState newState)
    {
        if (state == newState) return;

        state = newState;
        stateToken++;
        stuckTimer = 0f;

        HandleStateChangedEvent(newState);
    }

    // entry actions: pick the new state's target, start its timer, recolour
    private void HandleStateChangedEvent(FishState newState)
    {
        switch (newState)
        {
            case FishState.LookingForFood:
                targetFish = null;
                PickWanderTarget();
                StartTimer(wanderTimeout);
                break;

            case FishState.Eating:
                fishRigidbody.linearVelocity = Vector3.zero;
                StartTimer(eatingDuration);
                break;

            case FishState.Fleeing:
                PickEscapeTarget();
                StartTimer(fleeTimeout);
                break;

            case FishState.BeingDigested:
                fishRigidbody.linearVelocity = Vector3.zero;
                fishRigidbody.isKinematic = true;
                fishCollider.enabled = false;
                digestStartSize = Size;
                StartTimer(digestionDuration);
                break;
        }

        fishRenderer.material.color = ColourFor(newState);
    }

    // everything runs in FixedUpdate, because the swimming is done with forces
    private void FixedUpdate()
    {
        if (state == FishState.NotInit) return;

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            ScanForNeighbours();
        }

        switch (state)
        {
            case FishState.LookingForFood:
                SwimTowards(targetPosition, 1f);

                if (HasArrived())
                {
                    PickWanderTarget();
                    StartTimer(wanderTimeout);
                }
                break;

            case FishState.ChasingFood:
                if (targetFish == null || !targetFish.IsActiveSwimmer)
                {
                    SetState(FishState.LookingForFood);
                    break;
                }

                targetPosition = targetFish.transform.position;   // the prey is moving
                SwimTowards(targetPosition, chaseMultiplier);

                if (Vector3.Distance(transform.position, targetPosition) <= biteDistance * (Size + targetFish.Size)) Bite();
                break;

            case FishState.Eating:
                break;                                            // stopped at one place

            case FishState.Fleeing:
                SwimTowards(targetPosition, fleeMultiplier);
                if (HasArrived()) PickEscapeTarget();
                break;

            case FishState.BeingDigested:
                Shrink();
                break;
        }

        CheckIfStuck();
    }

    // a fish pinned against the glass grinds away and gets nowhere, so watch for
    // it. the states with no target ignore the event, so they need no guard here
    private void CheckIfStuck()
    {
        stuckTimer = fishRigidbody.linearVelocity.magnitude > StuckSpeed ? 0f : stuckTimer + Time.fixedDeltaTime;

        if (stuckTimer < StuckTime) return;

        stuckTimer = 0f;
        HandleStuckEvent();
    }

    // a fish big enough to eat this one has come into range
    private void HandlePredatorNearEvent(Fish predator)
    {
        switch (state)
        {
            case FishState.LookingForFood:
            case FishState.ChasingFood:
            case FishState.Eating:
                targetFish = predator;
                SetState(FishState.Fleeing);
                break;

            case FishState.Fleeing:
                targetFish = predator;      // already running, re-aim at the closest threat
                PickEscapeTarget();
                break;
        }
    }

    // a smaller fish has come into range
    private void HandlePreyNearEvent(Fish prey)
    {
        switch (state)
        {
            case FishState.LookingForFood:
                targetFish = prey;
                SetState(FishState.ChasingFood);
                prey.ReceiveThreat(this);
                break;
        }
    }

    // nothing in range worth reacting to
    private void HandleAllClearEvent()
    {
        switch (state)
        {
            case FishState.ChasingFood:     // the prey got away
            case FishState.Fleeing:         // far enough clear
                SetState(FishState.LookingForFood);
                break;
        }
    }

    // the timer the current state started has expired
    private void HandleTimerEvent()
    {
        switch (state)
        {
            case FishState.LookingForFood:
                PickWanderTarget();         // stuck on one target too long
                StartTimer(wanderTimeout);
                break;

            case FishState.Eating:
            case FishState.Fleeing:
                SetState(FishState.LookingForFood);
                break;

            case FishState.BeingDigested:
                Destroy(gameObject);
                break;
        }
    }

    // getting nowhere for a while
    private void HandleStuckEvent()
    {
        switch (state)
        {
            case FishState.LookingForFood:
                PickWanderTarget();         // try somewhere else in the tank
                StartTimer(wanderTimeout);
                break;

            case FishState.ChasingFood:
                SetState(FishState.LookingForFood);     // the prey isn't worth it
                break;

            case FishState.Fleeing:
                PickEscapeTarget();         // try a different way out
                break;
        }
    }

    private void Bite()
    {
        targetFish.ReceiveEaten();
        SetState(FishState.Eating);
    }

    // "I'm about to eat you", so the prey reacts before its own scan would
    public void ReceiveThreat(Fish predator)
    {
        if (IsActiveSwimmer) HandlePredatorNearEvent(predator);
    }

    // "I've eaten you" - the only way into BeingDigested
    public void ReceiveEaten()
    {
        if (IsActiveSwimmer) SetState(FishState.BeingDigested);
    }

    // one query answers both questions: is anything here going to eat me, and is
    // anything here small enough to eat. a predator always wins
    private void ScanForNeighbours()
    {
        if (!IsActiveSwimmer) return;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, senseRadius, neighbours);

        Fish predator = null, prey = null;
        float predatorDistance = float.MaxValue, preyDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Fish other = neighbours[i].GetComponentInParent<Fish>();

            if (other == null || other == this || !other.IsActiveSwimmer) continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);

            if (other.Size >= Size * sizeAdvantage && distance < predatorDistance)
            {
                predator = other;
                predatorDistance = distance;
            }
            else if (other.Size * sizeAdvantage <= Size && distance < preyDistance)
            {
                prey = other;
                preyDistance = distance;
            }
        }

        if (predator != null) HandlePredatorNearEvent(predator);
        else if (prey != null) HandlePreyNearEvent(prey);
        else HandleAllClearEvent();
    }

    private void SwimTowards(Vector3 position, float speedMultiplier)
    {
        Vector3 direction = position - transform.position;

        if (direction.sqrMagnitude < 0.0001f) return;

        // blend in a push off the glass, so a fish crossing the tank slides along
        // a wall in its way instead of pressing into it
        direction = direction.normalized + aquarium.AwayFromWalls(transform.position, WallAvoidDistance) * WallAvoidStrength;

        if (direction.sqrMagnitude < 0.0001f) return;

        fishRigidbody.AddForce(direction.normalized * swimForce * speedMultiplier, ForceMode.Acceleration);

        // bigger fish swim faster, which is what lets a predator run its prey down
        float speedLimit = maxSpeed * (0.5f + Size) * speedMultiplier;

        if (fishRigidbody.linearVelocity.magnitude > speedLimit)
        {
            fishRigidbody.linearVelocity = fishRigidbody.linearVelocity.normalized * speedLimit;
        }

        FaceDirectionOfTravel();
    }

    // face the way it is going, same as the aircraft in week 3
    private void FaceDirectionOfTravel()
    {
        Vector3 velocity = fishRigidbody.linearVelocity;

        if (velocity.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), turnSpeed * Time.fixedDeltaTime);
    }

    private bool HasArrived() => Vector3.Distance(transform.position, targetPosition) <= arriveDistance;

    private void PickWanderTarget() => targetPosition = aquarium.RandomPointInside();

    // away from the predator, but bent off the glass by the same wall push used
    // for steering - straight away would clamp onto a wall and corner the fish
    private void PickEscapeTarget()
    {
        Vector3 away = targetFish != null ? transform.position - targetFish.transform.position : Random.insideUnitSphere;

        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitSphere;

        Vector3 direction = away.normalized + aquarium.AwayFromWalls(transform.position, WallAvoidDistance) * EscapeWallBias;

        if (direction.sqrMagnitude < 0.0001f) direction = away;

        targetPosition = aquarium.ClampInside(transform.position + direction.normalized * fleeDistance);
    }

    private void StartTimer(float duration)
    {
        stateToken++;
        RunTimer(duration, stateToken);
    }

    private async void RunTimer(float duration, int token)
    {
        await Awaitable.WaitForSecondsAsync(duration);

        // the fish may have been destroyed, or left the state that set this
        if (this == null || token != stateToken) return;

        HandleTimerEvent();
    }

    // the visible part of being digested; the timer destroys what is left
    private void Shrink()
    {
        float rate = digestStartSize / digestionDuration;

        transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, rate * Time.fixedDeltaTime);
    }

    // colour makes the state readable from outside the tank while testing
    private Color ColourFor(FishState fishState)
    {
        switch (fishState)
        {
            case FishState.ChasingFood: return new Color(1f, 0.55f, 0.1f);
            case FishState.Eating: return new Color(0.2f, 0.9f, 0.3f);
            case FishState.Fleeing: return new Color(1f, 0.9f, 0.2f);
            case FishState.BeingDigested: return new Color(0.5f, 0.15f, 0.15f);
            default: return new Color(0.35f, 0.65f, 1f);
        }
    }
}
