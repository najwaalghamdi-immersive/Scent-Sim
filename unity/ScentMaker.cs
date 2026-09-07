using UnityEngine;

/// <summary>
/// A reusable scent definition: create one asset per scent (Assets > Create
/// > Scriptable Objects > ScentMaker), configure its port/intensity/fan
/// time once, then assign it to as many ScentTrigger volumes as you want —
/// they all fire the same command without duplicating the numbers.
/// </summary>
[CreateAssetMenu(fileName = "ScentMaker", menuName = "Scriptable Objects/ScentMaker")]
public class ScentMaker : ScriptableObject
{
    [Header("Scent")]
    public string scentName = "Unnamed Scent";

    [Header("Olorama Command")]
    [Tooltip("Physical cartridge port, 1-10.")]
    [Range(1, 10)] public int port = 1;
    [Tooltip("100-500. The manual recommends 100-300.")]
    [Range(100, 500)] public int intensity = 100;
    [Tooltip("Fan run time in milliseconds, 1000-9000.")]
    [Range(1000, 9000)] public int fanMs = 2000;

    /// <summary>
    /// Sends this scent's command through the scene's OloramaUDPSender.
    /// Safe to call repeatedly (e.g. from multiple triggers using the same
    /// asset) — each call is an independent UDP send.
    /// </summary>
    public void Activate()
    {
        if (OloramaUDPSender.Instance == null)
        {
            Debug.LogError($"[Olorama] no OloramaUDPSender in the scene — can't activate \"{scentName}\".");
            return;
        }

        OloramaUDPSender.Instance.SendScentMessage(port, intensity, fanMs);
    }
}
