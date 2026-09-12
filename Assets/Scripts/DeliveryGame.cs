using UnityEngine;

namespace RiskyDelivery
{
    public sealed class DeliveryGame : MonoBehaviour
    {
        public enum RunState { Playing, Delivered, Failed }
        public RunState State { get; private set; }
        public Rigidbody Cart { get; private set; }
        public float Elapsed { get; private set; }
        public int Chapter { get; private set; } = 1;
        public int CargoHealth { get; private set; } = 100;
        public int Rating => State != RunState.Delivered ? 0 : CargoHealth >= 90 ? 3 : CargoHealth >= 50 ? 2 : 1;
        private GameObject roadworks, rainCourse;
        private Light sun;
        private PhysicsMaterial cartFriction;
        public bool OnWetRoad => Chapter == 3 && Cart != null && RainyRoad.Contains(Cart.position);
        public string ChapterName => Chapter == 1 ? "01 / TRAINING" : Chapter == 2 ? "02 / ROADWORKS" : "03 / RAIN RUN";
        private Material parcelMaterial;
        private float lastImpactTime = -10, impactFlash;
        private int lastDamage;
        public Vector3 Destination => new Vector3(0, 0, 25);
        private Transform cargo;
        private Camera followCamera;
        private Vector2 input, tilt, tiltVelocity;
        private Vector3 previousVelocity;
        private bool braking;
        private GUIStyle titleStyle, textStyle, smallStyle;
        public bool Automated { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<DeliveryGame>() == null)
                new GameObject("Risky Delivery").AddComponent<DeliveryGame>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BuildWorld();
            StartChapter(1);
        }

        private Material Mat(Color color)
        {
            var template = Resources.Load<Material>("WorldMaterial");
            var material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", 0.15f);
            return material;
        }

        private GameObject Box(string label, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Destroy(go.GetComponent<Collider>());
            return go;
        }

