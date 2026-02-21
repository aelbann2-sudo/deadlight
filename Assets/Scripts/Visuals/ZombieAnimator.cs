using UnityEngine;

namespace Deadlight.Visuals
{
    public enum ZombieAnimState { Idle, Walking, Attacking, Hit, Dying, Dead }

    public class ZombieAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private ZombieAnimState currentState = ZombieAnimState.Idle;
        private float animTimer;
        private int currentFrame;
        private float stateTimer;
        private ProceduralSpriteGenerator.ZombieType zombieType = ProceduralSpriteGenerator.ZombieType.Basic;
        private float scaleVariation = 1f;
        private Color tintVariation = Color.white;

        private static readonly int[] framesPerState = { 2, 4, 4, 2, 3, 1 };
        private static readonly float[] frameDurations = { 0.5f, 0.15f, 0.1f, 0.15f, 0.2f, 0f };

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            ApplyVisualVariety();
        }

        private void ApplyVisualVariety()
        {
            scaleVariation = Random.Range(0.85f, 1.15f);
            transform.localScale = Vector3.one * scaleVariation;

            float hueShift = Random.Range(-0.05f, 0.05f);
            tintVariation = new Color(1f, 1f + hueShift, 1f);

            if (spriteRenderer != null)
                spriteRenderer.color = tintVariation;
        }

        public void SetZombieType(ProceduralSpriteGenerator.ZombieType type)
        {
            zombieType = type;
            UpdateSprite();
        }

        private void Update()
        {
            int stateIndex = (int)currentState;
            float duration = frameDurations[stateIndex];
            int frameCount = framesPerState[stateIndex];

            animTimer += Time.deltaTime;

            if (duration > 0f && animTimer >= duration)
            {
                animTimer -= duration;
                currentFrame++;

                if (currentFrame >= frameCount)
                {
                    switch (currentState)
                    {
                        case ZombieAnimState.Hit:
                            SetState(ZombieAnimState.Idle);
                            break;
                        case ZombieAnimState.Dying:
                            SetState(ZombieAnimState.Dead);
                            break;
                        case ZombieAnimState.Attacking:
                            SetState(ZombieAnimState.Idle);
                            break;
                        default:
                            currentFrame = 0;
                            break;
                    }
                }
            }

            UpdateSprite();

            if (currentState != ZombieAnimState.Hit &&
                currentState != ZombieAnimState.Dying &&
                currentState != ZombieAnimState.Dead &&
                currentState != ZombieAnimState.Attacking &&
                rb != null)
            {
                float speed = rb.linearVelocity.magnitude;
                SetState(speed > 0.3f ? ZombieAnimState.Walking : ZombieAnimState.Idle);
            }
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null || rb == null) return;

            Vector2 vel = rb.linearVelocity;
            int direction;

            if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
                direction = vel.x < 0 ? 2 : 3;
            else
                direction = vel.y > 0 ? 1 : 0;

            spriteRenderer.sprite = ProceduralSpriteGenerator.CreateZombieSprite(zombieType, direction, currentFrame);
            spriteRenderer.color = tintVariation;
            spriteRenderer.flipX = vel.x < 0;
        }

        private void SetState(ZombieAnimState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            currentFrame = 0;
            animTimer = 0f;
        }

        public void PlayAttack()
        {
            SetState(ZombieAnimState.Attacking);
        }

        public void PlayHit()
        {
            SetState(ZombieAnimState.Hit);
        }

        public void PlayDeath()
        {
            SetState(ZombieAnimState.Dying);
        }

        public bool IsAnimationDone()
        {
            return currentState == ZombieAnimState.Dead;
        }
    }
}
