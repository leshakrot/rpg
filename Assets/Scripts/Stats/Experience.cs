using GameDevTV.Saving;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace RPG.Stats
{
    public class Experience : MonoBehaviour, ISaveable
    {
        [SerializeField] private float _experiencePoints = 0;
        [SerializeField] private UnityEvent<float> _gainExperience;

        //public delegate void ExperienceGainedDelegate();
        public event Action onExperienceGained;

        private void Update()
        {
            if (Input.GetKey(KeyCode.E))
            {
	            GainExperience(Time.deltaTime * 10000);
            }
        }

        public void GainExperience(float experience)
        {
            _experiencePoints += experience;
            _gainExperience.Invoke(experience);
            onExperienceGained();
        }

        public object CaptureState()
        {
            return _experiencePoints;
        }

        public void RestoreState(object state)
        {
            _experiencePoints = (float)state;
        }

        public float GetPoints()
        {
            return _experiencePoints;
        }
    }
}
