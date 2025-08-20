using UnityEngine;

namespace RPG.Control
{
    /// <summary>
    /// Компонент с конкретными действиями для специализированных NPC
    /// Присоединяется к NPC для определения их уникальных действий
    /// </summary>
    public class SpecificNPCActions : MonoBehaviour
    {
        [Header("Fisherman Actions")]
        public Transform fishingSpot;
        public GameObject fishingRod;
        public AudioClip[] fishingSounds;

        [Header("Blacksmith Actions")]
        public Transform anvil;
        public GameObject hammer;
        public ParticleSystem sparks;
        public AudioClip[] hammingSounds;

        [Header("Merchant Actions")]
        public Transform shopCounter;
        public GameObject[] goodsToShow;
        public AudioClip[] tradingSounds;

        [Header("Farmer Actions")]
        public Transform[] farmingSpots;
        public GameObject[] farmingTools;
        public AudioClip[] farmingSounds;

        [Header("Guard Actions")]
        public Transform[] patrolPoints;
        public GameObject weapon;
        public AudioClip[] alertSounds;

        private AudioSource _audioSource;
        private Animator _animator;
        private PeacefulNPC _npc;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _animator = GetComponent<Animator>();
            _npc = GetComponent<PeacefulNPC>();
        }

        #region Fisherman Actions
        public void StartFishing()
        {
            Debug.Log($"{_npc.NPCName} начинает ловить рыбу");
            
            if (fishingSpot != null)
            {
                transform.LookAt(fishingSpot);
            }
            
            if (fishingRod != null)
            {
                fishingRod.SetActive(true);
            }
            
            if (_animator != null)
            {
                _animator.SetTrigger("StartFishing");
                _animator.SetBool("IsFishing", true);
            }
            
            PlayRandomSound(fishingSounds);
        }

        public void StopFishing()
        {
            Debug.Log($"{_npc.NPCName} заканчивает рыбалку");
            
            if (fishingRod != null)
            {
                fishingRod.SetActive(false);
            }
            
            if (_animator != null)
            {
                _animator.SetBool("IsFishing", false);
            }
        }

        public void CatchFish()
        {
            Debug.Log($"{_npc.NPCName} поймал рыбу!");
            
            if (_animator != null)
            {
                _animator.SetTrigger("CatchFish");
            }
            
            PlayRandomSound(fishingSounds);
        }
        #endregion

        #region Blacksmith Actions
        public void StartForging()
        {
            Debug.Log($"{_npc.NPCName} начинает ковать");
            
            if (anvil != null)
            {
                transform.LookAt(anvil);
            }
            
            if (hammer != null)
            {
                hammer.SetActive(true);
            }
            
            if (sparks != null)
            {
                sparks.Play();
            }
            
            if (_animator != null)
            {
                _animator.SetTrigger("StartForging");
                _animator.SetBool("IsForging", true);
            }
            
            PlayRandomSound(hammingSounds);
        }

        public void StopForging()
        {
            Debug.Log($"{_npc.NPCName} заканчивает ковку");
            
            if (hammer != null)
            {
                hammer.SetActive(false);
            }
            
            if (sparks != null)
            {
                sparks.Stop();
            }
            
            if (_animator != null)
            {
                _animator.SetBool("IsForging", false);
            }
        }

        public void HitAnvil()
        {
            Debug.Log($"{_npc.NPCName} бьет по наковальне");
            
            if (_animator != null)
            {
                _animator.SetTrigger("HitAnvil");
            }
            
            if (sparks != null)
            {
                sparks.Emit(10);
            }
            
            PlayRandomSound(hammingSounds);
        }
        #endregion

