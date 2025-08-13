using Unity.Cinemachine;
using RPG.Attributes;
using RPG.SceneManagement;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace RPG.Control
{
    public class Respawner : MonoBehaviour
    {
        [SerializeField] Transform respawnLocation;
        [SerializeField] float respawnDelay = 3f;
        [SerializeField] float fadeTime = 0.2f;
        [SerializeField] float healthRegenPercentage = 40f;
        [SerializeField] float enemyHealthRegenPercentage = 60f;

        private void Awake()
        {
            GetComponent<Health>().onDie.AddListener(Respawn);
        }



        private void Start()
        {
            if (GetComponent<Health>().IsDead())
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            SavingWrapper savingWrapper = FindAnyObjectByType<SavingWrapper>();
            savingWrapper.Save();
            yield return new WaitForSeconds(respawnDelay);
            Fader fader = FindAnyObjectByType<Fader>();
            yield return fader.FadeOut(fadeTime);
            RespawnPlayer();
            ResetEnemies();
            savingWrapper.Save();
            yield return fader.FadeIn(fadeTime);
        }

        private void ResetEnemies()
        {
            foreach (AIController enemyControllers in FindObjectsByType<AIController>(FindObjectsSortMode.None))
            {
                enemyControllers.Reset();
                Health health = enemyControllers.GetComponent<Health>();
                if (health && !health.IsDead())
                {
                    health.Heal(health.GetMaxHealthPoints() * enemyHealthRegenPercentage / 100);
                }
            }
        }

        private void RespawnPlayer()
        {
            Vector3 positionDelta = respawnLocation.position - transform.position;
            GetComponent<NavMeshAgent>().Warp(respawnLocation.position);
            Health health = GetComponent<Health>();
            health.Heal(health.GetMaxHealthPoints() * healthRegenPercentage / 100);
            //ICinemachineCamera activeVirtualCamera = FindAnyObjectByType<CinemachineBrain>().ActiveVirtualCamera;
            //if (activeVirtualCamera.Follow == transform)
            //{
            //    activeVirtualCamera.OnTargetObjectWarped(transform, positionDelta);
            //}
        }
    }
}
