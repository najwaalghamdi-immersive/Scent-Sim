using UnityEngine;

/// <summary>
/// A "scent gun": raycasts forward from the muzzle and activates a
/// ScentMaker when it hits something. Fires either a scent fixed in the
/// Inspector (a gun permanently loaded with one cartridge), or — leave
/// Fixed Scent empty — whatever the player most recently picked via
/// ScentSelection (a gun that fires "whatever's loaded").
///
/// Wire Fire() to an XR controller's Activate/Select UnityEvent, an Input
/// Action's performed callback, or leave Use Legacy Fire Button on for a
/// quick non-VR test (defaults to the "Fire1" axis, i.e. left ctrl/mouse
/// button in Unity's default Input Manager).
/// </summary>
public class ScentGun : MonoBehaviour
{
    [Tooltip("Leave empty to fire whatever ScentSelection.Instance.Current is set to.")]
    [SerializeField] private ScentMaker fixedScent;
    [Tooltip("Raycast origin/direction. Defaults to this GameObject's transform if left empty.")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Only activate the scent when the raycast hits an object with this tag. Leave blank to fire on any hit.")]
    [SerializeField] private string requiredHitTag = "";

    [Header("Legacy input (optional, for quick testing without VR/XR wired up)")]
    [SerializeField] private bool useLegacyFireButton = true;
    [SerializeField] private string fireButton = "Fire1";

    void Update()
    {
        if (useLegacyFireButton && Input.GetButtonDown(fireButton))
            Fire();
    }

    /// <summary>Call this from your input/interaction layer to pull the trigger.</summary>
    public void Fire()
    {
        ScentMaker scent = fixedScent != null ? fixedScent : ScentSelection.Instance?.Current;
        if (scent == null)
        {
            Debug.LogWarning($"[Olorama] {name} has no scent loaded — assign Fixed Scent, or choose one via ScentSelection first.");
            return;
        }

        Transform origin = muzzle != null ? muzzle : transform;
        if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, range, hitMask))
            return;

        if (!string.IsNullOrEmpty(requiredHitTag) && !hit.collider.CompareTag(requiredHitTag))
            return;

        scent.Activate();
    }
}
