using UnityEngine;

namespace RiskyDelivery
{
    // Sudden acceleration strains the courier's grip until the parcel drops.
    public sealed class CargoBalance
    {
        private readonly Transform cargo;
        public static readonly Vector3 HoldPosition = new Vector3(0, 0.82f, 0.66f);
        public static readonly Vector3 ParcelSize = new Vector3(1.1f, 0.86f, 1.1f);
        public static readonly Vector3 ParcelCenter = Vector3.up * 0.43f;
        private Vector2 offset, velocity;
        private GameObject dropped;
        public bool HasFallen { get; private set; }
        public Rigidbody FallenBody { get; private set; }
        public float Risk => Mathf.Clamp01(new Vector2(offset.x / 0.52f, offset.y / 0.65f).magnitude);

        public CargoBalance(Transform cargo) { this.cargo = cargo; }

        public void Reset()
        {
            if (dropped != null)
            {
                dropped.SetActive(false);
                Object.Destroy(dropped);
            }
            HasFallen = false;
            FallenBody = null;
            offset = velocity = Vector2.zero;
            cargo.gameObject.SetActive(true);
            cargo.localPosition = HoldPosition;
        }

        public void Step(DeliveryGame game, Vector3 acceleration)
        {
            if (game.Chapter != 4 || HasFallen) return;
            float dt = Time.fixedDeltaTime;
            Vector2 load = new Vector2(-acceleration.x, -acceleration.z - HillRoad.Grade(game.Cart.position.z) * 9.81f);
            // Static friction holds gentle driving. Sudden changes overcome it and shift the load.
            Vector2 slipForce = load.magnitude > 3.5f ? load.normalized * (load.magnitude - 3.5f) * 0.35f : Vector2.zero;
            velocity += (slipForce - velocity * 1.8f - offset * 0.8f) * dt;
            offset += velocity * dt;
            cargo.localPosition = HoldPosition + cargo.parent.InverseTransformDirection(new Vector3(offset.x, 0, offset.y));
            if (Risk < 1) return;
            HasFallen = true;
            dropped = Object.Instantiate(cargo.gameObject, cargo.position, cargo.rotation);
            dropped.name = "Fallen parcel";
            var collider = dropped.AddComponent<BoxCollider>();
            collider.center = ParcelCenter;
            collider.size = ParcelSize;
            var body = dropped.AddComponent<Rigidbody>();
            FallenBody = body;
            body.mass = 0.7f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = game.Cart.linearVelocity + new Vector3(velocity.x, 1, velocity.y);
            body.angularVelocity = new Vector3(velocity.y, 0.5f, -velocity.x) * 3;
            cargo.gameObject.SetActive(false);
            game.FailCargoDrop();
        }
    }
}
