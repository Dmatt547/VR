using UnityEngine;

// goes on an empty object in the scene - the top of the air traffic system
// generates several aircraft manager prefabs, so you end up with multiple
// aircraft each flying their own route
public class AircraftSpawner : MonoBehaviour
{
    [Header("Routes")]
    [SerializeField] private GameObject aircraftManagerPrefab;
    [SerializeField] private int numberOfRoutes = 4;

    [Header("Placement")]
    [SerializeField] private float areaSize = 12f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 8f;

    [Header("Appearance")]
    [SerializeField] private Color[] routeColours = { Color.cyan, Color.magenta, Color.green, Color.yellow };

    private void Start()
    {
        for (int i = 0; i < numberOfRoutes; i++)
        {
            GameObject routeObject = Instantiate(aircraftManagerPrefab, GetRandomPosition(), Quaternion.identity, transform);
            AircraftManager route = routeObject.GetComponent<AircraftManager>();

            // give each route its own heading and colour, so the paths cross
            // over each other and the aircraft are easy to tell apart
            route.SetRouteDirection(GetRandomDirection());
            route.SetRouteColour(routeColours[i % routeColours.Length]);
        }
    }

    private Vector3 GetRandomPosition()
    {
        float half = areaSize * 0.5f;

        return transform.position + new Vector3(
            Random.Range(-half, half),
            Random.Range(minHeight, maxHeight),
            Random.Range(-half, half));
    }

    // a flat heading on the XZ plane, so routes stay level with the ground
    private Vector3 GetRandomDirection()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
    }
}
