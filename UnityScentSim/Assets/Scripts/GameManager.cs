using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private OloramaUDPSender scentSender;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        scentSender.SendScentMessage(3, 200, true, 4000);
    }
}