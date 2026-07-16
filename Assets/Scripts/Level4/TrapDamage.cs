using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrapDamage : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float damageCooldown = 2f; 

    private bool hasDamagedPlayer;
    private bool wasPlayerInside;
    private Collider trapCollider;
    private PlayerController playerController;
    private CharacterController playerCharacterController;

    private float cooldownTimer; 

    private void Awake()
    {
        trapCollider = GetComponent<Collider>();
        CachePlayerReferences();
    }

    private void Update()
    {
        if (trapCollider == null)
        {
            return;
        }

        CachePlayerReferences();

        if (playerController == null || playerCharacterController == null)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        bool isPlayerInside = trapCollider.bounds.Intersects(playerCharacterController.bounds);

        if (isPlayerInside)
        {
            if (cooldownTimer <= 0f)
            {
                playerController.TakeDamage(damageAmount);
                cooldownTimer = damageCooldown; 
                hasDamagedPlayer = true;
            }
        }

        if (!isPlayerInside && wasPlayerInside)
        {
            hasDamagedPlayer = false;
        }

        wasPlayerInside = isPlayerInside;
    }

    private void CachePlayerReferences()
    {
        if (playerController != null && playerCharacterController != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = playerObject.GetComponent<PlayerController>() ?? playerObject.GetComponentInParent<PlayerController>();
        }

        if (playerCharacterController == null)
        {
            playerCharacterController = playerObject.GetComponent<CharacterController>() ?? playerObject.GetComponentInParent<CharacterController>();
        }
    }
}