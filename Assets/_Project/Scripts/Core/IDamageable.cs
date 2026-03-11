namespace DeepShift.Core
{
    /// <summary>
    /// Implemented by any GameObject that can receive damage from player tools or hazards.
    /// Allows DrillController (and future tools) to deal damage without knowing the concrete type.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Apply <paramref name="amount"/> damage to this object.</summary>
        void TakeDamage(int amount);
    }
}
