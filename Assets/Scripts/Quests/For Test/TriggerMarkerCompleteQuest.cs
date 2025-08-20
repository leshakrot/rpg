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
                try
                {
                    questCompletion.CompleteObjective();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"TriggerMarkerCompleteQuest: Error completing objective - {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("TriggerMarkerCompleteQuest: QuestCompletion component not found!");
            }
        }
    }
}
