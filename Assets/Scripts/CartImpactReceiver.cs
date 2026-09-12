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
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (Mathf.Abs(normal.y) > 0.5f) continue;
                impactSpeed = Mathf.Max(impactSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal)));
            }
            Game.RegisterImpact(impactSpeed);
        }
    }
}
