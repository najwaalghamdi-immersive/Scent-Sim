using UnityEngine;

/// <summary>
/// A menu of available scents. Wire one entry per UI button's OnClick (or
/// per XR interactable's Select event) to Choose(index) — e.g. button 0 for
/// scents[0], button 1 for scents[1]. Choosing an entry sets it as the
/// scene's current scent via ScentSelection, which immediately fires it
/// (if ScentSelection.activateOnChoose is on) and arms it for any
/// ScentTrigger or ScentGun that reads ScentSelection.Instance.Current.
/// </summary>
public class ScentMenu : MonoBehaviour
{
    [SerializeField] private ScentMaker[] scents;

    public void Choose(int index)
    {
        if (scents == null || index < 0 || index >= scents.Length)
        {
            Debug.LogWarning($"[Olorama] ScentMenu index {index} out of range (0-{(scents?.Length ?? 0) - 1}).");
            return;
        }

        if (ScentSelection.Instance == null)
        {
            Debug.LogError("[Olorama] no ScentSelection in the scene — add one before using ScentMenu.");
            return;
        }

        ScentSelection.Instance.Choose(scents[index]);
    }
}
