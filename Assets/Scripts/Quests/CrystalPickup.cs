using UnityEngine;
using RPG.Quests;

public class CrystalPickup : MonoBehaviour
{
    [SerializeField] private Quest _quest;
    [SerializeField] private string _objectiveReference;
    [SerializeField] private GameObject _crystalVisual;
    [SerializeField] private ParticleSystem _pickupEffect;
    [SerializeField] private AudioClip _pickupSound;

    private bool _isPickedUp = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && _pickupSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isPickedUp) return;
        
        if (other.CompareTag("Player"))
        {
            QuestList questList = other.GetComponent<QuestList>();
            if (questList != null)
            {
                QuestStatus status = questList.GetQuestStatus(_quest);
                if (status != null && !status.IsObjectiveComplete(_objectiveReference))
                {
                    PickupCrystal(questList);
                }
            }
        }
    }

    private void PickupCrystal(QuestList questList)
    {
        _isPickedUp = true;
        
        // Завершаем цель квеста
        questList.CompleteObjective(_quest, _objectiveReference);

        // Воспроизводим эффекты
        if (_pickupEffect != null)
        {
            _pickupEffect.Play();
        }

        if (_pickupSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_pickupSound);
        }

        // Скрываем визуальную модель кристалла
        if (_crystalVisual != null)
        {
            _crystalVisual.SetActive(false);
        }

        // Уничтожаем объект после проигрывания эффектов
        float destroyDelay = 0f;
        if (_pickupEffect != null)
        {
            destroyDelay = _pickupEffect.main.duration;
        }
        if (_pickupSound != null)
        {
            destroyDelay = Mathf.Max(destroyDelay, _pickupSound.length);
        }
        
        Destroy(gameObject, destroyDelay);
    }
} 