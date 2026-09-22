using System.Collections;
using UnityEngine;
using RPG.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace RPG.UI
{
    public class SaveLoadUI : MonoBehaviour
    {
        [SerializeField] Transform contentRoot;
        [SerializeField] GameObject buttonPrefab;

        private SavingWrapper savingWrapper;

        private void OnEnable()
        {
            savingWrapper = FindObjectOfType<SavingWrapper>();

            if (savingWrapper == null)
                return;

            StartCoroutine(BuildSaveList());
        }

        private IEnumerator BuildSaveList()
        {
            yield return savingWrapper.WaitForSaveSystem();

            if (!isActiveAndEnabled)
                yield break;

            foreach (Transform child in contentRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (string save in savingWrapper.ListSaves())
            {
                GameObject buttonInstance =
                    Instantiate(buttonPrefab, contentRoot);

                TMP_Text textComp =
                    buttonInstance.GetComponentInChildren<TMP_Text>();

                if (textComp != null)
                {
                    textComp.text = save;
                }

                Button button =
                    buttonInstance.GetComponentInChildren<Button>();

                if (button != null)
                {
                    string saveName = save;

                    button.onClick.AddListener(() =>
                    {
                        savingWrapper.LoadGame(saveName);
                    });
                }
            }
        }
    }
}
