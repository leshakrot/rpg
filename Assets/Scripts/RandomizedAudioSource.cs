using UnityEngine;

public class RandomizedAudioSource : MonoBehaviour
{
    [Header("Если не задан, будет взят с этого объекта")]
    public AudioSource audioSource;
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.95f;
    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.05f;
    [Range(0f, 1f)]
    public float playChance = 0.8f; // 80% шанс проиграть звук

    public void Play()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;
        if (Random.value > playChance) return; // иногда пропускаем звук
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.Play();
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null || clip == null) return;
        if (Random.value > playChance) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
} 