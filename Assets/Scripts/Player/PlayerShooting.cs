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

        [Header("Weapon Switching")]
        [SerializeField] private WeaponData[] weaponSlots = new WeaponData[2];
        [SerializeField] private int[] slotAmmo = new int[2];
        [SerializeField] private int[] slotReserve = new int[2];
        private int activeSlot = 0;

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
        public event Action OnEmptyTriggerPulled;
        public event Action<int, int> OnLowAmmoWarning;
        public event Action<WeaponData> OnWeaponChanged;

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

            GenerateProceduralSounds();
            InitializeWeapon();
        }

        private void GenerateProceduralSounds()
        {
            try
            {
                if (shootSound == null)
                    shootSound = Audio.ProceduralAudioGenerator.GenerateGunshot("pistol");
                if (reloadSound == null)
                    reloadSound = Audio.ProceduralAudioGenerator.GenerateReload();
                if (emptyClickSound == null)
                    emptyClickSound = Audio.ProceduralAudioGenerator.GenerateEmptyClick();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerShooting] Failed to generate procedural sounds: {e.Message}");
            }
        }

        private void UpdateWeaponSound()
        {
            if (currentWeapon == null) return;
            try
            {
                string gunType = "pistol";
                if (currentWeapon.pelletsPerShot > 1) gunType = "shotgun";
                else if (currentWeapon.isAutomatic) gunType = "smg";
                else if (currentWeapon.damage >= 40) gunType = "explosion";
                shootSound = Audio.ProceduralAudioGenerator.GenerateGunshot(gunType);
            }
            catch (System.Exception) { }
        }

        private void Update()
        {
            HandleInput();
        }

        private void InitializeWeapon()
        {
            if (currentWeapon != null)
            {
                int magBonus = PlayerUpgrades.Instance != null ? PlayerUpgrades.Instance.MagazineBonus : 0;
                currentAmmo = currentWeapon.magazineSize + magBonus;
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
                    TryFire();
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                    TryFire();
            }

            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
                TryReload();

            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
                SwitchToSlot(activeSlot == 0 ? 1 : 0);
        }

        private void SwitchToSlot(int slot)
        {
            if (slot == activeSlot || isReloading) return;
            if (slot < 0 || slot >= weaponSlots.Length) return;
            if (weaponSlots[slot] == null) return;

            slotAmmo[activeSlot] = currentAmmo;
            slotReserve[activeSlot] = reserveAmmo;

            activeSlot = slot;
            currentWeapon = weaponSlots[slot];
            currentAmmo = slotAmmo[slot];
            reserveAmmo = slotReserve[slot];

            UpdateWeaponSound();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            OnWeaponChanged?.Invoke(currentWeapon);
        }

        public void SetSecondWeapon(WeaponData weapon)
        {
            weaponSlots[1] = weapon;
            if (weapon != null)
            {
                slotAmmo[1] = weapon.magazineSize;
                slotReserve[1] = weapon.magazineSize * 3;
                SwitchToSlot(1);
            }
        }

        private void TryFire()
        {
            if (isReloading) return;

            if (currentAmmo <= 0)
            {
                PlaySound(emptyClickSound);
                OnEmptyTriggerPulled?.Invoke();
                
                if (reserveAmmo > 0)
                {
                    TryReload();
                }
                return;
            }

            float effectiveFireRate = currentWeapon.fireRate;
            if (PlayerUpgrades.Instance != null)
                effectiveFireRate *= PlayerUpgrades.Instance.FireRateMultiplier;
            if (Time.time < lastFireTime + effectiveFireRate) return;

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
            EmitLowAmmoWarningIfNeeded();

            if (currentAmmo <= 0 && reserveAmmo > 0)
            {
                TryReload();
            }
        }

        private void SpawnBullet()
        {
            if (bulletPrefab == null || firePoint == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            
            Vector2 aimDir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
            float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            
            Vector3 spawnPos = (Vector2)transform.position + aimDir * 0.5f;

            float spread = currentWeapon?.spread ?? 0f;
            Quaternion spawnRot = Quaternion.Euler(0, 0, aimAngle - 90f);
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
                bullet.SetActive(true);
                
                var bulletComponent = bullet.GetComponent<Bullet>();
                if (bulletComponent != null)
                {
                    float dmg = currentWeapon.damage;
                    if (PlayerUpgrades.Instance != null)
                        dmg *= PlayerUpgrades.Instance.DamageMultiplier;
                    bulletComponent.Initialize(dmg, currentWeapon.bulletSpeed, currentWeapon.range);
                }
            }

            if (Core.GameEffects.Instance != null)
            {
                float flashScale = 0.35f;
                if ((currentWeapon?.isAutomatic ?? false))
                {
                    flashScale = 0.45f;
                }
                if ((currentWeapon?.pelletsPerShot ?? 1) > 1)
                {
                    flashScale = 0.6f;
                }

                Core.GameEffects.Instance.SpawnMuzzleFlash(spawnPos, spawnRot, flashScale);
                Core.GameEffects.Instance.ScreenShake(0.03f, 0.05f);
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

            int effectiveMag = currentWeapon.magazineSize + (PlayerUpgrades.Instance != null ? PlayerUpgrades.Instance.MagazineBonus : 0);
            int ammoNeeded = effectiveMag - currentAmmo;
            int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

            currentAmmo += ammoToLoad;
            reserveAmmo -= ammoToLoad;

            isReloading = false;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            EmitLowAmmoWarningIfNeeded();
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
            weaponSlots[0] = weapon;
            UpdateWeaponSound();

            if (weapon != null)
            {
                int defaultReserve = weapon.magazineSize * 3;
                reserveAmmo = Mathf.Max(reserveAmmo, defaultReserve);
                currentAmmo = weapon.magazineSize;
                slotAmmo[0] = currentAmmo;
                slotReserve[0] = reserveAmmo;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                OnWeaponChanged?.Invoke(currentWeapon);
                EmitLowAmmoWarningIfNeeded();
            }
        }

        public void ResetLoadout(WeaponData weapon, int reserveMagazines = 3)
        {
            currentWeapon = weapon;

            if (weapon == null)
            {
                currentAmmo = 0;
                reserveAmmo = 0;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
                return;
            }

            reserveMagazines = Mathf.Max(1, reserveMagazines);
            currentAmmo = weapon.magazineSize;
            reserveAmmo = Mathf.Clamp(weapon.magazineSize * reserveMagazines, 0, maxReserveAmmo);
            isReloading = false;

            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            EmitLowAmmoWarningIfNeeded();
        }

        public void SetBulletPrefab(GameObject prefab)
        {
            bulletPrefab = prefab;
        }

        public void SetFirePoint(Transform point)
        {
            firePoint = point;
        }

        private void EmitLowAmmoWarningIfNeeded()
        {
            if (currentWeapon == null)
            {
                return;
            }

            int threshold = Mathf.Max(2, Mathf.RoundToInt(currentWeapon.magazineSize * 0.2f));
            if (currentAmmo <= threshold)
            {
                OnLowAmmoWarning?.Invoke(currentAmmo, reserveAmmo);
            }
        }
    }
}
