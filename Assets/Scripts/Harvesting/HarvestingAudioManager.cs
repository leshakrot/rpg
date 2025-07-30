using UnityEngine;

namespace RPG.Harvesting
{
    public class HarvestingAudioManager : MonoBehaviour
    {
        [Header("Звуки добычи")]
        public AudioClip woodHitSound;
        public AudioClip oreHitSound;
        public AudioClip herbHitSound;
        public AudioClip resourceDepletedSound;
        public AudioClip resourceCompleteSound;

        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void PlayResourceHitSound(HarvestableResource resource)
        {
            if (resource == null) return;
            AudioClip clip = null;
            switch (resource.ResourceCategory)
            {
                case ResourceCategory.Wood:
                    clip = woodHitSound;
                    break;
                case ResourceCategory.Ore:
                    clip = oreHitSound;
                    break;
                case ResourceCategory.Herb:
                    clip = herbHitSound;
                    break;
            }
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        public void PlayResourceDepletedSound()
        {
            if (resourceDepletedSound != null)
                audioSource.PlayOneShot(resourceDepletedSound);
        }

        public void PlayResourceCompleteSound()
        {
            if (resourceCompleteSound != null)
                audioSource.PlayOneShot(resourceCompleteSound);
        }
    }
} 