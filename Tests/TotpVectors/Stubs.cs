namespace UdonSharp
{
    public class UdonSharpBehaviour { }
}

namespace UnityEngine
{
    public class SerializeField : System.Attribute { }
    public static class Debug
    {
        public static void Log(string message) { }
        public static void LogError(string message) { }
    }
}

namespace VRC.SDKBase
{
    public static class Networking
    {
        public static System.DateTime Now;
        public static System.DateTime GetNetworkDateTime() => Now;
    }
}

namespace BH2VSQ.Base
{
    public enum BaseRank { Visitor, Member, Admin }
    public class AuthenticationSession
    {
        public BaseRank rank;
        public long authenticatedTicks;
        public PlayerRegistry registry;
        public void Authenticate(BaseRank value) => rank = value;
    }
    public class PlayerRegistry
    {
        public void PublishRank(BaseRank rank) { }
    }
}
