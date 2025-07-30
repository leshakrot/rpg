using UnityEngine;

namespace RPG.UI.ExperienceText
{
    public class ExperienceTextManager : MonoBehaviour
    {
        [SerializeField] private ExperienceText _experienceTextPrefab = null;
        
        private static ExperienceTextManager instance;
        
        public static ExperienceTextManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ExperienceTextManager>();
                    
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ExperienceTextManager");
                        instance = go.AddComponent<ExperienceTextManager>();
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            // Находим игрока и добавляем ExperienceTextSpawner
            SetupPlayerExperienceTextSpawner();
        }
        
        private void SetupPlayerExperienceTextSpawner()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("ExperienceTextManager: Игрок не найден!");
                return;
            }
            
            // Проверяем, есть ли уже ExperienceTextSpawner
            ExperienceTextSpawner existingSpawner = player.GetComponentInChildren<ExperienceTextSpawner>();
            if (existingSpawner != null)
            {
                Debug.Log("ExperienceTextManager: ExperienceTextSpawner уже существует у игрока");
                return;
            }
            
            // Создаем ExperienceTextSpawner как дочерний объект игрока
            GameObject spawnerGO = new GameObject("Experience Text Spawner");
            spawnerGO.transform.SetParent(player.transform);
            spawnerGO.transform.localPosition = Vector3.zero;
            spawnerGO.transform.localRotation = Quaternion.identity;
            
            ExperienceTextSpawner spawner = spawnerGO.AddComponent<ExperienceTextSpawner>();
            
            // Назначаем префаб
            if (_experienceTextPrefab != null)
            {
                spawner.SetExperienceTextPrefab(_experienceTextPrefab);
            }
            else
            {
                Debug.LogError("ExperienceTextManager: Префаб ExperienceText не назначен!");
            }
            
            Debug.Log("ExperienceTextManager: ExperienceTextSpawner добавлен к игроку");
        }
        
        public void SetExperienceTextPrefab(ExperienceText prefab)
        {
            _experienceTextPrefab = prefab;
        }
    }
} 