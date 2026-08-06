using UnityEngine;

// goes on the aircraft prefab
// flies through its route's markers in turn, faces the direction of travel,
// and flashes red when it hits another aircraft or the terrain.
//
// the collider has "Is Trigger" ticked and the Rigidbody is kinematic, so the
// aircraft passes through things instead of bouncing off them, but still
// reports the contact through OnTriggerEnter
public class Aircraft : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float arriveDistance = 0.4f;

    [Header("Collision Response")]
    [SerializeField] private Color collisionColour = Color.red;
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private string terrainTag = "Terrain";

    private AircraftManager route;
    private Renderer aircraftRenderer;
    private Color normalColour;
    private int targetMarkerIndex = 0;
    private bool isFlashing = false;

    private void Awake()
    {
        aircraftRenderer = GetComponent<Renderer>();
    }

    // called by AircraftManager right after it spawns this aircraft
    public void SetRoute(AircraftManager newRoute)
    {
        route = newRoute;
        normalColour = route.RouteColour;
        aircraftRenderer.material.color = normalColour;
    }

    private void Update()
    {
        if (route == null) return;

        // wait rather than chase a marker the user is currently holding
        if (route.IsMarkerHeld(targetMarkerIndex)) return;

        Vector3 targetPosition = route.GetMarkerPosition(targetMarkerIndex);

        // the Time.deltaTime * speed pattern from the lectures - speed is in
        // metres per second, so the flight looks the same at any frame rate
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        TurnTowardsTarget(targetPosition);

        // arrived, so aim at the next marker - the modulo wraps back to the
        // first marker at the end of the route
        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            targetMarkerIndex = (targetMarkerIndex + 1) % route.MarkerCount;
        }
    }

    // bonus challenge: always face the direction of travel.
    // LookRotation builds the rotation pointing down the travel vector, and
    // RotateTowards eases into it so the aircraft turns rather than snapping
    private void TurnTowardsTarget(Vector3 targetPosition)
    {
        Vector3 travelDirection = targetPosition - transform.position;

        if (travelDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(travelDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        bool hitAircraft = other.GetComponent<Aircraft>() != null;
        bool hitTerrain = other.CompareTag(terrainTag);

        if (hitAircraft || hitTerrain)
        {
            FlashCollision();
        }
    }

    // same async pattern as the bead gun's fire rate limit
    private async void FlashCollision()
    {
        // ignore new collisions while already flashing, so overlapping hits
        // don't cut the flash short
        if (isFlashing) return;

        isFlashing = true;
        aircraftRenderer.material.color = collisionColour;

        await Awaitable.WaitForSecondsAsync(flashDuration);

        aircraftRenderer.material.color = normalColour;
        isFlashing = false;
    }
}
