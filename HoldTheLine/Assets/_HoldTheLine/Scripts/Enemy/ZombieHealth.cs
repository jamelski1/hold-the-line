using UnityEngine;
using System;

namespace HoldTheLine.Enemy
{
    public class ZombieHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 1;

        private int currentHealth;

        public event Action OnDeath;

        private void OnEnable()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
