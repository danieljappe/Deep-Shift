using UnityEngine;

namespace DeepShift.Enemies
{
    /// <summary>
    /// ScriptableObject that configures the threat budget system.
    /// Holds the enemy pool and the per-floor budget scaling formula.
    /// Read-only at runtime — never mutate during a shift.
    /// </summary>
    [CreateAssetMenu(menuName = "DeepShift/Enemies/ThreatBudgetConfig", fileName = "ThreatBudgetConfig")]
    public class ThreatBudgetConfig : ScriptableObject
    {
        [Header("Budget Scaling")]
        /// <summary>Threat points available on floor 1.</summary>
        public int baseBudget = 8;

        /// <summary>
        /// Additional points added per floor beyond floor 1.
        /// Formula: <c>baseBudget + (floorNumber - 1) * budgetPerFloor</c>
        /// Examples: Floor 1 = 8, Floor 2 = 12, Floor 3 = 16.
        /// </summary>
        public int budgetPerFloor = 4;

        [Header("Enemy Pool")]
        /// <summary>All enemy types available for spawning. Order does not matter — selection is randomised.</summary>
        public EnemySpawnDataSO[] enemyPool;

        /// <summary>
        /// Returns the total threat budget for the given floor number.
        /// </summary>
        public int GetBudget(int floorNumber)
        {
            return baseBudget + (floorNumber - 1) * budgetPerFloor;
        }
    }
}
