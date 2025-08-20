using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace RPG.Control
{
    [CreateAssetMenu(fileName = "New NPC Profession", menuName = "RPG/NPC/Profession")]
    public class NPCProfession : ScriptableObject
    {
        [Header("Profession Info")]
        public string professionName = "Житель";
        public string description = "Обычный житель деревни";
        
        [Header("Work Activities")]
        public string workActivityName = "Работает";
        [TextArea(3, 5)]
        public string workDescription = "Выполняет обычную работу";
        
        [Header("Default Schedule")]
        public List<ProfessionActivity> defaultSchedule = new List<ProfessionActivity>();
        
        [Header("Work Animations")]
        public string workAnimationTrigger = "Work";
        public string idleAnimationTrigger = "Idle";
        
        [Header("Audio")]
        public AudioClip[] workSounds;
        public AudioClip[] idleSounds;
    }

    [System.Serializable]
    public class ProfessionActivity
    {
        public string activityName = "Работает";
        public int startHour = 8;
        public int startMinute = 0;
        public float durationMinutes = 60f;
        public ActivityType activityType = ActivityType.Work;
        [TextArea(2, 4)]
        public string description = "Описание активности";
    }

    public enum ActivityType
    {
        Work,
        Eat,
        Sleep,
        Socialize,
        Patrol,
        Custom
    }
}