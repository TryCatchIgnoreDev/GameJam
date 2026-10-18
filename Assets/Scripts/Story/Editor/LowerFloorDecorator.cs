using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Story.Editor
{
    public static class LowerFloorDecorator
    {
        public const float FLOOR_Y = -75.17f;
        public const float SCALE = 1.5f;

        [MenuItem("Tools/Story/Decorate Lower Floor (1.5x)")]
        public static void Decorate()
        {
            var scene = EditorSceneManager.GetSceneByPath("Assets/Scenes/EvMarket.unity");
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/EvMarket.unity", OpenSceneMode.Additive);
            }
            EditorSceneManager.SetActiveScene(scene);

            // 1. Remove existing lower floor decoration if any
            var existing = GameObject.Find("[LOWER_FLOOR_DECORATION]");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // 2. Create master root
            var root = new GameObject("[LOWER_FLOOR_DECORATION]");
            Undo.RegisterCreatedObjectUndo(root, "Create Lower Floor Decoration");
            SceneManager.MoveGameObjectToScene(root, scene);

            // Sub-roots
            var kitchenRoot = CreateSubRoot(root, "01_Kitchen_Dining");
            var hallwayRoot = CreateSubRoot(root, "02_Entrance_Hallway");
            var loungeRoot = CreateSubRoot(root, "03_Living_Lounge");
            var studyRoot = CreateSubRoot(root, "04_Study_Library");
            var lightsRoot = CreateSubRoot(root, "05_Atmospheric_Lights");

            // Build Zones
            BuildKitchen(kitchenRoot);
            BuildHallway(hallwayRoot);
            BuildLounge(loungeRoot);
            BuildStudy(studyRoot);
            BuildLights(lightsRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LowerFloorDecorator] Lower floor successfully decorated at 1.5x scale and saved!");
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
                Debug.LogError($"[LowerFloorDecorator] Prefab not found: {prefabPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return null;

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Decor");
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
            if (prefab == null)
            {
                Debug.LogError($"[LowerFloorDecorator] Surface Prefab not found: {prefabPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return null;

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Tabletop Decor");
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

        private static void BuildKitchen(GameObject parent)
        {
            // Contemporary house prefabs: 0 = +X (East), 90 = -Z (South), 180 = -X (West), 270 = +Z (North)
            // OfficeEssentialsPack: 0 = +Z (North), 90 = +X (East), 180 = -Z (South), 270 = -X (West)

            // East Wall Appliances (facing West -X)
            // Refrigerator (contemporary house: 180 = West)
            Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/frige .prefab",
                new Vector3(15.9f, FLOOR_Y, 5.6f), new Vector3(0, 180, 0), parent);

            // Oven / Stove (contemporary house: 180 = West)
            Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/oven .prefab",
                new Vector3(15.9f, FLOOR_Y, 7.6f), new Vector3(0, 180, 0), parent);

            // Counter Cabinets (contemporary house: 180 = West)
            var counter1 = Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/cabnet small.prefab",
                new Vector3(15.9f, FLOOR_Y, 9.4f), new Vector3(0, 180, 0), parent);
            var counter2 = Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/cabnet small.prefab",
                new Vector3(15.9f, FLOOR_Y, 11.2f), new Vector3(0, 180, 0), parent);

            // Wall Cabinets above counter (mounted at Y = -72.0f, NO autoGround)
            Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/cabnet top.prefab",
                new Vector3(16.0f, -72.0f, 7.6f), new Vector3(0, 180, 0), parent, autoGround: false);
            Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/cabnet top.prefab",
                new Vector3(16.0f, -72.0f, 9.4f), new Vector3(0, 180, 0), parent, autoGround: false);
            Spawn("Assets/contemporary house/prefabs/room props/kichen and bathroom/cabnet top.prefab",
                new Vector3(16.0f, -72.0f, 11.2f), new Vector3(0, 180, 0), parent, autoGround: false);

            // Water Dispenser & Trash Can (OfficeEssentials: 270 = West)
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)WaterDispenser.prefab",
                new Vector3(16.1f, FLOOR_Y, 13.0f), new Vector3(0, 270, 0), parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)TrashCan.prefab",
                new Vector3(16.1f, FLOOR_Y, 4.2f), new Vector3(0, 270, 0), parent);

            // Counter decorations
            if (counter1 != null)
            {
                float counterY = GetSurfaceMaxY(counter1);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(15.7f, counterY, 9.4f), Vector3.zero, parent, counterY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(15.7f, counterY, 9.8f), new Vector3(0, 45, 0), parent, counterY);
            }

            // Dining Setup
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.04.prefab",
                new Vector3(11.5f, FLOOR_Y, 10.5f), Vector3.zero, parent, isRug: true);

            var diningTable = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)ConferenceTable.prefab",
                new Vector3(11.5f, FLOOR_Y, 10.5f), new Vector3(0, 90, 0), parent);

            // Dining Chairs (charcol ferais: 90 = South, 270 = North)
            // North chairs facing South (-Z) towards table
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(10.5f, FLOOR_Y, 11.7f), new Vector3(0, 90, 0), parent);
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(12.5f, FLOOR_Y, 11.7f), new Vector3(0, 90, 0), parent);

            // South chairs facing North (+Z) towards table
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(10.5f, FLOOR_Y, 9.3f), new Vector3(0, 270, 0), parent);
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(12.5f, FLOOR_Y, 9.3f), new Vector3(0, 270, 0), parent);

            // Dining table items
            if (diningTable != null)
            {
                float tableY = GetSurfaceMaxY(diningTable);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(10.5f, tableY, 11.0f), Vector3.zero, parent, tableY);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(12.5f, tableY, 11.0f), Vector3.zero, parent, tableY);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(10.5f, tableY, 10.0f), Vector3.zero, parent, tableY);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(12.5f, tableY, 10.0f), Vector3.zero, parent, tableY);

                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(11.1f, tableY, 10.5f), new Vector3(0, 30, 0), parent, tableY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(11.9f, tableY, 10.5f), new Vector3(0, 210, 0), parent, tableY);
            }

            // Dining ceiling light (mounted at Y = -71.2f, NO autoGround)
            Spawn("Assets/contemporary house/prefabs/lights/pendant light.prefab",
                new Vector3(11.5f, -71.2f, 10.5f), Vector3.zero, parent, autoGround: false);

            // Corner storage & plant against North wall
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves1.prefab",
                new Vector3(14.5f, FLOOR_Y, 16.0f), new Vector3(0, 180, 0), parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(8.5f, FLOOR_Y, 15.5f), Vector3.zero, parent);
        }

        private static void BuildHallway(GameObject parent)
        {
            // Runner Rug from stair landing towards door
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.08.prefab",
                new Vector3(10.1f, FLOOR_Y, -10.5f), Vector3.zero, parent, isRug: true);

            // Console Dresser against East wall (facing West -X)
            var console = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer2.prefab",
                new Vector3(15.8f, FLOOR_Y, -10.5f), new Vector3(0, 270, 0), parent);

            if (console != null)
            {
                float consoleY = GetSurfaceMaxY(console);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PenHolder.prefab",
                    new Vector3(15.6f, consoleY, -10.2f), Vector3.zero, parent, consoleY);
            }

            // Wall Clock above console on East wall (facing West -X, NO autoGround)
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Clock.prefab",
                new Vector3(16.5f, -72.6f, -10.5f), new Vector3(0, 180, 0), parent, autoGround: false);

            // Plants
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PlantBox.prefab",
                new Vector3(15.8f, FLOOR_Y, -7.0f), new Vector3(0, 270, 0), parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(7.8f, FLOOR_Y, -14.0f), Vector3.zero, parent);
        }

        private static void BuildLounge(GameObject parent)
        {
            // Media Wall against West wall (X = -4.98), facing EAST (+X)
            // TV Stand
            var tvStand = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Drawer1.prefab",
                new Vector3(-4.2f, FLOOR_Y, 9.0f), new Vector3(0, 90, 0), parent);

            if (tvStand != null)
            {
                float standY = GetSurfaceMaxY(tvStand);
                // TV on stand (contemporary house: rot 0 = faces East +X)
                SpawnOnSurface("Assets/contemporary house/prefabs/small props/TV .prefab",
                    new Vector3(-4.2f, standY, 9.0f), Vector3.zero, parent, standY);
            }

            // Flanking Bookcases (OfficeEssentials: rot 90 = faces East +X)
            var shelf1 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves1.prefab",
                new Vector3(-4.2f, FLOOR_Y, 5.5f), new Vector3(0, 90, 0), parent);
            var shelf2 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves2.prefab",
                new Vector3(-4.2f, FLOOR_Y, 12.5f), new Vector3(0, 90, 0), parent);

            // Books on shelves
            if (shelf1 != null)
            {
                SpawnOnSurface("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_01.prefab",
                    new Vector3(-4.2f, FLOOR_Y + 1.2f, 5.5f), Vector3.zero, parent, FLOOR_Y + 1.2f);
            }
            if (shelf2 != null)
            {
                SpawnOnSurface("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_02.prefab",
                    new Vector3(-4.2f, FLOOR_Y + 1.2f, 12.5f), Vector3.zero, parent, FLOOR_Y + 1.2f);
            }

            // Center Lounge Seating
            // Area Rug
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.01.prefab",
                new Vector3(0.5f, FLOOR_Y, 9.0f), Vector3.zero, parent, isRug: true);

            // Main Sofa (contemporary house: rot 180 = faces West -X towards TV)
            Spawn("Assets/contemporary house/prefabs/room props/sofas/white beaucoup.prefab",
                new Vector3(3.0f, FLOOR_Y, 9.0f), new Vector3(0, 180, 0), parent);

            // Coffee Table (OfficeEssentials: rot 90)
            var coffeeTable = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)CoffeTable.prefab",
                new Vector3(0.5f, FLOOR_Y, 9.0f), new Vector3(0, 90, 0), parent);

            if (coffeeTable != null)
            {
                float tableY = GetSurfaceMaxY(coffeeTable);
                SpawnOnSurface("Assets/contemporary house/prefabs/room props/kichen and bathroom/small props/plate.prefab",
                    new Vector3(0.5f, tableY, 9.0f), Vector3.zero, parent, tableY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(0.2f, tableY, 8.8f), new Vector3(0, 60, 0), parent, tableY);
            }

            // Side Armchairs framing coffee table (charcol ferais: 90 = South, 270 = North)
            // North armchair facing South (-Z) towards coffee table
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(0.5f, FLOOR_Y, 11.0f), new Vector3(0, 90, 0), parent);
            // South armchair facing North (+Z) towards coffee table
            Spawn("Assets/contemporary house/prefabs/room props/furniture/charcol ferais.prefab",
                new Vector3(0.5f, FLOOR_Y, 7.0f), new Vector3(0, 270, 0), parent);

            // Floor lamp next to sofa
            Spawn("Assets/contemporary house/prefabs/room props/sofas/lamp.001.prefab",
                new Vector3(3.0f, FLOOR_Y, 11.5f), Vector3.zero, parent);

            // Plants
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(4.5f, FLOOR_Y, 14.5f), Vector3.zero, parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PlantBox.prefab",
                new Vector3(-2.0f, FLOOR_Y, 15.5f), Vector3.zero, parent);
        }

        private static void BuildStudy(GameObject parent)
        {
            // Study Desk against West wall (X = -4.0f)
            // OfficeEssentials: rot 0 = desk width along Z, user sitting at +X facing -X towards wall
            var desk = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Desk1.prefab",
                new Vector3(-4.0f, FLOOR_Y, -9.0f), Vector3.zero, parent);

            // Office Chair facing into desk (-X, West): rot = (0, 160, 0) for natural angle
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)OfficeChair.prefab",
                new Vector3(-2.8f, FLOOR_Y, -9.0f), new Vector3(0, 160, 0), parent);

            if (desk != null)
            {
                float deskY = GetSurfaceMaxY(desk);
                // PC Monitor (OfficeEssentials: rot 0 = screen faces East +X)
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PC.prefab",
                    new Vector3(-4.0f, deskY, -9.0f), Vector3.zero, parent, deskY);

                // Desk Lamp (OfficeEssentials: rot 90 = angled over desk)
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DeskLight.prefab",
                    new Vector3(-4.0f, deskY, -10.0f), new Vector3(0, 90, 0), parent, deskY);

                // Accessories
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PenHolder.prefab",
                    new Vector3(-3.8f, deskY, -8.2f), Vector3.zero, parent, deskY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Stapler.prefab",
                    new Vector3(-3.8f, deskY, -8.5f), Vector3.zero, parent, deskY);
                SpawnOnSurface("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                    new Vector3(-3.6f, deskY, -9.6f), new Vector3(0, 120, 0), parent, deskY);
            }

            // PC Tower on Floor beside desk
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PCCase.prefab",
                new Vector3(-4.0f, FLOOR_Y, -7.8f), new Vector3(0, 90, 0), parent);

            // South Library Wall (Z = -18.2, facing North +Z)
            // OfficeEssentials: rot 0 = faces North (+Z)
            var shelf1 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves1.prefab",
                new Vector3(-2.0f, FLOOR_Y, -18.2f), Vector3.zero, parent);
            var shelf2 = Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves2.prefab",
                new Vector3(1.5f, FLOOR_Y, -18.2f), Vector3.zero, parent);

            if (shelf1 != null)
            {
                SpawnOnSurface("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_01.prefab",
                    new Vector3(-2.0f, FLOOR_Y + 1.2f, -18.2f), Vector3.zero, parent, FLOOR_Y + 1.2f);
            }
            if (shelf2 != null)
            {
                SpawnOnSurface("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_02.prefab",
                    new Vector3(1.5f, FLOOR_Y + 1.2f, -18.2f), Vector3.zero, parent, FLOOR_Y + 1.2f);
            }

            // Cozy Reading Nook (East side of study area, facing West into the room)
            Spawn("Assets/contemporary house/prefabs/rugs/colurfull rug.02.prefab",
                new Vector3(2.0f, FLOOR_Y, -10.0f), Vector3.zero, parent, isRug: true);

            // Blue Sofa facing West (-X): contemporary house: rot 180 = faces West
            Spawn("Assets/contemporary house/prefabs/room props/sofas/blue beaucoup.prefab",
                new Vector3(4.0f, FLOOR_Y, -10.0f), new Vector3(0, 180, 0), parent);

            // Coffee Table in front of reading sofa
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)CoffeTable.prefab",
                new Vector3(2.0f, FLOOR_Y, -10.0f), new Vector3(0, 90, 0), parent);

            // Reading Lamp next to reading sofa
            Spawn("Assets/contemporary house/prefabs/room props/sofas/lamp.001.prefab",
                new Vector3(4.0f, FLOOR_Y, -8.0f), Vector3.zero, parent);

            // Plants
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                new Vector3(-4.0f, FLOOR_Y, -15.5f), Vector3.zero, parent);
            Spawn("Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PlantBox.prefab",
                new Vector3(4.0f, FLOOR_Y, -14.0f), Vector3.zero, parent);
        }

        private static void BuildLights(GameObject parent)
        {
            Color warmLight = new Color(1.0f, 0.76f, 0.42f);

            CreateLight("Kitchen_Prep_Light", new Vector3(12.0f, -72.0f, 5.5f), warmLight, 8f, 3.2f, parent);
            CreateLight("Kitchen_Dining_Light", new Vector3(11.5f, -72.0f, 10.5f), warmLight, 9f, 3.8f, parent);
            CreateLight("Entrance_Door_Light", new Vector3(8.0f, -72.2f, -1.0f), warmLight, 7.5f, 2.8f, parent);
            CreateLight("Hallway_Stair_Light", new Vector3(10.1f, -72.0f, -10.0f), warmLight, 8.5f, 3.0f, parent);
            CreateLight("Lounge_Center_Light", new Vector3(0.5f, -71.8f, 9.0f), warmLight, 11f, 4.2f, parent);
            CreateLight("Lounge_Lamp_Light", new Vector3(3.0f, -73.0f, 11.5f), warmLight, 6f, 2.5f, parent);
            CreateLight("Study_Ambient_Light", new Vector3(0.0f, -71.8f, -9.0f), warmLight, 10f, 3.8f, parent);
            CreateLight("Study_Desk_Accent", new Vector3(-3.8f, -73.6f, -9.0f), new Color(1f, 0.88f, 0.65f), 4.5f, 2.8f, parent);
            CreateLight("Reading_Corner_Light", new Vector3(2.5f, -72.0f, -10.0f), warmLight, 7f, 2.8f, parent);
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

        [MenuItem("Tools/Story/Capture Lower Floor Screenshots")]
        public static void CaptureScreenshots()
        {
            var camGo = new GameObject("InspectionScreenshotCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            var dir = "Screenshots";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 1. Kitchen Dining View (from south looking north towards dining table and chairs)
            RenderView(cam, new Vector3(11.5f, -73.5f, 6.0f), new Vector3(11.5f, -74.0f, 10.5f), $"{dir}/v3_kitchen_dining.png");

            // 2. Kitchen Appliances & Prep View (from center looking at east wall)
            RenderView(cam, new Vector3(10.0f, -73.5f, 9.0f), new Vector3(16.0f, -73.5f, 9.0f), $"{dir}/v3_kitchen_appliances.png");

            // 3. Kitchen Poster & Napkin Clearance View (Player perspective looking at poster)
            RenderView(cam, new Vector3(9.84f, -73.5f, 6.5f), new Vector3(9.84f, -73.29f, 2.59f), $"{dir}/v3_kitchen_poster_clearance.png");

            // 4. Hallway & Foyer View (From stair landing towards entrance door and east console)
            RenderView(cam, new Vector3(10.1f, -73.5f, -14.0f), new Vector3(12.0f, -73.5f, -8.0f), $"{dir}/v3_hallway_foyer.png");

            // 5. Living Lounge (Looking West towards TV, coffee table, and armchairs)
            RenderView(cam, new Vector3(5.0f, -73.5f, 9.0f), new Vector3(-4.0f, -74.0f, 9.0f), $"{dir}/v3_living_lounge_tv.png");

            // 6. Living Lounge (Looking East towards sofa and armchairs)
            RenderView(cam, new Vector3(-3.0f, -73.5f, 9.0f), new Vector3(3.0f, -74.0f, 9.0f), $"{dir}/v3_living_lounge_sofa.png");

            // 7. Study Desk View (Looking West towards desk, chair, PC and desk lamp)
            RenderView(cam, new Vector3(-0.5f, -73.5f, -9.0f), new Vector3(-4.0f, -74.0f, -9.0f), $"{dir}/v3_study_desk.png");

            // 8. Study Reading Nook & Library View (Looking South-East towards blue sofa and bookshelves)
            RenderView(cam, new Vector3(-1.0f, -73.5f, -6.0f), new Vector3(2.5f, -74.0f, -12.0f), $"{dir}/v3_study_reading_nook.png");

            Object.DestroyImmediate(camGo);
            Debug.Log("[LowerFloorDecorator] All v3 inspection screenshots rendered successfully!");
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
