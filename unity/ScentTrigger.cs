using UnityEngine;

/// <summary>
/// Attach to a trigger volume (a Collider with Is Trigger checked). When
/// the tagged object enters, activates a ScentMaker — checked in this
/// order: a ScentSource on this same GameObject (so one ScentSource is the
/// single place a flower's smell is configured, shared with ScentGun),
/// then the Scent fixed in this component's own Inspector field, then
/// whatever the player currently has selected via ScentSelection. Either
/// this object or the one entering it needs a non-kinematic Rigidbody, or
/// OnTriggerEnter never fires.
/// </summary>
public class ScentTrigger : MonoBehaviour
{
    [Tooltip("Only used if this GameObject has no ScentSource. Leave empty too, and ScentSelection.Instance.Current is used instead.")]
    [SerializeField] private ScentMaker scent;
    [SerializeField] private string requiredTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        ScentSource source = GetComponent<ScentSource>();
        ScentMaker toFire = source != null && source.scent != null
            ? source.scent
            : (scent != null ? scent : ScentSelection.Instance?.Current);

        if (toFire == null)
        {
            Debug.LogWarning($"[Olorama] {name} has no ScentMaker assigned and none is currently selected.");
            return;
        }

        toFire.Activate();
    }
}
