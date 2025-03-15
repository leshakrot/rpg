using RPG.Control;
using RPG.Quests;
using UnityEngine;

public class TriggerMarkerCompleteQuest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PlayerController player))
        {
            if (TryGetComponent(out QuestCompletion questCompletion))
            {
                questCompletion.CompleteObjective();
            }

        }
    }
}
