using UnityEditor;
using UnityEngine;

/// <summary>
/// Home Energy Debugger - one-click collider setup for the imported house model.
/// Imported models (glTF/OBJ) never come with physics colliders attached, so this
/// walks the Structure, Floors, Doors and DoorFrames groups under the house instance
/// in the scene and adds a Mesh Collider to anything that doesn't already have one.
/// Roof is skipped on purpose (currently hidden, not needed for ground-level movement).
/// Safe to re-run: objects that already have a collider are left alone.
/// </summary>
public static class HouseColliderSetup
{
    private static readonly string[] GroupsToCollide = { "Structure", "Floors", "Doors", "DoorFrames" };

    [MenuItem("Tools/Home Energy Debugger/Add Colliders To House")]
    public static void AddColliders()
    {
        GameObject house = Selection.activeGameObject;
        if (house == null || house.transform.Find("Structure") == null)
        {
            house = GameObject.Find("home-energy-debugger-house");
        }

        if (house == null)
        {
            Debug.LogError("Could not find the house in the scene. Select the 'home-energy-debugger-house' " +
                            "root object in the Hierarchy first, then run this again.");
            return;
        }

        int added = 0;
        int skippedExisting = 0;

        foreach (string groupName in GroupsToCollide)
        {
            Transform group = house.transform.Find(groupName);
            if (group == null)
            {
                Debug.LogWarning($"Group '{groupName}' not found under {house.name}, skipping.");
                continue;
            }

            foreach (Transform child in group)
            {
                if (child.GetComponent<MeshFilter>() == null)
                {
                    continue; // not a mesh, nothing to collide against
                }

                if (child.GetComponent<Collider>() != null)
                {
                    skippedExisting++;
                    continue; // already has a collider, leave it as-is
                }

                MeshCollider collider = Undo.AddComponent<MeshCollider>(child.gameObject);
                collider.convex = false; // static level geometry, non-convex is accurate and fine
                added++;
            }
        }

        Debug.Log($"House collider setup done: added {added} Mesh Collider(s), " +
                  $"{skippedExisting} already had one. Roof was skipped intentionally.");
    }
}
