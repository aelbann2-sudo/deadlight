using UnityEngine;

namespace Deadlight.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

        [Header("Bounds")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX = -50f;
        [SerializeField] private float maxX = 50f;
        [SerializeField] private float minY = -50f;
        [SerializeField] private float maxY = 50f;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 0f;
        [SerializeField] private float shakeIntensity = 0.1f;

        private Vector3 shakeOffset;

        private void Start()
        {
            if (autoFindPlayer && target == null)
            {
                FindPlayer();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (autoFindPlayer) FindPlayer();
                return;
            }

            Vector3 desiredPosition = target.position + offset;

            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            if (shakeDuration > 0)
            {
                UpdateShake();
                smoothedPosition += shakeOffset;
            }

            transform.position = smoothedPosition;
        }

        private void FindPlayer()
        {
            var player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void UpdateShake()
        {
            shakeDuration -= Time.deltaTime;

            if (shakeDuration > 0)
            {
                shakeOffset = Random.insideUnitSphere * shakeIntensity;
                shakeOffset.z = 0;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        public void Shake(float duration, float intensity)
        {
            shakeDuration = duration;
            shakeIntensity = intensity;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            useBounds = true;
        }

        public void DisableBounds()
        {
            useBounds = false;
        }

        public void SetSmoothSpeed(float speed)
        {
            smoothSpeed = speed;
        }
    }
}
