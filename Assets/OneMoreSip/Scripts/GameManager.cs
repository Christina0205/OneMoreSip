using System.Collections.Generic;
using UnityEngine;

namespace OneMoreSip
{
    public enum GameMode { Walking, Pee, Ending }

    /// <summary>
    /// Central hub: owns global state (Drunk Level, Pee Score, Floor Hits),
    /// builds each "scene" at runtime, and drives mode transitions.
    /// Implemented as in-scene state machine so the build needs no Build Settings setup.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---- Tunable layout constants ----
        public const float MapLength = 48f;
        public const float FloorTopY = 0f;        // world Y of the floor surface
        public const float PlayerHalfHeight = 0.5f;

        // ---- Core global variables (persist across modes) ----
        public float DrunkLevel = 0f;             // 0 - 100
        public int PeeScore = 0;
        public int FloorHits = 0;

        public GameMode Mode { get; private set; }

        // ---- System references (set as each mode is built) ----
        public UIManager UI { get; private set; }
        public PlayerController Player { get; private set; }
        public BottleSystem Bottle { get; private set; }
        public DrunkEffectManager Effects { get; private set; }
        public PeeController Pee { get; private set; }
        public CameraShake Shake { get; private set; }

        private Transform _worldRoot;
        private Camera _cam;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SetupCamera();
            UI = gameObject.AddComponent<UIManager>();   // UI is mode-aware and persists
            BuildWalkingScene();
        }

        // -------------------------------------------------------------------
        // Camera
        // -------------------------------------------------------------------
        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 5f;
            _cam.backgroundColor = new Color(0.10f, 0.10f, 0.16f); // solid background
            _cam.transform.position = new Vector3(5f, 2f, -10f);

            Shake = _cam.GetComponent<CameraShake>();
            if (Shake == null) Shake = _cam.gameObject.AddComponent<CameraShake>();
        }

        public Camera Cam => _cam;

        // -------------------------------------------------------------------
        // World root helpers
        // -------------------------------------------------------------------
        private void ResetWorld()
        {
            if (_worldRoot != null) Destroy(_worldRoot.gameObject);
            _worldRoot = new GameObject("World").transform;

            Player = null; Bottle = null; Effects = null; Pee = null;
        }

        // -------------------------------------------------------------------
        // SCENE 1 : Walking to the dorm
        // -------------------------------------------------------------------
        public void BuildWalkingScene()
        {
            Mode = GameMode.Walking;
            ResetWorld();

            // Floor (dark gray rectangle spanning the whole map)
            PrimitiveFactory.CreateBox("Floor", new Vector2(MapLength / 2f, FloorTopY - 0.5f),
                new Vector2(MapLength + 4f, 1f), new Color(0.20f, 0.20f, 0.22f), _worldRoot, -5);

            // Dorm entrance (blue rectangle, far right)
            var dorm = PrimitiveFactory.CreateBox("DormEntrance", new Vector2(MapLength - 1f, 1.5f),
                new Vector2(1.5f, 3f), new Color(0.25f, 0.45f, 0.95f), _worldRoot, -1);
            var dormCol = dorm.AddComponent<BoxCollider2D>();
            dormCol.isTrigger = true;
            dorm.AddComponent<DormTrigger>();

            // Player (white square, far left)
            var playerGo = PrimitiveFactory.CreateBox("Player", new Vector2(1f, PlayerHalfHeight),
                new Vector2(1f, 1f), Color.white, _worldRoot, 2);
            var pCol = playerGo.AddComponent<BoxCollider2D>();
            pCol.isTrigger = true;
            playerGo.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            // Order matters: BottleSystem must exist before the components that look it up in Awake.
            Bottle = playerGo.AddComponent<BottleSystem>();
            Player = playerGo.AddComponent<PlayerController>();
            Effects = playerGo.AddComponent<DrunkEffectManager>();

            // 5 alcohol bottles spread across the longer level (green squares)
            float[] bottleX = { 6f, 15f, 24f, 33f, 42f };
            foreach (float bx in bottleX)
            {
                var b = PrimitiveFactory.CreateBox("Bottle", new Vector2(bx, 0.4f),
                    new Vector2(0.5f, 0.9f), new Color(0.20f, 0.75f, 0.25f), _worldRoot, 1);
                var bc = b.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                b.AddComponent<BottlePickup>();
            }

            // 3 patrolling NPCs (gray squares) - wide patrol zones, large gaps between them
            CreateNPC(12f, 8f, 17f);
            CreateNPC(26f, 22f, 31f);
            CreateNPC(40f, 36f, 44f);

            UI.SwitchToWalking();
        }

        private void CreateNPC(float startX, float minX, float maxX)
        {
            var npcGo = PrimitiveFactory.CreateBox("NPC", new Vector2(startX, PlayerHalfHeight),
                new Vector2(1f, 1f), new Color(0.55f, 0.55f, 0.58f), _worldRoot, 1);
            var col = npcGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var npc = npcGo.AddComponent<NPCDetection>();
            npc.Init(minX, maxX);
        }

        // -------------------------------------------------------------------
        // SCENE 2 : Pee minigame
        // -------------------------------------------------------------------
        public void BuildPeeScene()
        {
            Mode = GameMode.Pee;
            ResetWorld();

            // Re-frame the camera for the fixed pee arena.
            Shake.StopShake();
            _cam.transform.position = new Vector3(5f, 2.5f, -10f);
            _cam.orthographicSize = 5f;
            Shake.SetBasePosition(_cam.transform.position);

            // Floor at the bottom (penalty zone) - dark gray
            var floor = PrimitiveFactory.CreateBox("PeeFloor", new Vector2(5f, -2f),
                new Vector2(24f, 0.5f), new Color(0.20f, 0.20f, 0.22f), _worldRoot, -5);
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.isTrigger = true;
            floor.AddComponent<PeeTarget>().Init(PeeTargetType.Floor);

            // Back wall behind the targets - a tall penalty zone so ANY pee that misses
            // the painting/urinal/trash (flies past or around them) is deducted.
            var wall = PrimitiveFactory.CreateBox("PeeWall", new Vector2(10.5f, 6f),
                new Vector2(0.6f, 24f), new Color(0.16f, 0.16f, 0.20f), _worldRoot, -6);
            var wallCol = wall.AddComponent<BoxCollider2D>();
            wallCol.isTrigger = true;
            wall.AddComponent<PeeTarget>().Init(PeeTargetType.Miss);

            // Player fixed on the left (white square)
            PrimitiveFactory.CreateBox("PeePlayer", new Vector2(-1.5f, 0.5f),
                new Vector2(1f, 1.4f), Color.white, _worldRoot, 2);

            // Targets on the right: Painting (top), Urinal (middle), Trash (bottom)
            CreatePeeTarget("Painting", new Vector2(8.5f, 4.0f), new Vector2(1.6f, 1.6f),
                new Color(0.85f, 0.20f, 0.20f), PeeTargetType.Painting);
            CreatePeeTarget("Urinal", new Vector2(8.5f, 1.0f), new Vector2(1.6f, 1.6f),
                new Color(0.30f, 0.85f, 0.90f), PeeTargetType.Urinal);
            CreatePeeTarget("TrashCan", new Vector2(8.5f, -1.2f), new Vector2(1.6f, 1.6f),
                new Color(0.90f, 0.85f, 0.25f), PeeTargetType.Trash);

            // Pee controller lives on its own object at the player's nozzle position.
            var peeGo = new GameObject("PeeController");
            peeGo.transform.SetParent(_worldRoot);
            peeGo.transform.position = new Vector2(-1.0f, 0.9f);
            Pee = peeGo.AddComponent<PeeController>();

            UI.SwitchToPee();
        }

        private void CreatePeeTarget(string name, Vector2 pos, Vector2 size, Color color, PeeTargetType type)
        {
            var go = PrimitiveFactory.CreateBox(name, pos, size, color, _worldRoot, 1);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PeeTarget>().Init(type);
        }

        // -------------------------------------------------------------------
        // Endings
        // -------------------------------------------------------------------
        public void ArriveAtDorm()
        {
            if (Mode != GameMode.Walking) return;

            if (DrunkLevel < 40f)
            {
                ShowEnding(false);
            }
            else
            {
                BuildPeeScene();
            }
        }

        public void EndPeeMode()
        {
            ShowEnding(true);
        }

        private void ShowEnding(bool madeItDrunk)
        {
            Mode = GameMode.Ending;
            if (_worldRoot != null) Destroy(_worldRoot.gameObject);
            Shake.StopShake();
            UI.ShowEnding(madeItDrunk);
        }

        public void Restart()
        {
            DrunkLevel = 0f;
            PeeScore = 0;
            FloorHits = 0;
            SetupCamera();
            BuildWalkingScene();
        }

        // -------------------------------------------------------------------
        // Camera follow (walking mode only)
        // -------------------------------------------------------------------
        private void LateUpdate()
        {
            if (Mode == GameMode.Walking && Player != null)
            {
                float halfW = _cam.orthographicSize * _cam.aspect;
                float targetX = Mathf.Clamp(Player.transform.position.x, halfW, MapLength - halfW);
                Vector3 baseP = new Vector3(targetX, 2f, -10f);
                // CameraShake adds its offset on top of this base each frame.
                Shake.SetBasePosition(baseP);
            }
        }
    }
}
