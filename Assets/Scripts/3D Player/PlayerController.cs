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

    // ================= STATE MANAGEMENT =================
    private bool gameStarted = false;

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
    [SerializeField] private LayerMask interactableLayer;

    private IInteractable currentTargetInteractable;
    private BoxController currentTargetBox;
    private RotatingJunction currentTargetJunction;

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

    [Header("Audio")]
    [SerializeField] private AudioClip[] hitSounds = new AudioClip[3];
    [SerializeField] private AudioClip[] swingSounds = new AudioClip[3];
    [SerializeField] private AudioClip blockSound;

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

    // ================= CHARACTER CONTROLLER ADJUSTMENTS =================
    private float originalRadius;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (characterController != null)
        {
            originalRadius = characterController.radius;
        }

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
        if (!gameStarted) return false;

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

        yield return new WaitForSeconds(0.5f);
        PlayRandomSound2D(swingSounds);

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - 0.5f));

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

        PlayRandomSound2D(hitSounds);

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

            ResetControllerWidth();
            TakeHit();
        }
    }

    public bool TryBlockAttack(Vector3 attackerPosition)
    {
        if (currentState != PlayerState.Block)
            return false;

        ForceFacePosition(attackerPosition, blockFaceDuration);

        if (blockSound != null)
        {
            GameObject sfxObj = new GameObject("Temp_Block_SFX");
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.clip = blockSound;
            source.spatialBlend = 0f; // Pure 2D
            source.volume = 1f;
            source.Play();
            Destroy(sfxObj, blockSound.length);
        }

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

        ResetControllerWidth();

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

        if (characterController != null)
        {
            characterController.radius = originalRadius * 1.25f;
        }

        if (animator != null)
            animator.SetBool(isBlockingParam, true);
    }

    private void StopBlocking()
    {
        if (currentState != PlayerState.Block) return;

        currentState = PlayerState.Idle;

        ResetControllerWidth();

        if (animator != null)
            animator.SetBool(isBlockingParam, false);
    }

    private void ResetControllerWidth()
    {
        if (characterController != null)
        {
            characterController.radius = originalRadius;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable") || (other.transform.parent != null && other.transform.parent.CompareTag("Interactable")))
        {
            Debug.Log($"[PlayerController Log] Player entered trigger range of: {other.gameObject.name}");

            IInteractable interactable = other.GetComponentInParent<IInteractable>() ?? other.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentTargetInteractable = interactable;

                if (interactable is BoxController box) box.ShowPrompt();
                else if (interactable is RotatingJunction junction) junction.ShowPrompt();
                else if (interactable is PowerSource source) source.ShowPrompt();
                else if (interactable is PowerDestination dest) dest.ShowPrompt();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable") || (other.transform.parent != null && other.transform.parent.CompareTag("Interactable")))
        {
            Debug.Log($"[PlayerController Log] Player exited trigger range of: {other.gameObject.name}");

            if (currentTargetInteractable != null)
            {
                if (currentTargetInteractable is BoxController box) box.HidePrompt();
                else if (currentTargetInteractable is RotatingJunction junction) junction.HidePrompt();
                else if (currentTargetInteractable is PowerSource source) source.HidePrompt();
                else if (currentTargetInteractable is PowerDestination dest) dest.HidePrompt();
            }

            currentTargetInteractable = null;
            currentTargetBox = null;
        }
    }

    private void TryInteract()
    {
        Debug.Log($"[Interact Input] Button pressed! currentState: {currentState} | gameStarted: {gameStarted}");

        if (!CanAct())
        {
            Debug.LogWarning($"[Interact Input] Interaction blocked! CanAct() returned false. Current state: {currentState}, gameStarted: {gameStarted}");
            return;
        }

        if (currentTargetInteractable != null)
        {
            Debug.Log($"[Interact Input] SUCCESS: Valid target found. Executing routine on: {((MonoBehaviour)currentTargetInteractable).gameObject.name}");
            StartCoroutine(InteractRoutine());
        }
        else
        {
            Debug.LogWarning("[Interact Input] FAILED: Key pressed, but 'currentTargetInteractable' is currently NULL. The player is not registered as standing inside any trigger zone.");
        }
    }

    private IEnumerator InteractRoutine()
    {
        currentState = PlayerState.Interact;
        Debug.Log("[Interact Input] State set to 'Interact'. Playing animation trigger...");

        if (animator != null)
            animator.SetTrigger(interactTriggerParam);

        if (currentTargetInteractable != null)
        {
            Debug.Log($"[Interact Input] Sending Interact() message directly to target script...");
            currentTargetInteractable.Interact(gameObject);
        }
        else
        {
            Debug.LogError("[Interact Input] Critical error: Target reference vanished mid-coroutine frame context!");
        }

        yield return new WaitForSeconds(0.5f);

        if (currentState == PlayerState.Interact)
        {
            currentState = PlayerState.Idle;
            Debug.Log("[Interact Input] State returned to 'Idle'. Interaction loop finished.");
        }
    }

    public void Heal(int amount)
    {
        if (currentState == PlayerState.Dead)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Player healed by {amount}. Current health: {currentHealth}/{maxHealth}");
    }

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

    // Explicit 2D Sound Spawner to handle overlapping cleanly at full, original volume
    private void PlayRandomSound2D(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            AudioClip clip = clips[index];
            if (clip != null)
            {
                GameObject sfxObj = new GameObject("Temp_2D_SFX");
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.clip = clip;
                source.spatialBlend = 0f; // Forces 2D Full Volume
                source.volume = 1f;
                source.Play();
                Destroy(sfxObj, clip.length);
            }
        }
    }

    public void SetGameStarted(bool started)
    {
        gameStarted = started;
    }

    public void EnableGameplayInput(bool enable)
    {
        if (playerInput == null) return;

        if (enable)
        {
            playerInput.ActivateInput();
        }
        else
        {
            playerInput.DeactivateInput();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}