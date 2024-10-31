using UnityEngine;
using UnityEngine.Events;

namespace RPG.Dialogue
{
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private string _action;
        [SerializeField] private UnityEvent _onTrigger;

        public void Trigger(string actionToTrigger)
        {
            if(actionToTrigger == _action)
            {
                _onTrigger.Invoke();
            }
        }
    }
}