        #region Merchant Actions
        public void StartTrading()
        {
            Debug.Log($"{_npc.NPCName} открывает торговлю");
            
            if (shopCounter != null)
            {
                Vector3 direction = shopCounter.position - transform.position;
                direction.y = 0;
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            ShowGoods(true);
            
            if (_animator != null)
            {
                _animator.SetTrigger("StartTrading");
                _animator.SetBool("IsTrading", true);
            }
            
            PlayRandomSound(tradingSounds);
        }

        public void StopTrading()
        {
            Debug.Log($"{_npc.NPCName} закрывает торговлю");
            
            ShowGoods(false);
            
            if (_animator != null)
            {
                _animator.SetBool("IsTrading", false);
            }
        }

        public void ShowGoods(bool show)
        {
            if (goodsToShow != null)
            {
                foreach (var good in goodsToShow)
                {
                    if (good != null)
                    {
                        good.SetActive(show);
                    }
                }
            }
        }

        public void MakeTransaction()
        {
            Debug.Log($"{_npc.NPCName} совершает сделку");
            
            if (_animator != null)
            {
                _animator.SetTrigger("MakeTransaction");
            }
            
            PlayRandomSound(tradingSounds);
        }
        #endregion

        #region Farmer Actions
        public void StartFarming()
        {
            Debug.Log($"{_npc.NPCName} начинает работать на ферме");
            
            ShowFarmingTools(true);
            
            if (_animator != null)
            {
                _animator.SetTrigger("StartFarming");
                _animator.SetBool("IsFarming", true);
            }
            
            PlayRandomSound(farmingSounds);
        }

        public void StopFarming()
        {
            Debug.Log($"{_npc.NPCName} заканчивает работу на ферме");
            
            ShowFarmingTools(false);
            
            if (_animator != null)
            {
                _animator.SetBool("IsFarming", false);
            }
        }

        public void ShowFarmingTools(bool show)
        {
            if (farmingTools != null)
            {
                foreach (var tool in farmingTools)
                {
                    if (tool != null)
                    {
                        tool.SetActive(show);
                    }
                }
            }
        }

        public void TendCrops()
        {
            Debug.Log($"{_npc.NPCName} ухаживает за посевами");
            
            if (_animator != null)
            {
                _animator.SetTrigger("TendCrops");
            }
            
            PlayRandomSound(farmingSounds);
        }

        public void HarvestCrops()
        {
            Debug.Log($"{_npc.NPCName} собирает урожай");
            
            if (_animator != null)
            {
                _animator.SetTrigger("HarvestCrops");
            }
            
            PlayRandomSound(farmingSounds);
        }
        #endregion

        #region Guard Actions
        public void StartPatrolling()
        {
            Debug.Log($"{_npc.NPCName} начинает патрулирование");
            
            if (weapon != null)
            {
                weapon.SetActive(true);
            }
            
            if (_animator != null)
            {
                _animator.SetTrigger("StartPatrol");
                _animator.SetBool("IsPatrolling", true);
            }
        }

        public void StopPatrolling()
        {
            Debug.Log($"{_npc.NPCName} заканчивает патрулирование");
            
            if (weapon != null)
            {
                weapon.SetActive(false);
            }
            
            if (_animator != null)
            {
                _animator.SetBool("IsPatrolling", false);
            }
        }

        public void AlertMode()
        {
            Debug.Log($"{_npc.NPCName} в состоянии тревоги");
            
            if (_animator != null)
            {
                _animator.SetTrigger("Alert");
            }
            
            PlayRandomSound(alertSounds);
        }

        public void Salute()
        {
            Debug.Log($"{_npc.NPCName} отдает честь");
            
            if (_animator != null)
            {
                _animator.SetTrigger("Salute");
            }
        }
        #endregion

        #region General Actions
        public void WaveGreeting()
        {
            Debug.Log($"{_npc.NPCName} машет рукой в приветствии");
            
            if (_animator != null)
            {
                _animator.SetTrigger("Wave");
            }
        }

        public void LookAround()
        {
            Debug.Log($"{_npc.NPCName} осматривается вокруг");
            
            if (_animator != null)
            {
                _animator.SetTrigger("LookAround");
            }
        }

        public void RestBreak()
        {
            Debug.Log($"{_npc.NPCName} делает перерыв");
            
            if (_animator != null)
            {
                _animator.SetTrigger("Rest");
            }
        }
        #endregion

        private void PlayRandomSound(AudioClip[] sounds)
        {
            if (sounds == null || sounds.Length == 0 || _audioSource == null) return;
            
            AudioClip randomClip = sounds[Random.Range(0, sounds.Length)];
            if (randomClip != null)
            {
                _audioSource.PlayOneShot(randomClip);
            }
        }

        // Публичные методы для настройки в UnityEvents
        public void SetAnimationTrigger(string triggerName)
        {
            if (_animator != null && !string.IsNullOrEmpty(triggerName))
            {
                _animator.SetTrigger(triggerName);
            }
        }

        public void SetAnimationBool(string boolName, bool value)
        {
            if (_animator != null && !string.IsNullOrEmpty(boolName))
            {
                _animator.SetBool(boolName, value);
            }
        }

        public void PlaySoundByName(string soundName)
        {
            // Можно реализовать систему воспроизведения звуков по имени
            Debug.Log($"Воспроизведение звука: {soundName}");
        }
    }
}