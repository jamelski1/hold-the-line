using UnityEngine;

namespace HoldTheLine.Player
{
    public class TargetingSystem : MonoBehaviour
    {
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private LayerMask enemyLayer;

        private Transform currentTarget;

        public Transform CurrentTarget => currentTarget;
        public bool HasTarget => currentTarget != null;

        private void Update()
        {
            FindNearestEnemy();
        }

        private void FindNearestEnemy()
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayer);

            float closestDistance = float.MaxValue;
            Transform closestEnemy = null;

            foreach (var enemy in enemies)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy.transform;
                }
            }

            currentTarget = closestEnemy;
        }

        public Vector2 GetTargetDirection()
        {
            // Always shoot upward for this game
            return Vector2.up;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
