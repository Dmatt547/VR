using System.Collections.Generic;
using UnityEngine;

// goes on the Aircraft Manager prefab
// builds one route: a line of grabbable markers, plus an aircraft to fly it
public class AircraftManager : MonoBehaviour
{
    [Header("Markers")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private int numberOfMarkers = 5;
    [SerializeField] private float markerSpacing = 3f;
    [SerializeField] private Vector3 routeDirection = Vector3.forward;

    [Header("Aircraft")]
    [SerializeField] private GameObject aircraftPrefab;
    [SerializeField] private Color routeColour = Color.cyan;

    private readonly List<FlightMarker> markers = new List<FlightMarker>();

    public int MarkerCount => markers.Count;
    public Color RouteColour => routeColour;

    private void Start()
    {
        BuildRoute();
        SpawnAircraft();
    }

    // arrange the markers in a straight line centred on this object. that's
    // only the starting layout - the user drags them around in VR to sketch
    // the actual flight path
    private void BuildRoute()
    {
        Vector3 direction = routeDirection.normalized;
        float lineLength = (numberOfMarkers - 1) * markerSpacing;
        Vector3 lineStart = transform.position - direction * lineLength * 0.5f;

        for (int i = 0; i < numberOfMarkers; i++)
        {
            Vector3 spawnPosition = lineStart + direction * markerSpacing * i;

            GameObject markerObject = Instantiate(markerPrefab, spawnPosition, Quaternion.identity, transform);
            markers.Add(markerObject.GetComponent<FlightMarker>());
        }
    }

    private void SpawnAircraft()
    {
        GameObject aircraftObject = Instantiate(aircraftPrefab, markers[0].transform.position, Quaternion.identity, transform);

        // hand the aircraft its route so it knows which markers to follow
        aircraftObject.GetComponent<Aircraft>().SetRoute(this);
    }

    // the aircraft asks for these every frame rather than caching them, because
    // the markers are grabbable and move while the user is dragging them
    public Vector3 GetMarkerPosition(int index) => markers[index].transform.position;

    public bool IsMarkerHeld(int index) => markers[index].IsHeld;

    // called by AircraftSpawner so each route gets its own heading and colour
    public void SetRouteDirection(Vector3 direction) => routeDirection = direction;

    public void SetRouteColour(Color colour) => routeColour = colour;
}
