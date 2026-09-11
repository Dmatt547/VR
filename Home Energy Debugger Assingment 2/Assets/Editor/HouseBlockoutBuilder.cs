using UnityEditor;
using UnityEngine;

/// <summary>
/// Home Energy Debugger - one-click house blockout.
/// Builds a simple 4-room house (Kitchen, Living Room, Laundry, Garage)
/// out of primitive cubes so movement/locomotion can be tested against real walls
/// before final art passes. Safe to re-run: deletes any previous blockout first.
///
/// Layout (top-down, +X = east, +Z = north), each room is 6m x 6m:
///   Kitchen (NW)      | Living Room (NE)
///   -------------------------------------
///   Laundry (SW)      | Garage (SE)
/// </summary>
public static class HouseBlockoutBuilder
{
    private const float RoomSize = 6f;       // each room is RoomSize x RoomSize
    private const float WallHeight = 3f;
    private const float WallThickness = 0.2f;
    private const float DoorWidth = 1.5f;    // gap left in interior walls for doorways

    [MenuItem("Tools/Home Energy Debugger/Build House Blockout")]
    public static void BuildBlockout()
    {
        // Clear any previous blockout so this is safe to re-run.
        GameObject existing = GameObject.Find("House_Blockout");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject("House_Blockout");
        Undo.RegisterCreatedObjectUndo(root, "Build House Blockout");

        BuildFloor(root.transform);
        BuildOuterWalls(root.transform);
        BuildInteriorWalls(root.transform);
        BuildRoomMarkers(root.transform);

        Selection.activeGameObject = root;
        Debug.Log("House blockout built: 4 rooms (Kitchen, Living Room, Laundry, Garage), each 6m x 6m.");
    }

    private static void BuildFloor(Transform parent)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        floor.transform.localScale = new Vector3(RoomSize * 2f, 0.2f, RoomSize * 2f);
    }

    private static void BuildOuterWalls(Transform parent)
    {
        GameObject outer = new GameObject("Outer_Walls");
        outer.transform.SetParent(parent);

        float half = RoomSize; // house spans -RoomSize..+RoomSize on both axes

        // North and south outer walls (run along X)
        CreateWall(outer.transform, "Wall_North", new Vector3(0f, WallHeight / 2f, half), RoomSize * 2f, WallThickness, true);
        CreateWall(outer.transform, "Wall_South", new Vector3(0f, WallHeight / 2f, -half), RoomSize * 2f, WallThickness, true);

        // East and west outer walls (run along Z)
        CreateWall(outer.transform, "Wall_East", new Vector3(half, WallHeight / 2f, 0f), RoomSize * 2f, WallThickness, false);
        CreateWall(outer.transform, "Wall_West", new Vector3(-half, WallHeight / 2f, 0f), RoomSize * 2f, WallThickness, false);
    }

    private static void BuildInteriorWalls(Transform parent)
    {
        GameObject interior = new GameObject("Interior_Walls");
        interior.transform.SetParent(parent);

        // Vertical divider (x = 0) between Kitchen/Living (north half, z 0..6)
        BuildWallWithDoorway(interior.transform, "Divider_Kitchen_Living", new Vector3(0f, WallHeight / 2f, RoomSize / 2f), RoomSize, false);

        // Vertical divider (x = 0) between Laundry/Garage (south half, z -6..0)
        BuildWallWithDoorway(interior.transform, "Divider_Laundry_Garage", new Vector3(0f, WallHeight / 2f, -RoomSize / 2f), RoomSize, false);

        // Horizontal divider (z = 0) between Kitchen/Laundry (west half, x -6..0)
        BuildWallWithDoorway(interior.transform, "Divider_Kitchen_Laundry", new Vector3(-RoomSize / 2f, WallHeight / 2f, 0f), RoomSize, true);

        // Horizontal divider (z = 0) between Living/Garage (east half, x 0..6)
        BuildWallWithDoorway(interior.transform, "Divider_Living_Garage", new Vector3(RoomSize / 2f, WallHeight / 2f, 0f), RoomSize, true);
    }

    /// <summary>
    /// Builds a wall of given length centered at wallCenter, split into two segments
    /// with a DoorWidth gap in the middle so the player can walk between rooms.
    /// alongX = true means the wall extends along the X axis; otherwise along Z.
    /// </summary>
    private static void BuildWallWithDoorway(Transform parent, string name, Vector3 wallCenter, float fullLength, bool alongX)
    {
        float segmentLength = (fullLength - DoorWidth) / 2f;
        float offset = (segmentLength / 2f) + (DoorWidth / 2f);

        Vector3 dir = alongX ? Vector3.right : Vector3.forward;

        CreateWall(parent, name + "_A", wallCenter - dir * offset, segmentLength, WallThickness, alongX);
        CreateWall(parent, name + "_B", wallCenter + dir * offset, segmentLength, WallThickness, alongX);
    }

    private static void CreateWall(Transform parent, string name, Vector3 center, float length, float thickness, bool alongX)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = center;
        wall.transform.localScale = alongX
            ? new Vector3(length, WallHeight, thickness)
            : new Vector3(thickness, WallHeight, length);
    }

    /// <summary>
    /// Empty transforms marking the center of each room, useful later for
    /// spawning appliances, scan targets, or teleport anchors.
    /// </summary>
    private static void BuildRoomMarkers(Transform parent)
    {
        GameObject markers = new GameObject("Room_Markers");
        markers.transform.SetParent(parent);

        CreateMarker(markers.transform, "Room_Kitchen", new Vector3(-RoomSize / 2f, 0f, RoomSize / 2f));
        CreateMarker(markers.transform, "Room_LivingRoom", new Vector3(RoomSize / 2f, 0f, RoomSize / 2f));
        CreateMarker(markers.transform, "Room_Laundry", new Vector3(-RoomSize / 2f, 0f, -RoomSize / 2f));
        CreateMarker(markers.transform, "Room_Garage", new Vector3(RoomSize / 2f, 0f, -RoomSize / 2f));
    }

    private static void CreateMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
    }
}
