using UnityEngine;

// Goes on an empty "Fish Manager" object. Stocks the tank at startup. Sizes are
// spread evenly rather than randomised, so there are always predators and prey.
public class FishManager : MonoBehaviour
{
    [SerializeField] private Aquarium aquarium;
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private int numberOfFish = 12;
    [SerializeField] private float smallestSize = 0.3f;
    [SerializeField] private float largestSize = 0.9f;

    private void Start()
    {
        for (int i = 0; i < numberOfFish; i++)
        {
            float size = numberOfFish > 1
                ? Mathf.Lerp(smallestSize, largestSize, (float)i / (numberOfFish - 1))
                : largestSize;

            GameObject fishObject = Instantiate(fishPrefab, aquarium.RandomPointInside(), RandomHeading(), transform);
            fishObject.name = $"Fish {i + 1}";

            // hand the fish its tank and size, the same way the aircraft was
            // handed its route in week 3
            fishObject.GetComponent<Fish>().Initialise(aquarium, size);
        }
    }

    // a flat heading, so the fish start level rather than nose-down
    private Quaternion RandomHeading() => Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
}
