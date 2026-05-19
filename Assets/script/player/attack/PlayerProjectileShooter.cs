using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerProjectileShooter : MonoBehaviour
{
    [SerializeField] private PlayerProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 15f;

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Fire();
        }
    }

    private void Fire()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        PlayerProjectile projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, origin.position, origin.rotation)
            : CreateDefaultProjectile(origin.position, origin.rotation);

        Rigidbody rigidbody = projectile.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = origin.forward * projectileSpeed;
    }

    private static PlayerProjectile CreateDefaultProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject projectileObject = new GameObject("Player Projectile");
        projectileObject.transform.SetPositionAndRotation(position, rotation);
        projectileObject.AddComponent<Rigidbody>();
        projectileObject.AddComponent<SphereCollider>();
        return projectileObject.AddComponent<PlayerProjectile>();
    }
}
