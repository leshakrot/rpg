using RPG.Control;
using GameDevTV.Saving;
using UnityEngine;
using UnityEngine.Playables;

namespace RPG.Cinematics
{
    public class CinematicTrigger : MonoBehaviour, ISaveable
    {
        private bool _isTriggered;


        private void OnTriggerEnter(Collider other)
        {
            if(other.TryGetComponent(out PlayerController player) && !_isTriggered)
            {
                _isTriggered = true;
                GetComponent<PlayableDirector>().Play();               
            }           
        }

        public object CaptureState()
        {
            return _isTriggered;
        }

        public void RestoreState(object state)
        {
            _isTriggered = (bool)state;
        }
    }
}
