using UnityEngine;

namespace RPG.UI.ExperienceText
{
    public class ExperienceTextSpawner : MonoBehaviour
    {
        [SerializeField] private ExperienceText _experienceTextPrefab = null;

        public void Spawn(float experienceAmount)
        {
            ExperienceText instance = Instantiate<ExperienceText>(_experienceTextPrefab, transform);
            instance.SetValue(experienceAmount);
        }
        
        public void SetExperienceTextPrefab(ExperienceText prefab)
        {
            _experienceTextPrefab = prefab;
        }
    }
} 