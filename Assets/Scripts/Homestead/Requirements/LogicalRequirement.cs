using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Combines multiple requirements with AND/OR logic.
    /// Supports nested logical operators.
    /// </summary>
    [CreateAssetMenu(fileName = "New Logical Requirement", menuName = "Homestead/Requirements/Logical (AND/OR)", order = 5)]
    public class LogicalRequirement : BuildingRequirement
    {
        public enum LogicalOperator
        {
            AND,
            OR
        }
        
        [SerializeField] private LogicalOperator logicalOperator = LogicalOperator.AND;
        [SerializeField] private BuildingRequirement[] subRequirements;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                Debug.LogWarning("LogicalRequirement: No sub-requirements defined");
                return true; // Empty logical requirement is considered valid
            }
            
            if (logicalOperator == LogicalOperator.AND)
            {
                // All sub-requirements must pass
                foreach (var requirement in subRequirements)
                {
                    if (requirement == null)
                    {
                        Debug.LogWarning("LogicalRequirement: Null sub-requirement in AND group");
                        continue;
                    }
                    
                    if (!requirement.Validate(skipResourceChecks))
                    {
                        return false;
                    }
                }
                return true;
            }
            else // OR
            {
                // At least one sub-requirement must pass
                foreach (var requirement in subRequirements)
                {
                    if (requirement == null)
                    {
                        Debug.LogWarning("LogicalRequirement: Null sub-requirement in OR group");
                        continue;
                    }
                    
                    if (requirement.Validate(skipResourceChecks))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public override string GetDescription()
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                return "No requirements";
            }
            
            string operatorText = logicalOperator == LogicalOperator.AND ? "AND" : "OR";
            string result = $"({operatorText}): ";
            
            for (int i = 0; i < subRequirements.Length; i++)
            {
                if (subRequirements[i] != null)
                {
                    result += subRequirements[i].GetDescription();
                    if (i < subRequirements.Length - 1)
                    {
                        result += $" {operatorText} ";
                    }
                }
            }
            
            return result;
        }
        
        public override string GetMissingInfo()
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                return "No requirements";
            }
            
            string result = "";
            
            if (logicalOperator == LogicalOperator.AND)
            {
                // Show all failed requirements
                foreach (var requirement in subRequirements)
                {
                    if (requirement != null && !requirement.Validate())
                    {
                        if (result.Length > 0) result += "\n";
                        result += "✗ " + requirement.GetMissingInfo();
                    }
                }
                
                if (string.IsNullOrEmpty(result))
                {
                    result = "All requirements met";
                }
            }
            else // OR
            {
                // Show all requirements with status
                bool anyMet = false;
                foreach (var requirement in subRequirements)
                {
                    if (requirement != null)
                    {
                        bool met = requirement.Validate();
                        if (met) anyMet = true;
                        
                        if (result.Length > 0) result += "\n";
                        result += (met ? "✓ " : "✗ ") + requirement.GetDescription();
                    }
                }
                
                if (anyMet)
                {
                    result = "At least one requirement met:\n" + result;
                }
                else
                {
                    result = "Need at least one:\n" + result;
                }
            }
            
            return result;
        }
        
        public override bool DeductResources()
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                return true;
            }
            
            // For AND: deduct from all requirements
            // For OR: deduct only from requirements that passed validation
            foreach (var requirement in subRequirements)
            {
                if (requirement == null) continue;
                
                if (logicalOperator == LogicalOperator.AND)
                {
                    if (!requirement.DeductResources())
                    {
                        return false;
                    }
                }
                else // OR
                {
                    // Only deduct from the first requirement that validates
                    if (requirement.Validate())
                    {
                        return requirement.DeductResources();
                    }
                }
            }
            
            return true;
        }
    }
}
