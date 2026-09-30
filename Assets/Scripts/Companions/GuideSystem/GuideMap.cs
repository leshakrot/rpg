using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
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

            // Свежий скан: id → (запись, сцена-источник, для диагностики).
            var scanOrder   = new List<TargetEntry>();
            var scannedById = new Dictionary<string, TargetEntry>();
            var newLinks    = new List<LinkEntry>();

            var duplicateReports     = new List<string>(); // id уже встречался в этом скане
            var missingIdReports     = new List<string>(); // GuideTarget без targetId
            var disabledSceneReports = new List<string>(); // цель лежит в сцене, выключенной в Build Settings

            int buildIndex = 0;

            foreach (var s in EditorBuildSettings.scenes)
            {
                Scene opened = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);

                if (!opened.IsValid() || !opened.isLoaded)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                    EditorUtility.DisplayDialog("GuideMap: Bake прерван",
                        $"Не удалось открыть сцену '{s.path}'.\nБейк остановлен, карта НЕ изменена.", "OK");
                    return;
                }

                if (!s.enabled)
                {
                    // Сцену не бейкаем (у неё нет build index), но цели в ней найти стоит —
                    // это частая причина, почему цель "пропадает": сцену выключили в Build Settings.
                    foreach (var t in FindObjectsOfType<GuideTarget>(true))
                    {
                        if (!string.IsNullOrEmpty(t.TargetId))
                            disabledSceneReports.Add($"'{t.TargetId}' — {t.name} ({s.path})");
                    }
                    continue;
                }

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
                        missingIdReports.Add($"{t.name} ({s.path})");
                        continue;
                    }

                    if (scannedById.ContainsKey(t.TargetId))
                    {
                        duplicateReports.Add($"'{t.TargetId}' — {t.name} ({s.path}), первым остался предыдущий");
                        continue;
                    }

                    var entry = new TargetEntry { id = t.TargetId, sceneIndex = buildIndex };
                    scannedById[t.TargetId] = entry;
                    scanOrder.Add(entry);
                }

                buildIndex++;
            }

            EditorSceneManager.RestoreSceneManagerSetup(setup);

            // ── слияние со старым списком: порядок существующих целей сохраняется,
            //    новые добавляются в конец; ничего не удаляется без подтверждения ──
            var merged      = new List<TargetEntry>();
            var usedIds     = new HashSet<string>();
            var removed     = new List<string>();

            foreach (var existing in targets)
            {
                if (scannedById.TryGetValue(existing.id, out TargetEntry fresh))
                {
                    merged.Add(fresh);
                    usedIds.Add(existing.id);
                }
                else
                {
                    removed.Add(existing.id);
                }
            }

            int addedCount = 0;
            foreach (var t in scanOrder)
            {
                if (usedIds.Contains(t.id)) continue;
                merged.Add(t);
                usedIds.Add(t.id);
                addedCount++;
            }

            if (duplicateReports.Count > 0)
                Debug.LogWarning("[GuideMap] Дубликаты targetId (пропущены):\n" + string.Join("\n", duplicateReports));
            if (missingIdReports.Count > 0)
                Debug.LogWarning("[GuideMap] GuideTarget без targetId:\n" + string.Join("\n", missingIdReports));
            if (disabledSceneReports.Count > 0)
                Debug.LogWarning("[GuideMap] Цели в сценах, выключенных в Build Settings (не попали в карту):\n" + string.Join("\n", disabledSceneReports));

            if (removed.Count > 0)
            {
                string removedList = string.Join("\n", removed);
                bool confirmed = EditorUtility.DisplayDialog(
                    "GuideMap: часть целей не найдена",
                    $"При повторном скане не найдены {removed.Count} ранее сохранённых целей:\n\n{removedList}\n\n" +
                    "Причина обычно одна из: targetId у объекта поменяли/объект удалили, " +
                    "сцена с ним выключена в Build Settings (см. предупреждение в консоли), " +
                    "или у двух объектов совпал targetId (см. предупреждение о дубликатах).\n\n" +
                    "Удалить эти записи из карты?",
                    "Удалить", "Отмена — не трогать карту");

                if (!confirmed)
                {
                    Debug.Log("[GuideMap] Bake отменён пользователем — карта не изменена.");
                    return;
                }
            }

            targets = merged;
            links   = newLinks;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GuideMap] Bake готов: целей {targets.Count} (+{addedCount} новых, -{removed.Count} удалено), связей {links.Count}", this);
        }
#endif

    }
}
