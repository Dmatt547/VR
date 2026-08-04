using System.Collections.Generic;
using UnityEngine;

// goes on the Aircraft Manager object (make this a prefab)
// builds one route: a sequence of grabbable markers in a straight line,
// then spawns a single aircraft that flies through them in turn
public class AircraftManager : MonoBehaviour
{
    [Header("Markers")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private int numberOfMarkers = 5;
    [SerializeField] private float markerSpacing = 0.6f;
    [SerializeField] private Vector3 routeDirection = Vector3.forward;

    [Header("Aircraft")]
    [SerializeField] private GameObject aircraftPrefab;
    [SerializeField] private Color routeColour = Color.cyan;

    [Header("Path Display")]
    [SerializeField] private LineRenderer pathLine;

    private readonly List<FlightMarker> markers = new List<FlightMarker>();
    private Aircraft aircraft;

    public int MarkerCount => markers.Count;
    public Color RouteColour => routeColour;

    private void Start()
    {
        BuildRoute();
        SpawnAircraft();
    }

    // the markers are grabbable, so the path can move every frame - redraw it
    private void Update()
    {
        DrawPath();
    }

    // arrange the markers in a straight line, centred on this object.
    // a straight line is just the starting configuration - the user drags the
    // markers around in VR to sketch the actual flight path
    private void BuildRoute()
    {
        if (markerPrefab == null)
        {
            Debug.LogWarning("AircraftManager is missing a Marker Prefab reference in the Inspector.");
            return;
        }

        Vector3 direction = routeDirection.normalized;
        float lineLength = (numberOfMarkers - 1) * markerSpacing;
        Vector3 lineStart = transform.position - (direction * lineLength * 0.5f);

        for (int i = 0; i < numberOfMarkers; i++)
        {
            Vector3 spawnPosition = lineStart + (direction * markerSpacing * i);

            GameObject markerObject = Instantiate(markerPrefab, spawnPosition, Quaternion.identity, transform);
            markerObject.name = "Flight Marker " + i;

            FlightMarker marker = markerObject.GetComponent<FlightMarker>();
            if (marker != null)
            {
                markers.Add(marker);
            }
        }
    }

    private void SpawnAircraft()
    {
        if (aircraftPrefab == null)
        {
            Debug.LogWarning("AircraftManager is missing an Aircraft Prefab reference in the Inspector.");
            return;
        }

        if (markers.Count == 0) return;

        GameObject aircraftObject = Instantiate(aircraftPrefab, markers[0].transform.position, Quaternion.identity, transform);
        aircraftObject.name = "Aircraft";

        aircraft = aircraftObject.GetComponent<Aircraft>();
        if (aircraft != null)
        {
            // hand the aircraft its route so it knows which markers to follow
            aircraft.SetRoute(this);
        }
    }

    // called by the aircraft each time it needs its next target
    public Vector3 GetMarkerPosition(int index)
    {
        if (index < 0 || index >= markers.Count) return transform.position;

        return markers[index].transform.position;
    }

    public bool IsMarkerHeld(int index)
    {
        if (index < 0 || index >= markers.Count) return false;

        return markers[index].IsHeld;
    }

    // draws the sketched path so the user can see the route they've made
    private void DrawPath()
    {
        if (pathLine == null || markers.Count == 0) return;

        pathLine.positionCount = markers.Count;

        for (int i = 0; i < markers.Count; i++)
        {
            pathLine.SetPosition(i, markers[i].transform.position);
        }
    }

    // called by AircraftSpawner so each route gets its own colour
    public void SetRouteColour(Color colour)
    {
        routeColour = colour;

        if (pathLine != null)
        {
            pathLine.startColor = colour;
            pathLine.endColor = colour;
        }
    }

    public void SetRouteDirection(Vector3 direction)
    {
        routeDirection = direction;
    }
}
