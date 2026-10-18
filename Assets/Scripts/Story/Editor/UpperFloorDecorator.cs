using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Story.Editor
{
    public static class UpperFloorDecorator
    {
        public const float FLOOR_Y = -69.98f;
        public const float SCALE = 1.35f;

        [MenuItem("Tools/Story/Decorate Upper Floor")]
        public static void Decorate()
        {
            var scene = EditorSceneManager.GetSceneByPath("Assets/Scenes/EvMarket.unity");
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/EvMarket.unity", OpenSceneMode.Additive);
            }
            EditorSceneManager.SetActiveScene(scene);

            // 1. Remove existing upper floor decoration if any
            var existing = GameObject.Find("[UPPER_FLOOR_DECORATION]");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // Hide old upper lounge clutter under House if present
            var oldB = GameObject.Find("/House/blue beaucoup");
            if (oldB != null) Undo.RecordObject(oldB, "Hide oldB"); oldB?.SetActive(false);
            var oldW = GameObject.Find("/House/white beaucoup");
            if (oldW != null) Undo.RecordObject(oldW, "Hide oldW"); oldW?.SetActive(false);
            var oldR = GameObject.Find("/House/rose wood boîte");
            if (oldR != null) Undo.RecordObject(oldR, "Hide oldR"); oldR?.SetActive(false);
            var oldRug = GameObject.Find("/House/colurfull rug.07");
            if (oldRug != null) Undo.RecordObject(oldRug, "Hide oldRug"); oldRug?.SetActive(false);

            // 2. Create master root
            var root = new GameObject("[UPPER_FLOOR_DECORATION]");
            Undo.RegisterCreatedObjectUndo(root, "Create Upper Floor Decoration");
            SceneManager.MoveGameObjectToScene(root, scene);

            // Sub-roots
            var bedroomRoot = CreateSubRoot(root, "01_Master_Bedroom");
            var loungeRoot = CreateSubRoot(root, "02_Upper_Lounge");
            var diningRoot = CreateSubRoot(root, "03_Dining_Hallway");
            var stairsRoot = CreateSubRoot(root, "04_Stair_Landing");
            var lightsRoot = CreateSubRoot(root, "05_Upper_Atmospheric_Lights");

            // Build Zones
            BuildBedroom(bedroomRoot);
            BuildLounge(loungeRoot);
            BuildDiningEnhancements(diningRoot);
            BuildStairHallway(stairsRoot);
            BuildLights(lightsRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UpperFloorDecorator] Upper floor successfully decorated and saved!");
        }

        private static GameObject CreateSubRoot(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static GameObject Spawn(string prefabPath, Vector3 targetPos, Vector3 targetRot, GameObject parent, float scale = SCALE, bool autoGround = true, bool isRug = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[UpperFloorDecorator] Prefab not found: {prefabPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return null;

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Upper Decor");
            instance.tag = "house";
            instance.transform.SetParent(parent.transform);
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.eulerAngles = targetRot;
            instance.transform.position = targetPos;

            if (autoGround)
            {
                var rends = instance.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    float minY = float.MaxValue;
                    foreach (var r in rends)
                    {
                        if (r.bounds.min.y < minY) minY = r.bounds.min.y;
                    }

                    float desiredY = isRug ? (FLOOR_Y + 0.015f) : FLOOR_Y;
                    instance.transform.position += new Vector3(0, desiredY - minY, 0);
                }
            }

            return instance;
        }

        private static GameObject SpawnOnSurface(string prefabPath, Vector3 targetPos, Vector3 targetRot, GameObject parent, float surfaceY, float scale = SCALE)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return null;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return null;

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Tabletop Upper Decor");
            instance.tag = "house";
            instance.transform.SetParent(parent.transform);
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.eulerAngles = targetRot;
            instance.transform.position = targetPos;

            var rends = instance.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                float minY = float.MaxValue;
                foreach (var r in rends)
                {
                    if (r.bounds.min.y < minY) minY = r.bounds.min.y;
                }
                instance.transform.position += new Vector3(0, surfaceY - minY, 0);
            }

            return instance;
        }

        private static float GetSurfaceMaxY(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            float maxY = float.MinValue;
            foreach (var r in rends)
            {
                if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;
            }
            return maxY;
        }

        private static void BuildBedroom(GameObject parent)
        {
            // Bed (white Bonsoir) is at (14.74, -69.17, 15.31)
            // 1. Cozy Bedroom Rug under/in front of the bed
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.05.prefab",
                new Vector3(13.8f, FLOOR_Y, 13.5f), Vector3.zero, parent, scale: 1.5f, isRug: true);

            // 2. Left Nightstand beside bed
            var nightstand1 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer1.prefab",
                new Vector3(11.8f, FLOOR_Y, 15.3f), new Vector3(0, 180, 0), parent);
            if (nightstand1 != null)
            {
                float nsY = GetSurfaceMaxY(nightstand1);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/sofas/lamp.001.prefab",
                    new Vector3(11.8f, nsY, 15.3f), Vector3.zero, parent, nsY, scale: 0.9f);
            }

            // 3. Right Nightstand beside bed
            var nightstand2 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer1.prefab",
                new Vector3(16.5f, FLOOR_Y, 15.3f), new Vector3(0, 180, 0), parent);
            if (nightstand2 != null)
            {
                float nsY = GetSurfaceMaxY(nightstand2);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PenHolder.prefab",
                    new Vector3(16.5f, nsY, 15.3f), Vector3.zero, parent, nsY, scale: 1.0f);
            }

            // 4. Reading Chair in Bedroom Corner
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(9.2f, FLOOR_Y, 13.2f), new Vector3(0, 45, 0), parent);

            // 5. Wardrobe / Dresser along West dividing wall of Bedroom (X ≈ 7.8)
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer2.prefab",
                new Vector3(7.8f, FLOOR_Y, 8.5f), new Vector3(0, 90, 0), parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves1.prefab",
                new Vector3(7.8f, FLOOR_Y, 12.0f), new Vector3(0, 90, 0), parent);

            // 6. Plants in bedroom
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(8.0f, FLOOR_Y, 15.5f), Vector3.zero, parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PlantBox.prefab",
                new Vector3(16.0f, FLOOR_Y, 10.0f), new Vector3(0, 270, 0), parent);

            // 7. Proper Study Desk workstation near bedroom door
            var desk = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Desk1.prefab",
                new Vector3(15.8f, FLOOR_Y, 4.5f), new Vector3(0, 180, 0), parent);
            if (desk != null)
            {
                float deskY = GetSurfaceMaxY(desk);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PC.prefab",
                    new Vector3(15.8f, deskY, 4.5f), new Vector3(0, 180, 0), parent, deskY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DeskLight.prefab",
                    new Vector3(15.8f, deskY, 5.3f), new Vector3(0, 180, 0), parent, deskY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(15.6f, deskY, 3.8f), new Vector3(0, 30, 0), parent, deskY);
            }
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)OfficeChair.prefab",
                new Vector3(14.6f, FLOOR_Y, 4.5f), new Vector3(0, 0, 0), parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PCCase.prefab",
                new Vector3(15.8f, FLOOR_Y, 3.5f), new Vector3(0, 180, 0), parent);
        }

        private static void BuildLounge(GameObject parent)
        {
            // Upper Lounge (X: -4.5 to 5.5, Z: 3.0 to 16.5)
            // 1. Media Center against West wall (X = -4.2)
            var tvStand = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer1.prefab",
                new Vector3(-4.2f, FLOOR_Y, 10.5f), new Vector3(0, 90, 0), parent);
            if (tvStand != null)
            {
                float standY = GetSurfaceMaxY(tvStand);
                SpawnOnSurface("Assets/contemporary house/prefabs/small props/TV .prefab",
                    new Vector3(-4.2f, standY, 10.5f), Vector3.zero, parent, standY);
            }

            // Flanking Bookcases
            var shelf1 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves1.prefab",
                new Vector3(-4.2f, FLOOR_Y, 7.5f), new Vector3(0, 90, 0), parent);
            var shelf2 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves2.prefab",
                new Vector3(-4.2f, FLOOR_Y, 13.5f), new Vector3(0, 90, 0), parent);

            if (shelf1 != null)
            {
                SpawnOnSurface("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_01.prefab",
                    new Vector3(-4.2f, FLOOR_Y + 1.2f, 7.5f), Vector3.zero, parent, FLOOR_Y + 1.2f);
            }
            if (shelf2 != null)
            {
                SpawnOnSurface("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_02.prefab",
                    new Vector3(-4.2f, FLOOR_Y + 1.2f, 13.5f), Vector3.zero, parent, FLOOR_Y + 1.2f);
            }

            // 2. Center Seating Area
            // Area Rug
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.01.prefab",
                new Vector3(0.5f, FLOOR_Y, 10.5f), Vector3.zero, parent, scale: 1.5f, isRug: true);

            // Main Sofa facing TV (West -X)
            Spawn("Assets/contemporary house/prefabs/room props/sofas/white beaucoup.prefab",
                new Vector3(3.0f, FLOOR_Y, 10.5f), new Vector3(0, 180, 0), parent);

            // Coffee Table
            var coffeeTable = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)CoffeTable.prefab",
                new Vector3(0.5f, FLOOR_Y, 10.5f), new Vector3(0, 90, 0), parent);
            if (coffeeTable != null)
            {
                float ctY = GetSurfaceMaxY(coffeeTable);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(0.5f, ctY, 10.5f), Vector3.zero, parent, ctY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(0.2f, ctY, 10.3f), new Vector3(0, 45, 0), parent, ctY);
            }

            // Armchairs framing table
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(0.5f, FLOOR_Y, 12.8f), new Vector3(0, 90, 0), parent);
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(0.5f, FLOOR_Y, 8.2f), new Vector3(0, 270, 0), parent);

            // Floor Lamp
            Spawn("Assets/contemporary house/prefabs/room props/sofas/lamp.001.prefab",
                new Vector3(3.0f, FLOOR_Y, 13.0f), Vector3.zero, parent);

            // Plants
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(4.5f, FLOOR_Y, 15.0f), Vector3.zero, parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PlantBox.prefab",
                new Vector3(-2.0f, FLOOR_Y, 15.5f), Vector3.zero, parent);
        }

        private static void BuildDiningEnhancements(GameObject parent)
        {
            // Credenza along East dividing wall (X ≈ 5.5, Z = -3.5)
            var sideboard = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer2.prefab",
                new Vector3(5.5f, FLOOR_Y, -3.5f), new Vector3(0, 270, 0), parent);
            if (sideboard != null)
            {
                float sbY = GetSurfaceMaxY(sideboard);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PenHolder.prefab",
                    new Vector3(5.5f, sbY, -3.5f), Vector3.zero, parent, sbY);
            }

            // Decorative Corner Plant
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(-4.0f, FLOOR_Y, -4.0f), Vector3.zero, parent);
        }

        private static void BuildStairHallway(GameObject parent)
        {
            // Runner Rug along Hallway path towards stairs
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.08.prefab",
                new Vector3(10.0f, FLOOR_Y, -8.0f), Vector3.zero, parent, scale: 1.4f, isRug: true);

            // Console Table on East Hallway Wall (facing West)
            var console = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer2.prefab",
                new Vector3(15.8f, FLOOR_Y, -8.0f), new Vector3(0, 270, 0), parent);
            if (console != null)
            {
                float cY = GetSurfaceMaxY(console);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PenHolder.prefab",
                    new Vector3(15.6f, cY, -7.8f), Vector3.zero, parent, cY);
            }

            // Wall Clock above console (mounted on wall, NO autoGround)
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Clock.prefab",
                new Vector3(16.5f, -67.5f, -8.0f), new Vector3(0, 180, 0), parent, autoGround: false);

            // Planters along hallway
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PlantBox.prefab",
                new Vector3(15.8f, FLOOR_Y, -4.0f), new Vector3(0, 270, 0), parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(7.5f, FLOOR_Y, -14.0f), Vector3.zero, parent);
        }

        private static void BuildLights(GameObject parent)
        {
            Color warmLight = new Color(1.0f, 0.76f, 0.45f);

            // Master Bedroom Lights
            CreateLight("Bedroom_Main_Ceiling_Light", new Vector3(12.5f, -66.5f, 11.0f), warmLight, 12f, 3.6f, parent);
            CreateLight("Bedroom_Bedside_Warm_Light", new Vector3(12.0f, -68.0f, 15.0f), warmLight, 6f, 2.2f, parent);
            CreateLight("Bedroom_Desk_Accent_Light", new Vector3(15.5f, -68.0f, 4.5f), new Color(1f, 0.88f, 0.65f), 5f, 2.5f, parent);

            // Upper Lounge Light
            CreateLight("Upper_Lounge_Ceiling_Light", new Vector3(0.5f, -66.5f, 10.5f), warmLight, 12f, 3.8f, parent);
            CreateLight("Upper_Lounge_Lamp_Light", new Vector3(3.0f, -67.8f, 13.0f), warmLight, 6f, 2.2f, parent);

            // Dining Table Light Boost
            CreateLight("Dining_Table_Warm_Light", new Vector3(4.5f, -66.5f, -7.5f), warmLight, 10f, 4.0f, parent);

            // Hallway & Stair Lights
            CreateLight("Hallway_North_Light", new Vector3(10.0f, -66.8f, -4.0f), warmLight, 9f, 3.2f, parent);
            CreateLight("Hallway_Stairs_Light", new Vector3(10.0f, -66.8f, -12.0f), warmLight, 10f, 3.5f, parent);
        }

        private static void CreateLight(string name, Vector3 pos, Color color, float range, float intensity, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
        }

        [MenuItem("Tools/Story/Capture Upper Floor Screenshots")]
        public static void CaptureScreenshots()
        {
            var camGo = new GameObject("InspectionUpperCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            var dir = "Screenshots";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 1. Master Bedroom Bed & Decor View
            RenderView(cam, new Vector3(10.5f, -68.0f, 9.0f), new Vector3(14.5f, -68.8f, 15.0f), $"{dir}/v1_upper_bedroom_master.png");

            // 2. Master Bedroom Study Workstation View
            RenderView(cam, new Vector3(12.0f, -68.2f, 6.5f), new Vector3(15.8f, -68.8f, 4.5f), $"{dir}/v1_upper_bedroom_study.png");

            // 3. Upper Lounge Looking at TV & Coffee Table
            RenderView(cam, new Vector3(3.5f, -68.0f, 10.5f), new Vector3(-4.0f, -68.8f, 10.5f), $"{dir}/v1_upper_lounge_tv.png");

            // 4. Upper Lounge Looking towards Sofa & Armchairs
            RenderView(cam, new Vector3(-3.0f, -68.0f, 10.5f), new Vector3(3.0f, -68.8f, 10.5f), $"{dir}/v1_upper_lounge_sofa.png");

            // 5. Dining Table Player Perspective (Wake-up View)
            RenderView(cam, new Vector3(4.5f, -68.3f, -9.5f), new Vector3(4.0f, -68.8f, -6.5f), $"{dir}/v1_upper_dining_friends.png");

            // 6. Hallway looking towards Stairs Landing
            RenderView(cam, new Vector3(10.0f, -68.0f, -3.0f), new Vector3(10.0f, -68.8f, -14.0f), $"{dir}/v1_upper_stairs_hallway.png");

            Object.DestroyImmediate(camGo);
            Debug.Log("[UpperFloorDecorator] All 6 upper floor inspection screenshots rendered successfully!");
        }

        private static void RenderView(Camera cam, Vector3 pos, Vector3 lookAt, string filePath)
        {
            cam.transform.position = pos;
            cam.transform.LookAt(lookAt);

            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            cam.Render();

            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = prev;
            Object.DestroyImmediate(rt);

            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
