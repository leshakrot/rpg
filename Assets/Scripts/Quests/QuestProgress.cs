using UnityEngine;
using RPG.Quests;

public class QuestProgress : MonoBehaviour
{
    [SerializeField] private QuestList questList;
    [SerializeField] private Quest questReference;
    [SerializeField] private string objectiveReference;
    [SerializeField] private int amount;

    public void AddProgress()
    {
        questList.AddProgress(questReference, objectiveReference, amount);
    }
}
