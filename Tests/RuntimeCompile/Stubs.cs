using System;

namespace UnityEngine
{
    public class Object { }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform;
        public T GetComponentInParent<T>() where T : Component => null;
        public T GetComponent<T>() where T : Component => null;
        public T GetComponentInChildren<T>(bool includeInactive) where T : Component => null;
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => Array.Empty<T>();
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public class CanvasGroup : Behaviour { public float alpha; }
    public class RectTransform : Transform { public Vector2 sizeDelta; }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public class GameObject : Object { public bool activeSelf; public bool activeInHierarchy; public void SetActive(bool value) { activeSelf = value; activeInHierarchy = value; } public T GetComponent<T>() where T : Component => null; }
    public enum KeyCode { Tab }
    public static class Input { public static bool GetKeyDown(KeyCode key) => false; public static bool GetKey(KeyCode key) => false; }
    public class Transform : Component
    {
        public Transform parent;
        public Transform root => this;
        public int childCount;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 InverseTransformPoint(Vector3 position) => default;
        public Transform GetChild(int index) => null;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 back => new Vector3();
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3();
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3();
        public static Vector3 operator *(Vector3 a, float b) => new Vector3();
    }
    public struct Quaternion
    {
        public static Vector3 operator *(Quaternion a, Vector3 b) => new Vector3();
    }
    public static class Time { public static float time; }
    public struct Color { public Color(float r, float g, float b, float a = 1f) { } public static Color white => new Color(); }
    public class BoxCollider : Behaviour { public Vector3 center; public Vector3 size; }
    public static class Mathf
    {
        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static float Clamp01(float value) => Math.Clamp(value, 0, 1);
        public static float Abs(float value) => Math.Abs(value);
    }
    public static class Debug { public static void Log(string message) { } public static void LogError(string message) { } }
    public class AudioClip : Object { }
    public class AudioSource : Behaviour
    {
        public float volume;
        public void PlayOneShot(AudioClip clip) { }
    }
    public class ScriptableObject : Object { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public class HideInInspectorAttribute : Attribute { }
    public class SerializeField : Attribute { }
}

namespace TMPro
{
    public class TMP_Text : UnityEngine.Behaviour { public string text; public UnityEngine.Color color; }
    public class TMP_InputField : UnityEngine.Behaviour { public string text; }
}

namespace UnityEngine.UI
{
    public class Image : UnityEngine.Behaviour { public UnityEngine.Color color; public float fillAmount; }
}

namespace UdonSharp
{
    public class UdonSharpBehaviour : UnityEngine.MonoBehaviour
    {
        public virtual void OnPlayerJoined(VRC.SDKBase.VRCPlayerApi player) { }
        public virtual void OnPlayerLeft(VRC.SDKBase.VRCPlayerApi player) { }
        public virtual void OnPlayerRestored(VRC.SDKBase.VRCPlayerApi player) { }
        public virtual void OnPlayerTriggerEnter(VRC.SDKBase.VRCPlayerApi player) { }
        public virtual void OnDeserialization() { }
        public void RequestSerialization() { }
        public void SendCustomEventDelayedSeconds(string eventName, float seconds) { }
    }
    public enum BehaviourSyncMode { Manual }
    public class UdonBehaviourSyncModeAttribute : Attribute { public UdonBehaviourSyncModeAttribute(BehaviourSyncMode mode) { } }
    public class UdonSyncedAttribute : Attribute { }
}

namespace VRC.SDKBase
{
    public static class Utilities { public static bool IsValid(object value) => value != null; }
    public static class Networking
    {
        public static VRCPlayerApi LocalPlayer;
        public static DateTime GetNetworkDateTime() => DateTime.UtcNow;
        public static void SetOwner(VRCPlayerApi player, UnityEngine.GameObject obj) { }
        public static bool IsOwner(UnityEngine.GameObject obj) => true;
    }
    public class VRCPlayerApi
    {
        public static VRCPlayerApi[] TestPlayers = Array.Empty<VRCPlayerApi>();
        public int playerId;
        public bool isLocal;
        public string displayName;
        public int teleportCount;
        public static void GetPlayers(VRCPlayerApi[] players) { }
        public static int GetPlayerCount() => 0;
        public static VRCPlayerApi GetPlayerById(int id)
        {
            foreach (VRCPlayerApi player in TestPlayers) if (player.playerId == id) return player;
            return null;
        }
        public UnityEngine.Vector3 GetPosition() => default;
        public UnityEngine.Quaternion GetRotation() => default;
        public void TeleportTo(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation) { teleportCount++; }
        public void Respawn() { }
        public struct TrackingData
        {
            public UnityEngine.Vector3 position;
            public UnityEngine.Quaternion rotation;
        }
        public enum TrackingDataType { Head }
        public TrackingData GetTrackingData(TrackingDataType type) => default;
    }
}

namespace VRC.SDK3.Persistence
{
    public static class PlayerData
    {
        public struct Info { }
        public static int GetInt(VRC.SDKBase.VRCPlayerApi player, string key) => 0;
        public static bool TryGetBool(VRC.SDKBase.VRCPlayerApi player, string key, out bool value) { value = false; return false; }
        public static bool TryGetFloat(VRC.SDKBase.VRCPlayerApi player, string key, out float value) { value = 0; return false; }
        public static void SetInt(string key, int value) { }
        public static void SetBool(string key, bool value) { }
        public static void SetFloat(string key, float value) { }
    }
}
