using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Sends real UDP activation commands to an Olorama Professional 10-Scents
/// Generator, per the user guide's "Activation via API" section:
///   OUT,[port 01-10],[intensity 0100-0500],1,[fan time 1000-9000 ms],1000
/// Mirrors tools/protocol.js and tools/activate-scent.js in this repo so
/// Unity and the Node CLI stay in sync rather than drifting apart.
/// </summary>
public class OloramaUDPSender : MonoBehaviour
{
    [Header("Olorama UDP Settings")]
    [Tooltip("Device IP, or a broadcast address (x.x.x.255) if the unit hasn't picked up a normal DHCP lease and is only reachable via link-local broadcast.")]
    public string targetIP = "169.254.255.255";
    public int targetPort = 5010;

    /// <summary>
    /// The scene's sender, so a ScriptableObject (which can't hold a scene
    /// reference in the Inspector) can reach it — see ScentMaker.Activate().
    /// Put exactly one OloramaUDPSender in the scene.
    /// </summary>
    public static OloramaUDPSender Instance { get; private set; }

    const int PortMin = 1, PortMax = 10;
    const int IntensityMin = 100, IntensityMax = 500;
    const int FanMsMin = 1000, FanMsMax = 9000;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Olorama] a second OloramaUDPSender ({name}) exists; keeping {Instance.name}.");
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Builds the wire message without sending it, so the exact string can
    /// be unit-tested (see Tests/OloramaProtocolTests.cs) without needing a
    /// real socket or a Unity play session.
    /// </summary>
    public static string BuildMessage(int scentPort, int intensity, int fanMs)
    {
        scentPort = Mathf.Clamp(scentPort, PortMin, PortMax);
        intensity = Mathf.Clamp(intensity, IntensityMin, IntensityMax);
        fanMs = Mathf.Clamp(fanMs, FanMsMin, FanMsMax);

        // No spaces after the commas: this is the exact format that was
        // confirmed against real hardware (see tools/protocol.js). A
        // version of this file with "OUT, {0:00}, ..." — spaces after each
        // comma — sent a message the device's parser silently ignored.
        // "1" and the trailing "1000" are the two internal fields the
        // manual says never to change; they aren't exposed as parameters.
        return string.Format("OUT,{0:00},{1:0000},1,{2:00000},1000", scentPort, intensity, fanMs);
    }

    public void SendScentMessage(int scentPort, int intensity = 100, int fanMs = 2000)
    {
        if (scentPort < PortMin || scentPort > PortMax)
            Debug.LogWarning($"[Olorama] port {scentPort} is outside {PortMin}-{PortMax}; clamped.");
        if (intensity < IntensityMin || intensity > IntensityMax)
            Debug.LogWarning($"[Olorama] intensity {intensity} is outside {IntensityMin}-{IntensityMax}; clamped.");
        if (fanMs < FanMsMin || fanMs > FanMsMax)
            Debug.LogWarning($"[Olorama] fanMs {fanMs} is outside {FanMsMin}-{FanMsMax}; clamped.");

        string message = BuildMessage(scentPort, intensity, fanMs);

        // Trim before parsing: a copy-pasted IP picks up trailing
        // whitespace/newlines surprisingly often, and IPAddress.Parse
        // rejects that with the unhelpful "An invalid IP address was
        // specified" — with no indication of *which* string it choked on.
        string ip = (targetIP ?? string.Empty).Trim();
        if (!IPAddress.TryParse(ip, out IPAddress address))
        {
            Debug.LogError($"[Olorama] Target IP \"{targetIP}\" is not a valid IP address — check the OloramaUDPSender Inspector field for a stray space, quote mark, or typo.");
            return;
        }

        Debug.Log($"[Olorama] Sending UDP to {ip}:{targetPort} -> {message}");

        try
        {
            using (UdpClient client = new UdpClient())
            {
                // Required to send to a broadcast address (e.g. *.255) —
                // without this, Send() throws a SocketException on most
                // platforms. Harmless when targetIP is a normal unicast
                // address instead.
                client.EnableBroadcast = true;
                byte[] data = Encoding.ASCII.GetBytes(message);
                client.Send(data, data.Length, new IPEndPoint(address, targetPort));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Olorama] UDP send failed: {ex.Message}");
        }
    }
}
