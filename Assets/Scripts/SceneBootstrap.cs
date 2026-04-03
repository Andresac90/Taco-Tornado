// SceneBootstrap.cs — Procedurally builds a playable prototype scene
// Attach to an empty GameObject in a blank scene. Press Play to auto-generate everything.

using UnityEngine;
using TacoTornado.Player;
using TacoTornado.Cooking;

namespace TacoTornado
{
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Press Play to generate the truck scene")]
        [SerializeField] private bool autoStartShift = true;

        private void Awake()
        {
            // ── MANAGERS ──
            CreateManagers();

            // ── LIGHTING ──
            CreateLighting();

            // ── TRUCK INTERIOR ──
            //CreateTruckGeometry();

            // ── PLAYER ──
            CreatePlayer();

            // ── STATIONS ──
            CreateStations();

            // ── GRILL ──
            CreateGrill();

            // ── ASSEMBLY PLATE ──
            CreateAssemblyPlate();

            // ── TRASH BIN ──
            CreateTrashBin();
        }

        private void Start()
        {
            if (autoStartShift && GameManager.Instance != null)
            {
                // Small delay so everything initializes
                Invoke(nameof(BeginShift), 0.5f);
            }
        }

        private void BeginShift()
        {
            GameManager.Instance.StartShift();
        }

        // ──────────────────────────────────────────────
        //  MANAGERS
        // ──────────────────────────────────────────────

        private void CreateManagers()
        {
            // GameManager
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // OrderManager
            if (OrderManager.Instance == null)
            {
                var omObj = new GameObject("OrderManager");
                omObj.AddComponent<OrderManager>();
            }
        }

        // ──────────────────────────────────────────────
        //  LIGHTING
        // ──────────────────────────────────────────────

        private void CreateLighting()
        {
            // Directional light
            var lightObj = new GameObject("DirectionalLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Warm interior point light
            var interiorLight = new GameObject("InteriorLight");
            var pLight = interiorLight.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1f, 0.85f, 0.6f);
            pLight.intensity = 1.5f;
            pLight.range = 6f;
            interiorLight.transform.position = new Vector3(0f, 2.5f, 0f);
        }
        

        // ──────────────────────────────────────────────
        //  PLAYER (Camera)
        // ──────────────────────────────────────────────

