using UnityEngine;

namespace DeepShift.Weapons
{
    /// <summary>
    /// Read-only data asset for a ranged weapon. Never mutate at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "DeepShift/Weapons/WeaponData")]
    public class WeaponDataSO : ScriptableObject
    {
        /// <summary>Display name shown in the HUD.</summary>
        public string displayName;
        /// <summary>Damage dealt per projectile hit.</summary>
        public int    damage          = 10;
        /// <summary>Shots per second.</summary>
        public float  fireRate        = 2f;
        /// <summary>World-units per second the projectile travels.</summary>
        public float  projectileSpeed = 12f;
        /// <summary>Maximum ammo capacity.</summary>
        public int    maxAmmo         = 12;
    }
}
