using UnityEngine;

namespace RiskyDelivery
{
    public sealed class DeliveryGame : MonoBehaviour
    {
        public enum RunState { Playing, Delivered, Failed }
        public RunState State { get; private set; }
        public Rigidbody Cart { get; private set; }
        public float Elapsed { get; private set; }
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
            Restart();
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
            var sun = new GameObject("Sun").AddComponent<Light>();
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
            Box("Left barrier", new Vector3(-7.3f, 0.35f, 8), new Vector3(0.5f, 0.7f, 52), white);
            Box("Right barrier", new Vector3(7.3f, 0.35f, 8), new Vector3(0.5f, 0.7f, 52), white);
            Box("Start barrier", new Vector3(0, 0.35f, -18), new Vector3(15, 0.7f, 0.5f), white);
            Box("End barrier", new Vector3(0, 0.35f, 34), new Vector3(15, 0.7f, 0.5f), white);
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
            var cartObject = new GameObject("Delivery cart");
            var body = Box("Cart body", Vector3.zero, new Vector3(1.5f, 0.65f, 2), teal);
            body.transform.SetParent(cartObject.transform, false);
            // The cart rolls; default box friction otherwise nearly cancels its motor force.
            body.GetComponent<Collider>().sharedMaterial = new PhysicsMaterial("Cart rolling friction")
            {
                staticFriction = 0.05f,
                dynamicFriction = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0
            };
            Cart = cartObject.AddComponent<Rigidbody>();
            Cart.mass = 4;
            Cart.constraints = RigidbodyConstraints.FreezeRotation;
            Cart.interpolation = RigidbodyInterpolation.Interpolate;
            Cart.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cargo = new GameObject("Cargo balance pivot").transform;
            cargo.SetParent(cartObject.transform, false);
            cargo.localPosition = new Vector3(0, 0.325f, 0);
            var parcel = Box("Fragile parcel", Vector3.zero, new Vector3(1.1f, 1.15f, 1.1f), yellow, false);
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

        public void Restart()
        {
            State = RunState.Playing;
            Elapsed = 0;
            input = tilt = tiltVelocity = Vector2.zero;
            braking = false;
            previousVelocity = Vector3.zero;
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
                SetControls(new Vector2(
                    (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)), Input.GetKey(KeyCode.Space));
            }
            if (State == RunState.Playing) Elapsed += Time.deltaTime;
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
            Vector3 force = Vector3.ClampMagnitude((desired - horizontal) * (braking ? 8 : 3), braking ? 20 : 12);
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
            GUI.Label(new Rect(34, 72, 400, 30), $"FIRST SHIFT   /   {Elapsed:0.0}s", textStyle);
            GUI.Label(new Rect(34, 108, 400, 48), "Deliver the parcel to the mint zone.\nStop inside the zone to finish.", smallStyle);
            GUI.Box(new Rect(18, 610, 660, 68), GUIContent.none);
            GUI.Label(new Rect(34, 620, 630, 28), "WASD / ARROWS  Move     SPACE  Brake     R  Restart", textStyle);
            GUI.Label(new Rect(34, 651, 620, 24), "Release movement keys to stop. Keep your parcel steady!", smallStyle);
            if (State == RunState.Delivered)
            {
                GUI.Box(new Rect(340, 250, 420, 190), GUIContent.none);
                GUI.Label(new Rect(365, 270, 380, 45), "DELIVERY COMPLETE!", titleStyle);
                GUI.Label(new Rect(365, 325, 360, 30), $"Delivery time: {Elapsed:0.0} seconds", textStyle);
                if (GUI.Button(new Rect(365, 377, 370, 42), "NEXT RUN  [R]")) Restart();
            }
        }
    }
}
