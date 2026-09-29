using System.Collections.Generic;
using UnityEngine;

namespace RPG.Companions
{
    /// <summary>
    /// Registry-скрипт на цели. Вешать на ВСЕГДА активный маркер (не на объект, который
    /// включается по ходу квеста — у выключенного OnEnable не сработает).
    /// Сцена цели попадает в GuideMap при бейке.
    /// </summary>
    public class GuideTarget : MonoBehaviour
    {
        [SerializeField] private string targetId;
        [Tooltip("Куда бежать собаке. Если пусто — позиция самого объекта.")]
        [SerializeField] private Transform guidePoint;

        public string TargetId => targetId;
        public Vector3 Position => guidePoint != null ? guidePoint.position : transform.position;

        private static readonly Dictionary<string, GuideTarget> Loaded = new();

        public static bool TryGetLoaded(string id, out GuideTarget target)
        {
            if (!string.IsNullOrEmpty(id) && Loaded.TryGetValue(id, out target) && target != null)
                return true;

            target = null;
            return false;
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(targetId)) Loaded[targetId] = this;
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(targetId) && Loaded.TryGetValue(targetId, out var t) && t == this)
                Loaded.Remove(targetId);
        }
    }
}
