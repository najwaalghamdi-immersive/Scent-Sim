using UnityEngine;

/// <summary>
/// Attach to anything that should have its own smell when shot or touched
/// (a flower, a corpse, a puddle...). ScentGun looks for this on whatever
/// it hits and fires this object's Scent instead of the gun's own loaded
/// scent — that's what makes "the bullet hit determines the scent" work.
/// ScentTrigger can use it the same way for collisions.
/// </summary>
public class ScentSource : MonoBehaviour
{
    public ScentMaker scent;
}