        private void CreatePlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1.6f, 0f); // standing height

            // Camera
            var camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // look slightly down at counter

            var cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.fieldOfView = 70f;
            cam.backgroundColor = new Color(0.5f, 0.7f, 0.9f);

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<FirstPersonLook>();
            var interaction = camObj.AddComponent<PlayerInteraction>();

            // Hold point (where held ingredient floats)
            var holdPoint = new GameObject("HoldPoint");
            holdPoint.transform.SetParent(camObj.transform);
            holdPoint.transform.localPosition = new Vector3(0.25f, -0.25f, 0.5f);

            // Assign hold point via reflection (or serialize in inspector — for bootstrap we use a field setter)
            var holdField = typeof(PlayerInteraction).GetField("holdPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (holdField != null)
                holdField.SetValue(interaction, holdPoint.transform);

            // Set interact layer to everything (prototype)
            var layerField = typeof(PlayerInteraction).GetField("interactLayer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (layerField != null)
                layerField.SetValue(interaction, (LayerMask)~0);
        }

        // ──────────────────────────────────────────────
        //  INGREDIENT STATIONS
        // ──────────────────────────────────────────────

        private void CreateStations()
        {
            float counterY = 1.1f; // top of counter
            float counterZ = 1.0f;

            // LEFT SIDE — Tortillas
            CreateIngredientStation("CornTortilla", IngredientType.CornTortilla,
                new Vector3(-1.4f, counterY, counterZ), new Color(1f, 0.9f, 0.6f), 0.2f);
            CreateIngredientStation("FlourTortilla", IngredientType.FlourTortilla,
                new Vector3(-0.9f, counterY, counterZ), new Color(0.95f, 0.92f, 0.8f), 0.2f);

            // CENTER — Proteins (raw, need grilling)
            CreateIngredientStation("CarneAsada", IngredientType.CarneAsada,
                new Vector3(-0.3f, counterY, counterZ), new Color(0.7f, 0.2f, 0.2f), 0.18f);
            CreateIngredientStation("Pollo", IngredientType.Pollo,
                new Vector3(0.1f, counterY, counterZ), new Color(0.95f, 0.8f, 0.5f), 0.18f);
            CreateIngredientStation("AlPastor", IngredientType.AlPastor,
                new Vector3(0.5f, counterY, counterZ), new Color(0.8f, 0.3f, 0.15f), 0.18f);

            // RIGHT SIDE — Toppings
            CreateIngredientStation("Cilantro", IngredientType.Cilantro,
                new Vector3(0.9f, counterY, counterZ), new Color(0.2f, 0.7f, 0.2f), 0.12f);
            CreateIngredientStation("Onion", IngredientType.Onion,
                new Vector3(1.2f, counterY, counterZ), new Color(0.95f, 0.95f, 0.85f), 0.12f);
            CreateIngredientStation("Cheese", IngredientType.Cheese,
                new Vector3(1.5f, counterY, counterZ), new Color(1f, 0.85f, 0.2f), 0.12f);

            // FAR RIGHT — Salsas
            CreateIngredientStation("SalsaVerde", IngredientType.SalsaVerde,
                new Vector3(1.2f, counterY, counterZ - 0.4f), new Color(0.2f, 0.6f, 0.15f), 0.1f);
            CreateIngredientStation("SalsaRoja", IngredientType.SalsaRoja,
                new Vector3(1.5f, counterY, counterZ - 0.4f), new Color(0.8f, 0.15f, 0.1f), 0.1f);
            CreateIngredientStation("Guacamole", IngredientType.Guacamole,
                new Vector3(1.2f, counterY, counterZ - 0.7f), new Color(0.4f, 0.65f, 0.2f), 0.1f);
        }

        private void CreateIngredientStation(string name, IngredientType type, Vector3 pos, Color color, float size)
        {
            // The station itself (a colored bowl/box)
            var station = CreateBox("Station_" + name, pos, Vector3.one * (size + 0.05f), color * 0.7f, true);

            // Add IngredientSource
            var source = station.AddComponent<IngredientSource>();
            source.ingredientType = type;

            // Create a prefab template (small cube) as the dispensed ingredient
            var prefab = CreateIngredientPrefab(name + "_Prefab", color, size);
            prefab.SetActive(false); // template, not visible

            var prefabField = typeof(IngredientSource).GetField("ingredientPrefab",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prefabField != null)
                prefabField.SetValue(source, prefab);

            // Spawn point
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(station.transform);
            spawnPoint.transform.localPosition = Vector3.up * 0.15f;

            var spawnField = typeof(IngredientSource).GetField("spawnPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnField != null)
                spawnField.SetValue(source, spawnPoint.transform);

            // Label
            // (Would use TextMeshPro in production — skipping for bootstrap)
        }

        private GameObject CreateIngredientPrefab(string name, Color color, float size)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.localScale = Vector3.one * size;
            obj.GetComponent<Renderer>().material.color = color;

            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.angularDamping = 2f;

            var ingredient = obj.AddComponent<Ingredient>();
            // ingredientType set by IngredientSource on spawn

            return obj;
        }

        // ──────────────────────────────────────────────
        //  GRILL
        // ──────────────────────────────────────────────

        private void CreateGrill()
        {
            float counterY = 1.05f;

            // Grill surface
            var grill = CreateBox("Grill", new Vector3(-0.3f, counterY, 0.4f),
                new Vector3(1.2f, 0.08f, 0.6f), new Color(0.2f, 0.2f, 0.2f), true);

            var grillStation = grill.AddComponent<GrillStation>();

            // Create grill slots
            Transform[] slots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = new GameObject($"GrillSlot_{i}");
                slot.transform.SetParent(grill.transform);
                float x = -0.3f + (i * 0.2f);
                slot.transform.localPosition = new Vector3(x, 0.08f, 0f);
                slots[i] = slot.transform;
            }

            var slotsField = typeof(GrillStation).GetField("grillSlots",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (slotsField != null)
                slotsField.SetValue(grillStation, slots);

            // Heat glow (red point light)
            var glow = new GameObject("GrillGlow");
            glow.transform.SetParent(grill.transform);
            glow.transform.localPosition = new Vector3(0, 0.1f, 0);
            var glowLight = glow.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.3f, 0.1f);
            glowLight.intensity = 0.8f;
            glowLight.range = 1.5f;
        }

        // ──────────────────────────────────────────────
        //  ASSEMBLY PLATE
        // ──────────────────────────────────────────────

        private void CreateAssemblyPlate()
        {
            float counterY = 1.08f;

            var plate = CreateBox("AssemblyPlate", new Vector3(0.5f, counterY, 0.4f),
                new Vector3(0.35f, 0.04f, 0.35f), new Color(0.9f, 0.88f, 0.82f), true);

            plate.AddComponent<TacoAssemblyPlate>();
        }

        // ──────────────────────────────────────────────
        //  TRASH BIN
        // ──────────────────────────────────────────────

        private void CreateTrashBin()
        {
            var trash = CreateBox("TrashBin", new Vector3(-1.6f, 0.4f, -0.5f),
                new Vector3(0.35f, 0.5f, 0.35f), new Color(0.3f, 0.3f, 0.3f), true);

            trash.AddComponent<TrashBin>();
        }

        // ──────────────────────────────────────────────
        //  UTILITY
        // ──────────────────────────────────────────────

        private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color, bool hasCollider)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;

            var renderer = obj.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;

            if (!hasCollider)
            {
                // Static geometry — keep collider but make it not interactable
                obj.layer = 0; // Default layer
            }

            return obj;
        }
    }
}
