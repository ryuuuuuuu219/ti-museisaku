using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpriteRenderer))]
public class enemy01 : MonoBehaviour
{
    public Info infoScript; // Reference to the Info script
    public bool criticalsanity => GetSanity() <= formchangeThreshold; // Check if sanity is below the threshold
    public float sanityDamage = 10f; // Amount of sanity damage to apply
    public float formchangeThreshold = 20f; // Threshold for form change

    public Sprite enemyFormA; // Reference to the first form of the enemy
    public GameObject enemyFormB; // Reference to the second form of the enemy

    public Shader formB_effect; // Reference to the shader for form B

    [SerializeField] private int maxHp = 20;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactAttackInterval = 1f;
    [SerializeField] private Transform target;

    private int currentHp;
    private Rigidbody enemyRigidbody;
    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;
    private Shader defaultMeshShader;
    private float nextContactAttackTime;

    private void Awake()
    {
        currentHp = maxHp;
        enemyRigidbody = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyRigidbody.useGravity = true;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        Collider enemyCollider = GetComponent<Collider>();
        enemyCollider.isTrigger = false;

        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        if (infoScript == null)
        {
            infoScript = FindAnyObjectByType<Info>();
        }
    }

    private void Start()
    {
        CreateFormBMeshObject();
        ApplySpriteForm(enemyFormA);
        ApplyMeshForm(false);
    }

    float GetSanity()
    {
        return infoScript != null ? infoScript.GetSanity() : float.MaxValue;
    }

    void FormManager()
    {
        if (criticalsanity)
        {
            ApplySpriteForm(null);
            ApplyMeshForm(true);

            if (enemyFormB != null)
            {
                enemyFormB.SetActive(true);
            }

            if (formB_effect != null && meshRenderer != null)
            {
                meshRenderer.material.shader = formB_effect;
            }
        }
        else
        {
            ApplySpriteForm(enemyFormA);
            ApplyMeshForm(false);

            if (enemyFormB != null)
            {
                enemyFormB.SetActive(false);
            }

            if (defaultMeshShader != null && meshRenderer != null)
            {
                meshRenderer.material.shader = defaultMeshShader;
            }
        }
    }

    void ApplySpriteForm(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.enabled = sprite != null;
    }

    void ApplyMeshForm(bool visible)
    {
        if (meshRenderer == null)
        {
            return;
        }

        meshRenderer.enabled = visible;
    }

    void CreateFormBMeshObject()
    {
        if (enemyFormB != null)
        {
            meshRenderer = enemyFormB.GetComponentInChildren<MeshRenderer>(true);
            defaultMeshShader = meshRenderer != null ? meshRenderer.sharedMaterial.shader : null;
            return;
        }

        enemyFormB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyFormB.name = "Enemy01 FormB Mesh";
        enemyFormB.transform.SetParent(transform, false);
        enemyFormB.transform.localPosition = Vector3.zero;
        enemyFormB.transform.localRotation = Quaternion.identity;
        enemyFormB.transform.localScale = Vector3.one;

        Collider formCollider = enemyFormB.GetComponent<Collider>();
        if (formCollider != null)
        {
            Destroy(formCollider);
        }

        meshRenderer = enemyFormB.GetComponent<MeshRenderer>();
        defaultMeshShader = meshRenderer != null ? meshRenderer.sharedMaterial.shader : null;
        enemyFormB.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        FormManager();
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