        private void BuildWorld()
        {
            foreach (var oldCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                oldCamera.gameObject.SetActive(false);
            RenderSettings.ambientLight = new Color(0.65f, 0.72f, 0.82f);
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            var road = Mat(new Color(0.17f, 0.22f, 0.29f));
            var grass = Mat(new Color(0.21f, 0.43f, 0.36f));
            var white = Mat(new Color(0.94f, 0.93f, 0.82f));
            var yellow = Mat(new Color(1f, 0.7f, 0.19f));
            var teal = Mat(new Color(0.12f, 0.85f, 0.72f));
            Box("Ground", new Vector3(0, -0.8f, 8), new Vector3(70, 1, 90), grass);
            Box("Delivery road", new Vector3(0, -0.15f, 8), new Vector3(14, 0.3f, 52), road);
            Barrier("Left barrier", new Vector3(-7.3f, 0.35f, 8), new Vector3(0.5f, 0.7f, 52), white);
            Barrier("Right barrier", new Vector3(7.3f, 0.35f, 8), new Vector3(0.5f, 0.7f, 52), white);
            Barrier("Start barrier", new Vector3(0, 0.35f, -18), new Vector3(15, 0.7f, 0.5f), white);
            Barrier("End barrier", new Vector3(0, 0.35f, 34), new Vector3(15, 0.7f, 0.5f), white);
            for (int z = -15; z < 32; z += 4)
                Box("Lane marking", new Vector3(0, 0.015f, z), new Vector3(0.12f, 0.02f, 1.5f), white, false);
            Box("Depot", new Vector3(0, 0.025f, -12), new Vector3(6, 0.04f, 5), yellow, false);
            Box("Delivery zone", Destination + Vector3.up * 0.035f, new Vector3(6, 0.05f, 5), teal, false);
            for (int i = 0; i < 9; i++)
            {
                float height = 2 + (i % 3) * 1.5f;
                var facade = Mat(Color.Lerp(new Color(0.3f, 0.42f, 0.52f), new Color(0.67f, 0.47f, 0.34f), (i % 4) / 3f));
                foreach (int side in new[] { -1, 1 })
                {
                    Box("Warehouse", new Vector3(side * 12, height / 2, -12 + i * 5.5f), new Vector3(6, height, 4), facade);
                    Box("Warehouse door", new Vector3(side * 8.95f, 0.85f, -12 + i * 5.5f), new Vector3(0.05f, 1.7f, 1.7f), white, false);
                }
            }
            roadworks = new GameObject("Chapter 2 - Roadworks");
            var orange = Mat(new Color(1f, 0.32f, 0.12f));
            for (int i = 0; i < 3; i++)
            {
                float x = i == 1 ? 2 : -2;
                float z = -3 + i * 10;
                var obstacle = Barrier("Construction barricade", new Vector3(x, 0.6f, z), new Vector3(7, 1.2f, 0.8f), orange);
                obstacle.transform.SetParent(roadworks.transform);
                for (int stripe = -2; stripe <= 2; stripe++)
                {
                    var mark = Box("Reflective stripe", new Vector3(x + stripe * 1.2f, 0.65f, z - 0.415f), new Vector3(0.35f, 0.85f, 0.025f), white, false);
                    mark.transform.rotation = Quaternion.Euler(0, 0, -25);
                    mark.transform.SetParent(roadworks.transform);
                }
                var guide = Box("Safe passage", new Vector3(i == 1 ? -3.8f : 3.8f, 0.03f, z), new Vector3(1.2f, 0.025f, 2), teal, false);
                guide.transform.SetParent(roadworks.transform);
            }
            rainCourse = new GameObject("Chapter 3 - Rain Run");
            var water = Mat(new Color(0.12f, 0.42f, 0.66f));
            water.SetFloat("_Glossiness", 0.8f);
            var reflection = Mat(new Color(0.46f, 0.72f, 0.84f));
            foreach (Rect patch in RainyRoad.Patches)
            {
                var puddle = Box("Slippery blue road", new Vector3(patch.center.x, 0.04f, patch.center.y), new Vector3(patch.width, 0.035f, patch.height), water, false);
                puddle.transform.SetParent(rainCourse.transform);
                for (float z = patch.yMin + 1; z < patch.yMax; z += 2)
                {
                    foreach (int side in new[] { -1, 1 })
                    {
                        var ripple = Box("Water reflection", new Vector3(side * 5.6f, 0.064f, z), new Vector3(1.1f, 0.012f, 0.09f), reflection, false);
                        ripple.transform.SetParent(rainCourse.transform);
                    }
                }
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -2 : 2;
                float z = i == 0 ? 4 : 21;
                var obstacle = Barrier("Flood diversion barrier", new Vector3(x, 0.6f, z), new Vector3(7, 1.2f, 0.8f), orange);
                obstacle.transform.SetParent(rainCourse.transform);
                for (int stripe = -2; stripe <= 2; stripe++)
                {
                    var mark = Box("Flood barrier reflector", new Vector3(x + stripe * 1.2f, 0.65f, z - 0.415f), new Vector3(0.35f, 0.85f, 0.025f), white, false);
                    mark.transform.rotation = Quaternion.Euler(0, 0, -25);
                    mark.transform.SetParent(rainCourse.transform);
                }
                var guide = Box("Rain route safe passage", new Vector3(-Mathf.Sign(x) * 3.8f, 0.04f, z), new Vector3(1.2f, 0.025f, 2), teal, false);
                guide.transform.SetParent(rainCourse.transform);
            }
            var cartObject = new GameObject("Delivery cart");
            cartObject.AddComponent<CartImpactReceiver>().Game = this;
            var body = Box("Cart body", Vector3.zero, new Vector3(1.5f, 0.65f, 2), teal);
            body.transform.SetParent(cartObject.transform, false);
            // The cart rolls; default box friction otherwise nearly cancels its motor force.
            cartFriction = new PhysicsMaterial("Cart rolling friction")
            {
                staticFriction = 0.05f,
                dynamicFriction = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0
            };
            body.GetComponent<Collider>().sharedMaterial = cartFriction;
            Cart = cartObject.AddComponent<Rigidbody>();
            Cart.mass = 4;
            Cart.constraints = RigidbodyConstraints.FreezeRotation;
            Cart.interpolation = RigidbodyInterpolation.Interpolate;
            Cart.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var rain = new GameObject("Rain around courier");
            rain.transform.SetParent(rainCourse.transform);
            rain.AddComponent<RainWeather>().Initialize(Cart.transform, reflection);
            cargo = new GameObject("Cargo balance pivot").transform;
            cargo.SetParent(cartObject.transform, false);
            cargo.localPosition = new Vector3(0, 0.325f, 0);
            parcelMaterial = Mat(new Color(1f, 0.7f, 0.19f));
            var parcel = Box("Fragile parcel", Vector3.zero, new Vector3(1.1f, 1.15f, 1.1f), parcelMaterial, false);
            parcel.transform.SetParent(cargo, false);
            parcel.transform.localPosition = Vector3.up * 0.575f;
            var tape = Box("Parcel tape", Vector3.zero, new Vector3(0.18f, 1.17f, 1.12f), white, false);
            tape.transform.SetParent(cargo, false);
            tape.transform.localPosition = Vector3.up * 0.575f;
            followCamera = new GameObject("Follow camera").AddComponent<Camera>();
            followCamera.tag = "MainCamera";
            followCamera.gameObject.AddComponent<AudioListener>();
            followCamera.orthographic = true;
            followCamera.orthographicSize = 10;
            followCamera.backgroundColor = new Color(0.11f, 0.16f, 0.23f);
            followCamera.clearFlags = CameraClearFlags.SolidColor;
            followCamera.transform.rotation = Quaternion.Euler(52, 0, 0);
        }

        private GameObject Barrier(string name, Vector3 position, Vector3 size, Material material)
        {
            var barrier = Box(name, position, size, material);
            barrier.AddComponent<DeliveryHazard>();
            return barrier;
        }

        public void StartChapter(int chapter)
        {
            Chapter = Mathf.Clamp(chapter, 1, 3);
            roadworks.SetActive(Chapter == 2);
            rainCourse.SetActive(Chapter == 3);
            sun.intensity = Chapter == 3 ? 0.85f : 1.2f;
            RenderSettings.ambientLight = Chapter == 3 ? new Color(0.5f, 0.62f, 0.75f) : new Color(0.65f, 0.72f, 0.82f);
            RenderSettings.fog = Chapter == 3;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.18f, 0.26f, 0.34f);
            RenderSettings.fogDensity = 0.012f;
            Restart();
        }

