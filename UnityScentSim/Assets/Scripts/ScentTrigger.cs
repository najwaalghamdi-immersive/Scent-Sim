using UnityEngine;

/// <summary>
/// Attach to a trigger volume (a Collider with Is Trigger checked). When
/// the tagged object enters, activates a ScentMaker — either the one fixed
/// in the Inspector, or, if left empty, whatever the player currently has
/// selected via ScentSelection (so the same volume fires whichever scent
/// was last chosen from a menu). Either this object or the one entering it
/// needs a non-kinematic Rigidbody, or OnTriggerEnter never fires.
/// </summary>
public class ScentTrigger : MonoBehaviour
{
    [Tooltip("Leave empty to fire whatever ScentSelection.Instance.Current is set to.")]
    [SerializeField] private ScentMaker scent;
    [SerializeField] private string requiredTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        ScentMaker toFire = scent != null ? scent : ScentSelection.Instance?.Current;
        if (toFire == null)
        {
            Debug.LogWarning($"[Olorama] {name} has no ScentMaker assigned and none is currently selected.");
            return;
        }

        toFire.Activate();
    }
}
