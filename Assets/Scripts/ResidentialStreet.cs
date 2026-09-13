using UnityEngine;
using static RiskyDelivery.WorldGeometry;

namespace RiskyDelivery
{
    public static class ResidentialStreet
    {
        public static void Create()
        {
            var root = new GameObject("Residential delivery district").transform;
            var stone = Mat(new Color(0.25f, 0.27f, 0.29f));
            var dark = Mat(new Color(0.045f, 0.055f, 0.07f));
            var wood = Mat(new Color(0.15f, 0.105f, 0.07f));
            var leaf = Mat(new Color(0.08f, 0.19f, 0.12f));
            var warm = Mat(new Color(1, 0.72f, 0.36f));
            warm.EnableKeyword("_EMISSION");
            warm.SetColor("_EmissionColor", new Color(1, 0.59f, 0.2f) * 1.4f);
            foreach (int side in new[] { -1, 1 })
            {
                Decoration(root, "Stone sidewalk", new Vector3(side * 8.4f, 0.06f, 8), new Vector3(2, 0.12f, 52), stone);
                for (int z = -17; z < 34; z++)
                    Decoration(root, "Paving joint", new Vector3(side * 8.4f, 0.125f, z), new Vector3(1.98f, 0.009f, 0.025f), dark);
                for (int i = 0; i < 9; i++)
                {
                    float z = -12 + i * 5.5f;
                    float height = 5.5f + i % 3 * 1.1f;
                    var facade = Mat(Color.Lerp(new Color(0.19f, 0.24f, 0.3f), new Color(0.38f, 0.3f, 0.24f), (i % 4) / 3f));
                    Decoration(root, "Townhouse", new Vector3(side * 12, height / 2, z), new Vector3(5.5f, height, 5.1f), facade);
                    Decoration(root, "Roof cornice", new Vector3(side * 11.85f, height, z), new Vector3(5.9f, 0.25f, 5.35f), dark);
                    Decoration(root, "Foundation", new Vector3(side * 9.2f, 0.26f, z), new Vector3(0.15f, 0.5f, 5.05f), stone);
                    Decoration(root, "Recessed front door", new Vector3(side * 9.23f, 1.25f, z), new Vector3(0.065f, 2.45f, 1.05f), wood);
                    Decoration(root, "Door handle", new Vector3(side * 9.17f, 1.15f, z + 0.35f), new Vector3(0.06f, 0.15f, 0.045f), warm);
                    Decoration(root, "Entrance canopy", new Vector3(side * 8.95f, 2.7f, z), new Vector3(0.8f, 0.16f, 1.75f), stone);
                    for (int floor = 0; floor < 2; floor++)
                    foreach (int offset in new[] { -1, 1 })
                    {
                        float windowZ = z + offset * 1.7f;
                        float y = 1.6f + floor * 2.65f;
                        Decoration(root, "Window surround", new Vector3(side * 9.21f, y, windowZ), new Vector3(0.15f, 1.5f, 1.02f), stone);
                        Decoration(root, "Warm window glass", new Vector3(side * 9.115f, y, windowZ), new Vector3(0.025f, 1.23f, 0.78f), (i + floor) % 3 == 0 ? dark : warm);
                        Decoration(root, "Window mullion", new Vector3(side * 9.09f, y, windowZ), new Vector3(0.035f, 1.27f, 0.06f), wood);
                    }
                    Decoration(root, "Mailbox", new Vector3(side * 8.7f, 0.75f, z + 0.95f), new Vector3(0.36f, 1.35f, 0.45f), dark);
                    Decoration(root, "Mail slot", new Vector3(side * 8.505f, 1.2f, z + 0.95f), new Vector3(0.02f, 0.035f, 0.27f), stone);
                    Decoration(root, "Planter", new Vector3(side * 8.65f, 0.28f, z - 1.05f), new Vector3(0.55f, 0.5f, 0.65f), wood);
                    for (int sprig = 0; sprig < 3; sprig++)
                    {
                        var bush = Decoration(root, "Angular shrub", new Vector3(side * 8.65f, 0.7f + sprig * 0.16f, z - 1.1f + sprig * 0.12f), new Vector3(0.55f, 0.48f, 0.48f), leaf);
                        bush.transform.localRotation = Quaternion.Euler(12, sprig * 35, 15);
                    }
                    Decoration(root, "Porch lantern", new Vector3(side * 9.05f, 2.1f, z + 0.8f), new Vector3(0.25f, 0.35f, 0.22f), warm);
                    if (i % 2 == 0)
                    {
                        var light = new GameObject("Warm porch light").AddComponent<Light>();
                        light.transform.SetParent(root, false);
                        light.transform.position = new Vector3(side * 8.6f, 2.3f, z);
                        light.type = LightType.Point;
                        light.color = new Color(1, 0.7f, 0.38f);
                        light.intensity = 1.6f;
                        light.range = 6;
                        Decoration(root, "Street lamp post", new Vector3(side * 7.8f, 2.4f, z + 2), new Vector3(0.12f, 4.8f, 0.12f), dark);
                        Decoration(root, "Street lamp arm", new Vector3(side * 7.5f, 4.75f, z + 2), new Vector3(0.75f, 0.12f, 0.12f), dark);
                        Decoration(root, "Street lamp lens", new Vector3(side * 7.2f, 4.7f, z + 2), new Vector3(0.5f, 0.09f, 0.34f), warm);
                        var lamp = new GameObject("Street lamp pool").AddComponent<Light>();
                        lamp.transform.SetParent(root, false);
                        lamp.transform.position = new Vector3(side * 7.1f, 4.5f, z + 2);
                        lamp.transform.rotation = Quaternion.Euler(75, side * -25, 0);
                        lamp.type = LightType.Spot;
                        lamp.spotAngle = 110;
                        lamp.range = 12;
                        lamp.intensity = 3.5f;
                        lamp.color = new Color(1, 0.76f, 0.43f);
                    }
                }
            }
            // The destination is a visible porch at the end of the playable street.
            Decoration(root, "Recipient home", new Vector3(0, 3.2f, 37), new Vector3(11, 6.4f, 4), stone);
            Decoration(root, "Recipient entrance", new Vector3(0, 1.6f, 34.97f), new Vector3(1.8f, 3.2f, 0.08f), wood);
            Decoration(root, "Recipient awning", new Vector3(0, 3.4f, 34.5f), new Vector3(3.5f, 0.2f, 1.4f), dark);
            foreach (int side in new[] { -1, 1 })
                Decoration(root, "Recipient window", new Vector3(side * 3, 2.3f, 34.94f), new Vector3(1.7f, 2.1f, 0.1f), warm);
            CreateVan(root, dark, warm);
        }

