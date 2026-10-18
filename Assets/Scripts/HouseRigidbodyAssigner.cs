using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HouseRigidbodyAssigner : MonoBehaviour
{
    [Header("--- Event Listener Ayarları ---")]
    [Tooltip("GameEvents Event 2 (Rigidbody Atama & Zemin Dilimleme) eventini dinlesin mi?")]
    public bool listenToEvent2 = true;

    [Header("--- Opsiyonel Debug Tuşu (İsteğe Bağlı) ---")]
    [Tooltip("Test amaçlı klavye tuşu ile de tetiklensin mi?")]
    public bool enableDebugKey = false;

#if ENABLE_INPUT_SYSTEM
    public Key debugKey = Key.M;
#else
    public KeyCode debugKey = KeyCode.M;
#endif

    private bool hasExecuted = false;

    void OnEnable()
    {
        if (listenToEvent2)
        {
            GameEvents.OnEvent2_AssignRigidbodies += HandleEvent2;
        }
    }

    void OnDisable()
    {
        if (listenToEvent2)
        {
            GameEvents.OnEvent2_AssignRigidbodies -= HandleEvent2;
        }
    }

    private void HandleEvent2()
    {
        ExecuteEvent2();
    }

    public void ExecuteEvent2()
    {
        if (hasExecuted) return;
        hasExecuted = true;

        SliceFloors();
        AssignRigidbodies();
    }

    void Update()
    {
        if (enableDebugKey && !hasExecuted)
        {
            bool pressed = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(debugKey))
            {
                pressed = true;
            }
#endif
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && debugKey != Key.None && debugKey != Key.IMESelected)
            {
                var keyControl = Keyboard.current[debugKey];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    pressed = true;
                }
            }
#endif
            if (pressed)
            {
                GameEvents.TriggerEvent2();
            }
        }
    }

    [ContextMenu("Event 2: Rigidbody Ekle & Zemin Dilimle")]
    public void ContextTriggerEvent2()
    {
        GameEvents.TriggerEvent2();
    }

    void SliceFloors()
    {
        GameObject[] floors = GameObject.FindGameObjectsWithTag("floor");
        foreach (GameObject floor in floors)
        {
            if (floor == null || !floor.activeInHierarchy) continue;
            SliceFloorInto16(floor);
        }
    }

    void SliceFloorInto16(GameObject floor)
    {
        Transform t = floor.transform;
        Vector3 floorPos = t.position;
        Quaternion floorRot = t.rotation;
        Transform parent = t.parent;

        Renderer origRenderer = floor.GetComponent<Renderer>();
        Material[] sharedMats = origRenderer != null ? origRenderer.sharedMaterials : null;

        // Materyallerin çift taraflı renderlanmasını garantiye al
        if (sharedMats != null)
        {
            foreach (Material m in sharedMats)
            {
                if (m != null && m.HasProperty("_Cull"))
                {
                    m.SetFloat("_Cull", (float)CullMode.Off);
                }
            }
        }

        BoxCollider bc = floor.GetComponent<BoxCollider>();
        Vector3 localScale = t.lossyScale;
        Vector3 boxCenter = bc != null ? bc.center : Vector3.zero;
        Vector3 boxSize = bc != null ? bc.size : Vector3.one;

        float totalW = boxSize.x * localScale.x;
        float totalH = boxSize.y * localScale.y;
        float totalL = boxSize.z * localScale.z;

        int cols = 4;
        int rows = 4;
        float pieceW = totalW / cols;
        float pieceH = totalH;
        float pieceL = totalL / rows;

        GameObject container = new GameObject(floor.name + "_Slices");
        if (parent != null)
        {
            container.transform.SetParent(parent, true);
        }
        container.transform.position = floorPos;
        container.transform.rotation = floorRot;

        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                float xOffset = -totalW * 0.5f + (x + 0.5f) * pieceW + boxCenter.x * localScale.x;
                float zOffset = -totalL * 0.5f + (z + 0.5f) * pieceL + boxCenter.z * localScale.z;
                float yOffset = boxCenter.y * localScale.y;

                Vector3 worldPos = floorPos + floorRot * new Vector3(xOffset, yOffset, zOffset);

                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = string.Format("{0}_Piece_{1}_{2}", floor.name, x, z);
                piece.transform.position = worldPos;
                piece.transform.rotation = floorRot;
                piece.transform.localScale = new Vector3(pieceW, pieceH, pieceL);
                piece.transform.SetParent(container.transform, true);

                piece.isStatic = false;

                if (sharedMats != null)
                {
                    Renderer pr = piece.GetComponent<Renderer>();
                    if (pr != null)
                    {
                        pr.sharedMaterials = sharedMats;
                    }
                }

                BoxCollider pieceBc = piece.GetComponent<BoxCollider>();
                if (pieceBc != null)
                {
                    // Yükseklikte hafif tolerans (zemine batmayı önlemek için)
                    pieceBc.size = new Vector3(1f, 1.2f, 1f);
                }

                Rigidbody rb = piece.AddComponent<Rigidbody>();
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // Hafif rastgele tork ile daha organik düşüş hissi
                rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
            }
        }

        // Orijinal zemini devre dışı bırak
        floor.SetActive(false);
    }

    void AssignRigidbodies()
    {
        GameObject[] houses = GameObject.FindGameObjectsWithTag("house");
        var handledMaterials = new HashSet<Material>();

        foreach (GameObject house in houses)
        {
            if (house == null || !house.activeInHierarchy) continue;

            // Alt objeler dahil tüm hiyerarşiyi statikten çıkar (Görselin havada asılı kalmasını önler)
            foreach (Transform t in house.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.isStatic = false;
            }

            // 1. İki tarafı da renderlamak için materyallerin Cull modunu Off (0) yap
            Renderer[] renderers = house.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && handledMaterials.Add(m))
                    {
                        if (m.HasProperty("_Cull"))
                        {
                            m.SetFloat("_Cull", (float)CullMode.Off);
                        }
                    }
                }
            }

            // 2. İnce duvarların yerin içine girmesini ve titremesini engellemek için BoxCollider'ları genişlet
            BoxCollider[] boxColliders = house.GetComponentsInChildren<BoxCollider>(true);
            foreach (BoxCollider box in boxColliders)
            {
                if (box != null)
                {
                    box.size += Vector3.one * 0.2f;
                }
            }

            // 3. MeshCollider'ları convex yap
            MeshCollider[] meshColliders = house.GetComponentsInChildren<MeshCollider>(true);
            foreach (MeshCollider mc in meshColliders)
            {
                if (mc != null && !mc.convex)
                {
                    mc.convex = true;
                }
            }

            // 4. Rigidbody ata ve zemine batmayı önlemek için Continuous yap
            Rigidbody rb = house.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = house.AddComponent<Rigidbody>();
            }
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
}
