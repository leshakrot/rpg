using RPG.Control;
using RPG.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace RPG.Cinematics
{
    public class CinematicControlRemover : MonoBehaviour
	{
		[SerializeField] private GameObject _hud;
		[SerializeField] private GameObject _uiCanvas;
        [SerializeField] private GameObject _mainCamera;
        [SerializeField] private GameObject _cinemachineBrainCamera;

        private PlayableDirector _playableDirector;
        private GameObject _player;
        private PlayerController _playerController;
        private ActionScheduler _actionScheduler;

        private void Awake()
        {
            _playableDirector = GetComponent<PlayableDirector>();
            _player = GameObject.FindWithTag("Player");
            _playerController = _player.GetComponent<PlayerController>();
            _actionScheduler = _player.GetComponent<ActionScheduler>(); 
        }

        private void OnEnable()
        {
            _playableDirector.played += DisableControl;
            _playableDirector.stopped += EnableControl;
        }

        private void OnDisable()
        {
            _playableDirector.played -= DisableControl;
            _playableDirector.stopped -= EnableControl;
        }

        private void DisableControl(PlayableDirector pd)
        {
            _actionScheduler.CancelCurrentAction();
	        _playerController.enabled = false;
	        _hud.SetActive(false);
	        _uiCanvas.SetActive(false);
            _mainCamera.SetActive(false);
            _cinemachineBrainCamera.SetActive(true);
        }

        private void EnableControl(PlayableDirector pd)
        {
	        _playerController.enabled = true;
	        _hud.SetActive(true);
	        _uiCanvas.SetActive(true);
            _cinemachineBrainCamera.SetActive(false);
            _mainCamera.SetActive(true);       
        }
    }
}
