using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3.1f;
        [SerializeField] private float sprintMultiplier = 1.25f;
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

        private AudioSource footstepSource;
        private AudioClip[] footstepClips;
        private float footstepTimer;
        private float footstepInterval = 0.35f;
        private int footstepStepIndex;

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

            InitFootstepAudio();
        }

        private void InitFootstepAudio()
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.volume = 0.16f;
            footstepSource.spatialBlend = 0f;
            footstepSource.dopplerLevel = 0f;

            try
            {
                footstepClips = new AudioClip[4];
                for (int i = 0; i < 4; i++)
                    footstepClips[i] = Audio.ProceduralAudioGenerator.GenerateFootstep(i);
            }
            catch (System.Exception) { footstepClips = null; }
        }

        private void Update()
        {
            if (!canMove) return;

            HandleInput();
            HandleAiming();
            HandleStamina();
            HandleFootsteps();
        }

        private void HandleFootsteps()
        {
            if (footstepClips == null || footstepSource == null) return;
            float currentSpeed = rb != null ? rb.linearVelocity.magnitude : moveInput.magnitude;
            if (currentSpeed < 0.05f || isDodging)
            {
                footstepTimer = 0f;
                return;
            }

            float maxExpectedSpeed = Mathf.Max(0.01f, moveSpeed * Mathf.Max(1f, sprintMultiplier));
            float speedRatio = Mathf.Clamp01(currentSpeed / maxExpectedSpeed);
            float interval = footstepInterval * Mathf.Lerp(1.2f, 0.62f, speedRatio);
            if (isSprinting)
            {
                interval *= 0.86f;
            }

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                var clip = footstepClips[Random.Range(0, footstepClips.Length)];
                if (clip != null)
                {
                    bool isLeftStep = (footstepStepIndex++ % 2) == 0;
                    float stepVolume = Mathf.Lerp(0.08f, 0.2f, speedRatio);
                    if (isSprinting)
                    {
                        stepVolume += 0.04f;
                    }

                    if (!isLeftStep)
                    {
                        stepVolume *= 0.92f;
                    }

                    footstepSource.pitch = Random.Range(0.9f, 1.04f) + (isSprinting ? 0.02f : 0f);
                    footstepSource.panStereo = isLeftStep
                        ? Random.Range(-0.08f, -0.02f)
                        : Random.Range(0.02f, 0.08f);
                    footstepSource.PlayOneShot(clip, stepVolume);
                }
            }
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
                float effectiveSprintMult = sprintMultiplier;
                if (PlayerUpgrades.Instance != null)
                    effectiveSprintMult += PlayerUpgrades.Instance.SprintBonus;
                currentSpeed *= effectiveSprintMult;
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
        
        private float speedMultiplier = 1f;
        private float baseMoveSpeed = 3.1f;
        
        public void ApplySpeedMultiplier(float multiplier)
        {
            if (baseMoveSpeed == 0f) baseMoveSpeed = moveSpeed;
            speedMultiplier = multiplier;
            moveSpeed = baseMoveSpeed * speedMultiplier;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        }
    }
}
