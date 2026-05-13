using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Abstract base class for all building requirements.
    /// Extensible: new requirement types can be added by inheriting from this class.
    /// </summary>
    public abstract class BuildingRequirement : ScriptableObject
    {
        /// <summary>
        /// Validate if the requirement is met.
        /// </summary>
        /// <param name="skipResourceChecks">If true, skip inventory/currency checks (for free reconstruction)</param>
        /// <returns>True if requirement is met, false otherwise</returns>
        public abstract bool Validate(bool skipResourceChecks = false);
        
        /// <summary>
        /// Get a human-readable description of the requirement.
        /// </summary>
        public abstract string GetDescription();
        
        /// <summary>
        /// Get detailed information about why the requirement failed.
        /// </summary>
        public abstract string GetMissingInfo();
        
        /// <summary>
        /// Deduct resources if this requirement consumes resources.
        /// Called after validation passes.
        /// </summary>
        /// <returns>True if deduction successful, false otherwise</returns>
        public virtual bool DeductResources()
        {
            return true; // Default: no resources to deduct
        }
    }
}
