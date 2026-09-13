using UnityEngine;

namespace RiskyDelivery
{
    public sealed class CartImpactReceiver : MonoBehaviour
    {
        public DeliveryGame Game { private get; set; }

        private void OnCollisionEnter(Collision collision)
        {
            if (Game == null || collision.collider.GetComponent<DeliveryHazard>() == null) return;
            float impactSpeed = 0;
            Vector3 impactNormal = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (Mathf.Abs(normal.y) > 0.5f) continue;
                float speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
                if (speed <= impactSpeed) continue;
                impactSpeed = speed;
                impactNormal = normal;
            }
            Game.RegisterImpact(impactSpeed, impactNormal);
        }
    }
}
