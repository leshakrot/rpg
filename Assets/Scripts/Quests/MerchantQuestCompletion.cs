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
    private RandomizedAudioSource _randomizedAudioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && _questCompletionSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _randomizedAudioSource = GetComponent<RandomizedAudioSource>();
        if (_randomizedAudioSource == null && _audioSource != null)
        {
            _randomizedAudioSource = gameObject.AddComponent<RandomizedAudioSource>();
            _randomizedAudioSource.audioSource = _audioSource;
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

        if (_questCompletionSound != null && _randomizedAudioSource != null)
        {
            _randomizedAudioSource.PlayOneShot(_questCompletionSound);
        }
    }
} 