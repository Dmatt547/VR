using UnityEngine;
using UnityEngine.InputSystem;

// goes on an empty object where the patient should appear
//
// not marked, but you'll want it - once the patient is in six pieces there's no way back
// without leaving Play mode. press R for a clean one.
// same spawner pattern as week 2's item spawner and week 3's aircraft spawner
public class PatientSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject patientPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Reset")]
    [SerializeField] private Key resetKey = Key.R;

    private void Start()
    {
        if (spawnOnStart) Respawn();
    }

    private void Update()
    {
        if (Keyboard.current == null || resetKey == Key.None) return;

        if (Keyboard.current[resetKey].wasPressedThisFrame) Respawn();
    }

    // safe to hook to a UI button as well as the keyboard.
    // the ContextMenu also puts a "Respawn" item on the component's ⋮ menu, so you can fire
    // it from the Inspector without entering Play mode - handy for checking the wiring
    [ContextMenu("Respawn")]
    public void Respawn()
    {
        // check the prefab BEFORE clearing. a spawner with nothing to spawn should do
        // nothing at all, rather than emptying the table and leaving you with an empty scene
        if (patientPrefab == null)
        {
            Debug.LogWarning("PatientSpawner on '" + name + "' has no Patient Prefab assigned.", this);
            return;
        }

        // this script belongs on an empty in the scene, not on the patient. left on the
        // prefab, every spawned patient would immediately clear the table and delete itself
        if (GetComponent<CuttableObject>() != null)
        {
            Debug.LogWarning("PatientSpawner is on '" + name + "', which is cuttable. " +
                             "Remove it - it belongs on an empty scene object.", this);
            return;
        }

        ClearTable();

        Transform point = spawnPoint != null ? spawnPoint : transform;
        GameObject patient = Instantiate(patientPrefab, point.position, point.rotation);

        Debug.Log("Spawned '" + patient.name + "' at " + point.position, patient);
    }

    // the halves keep their CuttableObject component, so looking for that finds the
    // patient and every offcut still lying around
    public void ClearTable()
    {
        CuttableObject[] leftovers = FindObjectsByType<CuttableObject>(FindObjectsSortMode.None);

        for (int i = 0; i < leftovers.Length; i++) Destroy(leftovers[i].gameObject);
    }
}
