using UnityEngine;
using static RiskyDelivery.WorldGeometry;

namespace RiskyDelivery
{
    public sealed partial class DeliveryGame
    {
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
            var road = Asphalt();
            var grass = Mat(new Color(0.09f, 0.12f, 0.12f));
            var white = Mat(new Color(0.94f, 0.93f, 0.82f));
            var yellow = Mat(new Color(1f, 0.7f, 0.19f));
            var teal = Mat(new Color(0.12f, 0.85f, 0.72f));
            var curb = Mat(new Color(0.24f, 0.25f, 0.26f));
            Box("Ground", new Vector3(0, -0.8f, 8), new Vector3(70, 1, 90), grass);
            Box("Delivery road", new Vector3(0, -0.15f, 8), new Vector3(14, 0.3f, 52), road);
            Barrier("Left barrier", new Vector3(-7.3f, 0.35f, 8), new Vector3(0.5f, 0.7f, 52), curb);
            Barrier("Right barrier", new Vector3(7.3f, 0.35f, 8), new Vector3(0.5f, 0.7f, 52), curb);
            Barrier("Start barrier", new Vector3(0, 0.35f, -18), new Vector3(15, 0.7f, 0.5f), curb);
            Barrier("End barrier", new Vector3(0, 0.35f, 34), new Vector3(15, 0.7f, 0.5f), curb);
            for (int z = -15; z < 32; z += 4)
                Box("Lane marking", new Vector3(0, 0.015f, z), new Vector3(0.12f, 0.02f, 1.5f), white, false);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Depot parking outline", new Vector3(side * 3, 0.025f, -12), new Vector3(0.12f, 0.02f, 5), yellow, false);
                Box("Depot parking outline", new Vector3(0, 0.025f, -12 + side * 2.5f), new Vector3(6, 0.02f, 0.12f), yellow, false);
                Box("Delivery zone outline", Destination + new Vector3(side * 3, 0.035f, 0), new Vector3(0.18f, 0.025f, 5), teal, false);
                Box("Delivery zone outline", Destination + new Vector3(0, 0.035f, side * 2.5f), new Vector3(6, 0.025f, 0.18f), teal, false);
            }
            ResidentialStreet.Create();
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
            hillCourse = HillRoad.Create(road, yellow, teal);
            var cartObject = new GameObject("Courier physics body");
            cartObject.AddComponent<CartImpactReceiver>().Game = this;
            var body = Box("Cart body", Vector3.zero, new Vector3(1.5f, 0.65f, 2), teal);
            body.transform.SetParent(cartObject.transform, false);
            body.GetComponent<Renderer>().enabled = false;
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
            flatCartCollider = body.GetComponent<Collider>();
            // Rounded runners cross convex ramp crests without catching a box's leading edge.
            for (int i = 0; i < hillCartColliders.Length; i++)
            {
                var runner = cartObject.AddComponent<CapsuleCollider>();
                runner.direction = 2;
                runner.radius = 0.325f;
                runner.height = 2;
                runner.center = new Vector3(i == 0 ? -0.425f : 0.425f, 0, 0);
                runner.sharedMaterial = cartFriction;
                hillCartColliders[i] = runner;
            }
            Cart = cartObject.AddComponent<Rigidbody>();
            Cart.mass = 4;
            Cart.constraints = RigidbodyConstraints.FreezeRotation;
            Cart.interpolation = RigidbodyInterpolation.Interpolate;
            Cart.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            courier = new CourierVisual(cartObject.transform);
            var rain = new GameObject("Rain around courier");
            rain.transform.SetParent(rainCourse.transform);
            rain.AddComponent<RainWeather>().Initialize(Cart.transform, reflection);
            cargo = new GameObject("Cargo balance pivot").transform;
            cargo.SetParent(courier.Root, false);
            cargo.localPosition = CargoBalance.HoldPosition;
            cargo.localScale = Vector3.one * 0.82f;
            parcelMaterial = Mat(new Color(1f, 0.7f, 0.19f));
            var parcel = Box("Fragile parcel", Vector3.zero, CargoBalance.ParcelSize, parcelMaterial, false);
            parcel.transform.SetParent(cargo, false);
            parcel.transform.localPosition = CargoBalance.ParcelCenter;
            var tape = Box("Parcel tape", Vector3.zero, new Vector3(0.18f, 0.88f, 1.12f), white, false);
            tape.transform.SetParent(cargo, false);
            tape.transform.localPosition = CargoBalance.ParcelCenter;
            Decoration(cargo, "Shipping label", new Vector3(0, 0.49f, 0.56f), new Vector3(0.48f, 0.38f, 0.015f), white);
            var ink = Mat(new Color(0.12f, 0.09f, 0.06f));
            for (int bar = 0; bar < 11; bar++)
                Decoration(cargo, "Parcel barcode", new Vector3(-0.18f + bar * 0.035f, 0.44f, 0.573f), new Vector3(bar % 3 == 0 ? 0.022f : 0.01f, 0.17f, 0.008f), ink);
            Balance = new CargoBalance(cargo);
            Night = new NightTraffic(courier.Root, orange, road, white, yellow);
            followCamera = new GameObject("Follow camera").AddComponent<Camera>();
            followCamera.tag = "MainCamera";
            followCamera.gameObject.AddComponent<AudioListener>();
            followCamera.orthographic = false;
            followCamera.fieldOfView = 62;
            followCamera.nearClipPlane = 0.15f;
            followCamera.farClipPlane = 130;
            followCamera.backgroundColor = new Color(0.11f, 0.16f, 0.23f);
            followCamera.clearFlags = CameraClearFlags.SolidColor;
            followCamera.transform.rotation = Quaternion.Euler(23, 0, 0);
        }

    }
}