        public bool TryNextChapter()
        {
            if (State != RunState.Delivered || Chapter >= 3) return false;
            StartChapter(Chapter + 1);
            return true;
        }

        public void RegisterImpact(float normalSpeed)
        {
            // A scrape is measured along the contact normal, not total driving speed.
            if (State != RunState.Playing || normalSpeed <= 2.5f || Time.time - lastImpactTime < 0.4f) return;
            lastImpactTime = Time.time;
            lastDamage = Mathf.Clamp(Mathf.RoundToInt((normalSpeed - 2.5f) * 12), 1, 65);
            CargoHealth = Mathf.Max(0, CargoHealth - lastDamage);
            impactFlash = 0.8f;
            if (CargoHealth == 0) State = RunState.Failed;
        }

        public void Restart()
        {
            State = RunState.Playing;
            Elapsed = 0;
            CargoHealth = 100;
            impactFlash = 0;
            lastDamage = 0;
            lastImpactTime = -10;
            input = tilt = tiltVelocity = Vector2.zero;
            braking = false;
            previousVelocity = Vector3.zero;
            cartFriction.staticFriction = cartFriction.dynamicFriction = 0.05f;
            Cart.position = new Vector3(0, 0.5f, -12);
            Cart.linearVelocity = Cart.angularVelocity = Vector3.zero;
            cargo.localRotation = Quaternion.identity;
            followCamera.transform.position = Cart.position + new Vector3(0, 14, -11);
        }

        public void SetControls(Vector2 direction, bool brake)
        {
            input = Vector2.ClampMagnitude(direction, 1);
            braking = brake;
        }

        private void Update()
        {
            if (!Automated)
            {
                if (Input.GetKeyDown(KeyCode.R)) Restart();
                if (Input.GetKeyDown(KeyCode.Alpha1)) StartChapter(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) StartChapter(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) StartChapter(3);
                if (Input.GetKeyDown(KeyCode.Return)) TryNextChapter();
                SetControls(new Vector2(
                    (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)), Input.GetKey(KeyCode.Space));
            }
            if (State == RunState.Playing) Elapsed += Time.deltaTime;
            impactFlash = Mathf.Max(0, impactFlash - Time.deltaTime);
            parcelMaterial.color = impactFlash > 0 ? new Color(1, 0.18f, 0.12f) : Color.Lerp(new Color(0.6f, 0.18f, 0.1f), new Color(1, 0.7f, 0.19f), CargoHealth / 100f);
            cargo.localRotation = Quaternion.Euler(tilt.y, 0, -tilt.x);
        }

        private void FixedUpdate()
        {
            if (State != RunState.Playing)
            {
                Cart.linearVelocity = Vector3.zero;
                return;
            }
            Vector3 horizontal = new Vector3(Cart.linearVelocity.x, 0, Cart.linearVelocity.z);
            Vector3 desired = new Vector3(input.x, 0, input.y) * (braking ? 2 : 8);
            bool wet = OnWetRoad;
            cartFriction.staticFriction = cartFriction.dynamicFriction = wet ? 0.005f : 0.05f;
            // Low grip limits both braking and sideways correction, preserving momentum.
            float response = wet ? (braking ? 1.2f : 0.7f) : (braking ? 8 : 3);
            float traction = wet ? (braking ? 2.4f : 2.8f) : (braking ? 20 : 12);
            Vector3 force = Vector3.ClampMagnitude((desired - horizontal) * response, traction);
            Cart.AddForce(force, ForceMode.Acceleration);
            Vector3 acceleration = (horizontal - previousVelocity) / Time.fixedDeltaTime;
            previousVelocity = horizontal;
            Vector2 target = Vector2.ClampMagnitude(new Vector2(-acceleration.x, -acceleration.z) * 1.4f, 35);
            tilt = Vector2.SmoothDamp(tilt, target, ref tiltVelocity, 0.22f, Mathf.Infinity, Time.fixedDeltaTime);
            if (Mathf.Abs(Cart.position.x - Destination.x) < 2.5f && Mathf.Abs(Cart.position.z - Destination.z) < 2 && horizontal.magnitude < 1)
                State = RunState.Delivered;
        }

