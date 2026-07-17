using System.Collections;
using UnityEngine;

public class CannonController3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject fireballPrefab;

    [Header("Targeting Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float detectionRange = 15f; 

    [Header("Art Alignment")]
    [Tooltip("Base alignment offset to make the right-pointing sprite barrel track forward dynamically.")]
    [SerializeField] private float artZOffset = 0f; 

    [Header("Timing Ranges (Reflex Windows)")]
    [SerializeField] private float minFireInterval = 2.5f;
    [SerializeField] private float maxFireInterval = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;

    private Transform playerTransform;
    private PlayerController playerController;
    private Collider cannonCollider; 

    private void Start()
    {
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

        Vector3 targetDirection = playerTransform.position - transform.position;
        targetDirection.y = 0f;

        if (targetDirection.magnitude > detectionRange)
            return; 

        if (targetDirection.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            Quaternion combinedCorrection = Quaternion.Euler(90f, 0f, artZOffset);
            Quaternion targetRotation = lookRotation * combinedCorrection;

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

            PlayFireSound2D();
        }
    }

    private void PlayFireSound2D()
    {
        if (fireSound != null)
        {
            GameObject sfxObj = new GameObject("Temp_CannonFire_SFX");
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.clip = fireSound;
            source.spatialBlend = 0f; // Forces 2D Full Volume
            source.volume = 1f;
            source.Play();
            Destroy(sfxObj, fireSound.length);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}