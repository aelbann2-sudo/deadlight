using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float sprintStaminaCost = 20f;
        [SerializeField] private float staminaRegenRate = 15f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;

        [Header("References")]
        [SerializeField] private Camera mainCamera;

        [Header("Dodge Roll")]
        [SerializeField] private float dodgeSpeed = 15f;
        [SerializeField] private float dodgeDuration = 0.3f;
        [SerializeField] private float dodgeCooldown = 0.8f;
        [SerializeField] private float dodgeStaminaCost = 30f;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 mouseWorldPosition;
        private bool isSprinting;
        private bool canMove = true;
        private bool isDodging;
        private bool isInvincible;
        private float lastDodgeTime = -999f;
        private Vector2 dodgeDirection;

        public float MoveSpeed => moveSpeed;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public Vector2 MoveDirection => moveInput;
        public Vector2 AimDirection => (mouseWorldPosition - (Vector2)transform.position).normalized;
        public bool IsSprinting => isSprinting && moveInput.magnitude > 0;
        public bool IsDodging => isDodging;
        public bool IsInvincible => isInvincible;

        public System.Action<float> OnStaminaChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            currentStamina = maxStamina;
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            ApplyDifficultyModifiers();
        }

        private void ApplyDifficultyModifiers()
        {
            if (GameManager.Instance?.CurrentSettings != null)
            {
                var settings = GameManager.Instance.CurrentSettings;
            }
        }

        private void Update()
        {
            if (!canMove) return;

            HandleInput();
            HandleAiming();
            HandleStamina();
        }

        private void FixedUpdate()
        {
            if (!canMove) return;

            if (isDodging)
            {
                rb.linearVelocity = dodgeDirection * dodgeSpeed;
                return;
            }

            HandleMovement();
        }

        private void HandleInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(horizontal, vertical).normalized;

            isSprinting = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;

            if (Input.GetKeyDown(KeyCode.Space) && !isDodging &&
                Time.time >= lastDodgeTime + dodgeCooldown &&
                currentStamina >= dodgeStaminaCost)
            {
                StartDodge();
            }
        }

        private void StartDodge()
        {
            dodgeDirection = moveInput.magnitude > 0.1f ? moveInput : AimDirection;
            if (dodgeDirection.magnitude < 0.1f) dodgeDirection = Vector2.down;

            isDodging = true;
            isInvincible = true;
            lastDodgeTime = Time.time;
            currentStamina -= dodgeStaminaCost;
            OnStaminaChanged?.Invoke(currentStamina);

            var health = GetComponent<PlayerHealth>();
            if (health != null) health.SetInvincible(true);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.4f);

            StartCoroutine(DodgeRoutine());
        }

        private System.Collections.IEnumerator DodgeRoutine()
        {
            yield return new WaitForSeconds(dodgeDuration);
            isDodging = false;
            isInvincible = false;
            rb.linearVelocity = Vector2.zero;

            var health = GetComponent<PlayerHealth>();
            if (health != null) health.SetInvincible(false);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }

        private void HandleAiming()
        {
            if (mainCamera == null) return;

            mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        private void HandleMovement()
        {
            float currentSpeed = moveSpeed;

            if (IsSprinting)
            {
                currentSpeed *= sprintMultiplier;
            }

            rb.linearVelocity = moveInput * currentSpeed;
        }

        private void HandleStamina()
        {
            if (IsSprinting)
            {
                currentStamina -= sprintStaminaCost * Time.deltaTime;
                currentStamina = Mathf.Max(0, currentStamina);
            }
            else if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }

            OnStaminaChanged?.Invoke(currentStamina);
        }

        public void SetCanMove(bool value)
        {
            canMove = value;
            if (!canMove)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void ApplySpeedModifier(float modifier, float duration)
        {
            StartCoroutine(SpeedModifierCoroutine(modifier, duration));
        }

        private System.Collections.IEnumerator SpeedModifierCoroutine(float modifier, float duration)
        {
            float originalSpeed = moveSpeed;
            moveSpeed *= modifier;
            
            yield return new WaitForSeconds(duration);
            
            moveSpeed = originalSpeed;
        }

        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(currentStamina);
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        }
    }
}
