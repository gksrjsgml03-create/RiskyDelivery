using UnityEngine;
using static RiskyDelivery.WorldGeometry;

namespace RiskyDelivery
{
    // One chapter clock drives signals and real kinematic bodies; retries are deterministic.
    public sealed class NightTraffic
    {
        public enum Signal { Green, Amber, Red }
        private const float GreenDuration = 6, WarningDuration = 1, CrossingDuration = 2.5f;
        private const float HalfCycle = GreenDuration + WarningDuration + CrossingDuration;
        public static readonly float[] CrossingZ = { -1, 14 };
        private readonly GameObject root;
        private readonly Rigidbody[] vehicles = new Rigidbody[2];
        private readonly Material[] signals = new Material[2];
        private readonly Light headlight;
        public float Clock { get; private set; }
        public bool Active => root.activeSelf;
        public bool HeadlightEnabled => headlight.gameObject.activeInHierarchy;
        public Vector3 VehiclePosition(int index) => vehicles[index].position;
        private float Phase(int index) => Mathf.Repeat(Clock + index * 6, HalfCycle * 2);
        private float HalfPhase(int index) => Mathf.Repeat(Phase(index), HalfCycle);
        public Signal GetSignal(int index) => HalfPhase(index) < GreenDuration ? Signal.Green : HalfPhase(index) < GreenDuration + WarningDuration ? Signal.Amber : Signal.Red;
        public float GreenRemaining(int index) => GetSignal(index) == Signal.Green ? GreenDuration - HalfPhase(index) : 0;
        public float UntilGreen(int index) => GetSignal(index) == Signal.Green ? 0 : HalfCycle - HalfPhase(index);
        public int NextCrossing(float z) => z < CrossingZ[0] + 3 ? 0 : z < CrossingZ[1] + 3 ? 1 : -1;

        public NightTraffic(Transform cart, Material vehicleMaterial, Material dark, Material marking, Material lampMaterial)
        {
            root = new GameObject("Chapter 5 - Night shift");
            for (int i = 0; i < vehicles.Length; i++)
            {
                var vehicle = new GameObject("Crossing neighborhood car " + (i + 1));
                vehicle.transform.SetParent(root.transform, false);
                var collider = vehicle.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, 0.6f, 0);
                collider.size = new Vector3(2.4f, 1.1f, 1.6f);
                vehicle.AddComponent<DeliveryHazard>();
                var body = vehicle.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                vehicles[i] = body;
                Decoration(vehicle.transform, "Tug body", new Vector3(0, 0.55f, 0), new Vector3(2.4f, 0.7f, 1.6f), vehicleMaterial);
                var glass = Mat(new Color(0.035f, 0.08f, 0.13f));
                Decoration(vehicle.transform, "Passenger cabin", new Vector3(-0.1f, 1.05f, 0), new Vector3(1.2f, 0.65f, 1.25f), vehicleMaterial);
                Decoration(vehicle.transform, "Front windshield", new Vector3(0.51f, 1.09f, 0), new Vector3(0.025f, 0.4f, 1.08f), glass);
                Decoration(vehicle.transform, "Rear windshield", new Vector3(-0.71f, 1.09f, 0), new Vector3(0.025f, 0.4f, 1.08f), glass);
                var rubber = Mat(new Color(0.025f, 0.028f, 0.035f));
                foreach (int side in new[] { -1, 1 })
                {
                    Decoration(vehicle.transform, "Side window", new Vector3(-0.1f, 1.1f, side * 0.634f), new Vector3(1.05f, 0.38f, 0.02f), glass);
                    Decoration(vehicle.transform, "Window pillar", new Vector3(-0.1f, 1.1f, side * 0.649f), new Vector3(0.07f, 0.41f, 0.025f), vehicleMaterial);
                    foreach (int axle in new[] { -1, 1 })
                    {
                        Decoration(vehicle.transform, "Car tire", new Vector3(axle * 0.8f, 0.28f, side * 0.78f), new Vector3(0.5f, 0.5f, 0.18f), rubber);
                        Decoration(vehicle.transform, "Wheel hub", new Vector3(axle * 0.8f, 0.28f, side * 0.88f), new Vector3(0.22f, 0.22f, 0.025f), marking);
                        Decoration(vehicle.transform, "Car headlamp", new Vector3(axle * 1.21f, 0.65f, side * 0.53f), new Vector3(0.025f, 0.22f, 0.35f), lampMaterial);
                    }
                }
                signals[i] = new Material(lampMaterial);
                signals[i].EnableKeyword("_EMISSION");
                for (int side = -1; side <= 1; side += 2)
                {
                    Decoration(root.transform, "Crossing signal post", new Vector3(side * 6.4f, 1.4f, CrossingZ[i] - 3), new Vector3(0.15f, 2.8f, 0.15f), dark);
                    Decoration(root.transform, "Crossing signal", new Vector3(side * 6.4f, 2.9f, CrossingZ[i] - 3), new Vector3(0.65f, 0.6f, 0.3f), signals[i]);
                }
                Decoration(root.transform, "Stop line", new Vector3(0, 0.04f, CrossingZ[i] - 3), new Vector3(12, 0.025f, 0.18f), marking);
                for (float x = -5; x <= 5; x += 2)
                    Decoration(root.transform, "Cross traffic road marking", new Vector3(x, 0.04f, CrossingZ[i]), new Vector3(0.8f, 0.025f, 1.8f), marking);
                var lamp = new GameObject("Crossing street light").AddComponent<Light>();
                lamp.transform.SetParent(root.transform, false);
                lamp.transform.position = new Vector3(0, 4, CrossingZ[i]);
                lamp.type = LightType.Point;
                lamp.color = new Color(1, 0.75f, 0.4f);
                lamp.range = 11;
                lamp.intensity = 2.5f;
                lamp.renderMode = LightRenderMode.ForcePixel;
            }
            headlight = new GameObject("Courier headlight").AddComponent<Light>();
            headlight.transform.SetParent(cart, false);
            headlight.transform.localPosition = new Vector3(0, 1.1f, 1.1f);
            headlight.transform.localRotation = Quaternion.Euler(28, 0, 0);
            headlight.type = LightType.Spot;
            headlight.color = new Color(0.75f, 0.88f, 1);
            headlight.range = 18;
            headlight.spotAngle = 95;
            headlight.intensity = 6;
            headlight.renderMode = LightRenderMode.ForcePixel;
            SetActive(false);
        }

        public void SetActive(bool active)
        {
            root.SetActive(active);
            headlight.gameObject.SetActive(active);
        }

        public void Reset()
        {
            Clock = 0;
            UpdateVehicles(true);
        }

        public void Step(float deltaTime)
        {
            if (!Active) return;
            Clock += deltaTime;
            UpdateVehicles(false);
        }

        private void UpdateVehicles(bool reset)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                float phase = Phase(i), half = HalfPhase(i);
                float progress = Mathf.Clamp01((half - GreenDuration - WarningDuration) / CrossingDuration);
                float x = Mathf.Lerp(-5.5f, 5.5f, phase < HalfCycle ? progress : 1 - progress);
                var position = new Vector3(x, 0, CrossingZ[i]);
                if (reset) vehicles[i].position = position;
                else vehicles[i].MovePosition(position);
                Color color = GetSignal(i) == Signal.Green ? new Color(0.15f, 1, 0.5f) : GetSignal(i) == Signal.Amber ? new Color(1, 0.65f, 0.1f) : new Color(1, 0.12f, 0.08f);
                signals[i].color = color;
                signals[i].SetColor("_EmissionColor", color * 1.5f);
            }
        }
    }
}
