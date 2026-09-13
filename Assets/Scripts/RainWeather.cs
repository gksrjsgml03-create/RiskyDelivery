using UnityEngine;
using UnityEngine.Rendering;

namespace RiskyDelivery
{
    public sealed class RainWeather : MonoBehaviour
    {
        private Transform courier;
        private readonly Transform[] drops = new Transform[72];

        public void Initialize(Transform target, Material material)
        {
            courier = target;
            var random = new System.Random(73);
            for (int i = 0; i < drops.Length; i++)
            {
                var position = new Vector3((float)random.NextDouble() * 24 - 12, (float)random.NextDouble() * 14, (float)random.NextDouble() * 26 - 13);
                var drop = WorldGeometry.Decoration(transform, "Rain streak", position, new Vector3(0.025f, 0.6f, 0.025f), material);
                drop.transform.localRotation = Quaternion.Euler(-12, 0, -10);
                var renderer = drop.GetComponent<Renderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                drops[i] = drop.transform;
            }
        }

        private void LateUpdate()
        {
            if (courier == null) return;
            transform.position = new Vector3(courier.position.x, 0, courier.position.z);
            foreach (Transform drop in drops)
            {
                Vector3 position = drop.localPosition + new Vector3(-2, -17, -3) * Time.deltaTime;
                if (position.y < 0) position.y += 14;
                if (position.x < -12) position.x += 24;
                if (position.z < -13) position.z += 26;
                drop.localPosition = position;
            }
        }
    }
}
