using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.SceneManagement
{
    public class Fader : MonoBehaviour
    {
        [Header("Location Image")]
        [Tooltip("UI Image, отображающий картинку локации поверх/под затемнением на время перехода")]
        [SerializeField] Image locationImage;

        CanvasGroup canvasGroup;
        Coroutine currentActiveFade = null;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void FadeOutImmediate()
        {
            canvasGroup.alpha = 1;
        }

        /// <summary>
        /// Устанавливает картинку локации, которая будет видна во время затемнения.
        /// Вызывать перед FadeOut, чтобы нужная картинка уже была выставлена
        /// к моменту, когда экран полностью закроется.
        /// </summary>
        public void SetLocationImage(Sprite sprite)
        {
            if (locationImage == null)
            {
                Debug.LogWarning("Fader: locationImage не назначен в инспекторе.");
                return;
            }

            locationImage.sprite = sprite;
            locationImage.enabled = sprite != null;
        }

        public Coroutine FadeOut(float time)
        {
            return Fade(1, time);
        }

        public Coroutine FadeIn(float time)
        {
            return Fade(0, time);
        }

        public Coroutine Fade(float target, float time)
        {
            if (currentActiveFade != null)
            {
                StopCoroutine(currentActiveFade);
            }
            currentActiveFade = StartCoroutine(FadeRoutine(target, time));
            return currentActiveFade;
        }

        private IEnumerator FadeRoutine(float target, float time)
        {
            while (!Mathf.Approximately(canvasGroup.alpha, target))
            {
	            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.unscaledDeltaTime / time);
                yield return null;
            }
        }
    }
}