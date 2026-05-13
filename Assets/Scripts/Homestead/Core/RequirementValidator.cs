using System.Collections.Generic;
using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Validates building requirements and provides detailed feedback.
    /// </summary>
    public class RequirementValidator
    {
        /// <summary>
        /// Validate all requirements for a building/level.
        /// </summary>
        /// <param name="requirements">Array of requirements to validate</param>
        /// <param name="skipResourceChecks">If true, skip inventory/currency checks</param>
        /// <returns>Validation result with detailed information</returns>
        public RequirementValidationResult ValidateRequirements(BuildingRequirement[] requirements, bool skipResourceChecks = false)
        {
            var result = new RequirementValidationResult
            {
                IsValid = true,
                FailedRequirements = new List<RequirementFailureInfo>()
            };
            
            if (requirements == null || requirements.Length == 0)
            {
                return result; // No requirements = valid
            }
            
            foreach (var requirement in requirements)
            {
                if (requirement == null)
                {
                    Debug.LogWarning("RequirementValidator: Null requirement in array");
                    continue;
                }
                
                bool passed = requirement.Validate(skipResourceChecks);
                
                if (!passed)
                {
                    result.IsValid = false;
                    result.FailedRequirements.Add(new RequirementFailureInfo
                    {
                        requirement = requirement,
                        description = requirement.GetDescription(),
                        missingInfo = requirement.GetMissingInfo()
                    });
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Result of requirement validation.
    /// </summary>
    public class RequirementValidationResult
    {
        public bool IsValid;
        public List<RequirementFailureInfo> FailedRequirements;
        
        public string GetFailureSummary()
        {
            if (IsValid) return "All requirements met";
            
            string summary = "Missing requirements:\n";
            foreach (var failure in FailedRequirements)
            {
                summary += $"• {failure.missingInfo}\n";
            }
            
            return summary;
        }
    }
    
    /// <summary>
    /// Information about a failed requirement.
    /// </summary>
    public class RequirementFailureInfo
    {
        public BuildingRequirement requirement;
        public string description;
        public string missingInfo;
    }
}
