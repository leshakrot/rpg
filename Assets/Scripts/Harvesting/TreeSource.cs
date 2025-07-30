using UnityEngine;
using RPG.Control;
using RPG.Stats;

namespace RPG.Harvesting
{
    public class TreeSource : HarvestableSource
    {
        [Header("Настройки дерева")]
        [SerializeField] private string harvestAnimationTrigger = "StartChopping";
        [SerializeField] private string stopAnimationTrigger = "StopChopping";
        
        private Animator playerAnimator;
        
        protected override void StartHarvestAnimation()
        {
            if (currentPlayer != null)
            {
                playerAnimator = currentPlayer.GetComponent<Animator>();
                if (playerAnimator != null && !string.IsNullOrEmpty(harvestAnimationTrigger))
                {
                    playerAnimator.SetTrigger(harvestAnimationTrigger);
                    
                    // Если есть булевый параметр для продолжительной анимации
                    if (HasAnimatorParameter(playerAnimator, "IsChopping"))
                    {
                        playerAnimator.SetBool("IsChopping", true);
                    }
                }
                
                // Поворачиваем игрока к дереву
                Vector3 directionToTree = (transform.position - currentPlayer.transform.position).normalized;
                if (directionToTree != Vector3.zero)
                {
                    currentPlayer.transform.rotation = Quaternion.LookRotation(directionToTree);
                }
            }
        }
        
        protected override void StopHarvestAnimation()
        {
            if (playerAnimator != null)
            {
                if (!string.IsNullOrEmpty(stopAnimationTrigger))
                {
                    playerAnimator.SetTrigger(stopAnimationTrigger);
                }
                
                // Останавливаем булевый параметр если он есть
                if (HasAnimatorParameter(playerAnimator, "IsChopping"))
                {
                    playerAnimator.SetBool("IsChopping", false);
                }
                
                playerAnimator = null;
            }
        }
        
        private bool HasAnimatorParameter(Animator animator, string paramName)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
        
        protected override void HarvestSingleUnit()
        {
            base.HarvestSingleUnit();
            
            // Дополнительные эффекты для рубки дерева
            if (resource != null)
            {
                Debug.Log($"Срублена древесина: {resource.ResourceName}. Осталось: {remainingResources}");
            }
            
            // Можно добавить специфичные для дерева эффекты
            // например, падающие листья, стружка и т.д.
            CreateWoodChips();
        }
        
        private void CreateWoodChips()
        {
            // Простейший эффект стружки
            if (resource.HarvestEffect != null)
            {
                var effect = Instantiate(resource.HarvestEffect, transform.position + Vector3.up, Quaternion.identity);
                Destroy(effect.gameObject, 2f);
            }
        }
        
        public override CursorType GetCursorType()
        {
            if (isDepleted)
            {
                return CursorType.None;
            }
            
            // Проверяем доступность добычи для текущего игрока
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                int playerLevel = player.GetComponent<BaseStats>().GetLevel();
                var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();
                
                if (CanStartHarvesting(playerLevel, inventory))
                {
                    return CursorType.Harvesting;  // Зеленый курсор - можно рубить
                }
                else
                {
                    return CursorType.HarvestingBlocked;  // Красный курсор - нужен топор/уровень
                }
            }
            
            // Если нет игрока, показываем обычный курсор добычи
            return CursorType.Harvesting;
        }
    }
} 