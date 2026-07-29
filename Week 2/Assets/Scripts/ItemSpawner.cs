using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject itemPrefab;
    public int numberOfItems = 10;
    public float spawnInterval = 1f;

    void Start()
    {
        SpawnItems();
    }

    private async void SpawnItems()
    {
        for (int i = 0; i < numberOfItems; i++)
        {
            Instantiate(itemPrefab, transform.position, transform.rotation);
            await Awaitable.WaitForSecondsAsync(spawnInterval);
        }
    }
}