using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrapDamage : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;

    private bool hasDamagedPlayer;
    private bool wasPlayerInside;
    private Collider trapCollider;
    private PlayerController playerController;
    private CharacterController playerCharacterController;

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

        bool isPlayerInside = trapCollider.bounds.Intersects(playerCharacterController.bounds);

        if (isPlayerInside && !wasPlayerInside)
        {
            if (!hasDamagedPlayer)
            {
                playerController.TakeDamage(damageAmount);
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
