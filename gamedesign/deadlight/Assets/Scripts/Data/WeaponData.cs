using UnityEngine;

namespace Deadlight.Data
{
    public enum WeaponType
    {
        Pistol,
        Shotgun,
        AssaultRifle,
        SMG,
        GrenadeLauncher,
        Flamethrower,
        PlasmaCutter
    }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Deadlight/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponName = "New Weapon";
        [TextArea] public string description;
        public WeaponType weaponType;
        public Sprite icon;
        public GameObject prefab;

        [Header("Combat Stats")]
        [Tooltip("Damage per bullet/pellet")]
        public float damage = 10f;
        
        [Tooltip("Time between shots in seconds")]
        public float fireRate = 0.5f;
        
        [Tooltip("Maximum bullet travel distance")]
        public float range = 20f;
        
        [Tooltip("Bullet travel speed")]
        public float bulletSpeed = 30f;

        [Header("Ammo")]
        [Tooltip("Bullets per magazine")]
        public int magazineSize = 12;
        
        [Tooltip("Time to reload in seconds")]
        public float reloadTime = 1.5f;

        [Header("Spread & Accuracy")]
        [Tooltip("Accuracy spread in degrees (0 = perfectly accurate)")]
        public float spread = 2f;
        
        [Tooltip("Number of pellets per shot (for shotguns)")]
        public int pelletsPerShot = 1;

        [Header("Behavior")]
        [Tooltip("Can hold fire button to continuously shoot")]
        public bool isAutomatic = false;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;

        [Header("Visual Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject bulletTrailPrefab;
        public GameObject impactEffectPrefab;

        [Header("Unlock Requirements")]
        public int nightRequired = 1;
        public int pointCost = 0;

        public static WeaponData CreatePistol()
        {
            var weapon = CreateInstance<WeaponData>();
            weapon.weaponName = "Pistol";
            weapon.description = "Reliable sidearm. Accurate with moderate damage.";
            weapon.weaponType = WeaponType.Pistol;
            weapon.damage = 15f;
            weapon.fireRate = 0.3f;
            weapon.range = 25f;
            weapon.bulletSpeed = 35f;
            weapon.magazineSize = 12;
            weapon.reloadTime = 1.2f;
            weapon.spread = 2f;
            weapon.isAutomatic = false;
            weapon.nightRequired = 1;
            weapon.pointCost = 0;
            return weapon;
        }

        public static WeaponData CreateShotgun()
        {
            var weapon = CreateInstance<WeaponData>();
            weapon.weaponName = "Shotgun";
            weapon.description = "Devastating at close range. Fires multiple pellets.";
            weapon.weaponType = WeaponType.Shotgun;
            weapon.damage = 8f;
            weapon.fireRate = 0.8f;
            weapon.range = 12f;
            weapon.bulletSpeed = 25f;
            weapon.magazineSize = 6;
            weapon.reloadTime = 2.5f;
            weapon.spread = 15f;
            weapon.pelletsPerShot = 8;
            weapon.isAutomatic = false;
            weapon.nightRequired = 1;
            weapon.pointCost = 100;
            return weapon;
        }

        public static WeaponData CreateAssaultRifle()
        {
            var weapon = CreateInstance<WeaponData>();
            weapon.weaponName = "Assault Rifle";
            weapon.description = "Automatic rifle. Good balance of damage and fire rate.";
            weapon.weaponType = WeaponType.AssaultRifle;
            weapon.damage = 12f;
            weapon.fireRate = 0.12f;
            weapon.range = 30f;
            weapon.bulletSpeed = 40f;
            weapon.magazineSize = 30;
            weapon.reloadTime = 2f;
            weapon.spread = 4f;
            weapon.isAutomatic = true;
            weapon.nightRequired = 2;
            weapon.pointCost = 200;
            return weapon;
        }

        public static WeaponData CreateSMG()
        {
            var weapon = CreateInstance<WeaponData>();
            weapon.weaponName = "SMG";
            weapon.description = "High fire rate, lower damage. Great for crowds.";
            weapon.weaponType = WeaponType.SMG;
            weapon.damage = 8f;
            weapon.fireRate = 0.08f;
            weapon.range = 18f;
            weapon.bulletSpeed = 30f;
            weapon.magazineSize = 40;
            weapon.reloadTime = 1.8f;
            weapon.spread = 6f;
            weapon.isAutomatic = true;
            weapon.nightRequired = 2;
            weapon.pointCost = 150;
            return weapon;
        }

        public static WeaponData CreateGrenadeLauncher()
        {
            var weapon = CreateInstance<WeaponData>();
            weapon.weaponName = "Grenade Launcher";
            weapon.description = "Explosive area damage. Slow but devastating.";
            weapon.weaponType = WeaponType.GrenadeLauncher;
            weapon.damage = 50f;
            weapon.fireRate = 1.5f;
            weapon.range = 20f;
            weapon.bulletSpeed = 15f;
            weapon.magazineSize = 4;
            weapon.reloadTime = 3f;
            weapon.spread = 1f;
            weapon.isAutomatic = false;
            weapon.nightRequired = 3;
            weapon.pointCost = 350;
            return weapon;
        }

        public static WeaponData CreateFlamethrower()
        {
            var weapon = CreateInstance<WeaponData>();
            weapon.weaponName = "Flamethrower";
            weapon.description = "Continuous fire damage. Burns enemies over time.";
            weapon.weaponType = WeaponType.Flamethrower;
            weapon.damage = 5f;
            weapon.fireRate = 0.05f;
            weapon.range = 8f;
            weapon.bulletSpeed = 12f;
            weapon.magazineSize = 100;
            weapon.reloadTime = 3.5f;
            weapon.spread = 15f;
            weapon.isAutomatic = true;
            weapon.nightRequired = 4;
            weapon.pointCost = 400;
            return weapon;
        }
    }
}
