using UnityEngine;
using Deadlight.Core;
using Deadlight.Data;
using System;

namespace Deadlight.Player
{
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private WeaponData currentWeapon;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Ammo")]
        [SerializeField] private int currentAmmo;
        [SerializeField] private int reserveAmmo;
        [SerializeField] private int maxReserveAmmo = 200;

        [Header("State")]
        [SerializeField] private bool isReloading = false;
        [SerializeField] private float lastFireTime = 0f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptyClickSound;

        public WeaponData CurrentWeapon => currentWeapon;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;
        public bool CanFire => !isReloading && currentAmmo > 0 && Time.time >= lastFireTime + (currentWeapon?.fireRate ?? 0.5f);

        public event Action<int, int> OnAmmoChanged;
        public event Action OnWeaponFired;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            InitializeWeapon();
        }

        private void Update()
        {
            HandleInput();
        }

        private void InitializeWeapon()
        {
            if (currentWeapon != null)
            {
                currentAmmo = currentWeapon.magazineSize;
                reserveAmmo = currentWeapon.magazineSize * 3;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            }
        }

        private void HandleInput()
        {
            if (currentWeapon == null) return;

            if (currentWeapon.isAutomatic)
            {
                if (Input.GetMouseButton(0))
                {
                    TryFire();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    TryFire();
                }
            }

            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                TryReload();
            }
        }

        private void TryFire()
        {
            if (isReloading) return;

            if (currentAmmo <= 0)
            {
                PlaySound(emptyClickSound);
                
                if (reserveAmmo > 0)
                {
                    TryReload();
                }
                return;
            }

            if (Time.time < lastFireTime + currentWeapon.fireRate) return;

            Fire();
        }

        private void Fire()
        {
            lastFireTime = Time.time;
            currentAmmo--;

            SpawnBullet();

            PlaySound(shootSound ?? currentWeapon.fireSound);
            OnWeaponFired?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

            if (currentAmmo <= 0 && reserveAmmo > 0)
            {
                TryReload();
            }
        }

        private void SpawnBullet()
        {
            if (bulletPrefab == null || firePoint == null) return;

            Vector3 spawnPos = firePoint.position;
            Quaternion spawnRot = firePoint.rotation;

            float spread = currentWeapon?.spread ?? 0f;
            if (spread > 0)
            {
                float randomAngle = UnityEngine.Random.Range(-spread, spread);
                spawnRot *= Quaternion.Euler(0, 0, randomAngle);
            }

            int pellets = currentWeapon?.pelletsPerShot ?? 1;
            for (int i = 0; i < pellets; i++)
            {
                Quaternion pelletRot = spawnRot;
                if (pellets > 1)
                {
                    float pelletSpread = UnityEngine.Random.Range(-spread, spread);
                    pelletRot *= Quaternion.Euler(0, 0, pelletSpread);
                }

                GameObject bullet = Instantiate(bulletPrefab, spawnPos, pelletRot);
                
                var bulletComponent = bullet.GetComponent<Bullet>();
                if (bulletComponent != null)
                {
                    bulletComponent.Initialize(currentWeapon.damage, currentWeapon.bulletSpeed, currentWeapon.range);
                }
            }
        }

        private void TryReload()
        {
            if (isReloading) return;
            if (currentAmmo >= currentWeapon.magazineSize) return;
            if (reserveAmmo <= 0) return;

            StartCoroutine(ReloadCoroutine());
        }

        private System.Collections.IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReloadStarted?.Invoke();
            PlaySound(reloadSound ?? currentWeapon.reloadSound);

            yield return new WaitForSeconds(currentWeapon.reloadTime);

            int ammoNeeded = currentWeapon.magazineSize - currentAmmo;
            int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

            currentAmmo += ammoToLoad;
            reserveAmmo -= ammoToLoad;

            isReloading = false;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void AddAmmo(int amount)
        {
            reserveAmmo = Mathf.Min(reserveAmmo + amount, maxReserveAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }

        public void SetWeapon(WeaponData weapon)
        {
            currentWeapon = weapon;
            
            if (weapon != null)
            {
                currentAmmo = weapon.magazineSize;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            }
        }

        public void SetBulletPrefab(GameObject prefab)
        {
            bulletPrefab = prefab;
        }

        public void SetFirePoint(Transform point)
        {
            firePoint = point;
        }
    }
}
