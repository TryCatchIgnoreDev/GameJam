using UnityEngine;

namespace GameJam.Market
{
    [RequireComponent(typeof(Collider))]
    public class MarketTrigger : MonoBehaviour
    {
        [Header("--- Referanslar ---")]
        public MarketUIManager marketUI;

        [Header("--- Trigger Ayarları ---")]
        [Tooltip("Tetiklendikten sonra tekrar açılabilmesi için gereken bekleme süresi (saniye)")]
        public float triggerCooldown = 1.0f;

        [Header("--- Görsel Efektler (Opsiyonel) ---")]
        public Transform visualMesh;
        public float bobbingSpeed = 2f;
        public float bobbingAmount = 0.15f;
        public float rotationSpeed = 45f;

        private float lastTriggerTime = -10f;
        private Vector3 initialVisualPos;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (marketUI == null)
            {
                marketUI = GetComponentInChildren<MarketUIManager>(true);
            }

            if (visualMesh != null)
            {
                initialVisualPos = visualMesh.localPosition;
            }
        }

        private void Update()
        {
            if (visualMesh != null)
            {
                visualMesh.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                float newY = initialVisualPos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
                visualMesh.localPosition = new Vector3(initialVisualPos.x, newY, initialVisualPos.z);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - lastTriggerTime < triggerCooldown)
                return;

            if (IsPlayer(other))
            {
                lastTriggerTime = Time.time;

                MarketUIManager ui = marketUI != null ? marketUI : MarketUIManager.Instance;
                if (ui != null && !ui.IsMarketOpen)
                {
                    ui.OpenMarket();
                }
            }
        }

        private bool IsPlayer(Collider col)
        {
            if (col.CompareTag("Player"))
                return true;

            if (col.GetComponentInParent<FirstPersonMovement>() != null)
                return true;

            return false;
        }
    }
}
