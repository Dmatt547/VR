using UnityEngine;

// goes on the aircraft prefab
// flies through its route's markers in turn, turns to face where it's going,
// and flashes red when it hits another aircraft or the terrain.
//
// the collider on this prefab has "Is Trigger" ticked and the Rigidbody is
// kinematic, so the aircraft passes straight through things (no physics
// bounce) but still reports the collision through OnTriggerEnter.
public class Aircraft : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField] private bool loopRoute = true;

    [Header("Appearance")]
    [SerializeField] private Renderer aircraftRenderer;
    [SerializeField] private Color normalColour = Color.white;

    [Header("Collision Response")]
    [SerializeField] private Color collisionColour = Color.red;
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private string terrainTag = "Terrain";

    private AircraftManager route;
    private int targetMarkerIndex = 0;
    private bool isFlashing = false;

    private void Awake()
    {
        if (aircraftRenderer == null)
        {
            aircraftRenderer = GetComponentInChildren<Renderer>();
        }
    }

    // called by AircraftManager right after it spawns this aircraft
    public void SetRoute(AircraftManager newRoute)
    {
        route = newRoute;
        targetMarkerIndex = 0;

        if (route != null)
        {
            normalColour = route.RouteColour;
        }

        SetColour(normalColour);
    }

    private void Update()
    {
        if (route == null || route.MarkerCount == 0) return;

        // wait rather than chase a marker the user is currently holding
        if (route.IsMarkerHeld(targetMarkerIndex)) return;

        Vector3 targetPosition = route.GetMarkerPosition(targetMarkerIndex);

        MoveTowardsTarget(targetPosition);
        TurnTowardsTarget(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            AdvanceToNextMarker();
        }
    }

    // the Time.deltaTime * speed pattern from the lectures - speed is in
    // metres per second, so the aircraft flies at the same rate regardless
    // of frame rate
    private void MoveTowardsTarget(Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    // bonus challenge: always face the direction of travel.
    // LookRotation builds the quaternion pointing down the travel vector, and
    // RotateTowards eases into it so the aircraft banks around rather than
    // snapping to the new heading
    private void TurnTowardsTarget(Vector3 targetPosition)
    {
        Vector3 travelDirection = targetPosition - transform.position;

        if (travelDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(travelDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void AdvanceToNextMarker()
    {
        targetMarkerIndex++;

        if (targetMarkerIndex >= route.MarkerCount)
        {
            if (loopRoute)
            {
                targetMarkerIndex = 0;
            }
            else
            {
                targetMarkerIndex = route.MarkerCount - 1;
            }
        }
    }

    // triggers instead of collisions, so two aircraft can occupy the same
    // space and simply report the conflict rather than knocking each other
    // off their routes
    private void OnTriggerEnter(Collider other)
    {
        Aircraft otherAircraft = other.GetComponentInParent<Aircraft>();

        if (otherAircraft != null && otherAircraft != this)
        {
            FlashCollision();
            return;
        }

        if (other.CompareTag(terrainTag))
        {
            FlashCollision();
        }
    }

    // same async pattern as the bead gun's fire rate limit
    private async void FlashCollision()
    {
        if (isFlashing) return;

        isFlashing = true;
        SetColour(collisionColour);

        await Awaitable.WaitForSecondsAsync(flashDuration);

        // the aircraft may have been destroyed while we were waiting
        if (this == null) return;

        SetColour(normalColour);
        isFlashing = false;
    }

    private void SetColour(Color colour)
    {
        if (aircraftRenderer != null)
        {
            aircraftRenderer.material.color = colour;
        }
    }
}
