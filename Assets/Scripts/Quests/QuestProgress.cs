using UnityEngine;
using RPG.Quests;

public class QuestProgress : MonoBehaviour
{
    [SerializeField] private QuestList questList;
    [SerializeField] private Quest questReference; // Параметры для квеста
    [SerializeField] private string objectiveReference; // Параметры для цели
    [SerializeField] private int amount; // Количество прогресса

    // Метод без аргументов, доступный в UnityEvent
    public void AddProgress()
    {
        questList.AddProgress(questReference, objectiveReference, amount);
    }
}
