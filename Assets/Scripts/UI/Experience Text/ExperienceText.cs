using System;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI.ExperienceText
{
    public class ExperienceText : MonoBehaviour
    {
        [SerializeField] private Text _experienceText = null;

        public void DestroyText()
        {
            Destroy(gameObject);
        }

        public void SetValue(float amount)
        {
            _experienceText.text = String.Format("Опыт +{0:0}", amount);
        }
    }
} 