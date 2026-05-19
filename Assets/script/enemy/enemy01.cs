using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class enemy01 : MonoBehaviour
{
    public Info infoScript; // Reference to the Info script
    public bool criticalsanity = false; // Flag to track if critical sanity is active
    public float sanityDamage = 10f; // Amount of sanity damage to apply
    public float formchangeThreshold = 20f; // Threshold for form change

    public GameObject enemyFormA; // Reference to the first form of the enemy
    public GameObject enemyFormB; // Reference to the second form of the enemy

    public Shader formB_effect; // Reference to the shader for form B

    [SerializeField] private int maxHp = 20;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactAttackInterval = 1f;
    [SerializeField] private Transform target;

    private int currentHp;
    private Rigidbody enemyRigidbody;
    private float nextContactAttackTime;

    private void Awake()
    {
        currentHp = maxHp;
        enemyRigidbody = GetComponent<Rigidbody>();
        enemyRigidbody.useGravity = true;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        Collider enemyCollider = GetComponent<Collider>();
        enemyCollider.isTrigger = false;

        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            enemyRigidbody.linearVelocity = new Vector3(0f, enemyRigidbody.linearVelocity.y, 0f);
            return;
        }

        Vector3 horizontalVelocity = direction.normalized * moveSpeed;
        enemyRigidbody.linearVelocity = new Vector3(
            horizontalVelocity.x,
            enemyRigidbody.linearVelocity.y,
            horizontalVelocity.z);

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsPlayer(collision.collider) || Time.time < nextContactAttackTime)
        {
            return;
        }

        nextContactAttackTime = Time.time + contactAttackInterval;
        collision.collider.SendMessageUpwards("TakeDamage", contactDamage, SendMessageOptions.DontRequireReceiver);
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }

    private static bool IsPlayer(Collider collider)
    {
        return collider.CompareTag("Player")
            || collider.GetComponentInParent<FpsPlayerMotor>() != null;
    }
}
