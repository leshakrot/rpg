using System;
using System.Collections;
using System.Collections.Generic;
using RPG.Control;
using UnityEngine;

namespace RPG.Abilities.Targeting
{
    [CreateAssetMenu(fileName = "Delayed Click Targeting", menuName = "Abilities/Targeting/Delayed Click", order = 0)]
    public class DelayedClickTargeting : TargetingStrategy
    {
        PlayerController playerController;
        [SerializeField] private Texture2D cursorTexture;
        [SerializeField] private Vector2 cursorHotspot;
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private float areaAffectRadius;

        public override void StartTargeting(GameObject user, Action<IEnumerable<GameObject>> finished)
        {
            playerController = user.GetComponent<PlayerController>();
            playerController.StartCoroutine(Targeting(user, playerController, finished));
        }

        private IEnumerator Targeting(GameObject user, PlayerController playerController, Action<IEnumerable<GameObject>> finished)
        {
            playerController.enabled = false;
            while(true)
            {
                Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);

                if(Input.GetMouseButtonDown(0))
                {
                    yield return new WaitWhile(() => Input.GetMouseButton(0));
                    playerController.enabled = true;
                    finished(GetGameObjectsInRadius()); 
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerable<GameObject> GetGameObjectsInRadius()
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(PlayerController.GetMouseRay(), out raycastHit, 1000, layerMask))
            {
                RaycastHit[] hits = Physics.SphereCastAll(raycastHit.point, areaAffectRadius, Vector3.up, 0);
                foreach (var hit in hits)
                {
                    yield return hit.collider.gameObject;
                }
            }
        }
    }
}
