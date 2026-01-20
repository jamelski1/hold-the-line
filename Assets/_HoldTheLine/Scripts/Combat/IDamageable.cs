// IDamageable.cs - Interface for anything that can take damage
// Location: Assets/_HoldTheLine/Scripts/Combat/

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Interface for objects that can receive damage (zombies, upgrade targets)
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this object
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        void TakeDamage(float damage);

        /// <summary>
        /// Check if this object is still alive/active
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Get the transform for targeting purposes
        /// </summary>
        Transform GetTransform();
    }
}
