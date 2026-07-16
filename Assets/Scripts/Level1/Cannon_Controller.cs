using System.Collections;
using UnityEngine;

public class CannonController3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject fireballPrefab;

    [Header("Targeting Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float detectionRange = 15f; // Added: Cannon will only track and shoot within this radius

    [Header("Art Alignment")]
    [Tooltip("Base alignment offset to make the right-pointing sprite barrel track forward dynamically.")]
    [SerializeField] private float artZOffset = 0f; // Adjusted to match your working editor setting!

    [Header("Timing Ranges (Reflex Windows)")]
    [SerializeField] private float minFireInterval = 2.5f;
    [SerializeField] private float maxFireInterval = 5f;

    private Transform playerTransform;
    private PlayerController playerController;
    private Collider cannonCollider; // Changed from Collider2D to 3D Collider

    private void Start()
    {
        // Cache the 3D collider on this object or its children
        cannonCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        PlayerController targetPlayer = Object.FindFirstObjectByType<PlayerController>();
        if (targetPlayer != null)
        {
            playerController = targetPlayer;
            playerTransform = targetPlayer.transform;
            StartCoroutine(ShootingRoutine());
        }
    }

    private void Update()
    {
        if (playerTransform == null || playerController.IsDead)
            return;

        // Calculate direction to the player on the flat ground plane
        Vector3 targetDirection = playerTransform.position - transform.position;
        targetDirection.y = 0f;

        // Added: Check if the player is outside the fixed detection range
        if (targetDirection.magnitude > detectionRange)
            return; // Stop tracking and do not rotate if the player is too far

        if (targetDirection.sqrMagnitude > 0.001f)
        {
            // Formulate 3D plane look matrix tracking the player target
            Quaternion lookRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            // Keep the sprite laying flat (90 on X) and apply your working visual offset on Z
            Quaternion combinedCorrection = Quaternion.Euler(90f, 0f, artZOffset);
            Quaternion targetRotation = lookRotation * combinedCorrection;

            // Smoothly blend transform orientation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator ShootingRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        while (playerTransform != null && !playerController.IsDead)
        {
            float randomWait = Random.Range(minFireInterval, maxFireInterval);
            yield return new WaitForSeconds(randomWait);

            // Added: Double-check that the player is actually within range before firing a fireball
            if (!playerController.IsDead && IsPlayerInRange())
            {
                FireCannon();
            }
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;

        Vector3 flatPositionDiff = playerTransform.position - transform.position;
        flatPositionDiff.y = 0f;

        return flatPositionDiff.magnitude <= detectionRange;
    }

    private void FireCannon()
    {
        if (fireballPrefab != null && firePoint != null)
        {
            GameObject spawnedFireball = Instantiate(fireballPrefab, firePoint.position, firePoint.rotation);

            FireballProjectile3D projectileScript = spawnedFireball.GetComponent<FireballProjectile3D>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(cannonCollider, transform);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}