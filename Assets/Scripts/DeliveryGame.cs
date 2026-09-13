using UnityEngine;

namespace RiskyDelivery
{
    public sealed partial class DeliveryGame : MonoBehaviour
    {
        public enum RunState { Playing, Delivered, Failed }
        public RunState State { get; private set; }
        public Rigidbody Cart { get; private set; }
        public float Elapsed { get; private set; }
        public int Chapter { get; private set; } = 1;
        public int CargoHealth { get; private set; } = 100;
        public int Rating => State != RunState.Delivered ? 0 : CargoHealth >= 90 ? 3 : CargoHealth >= 50 ? 2 : 1;
        public static int ChapterCount => ChapterCatalog.Count;
        public CargoBalance Balance { get; private set; }
        public NightTraffic Night { get; private set; }
        public DeliveryProgress Progress { get; private set; }
        public DeliverySound Sound { get; private set; }
        private GameObject roadworks, rainCourse, hillCourse;
        private Light sun;
        private PhysicsMaterial cartFriction;
        private Collider flatCartCollider;
        private readonly Collider[] hillCartColliders = new Collider[2];
        public bool OnWetRoad => Chapter == 3 && Cart != null && RainyRoad.Contains(Cart.position);
        public static string ChapterLabel(int chapter) => ChapterCatalog.Get(chapter).Title;
        public string ChapterName => $"{Chapter:00} / {ChapterLabel(Chapter)}";
        private Material parcelMaterial;
        private float lastImpactTime = -10, impactFlash;
        private int lastDamage;
        public Vector3 Destination => new Vector3(0, 0, 25);
        private Transform cargo;
        private Camera followCamera;
        private Vector2 input, tilt, tiltVelocity;
        private Vector3 previousVelocity;
        private bool braking;
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
            string[] args = System.Environment.GetCommandLineArgs();
            Automated = System.Array.IndexOf(args, "-risky-smoke-test") >= 0 || System.Array.IndexOf(args, "-risky-preview") >= 0;
            Progress = new DeliveryProgress(Automated ? null : System.IO.Path.Combine(Application.persistentDataPath, "progress.json"));
            BuildWorld();
            Sound = new DeliverySound(gameObject, Progress.SoundEnabled);
            StartChapter(1);
            if (!Automated) ShowTitle();
        }

        public void StartChapter(int chapter)
        {
            Chapter = Mathf.Clamp(chapter, 1, ChapterCount);
            roadworks.SetActive(Chapter == 2);
            rainCourse.SetActive(Chapter == 3);
            hillCourse.SetActive(Chapter == 4);
            Night.SetActive(Chapter == 5);
            flatCartCollider.enabled = Chapter != 4;
            foreach (var runner in hillCartColliders) runner.enabled = Chapter == 4;
            sun.intensity = Chapter == 5 ? 0.16f : Chapter == 3 ? 0.85f : 1.2f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Chapter == 5 ? new Color(0.14f, 0.19f, 0.3f) : Chapter == 3 ? new Color(0.5f, 0.62f, 0.75f) : new Color(0.65f, 0.72f, 0.82f);
            RenderSettings.fog = Chapter == 3 || Chapter == 5;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Chapter == 5 ? new Color(0.025f, 0.045f, 0.085f) : new Color(0.18f, 0.26f, 0.34f);
            RenderSettings.fogDensity = Chapter == 5 ? 0.035f : 0.012f;
            followCamera.backgroundColor = Chapter == 5 ? RenderSettings.fogColor : new Color(0.11f, 0.16f, 0.23f);
            Restart();
        }

        public bool TryNextChapter()
        {
            if (View != ViewMode.Driving || State != RunState.Delivered || Chapter >= ChapterCount) return false;
            StartChapter(Chapter + 1);
            return true;
        }

        public void RegisterImpact(float normalSpeed)
        {
            // A scrape is measured along the contact normal, not total driving speed.
            if (View != ViewMode.Driving || State != RunState.Playing || normalSpeed <= 2.5f || Time.time - lastImpactTime < 0.4f) return;
            lastImpactTime = Time.time;
            lastDamage = Mathf.Clamp(Mathf.RoundToInt((normalSpeed - 2.5f) * 12), 1, 65);
            CargoHealth = Mathf.Max(0, CargoHealth - lastDamage);
            impactFlash = 0.8f;
            if (CargoHealth == 0) { State = RunState.Failed; Sound.Failed(); }
            else Sound.Impact();
        }

        public void Restart()
        {
            RestorePlayTime();
            Sound.Reset();
            Balance.Reset();
            Night.Reset();
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
            if (View != ViewMode.Driving) return;
            input = Vector2.ClampMagnitude(direction, 1);
            braking = brake;
        }

        private void Update()
        {
            if (!Automated) ReadPlayerInput();
            if (View == ViewMode.Driving && State == RunState.Playing) Elapsed += Time.deltaTime;
            impactFlash = Mathf.Max(0, impactFlash - Time.deltaTime);
            parcelMaterial.color = impactFlash > 0 ? new Color(1, 0.18f, 0.12f) : Color.Lerp(new Color(0.6f, 0.18f, 0.1f), new Color(1, 0.7f, 0.19f), CargoHealth / 100f);
            cargo.localRotation = Quaternion.Euler(tilt.y, 0, -tilt.x);
            Sound.Tick(new Vector2(Cart.linearVelocity.x, Cart.linearVelocity.z).magnitude, View == ViewMode.Driving && State == RunState.Playing);
        }

        private void FixedUpdate()
        {
            if (State != RunState.Playing)
            {
                Cart.linearVelocity = Vector3.zero;
                return;
            }
            if (View != ViewMode.Driving) return;
            Vector3 horizontal = new Vector3(Cart.linearVelocity.x, 0, Cart.linearVelocity.z);
            Night.Step(Time.fixedDeltaTime);
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
            Balance.Step(this, acceleration);
            if (View != ViewMode.Driving || State != RunState.Playing) return;
            Vector2 target = Vector2.ClampMagnitude(new Vector2(-acceleration.x, -acceleration.z) * 1.4f, 35);
            tilt = Vector2.SmoothDamp(tilt, target, ref tiltVelocity, 0.22f, Mathf.Infinity, Time.fixedDeltaTime);
            if (Mathf.Abs(Cart.position.x - Destination.x) < 2.5f && Mathf.Abs(Cart.position.z - Destination.z) < 2 && Mathf.Abs(Cart.position.y - 0.325f) < 0.3f && horizontal.magnitude < 1)
                CompleteDelivery();
        }

        private void CompleteDelivery()
        {
            bool alreadyComplete = Progress.IsComplete;
            State = RunState.Delivered;
            Cart.linearVelocity = Vector3.zero;
            Progress.Record(Chapter, Rating, Mathf.Max(0.01f, Elapsed));
            if (!alreadyComplete && Progress.IsComplete) View = ViewMode.CampaignComplete;
            Sound.Delivered(!alreadyComplete && Progress.IsComplete);
        }

        public void FailCargoDrop()
        {
            if (View != ViewMode.Driving || State != RunState.Playing) return;
            CargoHealth = 0;
            State = RunState.Failed;
            Sound.Failed();
        }

        private void LateUpdate()
        {
            Vector3 target = Cart.position + new Vector3(0, 14, -11);
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, target, 1 - Mathf.Exp(-7 * Time.deltaTime));
        }


    }
}
