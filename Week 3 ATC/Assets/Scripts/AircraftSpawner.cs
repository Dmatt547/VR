using UnityEngine;

// goes on an empty object in the scene - the top of the air traffic system
// generates several Aircraft Manager prefabs, so you end up with multiple
// aircraft each flying their own route across the table top
public class AircraftSpawner : MonoBehaviour
{
    [Header("Routes")]
    [SerializeField] private GameObject aircraftManagerPrefab;
    [SerializeField] private int numberOfRoutes = 4;

    [Header("Placement")]
    [SerializeField] private float areaSize = 2f;
    [SerializeField] private float minHeight = 0.3f;
    [SerializeField] private float maxHeight = 0.9f;

    [Header("Appearance")]
    [SerializeField] private Color[] routeColours =
    {
        Color.cyan,
        Color.magenta,
        Color.green,
        Color.yellow
    };

    private void Start()
    {
        SpawnRoutes();
    }

    private void SpawnRoutes()
    {
        if (aircraftManagerPrefab == null)
        {
            Debug.LogWarning("AircraftSpawner is missing an Aircraft Manager Prefab reference in the Inspector.");
            return;
        }

        for (int i = 0; i < numberOfRoutes; i++)
        {
            Vector3 spawnPosition = GetRandomPosition();

            GameObject routeObject = Instantiate(aircraftManagerPrefab, spawnPosition, Quaternion.identity, transform);
            routeObject.name = "Aircraft Manager " + i;

            AircraftManager route = routeObject.GetComponent<AircraftManager>();
            if (route == null) continue;

            // give each route its own heading and colour, so the paths cross
            // over each other and the aircraft are easy to tell apart
            route.SetRouteDirection(GetRandomDirection());
            route.SetRouteColour(GetRouteColour(i));
        }
    }

    private Vector3 GetRandomPosition()
    {
        float half = areaSize * 0.5f;

        float x = Random.Range(-half, half);
        float z = Random.Range(-half, half);
        float y = Random.Range(minHeight, maxHeight);

        return transform.position + new Vector3(x, y, z);
    }

    // a flat heading on the XZ plane, so routes stay level with the table top
    private Vector3 GetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);

        return rotation * Vector3.forward;
    }

    private Color GetRouteColour(int index)
    {
        if (routeColours == null || routeColours.Length == 0) return Color.cyan;

        return routeColours[index % routeColours.Length];
    }
}
