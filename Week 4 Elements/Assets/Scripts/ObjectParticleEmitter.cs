using UnityEngine;

// goes on an empty child object of the Sand and Ash spheres
//
// activity (h): a particle system where the particles are real GameObjects
// with their own rigidbody and collider. unity's built in particle system is
// cheaper, but its particles can't collide properly with the shelf or be
// picked up - these ones can, so the grains actually fall and pile up.
//
// same async spawn loop as the item spawner from week 2
public class ObjectParticleEmitter : MonoBehaviour
{
    [Header("Particle")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float spawnInterval = 0.15f;
    [SerializeField] private float particleLifetime = 4f;

    [Header("Launch")]
    [SerializeField] private Vector3 emitDirection = Vector3.down;
    [SerializeField] private float emitSpeed = 0.3f;
    [SerializeField] private float spreadAngle = 30f;

    [Header("Size")]
    [SerializeField] private float minScale = 0.008f;
    [SerializeField] private float maxScale = 0.02f;

    private bool isEmitting = true;

    private void Start()
    {
        EmitParticles();
    }

    private void OnDisable()
    {
        isEmitting = false;
    }

    private async void EmitParticles()
    {
        while (isEmitting)
        {
            SpawnParticle();

            await Awaitable.WaitForSecondsAsync(spawnInterval);

            // the sphere may have been destroyed while we were waiting, so
            // check before looping round again
            if (this == null) return;
        }
    }

    private void SpawnParticle()
    {
        if (particlePrefab == null) return;

        GameObject particle = Instantiate(particlePrefab, transform.position, Random.rotation);

        float scale = Random.Range(minScale, maxScale);
        particle.transform.localScale = new Vector3(scale, scale, scale);

        Rigidbody particleRigidbody = particle.GetComponent<Rigidbody>();

        if (particleRigidbody != null)
        {
            particleRigidbody.AddForce(GetLaunchDirection() * emitSpeed, ForceMode.VelocityChange);
        }

        // clean the grain up after its lifetime, otherwise they build up
        // forever and the frame rate drops
        Destroy(particle, particleLifetime);
    }

    // start from the emitter's own facing direction, then randomly tilt it a
    // few degrees so the grains spray out in a cone instead of a straight line
    private Vector3 GetLaunchDirection()
    {
        Vector3 direction = transform.TransformDirection(emitDirection.normalized);

        Quaternion spread = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f);

        return spread * direction;
    }
}
