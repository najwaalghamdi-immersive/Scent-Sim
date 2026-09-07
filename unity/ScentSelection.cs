using UnityEngine;

/// <summary>
/// Holds whichever ScentMaker the player currently has "loaded" — chosen
/// from a menu (see ScentMenu), a UI dropdown, or any other picker. One per
/// scene, the same singleton pattern as OloramaUDPSender. ScentTrigger and
/// ScentGun both fall back to this when they aren't hardwired to a fixed
/// ScentMaker, so a single "current scent" can be fired by collision, by a
/// gun, or immediately on being chosen.
/// </summary>
public class ScentSelection : MonoBehaviour
{
    public static ScentSelection Instance { get; private set; }

    [Tooltip("Fire the scent immediately when it's chosen, in addition to arming it for triggers/guns that read Current.")]
    public bool activateOnChoose = true;

    public ScentMaker Current { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Olorama] a second ScentSelection ({name}) exists; keeping {Instance.name}.");
            return;
        }
        Instance = this;
    }

    /// <summary>Called by ScentMenu (or your own UI) when the player picks a scent.</summary>
    public void Choose(ScentMaker scent)
    {
        Current = scent;
        if (activateOnChoose && scent != null)
            scent.Activate();
    }
}
