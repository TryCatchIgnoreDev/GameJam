using UnityEngine;

namespace GameJam.Story
{
    public class StoryAreaTrigger : MonoBehaviour
    {
        public enum AreaTriggerType
        {
            FriendsRoom = 0,
            StairsContractor = 1,
            StreetReturnDoor = 2
        }

        [Header("Tetikleyici Ayarları")]
        public AreaTriggerType triggerType = AreaTriggerType.FriendsRoom;
        public bool triggerOnlyOnce = true;
        [SerializeField] private bool hasTriggered = false;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnlyOnce && hasTriggered) return;

            // Oyuncu kontrolü
            if (other.CompareTag("Player") || other.GetComponentInParent<FirstPersonMovement>() != null || other.GetComponent<Camera>() != null)
            {
                hasTriggered = true;
                if (EvMarketStoryManager.Instance != null)
                {
                    EvMarketStoryManager.Instance.OnAreaTriggerEnter(triggerType);
                }
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }
    }
}
