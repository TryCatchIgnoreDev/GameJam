using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameJam.Story
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class StoryTriggerGizmo : MonoBehaviour
    {
        [Header("Görsel Etiket & Gizmo Ayarları")]
        public string displayName = "Trigger";
        [TextArea(1, 3)]
        public string description = "Bu trigger'ı sahnede istediğin yere taşı.";
        public Color gizmoColor = Color.cyan;
        public Vector3 gizmoSize = new Vector3(1.5f, 2f, 1.5f);
        public bool drawWireBox = true;
        public bool drawSolidBase = true;
        public bool drawForwardArrow = true;
        public bool drawLabel = false;
        public float labelHeightOffset = 1.6f;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;

            // Taban halkası / dairesi
            if (drawSolidBase)
            {
                Color baseCol = gizmoColor;
                baseCol.a = 0.25f;
                Gizmos.color = baseCol;
                Gizmos.DrawCube(transform.position + Vector3.up * 0.05f, new Vector3(gizmoSize.x, 0.1f, gizmoSize.z));
            }

            // Çerçeve kutusu
            if (drawWireBox)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(transform.position + Vector3.up * (gizmoSize.y * 0.5f), gizmoSize);
            }

            // İleri yön oku (Spawn ve kameralar için çok kritik)
            if (drawForwardArrow)
            {
                Gizmos.color = Color.yellow;
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                Vector3 dest = origin + transform.forward * 1.5f;
                Gizmos.DrawLine(origin, dest);
                Gizmos.DrawLine(dest, dest - transform.forward * 0.35f + transform.right * 0.2f);
                Gizmos.DrawLine(dest, dest - transform.forward * 0.35f - transform.right * 0.2f);
            }

#if UNITY_EDITOR
            if (drawLabel)
            {
                // Sahne görünümünde (Scene View) 3D metin etiketi çiz
                GUIStyle style = new GUIStyle();
                style.normal.textColor = gizmoColor;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;

                Vector3 labelPos = transform.position + Vector3.up * (gizmoSize.y + labelHeightOffset);
                Handles.Label(labelPos, "[" + gameObject.name + "]\n" + displayName, style);
            }
#endif
        }
    }
}
