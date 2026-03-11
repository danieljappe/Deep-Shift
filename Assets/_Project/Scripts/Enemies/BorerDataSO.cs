using UnityEngine;

namespace DeepShift.Enemies
{
    /// <summary>
    /// Read-only runtime data asset for the Borer enemy.
    /// All numeric tuning lives here — never mutate this during a run.
    /// </summary>
    [CreateAssetMenu(menuName = "DeepShift/Enemies/BorerData", fileName = "BorerData")]
    public class BorerDataSO : ScriptableObject
    {
        [Header("Combat")]
        /// <summary>Starting hit points.</summary>
        public int hp = 3;

        /// <summary>Damage dealt per lunge contact.</summary>
        public int damage = 5;

        [Header("Movement")]
        /// <summary>World-units per second in AGGRO state.</summary>
        public float moveSpeed = 4f;

        /// <summary>World-unit distance at which a lunge triggers.</summary>
        public float lungeRange = 1.2f;

        /// <summary>Seconds between successive lunges.</summary>
        public float lungeCooldown = 0.8f;

        [Header("Alert")]
        /// <summary>Duration of the ALERT state before transitioning to AGGRO.</summary>
        public float alertDuration = 0.5f;

        [Header("Vibration")]
        /// <summary>Manhattan tile radius that triggers ALERT immediately on a drill impact.</summary>
        public int innerVibrationRadius = 3;

        /// <summary>Manhattan tile radius in which drill impacts accumulate toward the threshold.</summary>
        public int outerVibrationRadius = 5;

        /// <summary>Number of outer-zone hits required to trigger ALERT.</summary>
        public int vibrationThreshold = 3;

        /// <summary>Seconds between each automatic -1 decay of the vibration counter while IDLE.</summary>
        public float vibrationDecayInterval = 3f;

        [Header("De-Aggro")]
        /// <summary>Tile distance beyond which the de-aggro timer starts.</summary>
        public float deAggroRadius = 5f;

        /// <summary>Seconds the player must stay beyond <see cref="deAggroRadius"/> before the Borer retreats.</summary>
        public float deAggroDelay = 3f;

        [Header("Hoist Swarm")]
        /// <summary>Tile radius within which a HoistCalled event activates swarm behaviour.</summary>
        public int hoistSwarmRadius = 8;

        [Header("Drops")]
        /// <summary>0–1 probability of dropping a chitin shard on death.</summary>
        [Range(0f, 1f)]
        public float chitinShardDropChance = 0.15f;

        [Header("Threat Budget")]
        /// <summary>Cost in the floor threat budget when this enemy is spawned.</summary>
        public int threatBudgetCost = 1;
    }
}
