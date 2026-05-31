using UnityEngine;

namespace RPG.Control
{
    public class PatrolPath : MonoBehaviour
    {
        const float waypointGizmoRadius = 0.3f;

        private void OnDrawGizmos()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                int j = GetNextIndex(i);
                
                // Бонус: точки с особым поведением (PatrolWaypoint) будут подсвечены желтым
                PatrolWaypoint customWaypoint = transform.GetChild(i).GetComponent<PatrolWaypoint>();
                if (customWaypoint != null && (customWaypoint.OverrideDwellTime || customWaypoint.OverrideSpeed))
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawSphere(GetWaypoint(i), waypointGizmoRadius);
                
                Gizmos.color = Color.white; // Линии всегда рисуем белым
                Gizmos.DrawLine(GetWaypoint(i), GetWaypoint(j));
            }
        }

        public int GetNextIndex(int i)
        {
            if (i < transform.childCount - 1) return i + 1;
            return 0;
        }

        public Vector3 GetWaypoint(int i)
        {
            return transform.GetChild(i).position;
        }

        // --- НОВЫЕ МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ ДАННЫХ ИЗ ТОЧКИ ---

        public float GetWaypointDwellTime(int i, float defaultTime)
        {
            PatrolWaypoint waypoint = transform.GetChild(i).GetComponent<PatrolWaypoint>();
            return (waypoint != null && waypoint.OverrideDwellTime) ? waypoint.DwellTime : defaultTime;
        }

        public float? GetWaypointSpeedOverride(int i)
        {
            PatrolWaypoint waypoint = transform.GetChild(i).GetComponent<PatrolWaypoint>();
            if (waypoint != null && waypoint.OverrideSpeed)
            {
                return waypoint.SpeedFraction;
            }
            return null; // Возвращаем null, если переопределения нет
        }
    }
}