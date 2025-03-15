using RPG.Control;
using RPG.Quests;
using UnityEngine;

public class TriggerMarkerStartQuest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent(out PlayerController player))
        {
            if(TryGetComponent(out QuestGiver questGiver))
            {
                questGiver.GiveQuest();
            }
            
        }
    }
}
