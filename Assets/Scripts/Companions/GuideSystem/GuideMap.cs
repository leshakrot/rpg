using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using RPG.SceneManagement;
#endif

namespace RPG.Companions
{
    /// <summary>
    /// Офлайн-карта мира для навигации между сценами: где лежит каждая цель и какие сцены
    /// связаны порталами (направленные рёбра). Заполняется кнопкой Bake (ПКМ по ассету).
    /// Индексы — build index (как в Portal.sceneToLoad).
    /// </summary>
    [CreateAssetMenu(fileName = "GuideMap", menuName = "Companions/Guide Map")]
    public class GuideMap : ScriptableObject
    {
        [Serializable] public struct TargetEntry { public string id; public int sceneIndex; }
        [Serializable] public struct LinkEntry   { public int fromScene; public int toScene; }

        [SerializeField] private List<TargetEntry> targets = new();
        [SerializeField] private List<LinkEntry>   links   = new();

        public bool TryGetTargetScene(string id, out int sceneIndex)
        {
            foreach (var t in targets)
                if (t.id == id) { sceneIndex = t.sceneIndex; return true; }

            sceneIndex = -1;
            return false;
        }

        /// <summary>Следующая сцена на кратчайшем (по числу переходов) пути from → to. -1 — пути нет.</summary>
        public int GetNextScene(int from, int to)
        {
            if (from == to) return from;

            var prev  = new Dictionary<int, int> { [from] = -1 };
            var queue = new Queue<int>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                foreach (var l in links)
                {
                    if (l.fromScene != cur || prev.ContainsKey(l.toScene)) continue;

                    prev[l.toScene] = cur;
                    if (l.toScene == to)
                    {
                        int step = to;
                        while (prev[step] != from) step = prev[step];
                        return step;
                    }
                    queue.Enqueue(l.toScene);
                }
            }
            return -1;
        }

#if UNITY_EDITOR
        [ContextMenu("Bake From Build Settings")]
        private void Bake()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();

            var newTargets = new List<TargetEntry>();
            var newLinks   = new List<LinkEntry>();
            int buildIndex = 0;

            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue; // build index считается только по включённым сценам

                EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);

                foreach (var portal in FindObjectsOfType<Portal>(true))
                {
                    if (portal.SceneToLoad < 0) continue;
                    var link = new LinkEntry { fromScene = buildIndex, toScene = portal.SceneToLoad };
                    if (!newLinks.Contains(link)) newLinks.Add(link);
                }

                foreach (var t in FindObjectsOfType<GuideTarget>(true))
                {
                    if (string.IsNullOrEmpty(t.TargetId))
                    {
                        Debug.LogWarning($"[GuideMap] GuideTarget без id: {t.name} ({s.path})");
                        continue;
                    }
                    if (newTargets.Exists(e => e.id == t.TargetId))
                        Debug.LogWarning($"[GuideMap] Дубликат id '{t.TargetId}' ({s.path})");
                    else
                        newTargets.Add(new TargetEntry { id = t.TargetId, sceneIndex = buildIndex });
                }

                buildIndex++;
            }

            EditorSceneManager.RestoreSceneManagerSetup(setup);

            targets = newTargets;
            links   = newLinks;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GuideMap] Bake: целей {targets.Count}, связей {links.Count}", this);
        }
#endif
    }
}
