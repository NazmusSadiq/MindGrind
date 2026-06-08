using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public bool IsDead => currentState == PlayerState.Dead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

    public enum ControlDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    private enum PlayerState
    {
        Idle,
        Move,
        Attack,
        Block,
        Interact,
        Hit,
        Dead
    }

    private PlayerState currentState = PlayerState.Idle;

    // ================= MOVEMENT =================
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private ControlDirection controlDirection = ControlDirection.Up;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedForce = -2f;

    private float verticalVelocity;

    // ================= INTERACTION =================
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayer;

    // ================= ATTACK =================
    [Header("Attack")]
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float attackRange = 2f;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float hitDuration = 0.4f;
    [SerializeField] private float blockFaceDuration = 0.2f;
    [SerializeField] private float attackFaceDuration = 0.2f;

    // ================= ANIMATION =================
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("UI")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private float deathMenuDelay = 3f;

    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string isSprintingParam = "IsSprinting";

    [SerializeField] private string attackBoolParam = "Attack";
    [SerializeField] private string interactTriggerParam = "Interact";
    [SerializeField] private string isBlockingParam = "IsBlocking";
    [SerializeField] private string hitBoolParam = "Hit";
    [SerializeField] private string deathBoolParam = "Dead";

    // ================= INPUT =================
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction attackAction;
    private InputAction blockAction;

    private CharacterController characterController;
    private int currentHealth;
    private bool attackDamageApplied;
    private float forcedFaceTimer;
    private Vector3 forcedFacePosition;
    private Coroutine hitCoroutine;
    private bool inputCallbacksRegistered;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        interactAction = playerInput.actions["Interact"];
        attackAction = playerInput.actions["Attack"];
        blockAction = playerInput.actions["Block"];
    }

    private void OnEnable()
    {
        RegisterInputCallbacks();
    }

    private void OnDisable()
    {
        UnregisterInputCallbacks();
    }

    private void RegisterInputCallbacks()
    {
        if (inputCallbacksRegistered || interactAction == null || attackAction == null || blockAction == null)
            return;

        interactAction.performed += OnInteractPerformed;
        attackAction.performed += OnAttackPerformed;
        blockAction.performed += OnBlockPerformed;
        blockAction.canceled += OnBlockCanceled;
        inputCallbacksRegistered = true;
    }

    private void UnregisterInputCallbacks()
    {
        if (!inputCallbacksRegistered || interactAction == null || attackAction == null || blockAction == null)
            return;

        interactAction.performed -= OnInteractPerformed;
        attackAction.performed -= OnAttackPerformed;
        blockAction.performed -= OnBlockPerformed;
        blockAction.canceled -= OnBlockCanceled;
        inputCallbacksRegistered = false;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        TryAttack();
    }

    private void OnBlockPerformed(InputAction.CallbackContext context)
    {
        StartBlocking();
    }

    private void OnBlockCanceled(InputAction.CallbackContext context)
    {
        StopBlocking();
    }

    private void Update()
    {
        UpdateForcedFacing();
        HandleMovement();
        UpdateAnimations();
    }

    private void UpdateForcedFacing()
    {
        if (forcedFaceTimer <= 0f)
            return;

        forcedFaceTimer -= Time.deltaTime;

        Vector3 direction = forcedFacePosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private bool IsLocked()
    {
        return currentState == PlayerState.Attack ||
               currentState == PlayerState.Block ||
               currentState == PlayerState.Interact ||
               currentState == PlayerState.Hit ||
               currentState == PlayerState.Dead;
    }

    private bool CanAct()
    {
        return currentState == PlayerState.Idle || currentState == PlayerState.Move;
    }

    private void HandleMovement()
    {
        if (IsLocked()) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        input = RemapMovementInput(input);

        bool sprint = sprintAction.ReadValue<float>() > 0.1f;

        Vector3 move = new Vector3(-input.y, 0f, input.x);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        if (move.x != 0f || move.z != 0f)
        {
            currentState = PlayerState.Move;

            Vector3 flatMove = new Vector3(move.x, 0f, move.z);

            Quaternion targetRot =
                Quaternion.LookRotation(flatMove.normalized, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            currentState = PlayerState.Idle;
        }

        float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);

        Vector3 finalMove =
            new Vector3(move.x * speed, move.y, move.z * speed);

        characterController.Move(finalMove * Time.deltaTime);
    }

    public void SetControlDirection(ControlDirection direction)
    {
        controlDirection = direction;
    }

    public void ShowGameOverMenu()
    {
        if (playerInput != null)
            playerInput.DeactivateInput();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);
    }

    private Vector2 RemapMovementInput(Vector2 input)
    {
        switch (controlDirection)
        {
            case ControlDirection.Right:
                return new Vector2(-input.y, input.x);
            case ControlDirection.Down:
                return -input;
            case ControlDirection.Left:
                return new Vector2(input.y, -input.x);
            default:
                return input;
        }
    }

    private void TryAttack()
    {
        if (!CanAct()) return;

        EnemyController closestEnemy = GetClosestLiveEnemyInRange();

        if (closestEnemy != null)
            ForceFacePosition(closestEnemy.transform.position, attackFaceDuration);

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        attackDamageApplied = false;
        currentState = PlayerState.Attack;

        if (animator != null)
        {
            int index = Random.Range(0, 2);
            animator.SetInteger("AttackIndex", index);

            animator.SetBool(attackBoolParam, true);
        }

        yield return new WaitForSeconds(attackDuration);

        if (animator != null)
            animator.SetBool(attackBoolParam, false);

        attackDamageApplied = false;

        if (currentState == PlayerState.Attack)
            currentState = PlayerState.Idle;
    }

    public void ApplyAttackDamage()
    {
        if (currentState != PlayerState.Attack || attackDamageApplied)
            return;

        attackDamageApplied = true;
        DetectEnemyHits();
    }

    private EnemyController GetClosestLiveEnemyInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        EnemyController closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy == null || enemy.IsDead)
                continue;

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void ForceFacePosition(Vector3 targetPosition, float duration)
    {
        forcedFacePosition = targetPosition;
        forcedFaceTimer = duration;
        UpdateForcedFacing();
    }

    private void DetectEnemyHits()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        HashSet<EnemyController> damagedEnemies = new HashSet<EnemyController>();
        EnemyController closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                if (enemy == null || enemy.IsDead || !damagedEnemies.Add(enemy))
                    continue;

                float distance = (enemy.transform.position - transform.position).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        if (closestEnemy != null)
            ForceFacePosition(closestEnemy.transform.position, attackFaceDuration);

        foreach (EnemyController enemy in damagedEnemies)
        {
            enemy.TakeDamage(damageAmount);
            Debug.Log("Hit enemy: " + enemy.name);
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentState == PlayerState.Dead)
            return;

        Debug.Log($"Player taking damage: {amount}. Health before: {currentHealth}");
        currentHealth -= amount;
        Debug.Log($"Player health after damage: {currentHealth}");

        if (currentHealth <= 0)
            Die();
        else
        {
            if (animator != null)
            {
                animator.SetBool(attackBoolParam, false);
                animator.SetBool(isBlockingParam, false);
                animator.SetBool(hitBoolParam, true);
            }

            TakeHit();
        }
    }

    public bool TryBlockAttack(Vector3 attackerPosition)
    {
        if (currentState != PlayerState.Block)
            return false;

        ForceFacePosition(attackerPosition, blockFaceDuration);

        return true;
    }

    private void Die()
    {
        currentHealth = 0;
        currentState = PlayerState.Dead;

        StopAllCoroutines();
        attackDamageApplied = false;
        forcedFaceTimer = 0f;
        hitCoroutine = null;

        if (animator != null)
        {
            animator.SetBool(attackBoolParam, false);
            animator.SetBool(isBlockingParam, false);
            animator.SetBool(hitBoolParam, false);
            animator.SetBool(deathBoolParam, true);
        }

        if (gameOverMenu != null)
            StartCoroutine(ShowGameOverMenuAfterDelay());
    }

    private IEnumerator ShowGameOverMenuAfterDelay()
    {
        yield return new WaitForSeconds(deathMenuDelay);
        ShowGameOverMenu();
    }

    private void TakeHit()
    {
        StopAllCoroutines();
        attackDamageApplied = false;
        forcedFaceTimer = 0f;

        currentState = PlayerState.Hit;

        hitCoroutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        yield return new WaitForSeconds(hitDuration);

        if (animator != null)
            animator.SetBool(hitBoolParam, false);

        hitCoroutine = null;

        if (currentState == PlayerState.Hit)
            currentState = PlayerState.Idle;
    }

    private void StartBlocking()
    {
        if (!CanAct()) return;

        currentState = PlayerState.Block;

        if (animator != null)
            animator.SetBool(isBlockingParam, true);
    }

    private void StopBlocking()
    {
        if (currentState != PlayerState.Block) return;

        currentState = PlayerState.Idle;

        if (animator != null)
            animator.SetBool(isBlockingParam, false);
    }

    // =====================================================
    // INTERACT
    // =====================================================

    private void TryInteract()
    {
        if (!CanAct()) return;

        StartCoroutine(InteractRoutine());
    }

    private IEnumerator InteractRoutine()
    {
        currentState = PlayerState.Interact;

        if (animator != null)
            animator.SetTrigger(interactTriggerParam);

        Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            hit.collider.GetComponent<IInteractable>()?.Interact(gameObject);
        }

        yield return new WaitForSeconds(1f);

        if (currentState == PlayerState.Interact)
            currentState = PlayerState.Idle;
    }

    // =====================================================
    // ANIMATION
    // =====================================================

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool moving = currentState == PlayerState.Move;
        bool sprint = moving && sprintAction.ReadValue<float>() > 0.1f;

        animator.SetBool(isMovingParam, moving);
        animator.SetBool(isSprintingParam, sprint);

        float animSpeed = moving ? (sprint ? 1f : 0.5f) : 0f;

        animator.SetFloat(moveSpeedParam, animSpeed, 0.1f, Time.deltaTime);
    }

    // optional debug gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

public interface IInteractable
{
    void Interact(GameObject interactor);
}