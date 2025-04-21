using UnityEngine;
using RPG.Quests;

public class MerchantQuestCompletion : MonoBehaviour
{
    [SerializeField] private Quest _quest;
    [SerializeField] private string _returnObjectiveRef = "return_to_merchant";
    [SerializeField] private GameObject _questCompletionEffect;
    [SerializeField] private AudioClip _questCompletionSound;

    private bool _isCompleted = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && _questCompletionSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCompleted) return;

        if (other.CompareTag("Player"))
        {
            QuestList questList = other.GetComponent<QuestList>();
            if (questList != null)
            {
                QuestStatus status = questList.GetQuestStatus(_quest);
                if (status != null)
                {
                    // Проверяем, собраны ли все кристаллы
                    bool allCrystalsCollected = 
                        status.IsObjectiveComplete("find_crystal_cave") &&
                        status.IsObjectiveComplete("find_crystal_forest") &&
                        status.IsObjectiveComplete("find_crystal_ruins");

                    if (allCrystalsCollected && !status.IsObjectiveComplete(_returnObjectiveRef))
                    {
                        CompleteQuest(questList);
                    }
                }
            }
        }
    }

    private void CompleteQuest(QuestList questList)
    {
        _isCompleted = true;

        // Завершаем последнюю цель квеста
        questList.CompleteObjective(_quest, _returnObjectiveRef);

        // Воспроизводим эффекты
        if (_questCompletionEffect != null)
        {
            _questCompletionEffect.SetActive(true);
        }

        if (_questCompletionSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_questCompletionSound);
        }
    }
} 