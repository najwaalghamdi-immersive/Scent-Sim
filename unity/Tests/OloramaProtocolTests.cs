using NUnit.Framework;

/// <summary>
/// EditMode tests for OloramaUDPSender.BuildMessage — mirrors
/// test/protocol.test.js so the Node and Unity implementations are checked
/// against the same cases. Requires Unity's Test Framework package
/// (com.unity.test-framework) and must live under an Editor assembly (e.g.
/// move/reference this file from an "Editor" folder, or an asmdef with
/// "includePlatforms": ["Editor"]) to run via Window > General > Test Runner.
/// </summary>
public class OloramaProtocolTests
{
    [Test]
    public void BuildMessage_PadsFieldsPerTheOloramaSpec()
    {
        Assert.AreEqual("OUT,02,0150,1,03000,1000", OloramaUDPSender.BuildMessage(2, 150, 3000));
        Assert.AreEqual("OUT,10,0100,1,01000,1000", OloramaUDPSender.BuildMessage(10, 100, 1000));
    }

    [Test]
    public void BuildMessage_HasNoSpacesAfterCommas()
    {
        // The bug that shipped in the first draft of OloramaUDPSender: a
        // format string with "OUT, {0:00}, ..." (spaces after commas) built
        // a message the real device's parser silently ignored.
        string message = OloramaUDPSender.BuildMessage(3, 200, 4000);
        Assert.AreEqual("OUT,03,0200,1,04000,1000", message);
        StringAssert.DoesNotContain(" ", message);
    }

    [Test]
    public void BuildMessage_ClampsOutOfRangeValuesInsteadOfSendingGarbage()
    {
        Assert.AreEqual("OUT,01,0100,1,01000,1000", OloramaUDPSender.BuildMessage(0, 50, 500));
        Assert.AreEqual("OUT,10,0500,1,09000,1000", OloramaUDPSender.BuildMessage(15, 999, 99999));
    }
}
