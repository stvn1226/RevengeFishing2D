using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleDetector : MonoBehaviour
{
    private LandEnemy parentEnemy;
    private Collider2D detectorCollider;

    private void Awake()
    {
        // Get the parent enemy components
        parentEnemy = GetComponentInParent<LandEnemy>();
        detectorCollider = GetComponent<Collider2D>();

        if (parentEnemy == null)
        {
            Debug.LogError($"IdleDetector on {gameObject.name} could not find parent Enemy component!");
        }

        if (detectorCollider == null || !detectorCollider.isTrigger)
        {
            Debug.LogError($"IdleDetector on {gameObject.name} needs a trigger collider!");
        }
    }

    /// <summary>
    /// Check if this enemy should avoid going idle due to overlapping with other idle enemies.
    /// Only non-fishing enemies will be prevented from going idle.
    /// </summary>
    /// <returns>True if this enemy should NOT go idle due to overlaps</returns>
    public bool ShouldAvoidIdle()
    {
        // If this enemy is fishing, they have priority and should not be moved
        if (parentEnemy != null && parentEnemy.fishingToolEquipped)
        {
            return false;
        }

        // Check for overlaps with other idle enemies
        return IsOverlappingWithIdleEnemy();
    }

    /// <summary>
    /// Perform an immediate overlap check to see if we're overlapping with any idle enemies.
    /// This is called on-demand rather than every frame.
    /// </summary>
    /// <returns>True if overlapping with at least one idle enemy</returns>
    public bool IsOverlappingWithIdleEnemy()
    {
        if (detectorCollider == null) return false;

        // Get all overlapping colliders in the IdleDetector layer
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1 << LayerMask.NameToLayer("IdleDetector"));
        filter.useTriggers = true;

        List<Collider2D> overlappingColliders = new List<Collider2D>();
        int numOverlapping = detectorCollider.OverlapCollider(filter, overlappingColliders);

        foreach (Collider2D overlappingCollider in overlappingColliders)
        {
            // Skip if it's our own collider
            if (overlappingCollider == detectorCollider) continue;

            IdleDetector otherDetector = overlappingCollider.GetComponent<IdleDetector>();
            if (otherDetector != null && otherDetector.parentEnemy != null)
            {
                // Check if the other enemy is idle and alive
                if (IsEnemyIdle(otherDetector.parentEnemy))
                {
                    Debug.Log($"{parentEnemy.name} found overlap with idle enemy {otherDetector.parentEnemy.name}");
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Get count of overlapping idle enemies for debugging purposes
    /// </summary>
    /// <returns>Number of overlapping idle enemies</returns>
    public int GetOverlappingIdleEnemyCount()
    {
        if (detectorCollider == null) return 0;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1 << LayerMask.NameToLayer("IdleDetector"));
        filter.useTriggers = true;

        List<Collider2D> overlappingColliders = new List<Collider2D>();
        detectorCollider.OverlapCollider(filter, overlappingColliders);

        int count = 0;
        foreach (Collider2D overlappingCollider in overlappingColliders)
        {
            if (overlappingCollider == detectorCollider) continue;

            IdleDetector otherDetector = overlappingCollider.GetComponent<IdleDetector>();
            if (otherDetector != null && IsEnemyIdle(otherDetector.parentEnemy))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsEnemyIdle(LandEnemy landEnemy)
    {
        if (landEnemy == null) return false;
        if (landEnemy.GetState() != Enemy.EnemyState.Alive) return false;

        return landEnemy.MovementStateLand == LandEnemy.LandMovementState.Idle;
    }

    // Optional: Visual debugging in Scene view
    private void OnDrawGizmosSelected()
    {
        // Draw the detection area
        Gizmos.color = Color.yellow;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            Gizmos.DrawWireSphere((Vector3)transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
        }

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Gizmos.DrawWireCube((Vector3)transform.position + (Vector3)boxCollider.offset, boxCollider.size);
        }
    }
}
