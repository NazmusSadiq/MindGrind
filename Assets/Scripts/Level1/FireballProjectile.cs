using UnityEngine;

public class FireballProjectile3D : MonoBehaviour
{
    [Header("Movement Metrics")]
    [SerializeField] private float travelSpeed = 12f;
    [SerializeField] private int damageValue = 10;
    [SerializeField] private float maxLifetime = 20f;

    private Collider fireballCollider;
    private Transform firingCannonTransform; // Added to store who shot this fireball

    private void Awake()
    {
        fireballCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    // UPDATED: Now accepts the cannon's transform so the player knows where to face when blocking
    public void Initialize(Collider cannonCollider, Transform cannonTransform)
    {
        firingCannonTransform = cannonTransform;

        if (fireballCollider != null && cannonCollider != null)
        {
            Physics.IgnoreCollision(fireballCollider, cannonCollider);
        }
    }

    private void Update()
    {
        // Moves smoothly along the flat floor plane axis direction
        transform.position += transform.up * travelSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object we hit is the Floor (by object name or by a "Floor" tag)
        if (other.gameObject.name == "Floor" || other.CompareTag("Floor"))
        {
            // Exit early without destroying the fireball or checking player damage
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                bool attackBlocked = player.TryBlockAttack(transform.position);

                if (attackBlocked)
                {
                    // Rotates the player toward the firing cannon instantly
                    if (firingCannonTransform != null)
                    {
                        Vector3 directionToCannon = firingCannonTransform.position - player.transform.position;
                        directionToCannon.y = 0f; // Keep the rotation strictly on the flat ground plane

                        if (directionToCannon.sqrMagnitude > 0.001f)
                        {
                            player.transform.rotation = Quaternion.LookRotation(directionToCannon.normalized, Vector3.up);
                        }
                    }
                }
                else
                {
                    player.TakeDamage(damageValue);
                }
            }
        }

        Destroy(gameObject);
    }
}