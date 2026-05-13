using UnityEngine;
using RPG.Inventories;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires specific amount of currency.
    /// </summary>
    [CreateAssetMenu(fileName = "New Currency Requirement", menuName = "Homestead/Requirements/Currency", order = 1)]
    public class CurrencyRequirement : BuildingRequirement
    {
        [SerializeField] private float requiredAmount;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (skipResourceChecks) return true;
            
            var purse = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Purse>();
            if (purse == null)
            {
                Debug.LogError("CurrencyRequirement: Player purse not found");
                return false;
            }
            
            return purse.GetBalance() >= requiredAmount;
        }
        
        public override string GetDescription()
        {
            return $"{requiredAmount} Gold";
        }
        
        public override string GetMissingInfo()
        {
            var purse = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Purse>();
            if (purse == null) return "Purse not found";
            
            float currentBalance = purse.GetBalance();
            float missing = Mathf.Max(0, requiredAmount - currentBalance);
            
            return $"Need {missing} more gold (have {currentBalance}/{requiredAmount})";
        }
        
        public override bool DeductResources()
        {
            var purse = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Purse>();
            if (purse == null) return false;
            
            purse.UpdateBalance(-requiredAmount);
            return true;
        }
    }
}
