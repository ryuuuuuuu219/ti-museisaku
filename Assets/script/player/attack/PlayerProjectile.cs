using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public sealed class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private int terrainParticleCount = 36;
    [SerializeField] private float terrainParticleLifetime = 0.45f;
    [SerializeField] private float terrainParticleSpeed = 2.8f;

    private void Awake()
    {
        ConfigureBody();
        ConfigureVisual();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        enemy01 enemy = collision.collider.GetComponentInParent<enemy01>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (IsTerrainCollision(collision.collider))
        {
            Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            SpawnTerrainParticles(hitPoint);
            Destroy(gameObject);
        }
    }

    private void ConfigureBody()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = 0.3f;
        sphereCollider.isTrigger = false;

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void ConfigureVisual()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.6f;

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            renderer = visual.GetComponent<Renderer>();
        }

        renderer.material.color = Color.white;
    }

    private static bool IsTerrainCollision(Collider collider)
    {
        return collider is TerrainCollider
            || collider.GetComponentInParent<Terrain>() != null
            || collider.name.Contains("Terrain");
    }

    private void SpawnTerrainParticles(Vector3 position)
    {
        GameObject effect = new GameObject("White Terrain Hit Particles");
        effect.transform.position = position;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startColor = Color.white;
        main.startLifetime = terrainParticleLifetime;
        main.startSpeed = terrainParticleSpeed;
        main.startSize = 0.08f;
        main.loop = false;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        particles.Emit(terrainParticleCount);
        Destroy(effect, terrainParticleLifetime + 0.2f);
    }
}
