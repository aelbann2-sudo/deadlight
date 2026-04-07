using UnityEngine;
using Deadlight.Core;
using Deadlight.Data;
using Deadlight.Systems;
using System;

namespace Deadlight.Player
{
    public class PlayerShooting : MonoBehaviour
    {
        private const int MaxWeaponSlots = 4;

        [Header("Weapon Settings")]
        [SerializeField] private WeaponData currentWeapon;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Weapon Switching")]
        [SerializeField] private WeaponData[] weaponSlots = new WeaponData[MaxWeaponSlots];
        [SerializeField] private int[] slotAmmo = new int[MaxWeaponSlots];
        [SerializeField] private int[] slotReserve = new int[MaxWeaponSlots];
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
        public int ActiveSlot => activeSlot;
        public int SlotCount => MaxWeaponSlots;
        public WeaponData GetSlotWeapon(int slot) =>
            (slot >= 0 && slot < weaponSlots.Length) ? weaponSlots[slot] : null;

        public event Action<int, int> OnAmmoChanged;
        public event Action OnWeaponFired;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action OnEmptyTriggerPulled;
        public event Action<int, int> OnLowAmmoWarning;
        public event Action<WeaponData> OnWeaponChanged;

        private void Awake()
        {
            EnsureSlotCapacity();
        }

        private void OnValidate()
        {
            EnsureSlotCapacity();
        }

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
                activeSlot = 0;
                weaponSlots[0] = currentWeapon;
                slotAmmo[0] = currentAmmo;
                slotReserve[0] = reserveAmmo;
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
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSlot(3);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f)
            {
                int next = GetNextOccupiedSlot(activeSlot, 1);
                if (next >= 0) SwitchToSlot(next);
            }
            else if (scroll < -0.01f)
            {
                int prev = GetNextOccupiedSlot(activeSlot, -1);
                if (prev >= 0) SwitchToSlot(prev);
            }
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
            SetWeaponInSlot(1, weapon, true);
        }

        public bool HasFreeWeaponSlot(int startSlot = 1)
        {
            return FindFirstEmptySlot(startSlot) >= 0;
        }

        public bool TryAddWeaponToLoadout(WeaponData weapon, bool switchToWeapon = true, int startSlot = 1)
        {
            if (weapon == null)
            {
                return false;
            }

            int slot = FindFirstEmptySlot(startSlot);
            if (slot < 0)
            {
                return false;
            }

            SetWeaponInSlot(slot, weapon, switchToWeapon);
            return true;
        }

        public bool HasWeaponType(WeaponType weaponType)
        {
            if (currentWeapon != null && currentWeapon.weaponType == weaponType)
            {
                return true;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null && weaponSlots[i].weaponType == weaponType)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryFire()
        {
            if (isReloading) return;
            bool hasInfiniteAmmo = PowerupSystem.Instance != null && PowerupSystem.Instance.HasInfiniteAmmo;

            if (currentAmmo <= 0 && !hasInfiniteAmmo)
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
            bool hasInfiniteAmmo = PowerupSystem.Instance != null && PowerupSystem.Instance.HasInfiniteAmmo;
            if (!hasInfiniteAmmo)
            {
                currentAmmo = Mathf.Max(0, currentAmmo - 1);
            }

            SpawnBullet();

            PlaySound(shootSound ?? currentWeapon.fireSound);
            OnWeaponFired?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            EmitLowAmmoWarningIfNeeded();

            if (!hasInfiniteAmmo && currentAmmo <= 0 && reserveAmmo > 0)
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
                    if (PowerupSystem.Instance != null)
                        dmg *= PowerupSystem.Instance.DamageMultiplier;
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
            activeSlot = 0;
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
            activeSlot = 0;

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                weaponSlots[i] = null;
                slotAmmo[i] = 0;
                slotReserve[i] = 0;
            }

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
            weaponSlots[0] = weapon;
            slotAmmo[0] = currentAmmo;
            slotReserve[0] = reserveAmmo;
            isReloading = false;

            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            OnWeaponChanged?.Invoke(currentWeapon);
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

        private void EnsureSlotCapacity()
        {
            weaponSlots = ResizeArray(weaponSlots, MaxWeaponSlots);
            slotAmmo = ResizeArray(slotAmmo, MaxWeaponSlots);
            slotReserve = ResizeArray(slotReserve, MaxWeaponSlots);
            activeSlot = Mathf.Clamp(activeSlot, 0, MaxWeaponSlots - 1);
        }

        private static T[] ResizeArray<T>(T[] source, int size)
        {
            if (source != null && source.Length == size)
            {
                return source;
            }

            var resized = new T[size];
            if (source != null && source.Length > 0)
            {
                Array.Copy(source, resized, Mathf.Min(source.Length, size));
            }

            return resized;
        }

        private int FindFirstEmptySlot(int startSlot)
        {
            int first = Mathf.Clamp(startSlot, 0, weaponSlots.Length - 1);
            for (int i = first; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetWeaponInSlot(int slot, WeaponData weapon, bool switchToWeapon)
        {
            if (weapon == null) return;
            if (slot < 0 || slot >= weaponSlots.Length) return;

            weaponSlots[slot] = weapon;
            slotAmmo[slot] = weapon.magazineSize;
            slotReserve[slot] = weapon.magazineSize * 3;

            if (switchToWeapon)
            {
                SwitchToSlot(slot);
            }
        }

        private int GetNextOccupiedSlot(int fromSlot, int direction)
        {
            if (weaponSlots == null || weaponSlots.Length == 0)
            {
                return -1;
            }

            int dir = direction >= 0 ? 1 : -1;
            int len = weaponSlots.Length;

            for (int step = 1; step <= len; step++)
            {
                int idx = (fromSlot + dir * step) % len;
                if (idx < 0) idx += len;
                if (weaponSlots[idx] != null)
                {
                    return idx;
                }
            }

            return -1;
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
