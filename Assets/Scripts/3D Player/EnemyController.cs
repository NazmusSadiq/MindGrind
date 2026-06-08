using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    public bool IsDead => currentState == EnemyState.Dead;
    public bool IsAware => isAware;

    private enum EnemyState
    {
        Patrol,
        Wait,
        Chase,
        Investigate,
        Attack,
        Hit,
        Dead
    }

    [Header("Patrol")]
    [SerializeField] private Transform patrolEndPoint;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPointDuration = 3f;
    [SerializeField] private float reachDistance = 0.15f;

    [Header("Detection")]
    [SerializeField] private float sightRange = 8f;
    [SerializeField] private float viewAngle = 75f;
    [SerializeField] private float forgetDuration = 3f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float hitDuration = 0.4f;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int damageAmount = 10;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string attackIndexParam = "AttackIndex";
    [SerializeField] private string attackBoolParam = "Attack";
    [SerializeField] private string hitBoolParam = "Hit";
    [SerializeField] private string deathBoolParam = "Dead";

    private CharacterController characterController;
    private EnemyState currentState = EnemyState.Patrol;
    private Transform playerTarget;
    private Vector3 patrolStartPoint;
    private Vector3 patrolEndPointWorldPosition;
    private bool movingToEndPoint = true;
    private bool isAttacking;
    private bool isTakingHit;
    private bool attackDamageApplied;
    private Coroutine waitCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine hitCoroutine;
    private int currentHealth;
    private bool isAware;

    private float forgetTimer;
    private Vector3 lastKnownPosition;

    public static bool IsAnyEnemyAware()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();

        foreach (EnemyController enemy in enemies)
        {
            if (enemy.IsAware)
                return true;
        }

        return false;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        patrolStartPoint = transform.position;

        if (patrolEndPoint != null)
            patrolEndPointWorldPosition = patrolEndPoint.position;
    }

    private void Update()
    {
        if (playerTarget != null)
        {
            PlayerController player = playerTarget.GetComponent<PlayerController>();

            if (player != null && player.IsDead)
            {
                playerTarget = null;
                isAware = false;
            }
        }

        if (playerTarget == null)
            FindPlayerTarget();

        if (currentState == EnemyState.Dead || isAttacking || isTakingHit)
        {
            if (currentState == EnemyState.Dead)
                isAware = false;

            UpdateAnimations();
            return;
        }

        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            forgetTimer = forgetDuration;
            isAware = true;

            if (playerTarget != null)
                lastKnownPosition = playerTarget.position;

            StopWaiting();
            HandleChase();

            // New: Alert nearby teammates since we see the player
            AlertNearbyAllies();
        }
        else if (isAware)
        {
            forgetTimer -= Time.deltaTime;
            if (forgetTimer <= 0f)
            {
                isAware = false;
                currentState = EnemyState.Investigate;
            }
            else
            {
                HandleChase();
                // New: Keep updating nearby teammates during the chase grace-period
                AlertNearbyAllies();
            }
        }
        else
        {
            if (currentState == EnemyState.Investigate)
            {
                HandleInvestigation();
                // New: Still alert allies while navigating to the investigation point
                AlertNearbyAllies();
            }
            else
            {
                HandlePatrol();
            }
        }

        UpdateAnimations();
    }

    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            playerTarget = player.transform;
    }

    private bool CanSeePlayer()
    {
        if (playerTarget == null || currentState == EnemyState.Dead)
            return false;

        PlayerController player = playerTarget.GetComponent<PlayerController>();

        if (player != null && player.IsDead)
            return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 target = playerTarget.position + Vector3.up * 0.5f;
        Vector3 direction = target - origin;

        if (direction.sqrMagnitude > sightRange * sightRange)
            return false;

        Vector3 directionToTargetXZ = new Vector3(direction.x, 0f, direction.z).normalized;
        Vector3 enemyForwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        float angleToPlayer = Vector3.Angle(enemyForwardXZ, directionToTargetXZ);

        if (angleToPlayer > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, sightRange))
            return hit.collider.CompareTag("Player");

        return false;
    }

    // New Method: Alerts unalerted enemies within half vision range
    private void AlertNearbyAllies()
    {
        float alertRadius = sightRange * 0.5f;
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, alertRadius);

        foreach (Collider col in nearbyColliders)
        {
            // Make sure we don't accidentally check ourselves
            if (col.gameObject == this.gameObject)
                continue;

            EnemyController ally = col.GetComponent<EnemyController>();

            // If we found a valid ally who isn't dead and isn't currently tracking the player
            if (ally != null && !ally.IsDead && !ally.IsAware && ally.currentState != EnemyState.Investigate)
            {
                ally.ReceiveExternalAlert(lastKnownPosition);
            }
        }
    }

    // New Method: Called by other enemies to pass along target positions
    public void ReceiveExternalAlert(Vector3 targetPosition)
    {
        if (currentState == EnemyState.Dead || isAttacking || isTakingHit)
            return;

        StopWaiting();

        lastKnownPosition = targetPosition;
        forgetTimer = forgetDuration;
        isAware = true;

        currentState = EnemyState.Chase;
    }

    private void HandlePatrol()
    {
        if (waitCoroutine != null || patrolEndPoint == null)
        {
            if (patrolEndPoint == null)
                currentState = EnemyState.Wait;

            return;
        }

        Vector3 destination = movingToEndPoint ? patrolEndPointWorldPosition : patrolStartPoint;
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= reachDistance * reachDistance)
        {
            waitCoroutine = StartCoroutine(PatrolWaitRoutine());
            return;
        }

        currentState = EnemyState.Patrol;
        Move(direction.normalized, patrolSpeed);
    }

    private IEnumerator PatrolWaitRoutine()
    {
        currentState = EnemyState.Wait;
        yield return new WaitForSeconds(waitAtPointDuration);
        movingToEndPoint = !movingToEndPoint;
        waitCoroutine = null;
        currentState = EnemyState.Patrol;
    }

    private void HandleInvestigation()
    {
        if (waitCoroutine != null)
            return;

        Vector3 direction = lastKnownPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= reachDistance * reachDistance)
        {
            waitCoroutine = StartCoroutine(InvestigationWaitRoutine());
            return;
        }

        Move(direction.normalized, chaseSpeed);
    }

    private IEnumerator InvestigationWaitRoutine()
    {
        currentState = EnemyState.Wait;
        yield return new WaitForSeconds(waitAtPointDuration);

        waitCoroutine = null;
        currentState = EnemyState.Patrol;
    }

    private void StopWaiting()
    {
        if (waitCoroutine == null)
            return;

        StopCoroutine(waitCoroutine);
        waitCoroutine = null;
    }

    private void HandleChase()
    {
        if (playerTarget == null)
            return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= attackRange * attackRange)
        {
            FaceDirectionImmediate(direction);

            if (!isAttacking && attackCoroutine == null)
                attackCoroutine = StartCoroutine(AttackRoutine());

            return;
        }

        if (isAttacking)
            return;

        currentState = EnemyState.Chase;
        Move(direction.normalized, chaseSpeed);
    }

    private void Move(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        FaceDirection(direction);
        characterController.Move(direction * speed * Time.deltaTime);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void FaceDirectionImmediate(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private IEnumerator AttackRoutine()
    {
        if (playerTarget != null)
        {
            Vector3 direction = playerTarget.position - transform.position;
            direction.y = 0f;
            FaceDirectionImmediate(direction);
        }

        isAttacking = true;
        isAware = true;
        attackDamageApplied = false;
        currentState = EnemyState.Attack;

        if (animator != null)
        {
            int index = Random.Range(0, 2);
            animator.SetInteger(attackIndexParam, index);
            animator.SetBool(attackBoolParam, true);
        }

        yield return new WaitForSeconds(attackDuration);

        if (animator != null)
            animator.SetBool(attackBoolParam, false);

        isAttacking = false;
        attackDamageApplied = false;
        attackCoroutine = null;

        if (CanSeePlayer())
        {
            isAware = true;
            forgetTimer = forgetDuration;
            currentState = EnemyState.Chase;
        }
        else
        {
            if (forgetTimer > 0)
            {
                currentState = EnemyState.Chase;
            }
            else
            {
                isAware = false;
                currentState = EnemyState.Investigate;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentState == EnemyState.Dead)
            return;

        Debug.Log($"Enemy taking damage: {amount}. Health before: {currentHealth}");
        currentHealth -= amount;
        Debug.Log($"Enemy health after damage: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        forgetTimer = forgetDuration;
        isAware = true;
        if (playerTarget != null)
            lastKnownPosition = playerTarget.position;

        TakeHit();
    }

    private void TakeHit()
    {
        StopWaiting();
        StopAttack();

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitRoutine());
    }

    public void ApplyAttackDamage()
    {
        if (!isAttacking || currentState != EnemyState.Attack || attackDamageApplied)
            return;

        attackDamageApplied = true;
        DetectPlayerHits();
    }

    private IEnumerator HitRoutine()
    {
        isTakingHit = true;
        currentState = EnemyState.Hit;

        if (animator != null)
            animator.SetBool(hitBoolParam, true);

        yield return new WaitForSeconds(hitDuration);

        if (animator != null)
            animator.SetBool(hitBoolParam, false);

        isTakingHit = false;
        hitCoroutine = null;

        if (CanSeePlayer())
        {
            isAware = true;
            forgetTimer = forgetDuration;
            currentState = EnemyState.Chase;
        }
        else
        {
            if (forgetTimer > 0)
            {
                currentState = EnemyState.Chase;
            }
            else
            {
                isAware = false;
                currentState = EnemyState.Investigate;
            }
        }
    }

    private void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        attackDamageApplied = false;

        if (animator != null)
            animator.SetBool(attackBoolParam, false);
    }

    private void Die()
    {
        currentHealth = 0;
        currentState = EnemyState.Dead;
        isAware = false;

        StopWaiting();
        StopAttack();

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            hitCoroutine = null;
        }

        isTakingHit = false;

        if (animator != null)
        {
            animator.SetBool(hitBoolParam, false);
            animator.SetBool(deathBoolParam, true);
        }
    }

    private void DetectPlayerHits()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        HashSet<PlayerController> damagedPlayers = new HashSet<PlayerController>();

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController player = hit.GetComponentInParent<PlayerController>();

                if (player == null || !damagedPlayers.Add(player))
                    continue;

                if (player.TryBlockAttack(transform.position))
                    continue;

                player.TakeDamage(damageAmount);
                Debug.Log("Hit player: " + hit.name);
            }
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        bool moving = currentState == EnemyState.Patrol || currentState == EnemyState.Chase || currentState == EnemyState.Investigate;
        bool running = currentState == EnemyState.Chase || currentState == EnemyState.Investigate;
        float animSpeed = 0f;

        if (currentState == EnemyState.Patrol)
            animSpeed = 0.5f;
        else if (currentState == EnemyState.Chase || currentState == EnemyState.Investigate)
            animSpeed = 1f;

        animator.SetBool(isMovingParam, moving);
        animator.SetBool(isRunningParam, running);
        animator.SetFloat(moveSpeedParam, animSpeed, 0.1f, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Draw Alert Range (Half of sight range)
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, sightRange * 0.5f);

        Gizmos.color = Color.blue;
        Vector3 forwardLook = transform.forward * sightRange;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forwardLook;
        Vector3 rightRayDirection = rightRayRotation * forwardLook;

        Gizmos.DrawLine(transform.position + Vector3.up, (transform.position + Vector3.up) + leftRayDirection);
        Gizmos.DrawLine(transform.position + Vector3.up, (transform.position + Vector3.up) + rightRayDirection);

        if (currentState == EnemyState.Investigate)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.up, lastKnownPosition + Vector3.up);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolEndPoint != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 endPoint = Application.isPlaying ? patrolEndPointWorldPosition : patrolEndPoint.position;
            Gizmos.DrawLine(Application.isPlaying ? patrolStartPoint : transform.position, endPoint);
            Gizmos.DrawSphere(endPoint, 0.15f);
        }
    }
}