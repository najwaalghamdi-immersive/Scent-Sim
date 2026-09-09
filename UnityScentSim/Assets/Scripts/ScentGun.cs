using UnityEngine;

/// <summary>
/// A "scent gun": raycasts forward from the muzzle and activates a
/// ScentMaker when it hits something. Which scent fires is decided in this
/// order:
///   1. Whatever ScentSource the hit object (or one of its parents) carries
///      — this is what makes "the bullet hit determines the scent" work,
///      e.g. every flower has its own ScentSource with its own smell.
///   2. Fixed Scent, if you set one in the Inspector (a gun permanently
///      loaded with one cartridge, ignoring what it hits).
///   3. Whatever the player most recently picked via ScentSelection.
///
/// Wire Fire() to an XR controller's Activate/Select UnityEvent, an Input
/// Action's performed callback, or leave Use Legacy Fire Button on for a
/// quick non-VR test (defaults to the "Fire1" axis, i.e. left ctrl/mouse
/// button in Unity's default Input Manager).
/// </summary>
public class ScentGun : MonoBehaviour
{
    [Tooltip("Only used if the hit object has no ScentSource of its own. Leave empty too, and ScentSelection.Instance.Current is used instead.")]
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
        Transform origin = muzzle != null ? muzzle : transform;
        if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, range, hitMask))
            return;

        if (!string.IsNullOrEmpty(requiredHitTag) && !hit.collider.CompareTag(requiredHitTag))
            return;

        ScentSource source = hit.collider.GetComponentInParent<ScentSource>();
        ScentMaker scent = source != null && source.scent != null
            ? source.scent
            : (fixedScent != null ? fixedScent : ScentSelection.Instance?.Current);

        if (scent == null)
        {
            Debug.LogWarning($"[Olorama] {name} hit \"{hit.collider.name}\", but it has no ScentSource and the gun has no scent loaded.");
            return;
        }

        scent.Activate();
    }
}