        private static void CreateVan(Transform parent, Material dark, Material lamp)
        {
            var van = new GameObject("Parked delivery van").transform;
            van.SetParent(parent, false);
            van.localPosition = new Vector3(-4.6f, 0, -15.5f);
            var collider = van.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 1.25f, 0);
            collider.size = new Vector3(2, 2.1f, 3.9f);
            van.gameObject.AddComponent<DeliveryHazard>();
            var paint = Mat(new Color(0.2f, 0.27f, 0.36f));
            var glass = Mat(new Color(0.025f, 0.065f, 0.1f));
            Decoration(van, "Van body", new Vector3(0, 1.25f, 0), new Vector3(2, 2.1f, 3.9f), paint);
            Decoration(van, "Windshield", new Vector3(0, 1.85f, 1.97f), new Vector3(1.7f, 0.7f, 0.03f), glass);
            Decoration(van, "Open cargo bay", new Vector3(0, 1.3f, -1.97f), new Vector3(1.73f, 1.7f, 0.04f), dark);
            var cardboard = Mat(new Color(0.66f, 0.43f, 0.22f));
            for (int i = 0; i < 3; i++)
                Decoration(van, "Loaded parcel", new Vector3(i % 2 * 0.7f - 0.35f, 0.65f + i / 2 * 0.65f, -2), new Vector3(0.65f, 0.62f, 0.1f), cardboard);
            foreach (int side in new[] { -1, 1 })
            {
                Decoration(van, "Headlamp", new Vector3(side * 0.7f, 0.8f, 1.98f), new Vector3(0.4f, 0.25f, 0.05f), lamp);
                foreach (int axle in new[] { -1, 1 })
                    Decoration(van, "Van tire", new Vector3(side, 0.38f, axle * 1.3f), new Vector3(0.2f, 0.7f, 0.7f), dark);
            }
        }
    }
}
