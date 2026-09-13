using UnityEngine;

namespace RiskyDelivery
{
    public static class WorldGeometry
    {
        public static Material Asphalt()
        {
            var material = Mat(new Color(0.23f, 0.25f, 0.28f));
            var texture = new Texture2D(128, 128, TextureFormat.RGB24, true);
            texture.name = "Procedural asphalt aggregate";
            var random = new System.Random(203);
            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
            {
                float grain = 0.28f + (float)random.NextDouble() * 0.3f;
                pixels[i] = new Color(grain, grain, grain);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.anisoLevel = 4;
            material.mainTexture = texture;
            material.mainTextureScale = new Vector2(7, 26);
            material.SetFloat("_Glossiness", 0.48f);
            return material;
        }

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