        private void LateUpdate()
        {
            Vector3 target = Cart.position + new Vector3(0, 14, -11);
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, target, 1 - Mathf.Exp(-7 * Time.deltaTime));
        }

        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };
                smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            }
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 700f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            GUI.Box(new Rect(18, 18, 420, 146), GUIContent.none);
            GUI.Label(new Rect(34, 28, 400, 42), "RISKY DELIVERY", titleStyle);
            GUI.Label(new Rect(34, 72, 400, 30), $"{ChapterName}   /   {Elapsed:0.0}s", textStyle);
            GUI.Label(new Rect(34, 108, 400, 48), Chapter == 1 ? "Deliver the parcel to the mint zone.\nStop inside the zone to finish." : Chapter == 2 ? "Follow the mint gaps: RIGHT - LEFT - RIGHT.\nBrake before turns. Protect the parcel!" : "Blue road = low grip. Brake BEFORE puddles.\nPass the barriers on the RIGHT, then LEFT.", smallStyle);
            GUI.Box(new Rect(18, 610, 660, 68), GUIContent.none);
            GUI.Label(new Rect(34, 620, 630, 28), "WASD / ARROWS  Move     SPACE  Brake     R  Restart", textStyle);
            GUI.Label(new Rect(34, 651, 620, 24), "Release movement keys to stop. Keep your parcel steady!", smallStyle);
            GUI.Box(new Rect(774, 18, 308, 118), GUIContent.none);
            GUI.Label(new Rect(792, 30, 275, 30), $"CARGO CONDITION   {CargoHealth}%", textStyle);
            Color healthColor = CargoHealth > 50 ? new Color(0.2f, 0.9f, 0.7f) : new Color(1, 0.35f, 0.2f);
            GUI.color = new Color(0.16f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(792, 70, 270, 16), Texture2D.whiteTexture);
            GUI.color = healthColor;
            GUI.DrawTexture(new Rect(792, 70, 270 * CargoHealth / 100f, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(792, 98, 280, 25), "Hard impacts damage your delivery.", smallStyle);
            if (impactFlash > 0 && State == RunState.Playing)
                GUI.Label(new Rect(430, 178, 340, 32), $"IMPACT!  -{lastDamage}% CARGO", textStyle);
            if (Chapter == 3)
            {
                GUI.Box(new Rect(774, 150, 308, 80), GUIContent.none);
                GUI.color = OnWetRoad ? new Color(0.5f, 0.85f, 1) : Color.white;
                GUI.Label(new Rect(792, 160, 280, 30), OnWetRoad ? "LOW GRIP / BRAKE EARLY" : "DRY ROAD / NORMAL GRIP", textStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(792, 196, 275, 25), $"Speed: {new Vector2(Cart.linearVelocity.x, Cart.linearVelocity.z).magnitude:0.0} m/s", smallStyle);
            }
            if (GUI.Button(new Rect(850, 572, 232, 30), "1  /  TRAINING")) StartChapter(1);
            if (GUI.Button(new Rect(850, 610, 232, 30), "2  /  ROADWORKS")) StartChapter(2);
            if (GUI.Button(new Rect(850, 648, 232, 30), "3  /  RAIN RUN")) StartChapter(3);
            if (State != RunState.Playing)
            {
                GUI.Box(new Rect(310, 225, 480, 270), GUIContent.none);
                GUI.Label(new Rect(334, 242, 440, 45), State == RunState.Delivered ? "DELIVERY COMPLETE!" : "PARCEL BROKEN!", titleStyle);
                GUI.Label(new Rect(334, 296, 440, 30), State == RunState.Delivered ? $"{Elapsed:0.0}s   /   Cargo {CargoHealth}%   /   Rating {Rating}/3" : "Slow down before hitting a barrier.", textStyle);
                if (State == RunState.Delivered && Chapter < 3)
                {
                    if (GUI.Button(new Rect(334, 350, 432, 48), $"CHAPTER {Chapter + 1}: {(Chapter == 1 ? "ROADWORKS" : "RAIN RUN")}  [ENTER]")) TryNextChapter();
                }
                else
                    GUI.Label(new Rect(334, 351, 432, 40), State == RunState.Delivered ? "Try again for a faster, safer delivery." : "Your cargo is lost. Try a safer route.", smallStyle);
                if (GUI.Button(new Rect(334, 418, 432, 48), "RETRY CHAPTER  [R]")) Restart();
            }
        }
    }
}
