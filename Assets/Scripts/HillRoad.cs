using UnityEngine;

namespace RiskyDelivery
{
    public static class HillRoad
    {
        public static float Height(float z) => z < -6 ? 0 : z < 3 ? (z + 6) * 0.2f : z < 9 ? 1.8f : z < 18 ? (18 - z) * 0.2f : 0;
        public static float Grade(float z) => z > -6 && z < 3 ? 0.2f : z > 9 && z < 18 ? -0.2f : 0;

        public static GameObject Create(Material road, Material rail, Material guide)
        {
            var root = new GameObject("Chapter 4 - Hill delivery");
            float[] stations = { -6, 3, 9, 18 };
            var surface = new GameObject("Continuous raised road");
            surface.transform.SetParent(root.transform, false);
            var mesh = new Mesh { name = "Hill road" };
            var vertices = new Vector3[stations.Length * 2];
            var triangles = new int[(stations.Length - 1) * 6];
            for (int i = 0; i < stations.Length; i++)
            {
                vertices[i * 2] = new Vector3(-7, Height(stations[i]), stations[i]);
                vertices[i * 2 + 1] = new Vector3(7, Height(stations[i]), stations[i]);
                if (i == stations.Length - 1) continue;
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial = road;
            // Shared vertices let PhysX treat crest edges as a continuous surface.
            surface.AddComponent<MeshCollider>().sharedMesh = mesh;
            for (int i = 0; i < stations.Length - 1; i++)
            {
                float start = stations[i], end = stations[i + 1];
                float angle = -Mathf.Atan2(Height(end) - Height(start), end - start) * Mathf.Rad2Deg;
                foreach (int side in new[] { -1, 1 })
                {
                    var position = new Vector3(side * 7.1f, (Height(start) + Height(end)) / 2 + 0.4f, (start + end) / 2);
                    var size = new Vector3(0.4f, 0.8f, Vector2.Distance(new Vector2(start, Height(start)), new Vector2(end, Height(end))) + 0.1f);
                    var barrier = WorldGeometry.Barrier("Hill guardrail", position, size, rail);
                    barrier.transform.SetParent(root.transform, false);
                    barrier.transform.rotation = Quaternion.Euler(angle, 0, 0);
                }
            }
            for (float z = -8; z <= 20; z += 2)
            {
                var marker = WorldGeometry.Decoration(root.transform, "Hill route marker", new Vector3(0, Height(z) + 0.025f, z), new Vector3(0.25f, 0.025f, 0.9f), guide);
                marker.transform.rotation = Quaternion.Euler(-Mathf.Atan(Grade(z)) * Mathf.Rad2Deg, 0, 0);
            }
            return root;
        }
    }
}
