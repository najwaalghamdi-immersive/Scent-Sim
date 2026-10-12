using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Drop on any MR-interactable object alongside an XRBaseInteractable
/// (e.g. XRSimpleInteractable) so reaching out and selecting it fires its
/// scent — XR Interaction Toolkit raises the same selectEntered event
/// whether the player used a controller ray + trigger press, a poke, or a
/// hand-tracking pinch, so this one component covers all of them. Works
/// alongside ScentTrigger (collision) and ScentGun (shot) on the same
/// object — all three check the same ScentSource first, so one asset is
/// the single place an object's smell is configured no matter how it's
/// triggered.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class ScentInteractable : MonoBehaviour
{
    [Tooltip("Only used if this GameObject has no ScentSource. Leave empty too, and ScentSelection.Instance.Current is used instead.")]
    [SerializeField] private ScentMaker scent;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        ScentSource source = GetComponent<ScentSource>();
        ScentMaker toFire = source != null && source.scent != null
            ? source.scent
            : (scent != null ? scent : ScentSelection.Instance?.Current);

        if (toFire == null)
        {
            Debug.LogWarning($"[Olorama] {name} was selected but has no ScentMaker assigned and none is currently selected.");
            return;
        }

        toFire.Activate();
    }
}
