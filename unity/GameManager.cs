using UnityEngine;

/// <summary>
/// Fires an Olorama scent when the tagged object (default: "Player") enters
/// this trigger volume. Attach to a GameObject with a Collider (Is Trigger
/// checked) and either it or the other object needs a non-kinematic
/// Rigidbody, or OnTriggerEnter never fires. Drag the GameObject holding
/// OloramaUDPSender into the scentSender field in the Inspector.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private OloramaUDPSender scentSender;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private int scentPort = 3;   // Gunfire & Gunpowder
    [SerializeField] private int intensity = 200;
    [SerializeField] private int fanMs = 4000;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        scentSender.SendScentMessage(scentPort, intensity, fanMs);
    }
}
