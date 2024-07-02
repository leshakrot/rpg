using RPG.Control;
using RPG.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace RPG.Cinematics
{
    public class CinematicControlRemover : MonoBehaviour
    {
        private PlayableDirector _playableDirector;
        private GameObject _player;
        private PlayerController _playerController;
        private ActionScheduler _actionScheduler;

        private void Start()
        {
            _playableDirector = GetComponent<PlayableDirector>();
            _player = GameObject.FindWithTag("Player");
            _playerController = _player.GetComponent<PlayerController>();
            _actionScheduler = _player.GetComponent<ActionScheduler>();


            _playableDirector.played += DisableControl;
            _playableDirector.stopped += EnableControl;
        }

        private void OnEnable()
        {
            
        }

        private void DisableControl(PlayableDirector pd)
        {
            _actionScheduler.CancelCurrentAction();
            _playerController.enabled = false;
        }

        private void EnableControl(PlayableDirector pd)
        {
            _playerController.enabled = true;
        }
    }
}
