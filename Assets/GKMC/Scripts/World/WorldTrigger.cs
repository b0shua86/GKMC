using UnityEngine;

namespace GKMC
{
    /// <summary>An invisible volume around each world. Entering it switches the track + atmosphere.</summary>
    public class WorldTrigger : MonoBehaviour
    {
        public int trackNumber;

        public static WorldTrigger Create(Transform parent, int trackNumber, Vector3 center, Vector3 size)
        {
            var go = new GameObject($"WorldTrigger_{trackNumber}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var t = go.AddComponent<WorldTrigger>();
            t.trackNumber = trackNumber;
            return t;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GKMCExperience.Instance != null)
                GKMCExperience.Instance.EnterWorld(trackNumber);
        }
    }
}
