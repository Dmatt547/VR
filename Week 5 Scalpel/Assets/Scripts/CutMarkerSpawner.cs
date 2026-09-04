using System.Collections.Generic;
using UnityEngine;

// Goes on an empty object in the scene.
// Drops a marker on each point where the cutting plane crossed an edge of the mesh.
// If they trace a clean ring, the plane and the mesh are in the same coordinate system -
// which is the quickest way to check the intersection maths.
public class CutMarkerSpawner : MonoBehaviour
{
    [Header("Marker")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private float markerScale = 0.012f;
    [SerializeField] private float markerLifetime = 4f;

    // smallest gap between two markers, in metres. a cut produces a couple of hundred
    // points on a sphere, and each is found twice, so raw they merge into a solid tube
    [SerializeField] private float minimumSpacing = 0.025f;

    [SerializeField] private int maxMarkers = 100;

    // turn off once the cutting works - this is a debugging aid, not a feature
    [SerializeField] private bool showMarkers = true;

    // where markers have already gone this cut, for the spacing check
    private readonly List<Vector3> placed = new List<Vector3>();

    // called by ScalpelTool with world-space points
    public void ShowPoints(List<Vector3> worldPoints)
    {
        if (!showMarkers || worldPoints == null) return;

        if (markerPrefab == null)
        {
            Debug.LogWarning("CutMarkerSpawner is missing a Marker Prefab reference in the Inspector.");
            return;
        }

        placed.Clear();

        for (int i = 0; i < worldPoints.Count; i++)
        {
            if (placed.Count >= maxMarkers) return;
            if (IsTooClose(worldPoints[i])) continue;

            placed.Add(worldPoints[i]);
            SpawnMarker(worldPoints[i]);
        }
    }

    // squared distances, so there's no square root per check
    private bool IsTooClose(Vector3 point)
    {
        float minimumSquared = minimumSpacing * minimumSpacing;

        for (int i = 0; i < placed.Count; i++)
        {
            if ((placed[i] - point).sqrMagnitude < minimumSquared) return true;
        }

        return false;
    }

    private void SpawnMarker(Vector3 position)
    {
        GameObject marker = Instantiate(markerPrefab, position, Quaternion.identity, transform);

        marker.transform.localScale = new Vector3(markerScale, markerScale, markerScale);

        // clean up after the lifetime, or they build up across cuts
        Destroy(marker, markerLifetime);
    }

    // hook to a UI toggle while testing
    public void SetMarkersVisible(bool visible) => showMarkers = visible;
}
