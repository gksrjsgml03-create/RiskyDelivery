using UnityEngine;

namespace RiskyDelivery
{
    public static class WorldGeometry
    {
        public static Material Mat(Color color)
        {
            var template = Resources.Load<Material>("WorldMaterial");
            var material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", 0.15f);
            return material;
        }

        public static GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid)
            {
                box.GetComponent<Collider>().enabled = false;
                Object.Destroy(box.GetComponent<Collider>());
            }
            return box;
        }

        public static GameObject Barrier(string name, Vector3 position, Vector3 size, Material material)
        {
            var box = Box(name, position, size, material);
            box.AddComponent<DeliveryHazard>();
            return box;
        }

        public static GameObject Decoration(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var box = Box(name, position, size, material, false);
            box.transform.SetParent(parent, false);
            return box;
        }
    }
}
