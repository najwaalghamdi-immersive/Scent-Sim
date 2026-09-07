using UnityEngine;

/// <summary>
/// Attach to a trigger volume (a Collider with Is Trigger checked). When
/// the tagged object enters, activates the assigned ScentMaker asset.
/// Either this object or the one entering it needs a non-kinematic
/// Rigidbody, or OnTriggerEnter never fires.
/// </summary>
public class ScentTrigger : MonoBehaviour
{
    [SerializeField] private ScentMaker scent;
    [SerializeField] private string requiredTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        if (scent == null)
        {
            Debug.LogWarning($"[Olorama] {name} has no ScentMaker assigned.");
            return;
        }

        scent.Activate();
    }
}
