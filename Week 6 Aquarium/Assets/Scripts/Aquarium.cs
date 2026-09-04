using UnityEngine;

// Goes on the empty "Aquarium" object at the centre of the tank. The glass is
// box colliders in the scene; this only decides where inside the tank a fish is
// allowed to aim.
public class Aquarium : MonoBehaviour
{
    [SerializeField] private Vector3 innerSize = new Vector3(8f, 4f, 8f);
    // targets stay this far off the glass, so a fish doesn't finish its trip
    // nose-first against a wall
    [SerializeField] private float wallMargin = 0.5f;

    public Vector3 RandomPointInside()
    {
        Vector3 half = HalfExtents();

        return transform.position + new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z));
    }

    // a fleeing fish aims away from its predator, which often lands outside the
    // glass, so the point gets pulled back in
    public Vector3 ClampInside(Vector3 position)
    {
        Vector3 half = HalfExtents();
        Vector3 local = position - transform.position;

        return transform.position + new Vector3(
            Mathf.Clamp(local.x, -half.x, half.x),
            Mathf.Clamp(local.y, -half.y, half.y),
            Mathf.Clamp(local.z, -half.z, half.z));
    }

    // a push away from any wall the position is close to: zero out in open
    // water, growing towards 1 per axis as the fish nears the glass. the fish
    // adds this to its steering so it slides along a wall instead of grinding
    // into it
    public Vector3 AwayFromWalls(Vector3 position, float distance)
    {
        Vector3 half = HalfExtents();
        Vector3 local = position - transform.position;

        return new Vector3(
            AxisPush(local.x, half.x, distance),
            AxisPush(local.y, half.y, distance),
            AxisPush(local.z, half.z, distance));
    }

    private static float AxisPush(float local, float half, float distance)
    {
        float room = half - Mathf.Abs(local);

        if (room >= distance) return 0f;

        float strength = 1f - Mathf.Max(0f, room) / distance;

        return local > 0f ? -strength : strength;
    }

    private Vector3 HalfExtents() => innerSize * 0.5f - Vector3.one * wallMargin;

    // shows the swimmable volume when selected, so the tank can be sized
    // against the glass without pressing play
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, innerSize);
    }
}
