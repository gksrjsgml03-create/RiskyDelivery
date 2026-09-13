using UnityEngine;
using static RiskyDelivery.WorldGeometry;

namespace RiskyDelivery
{
    // The visual rig turns independently from the stable physics body and world-relative controls.
    public sealed class CourierVisual
    {
        public Transform Root { get; }
        private readonly Transform leftLeg, rightLeg, torso;
        private float stride;

        public CourierVisual(Transform body)
        {
            Root = new GameObject("Courier facing").transform;
            Root.SetParent(body, false);
            var blue = Mat(new Color(0.045f, 0.13f, 0.34f));
            var trim = Mat(new Color(0.42f, 0.58f, 0.76f));
            var pants = Mat(new Color(0.035f, 0.05f, 0.085f));
            var skin = Mat(new Color(0.78f, 0.53f, 0.32f));
            var hair = Mat(new Color(0.065f, 0.045f, 0.035f));
            var shoes = Mat(new Color(0.025f, 0.03f, 0.04f));
            torso = Pivot(Root, "Upper body", new Vector3(0, 0.82f, 0));
            Decoration(torso, "Blue delivery jacket", new Vector3(0, 0.15f, 0), new Vector3(0.83f, 0.85f, 0.43f), blue);
            Decoration(torso, "Reflective back stripe", new Vector3(0, 0.38f, -0.224f), new Vector3(0.64f, 0.085f, 0.018f), trim);
            Decoration(torso, "Jacket seam", new Vector3(0, 0.16f, 0.223f), new Vector3(0.025f, 0.7f, 0.015f), pants);
            Decoration(torso, "Neck", new Vector3(0, 0.65f, 0), new Vector3(0.25f, 0.2f, 0.25f), skin);
            Decoration(torso, "Head", new Vector3(0, 0.91f, 0), new Vector3(0.49f, 0.51f, 0.46f), skin);
            Decoration(torso, "Hair back", new Vector3(0, 0.99f, -0.19f), new Vector3(0.51f, 0.39f, 0.1f), hair);
            Decoration(torso, "Blue cap", new Vector3(0, 1.2f, 0), new Vector3(0.56f, 0.22f, 0.53f), blue);
            Decoration(torso, "Cap visor", new Vector3(0, 1.11f, 0.32f), new Vector3(0.57f, 0.065f, 0.3f), blue);
            Decoration(torso, "Cap badge", new Vector3(0, 1.21f, 0.272f), new Vector3(0.17f, 0.12f, 0.015f), trim);
            foreach (int side in new[] { -1, 1 })
            {
                Decoration(torso, "Eye", new Vector3(side * 0.115f, 0.94f, 0.237f), new Vector3(0.055f, 0.065f, 0.018f), hair);
                var sleeve = Decoration(torso, "Carrying sleeve", new Vector3(side * 0.51f, 0.15f, 0.16f), new Vector3(0.25f, 0.56f, 0.27f), blue);
                sleeve.transform.localRotation = Quaternion.Euler(-30, 0, side * 8);
                Decoration(torso, "Forearm", new Vector3(side * 0.5f, -0.025f, 0.4f), new Vector3(0.24f, 0.22f, 0.45f), blue);
                Decoration(torso, "Hand under parcel", new Vector3(side * 0.46f, 0, 0.65f), new Vector3(0.2f, 0.19f, 0.22f), skin);
            }
            leftLeg = Leg(-1, pants, shoes, trim);
            rightLeg = Leg(1, pants, shoes, trim);
        }

        private static Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        private Transform Leg(int side, Material pants, Material shoes, Material trim)
        {
            var leg = Pivot(Root, "Walking leg", new Vector3(side * 0.23f, 0.55f, 0));
            Decoration(leg, "Trouser leg", new Vector3(0, -0.34f, 0), new Vector3(0.31f, 0.68f, 0.32f), pants);
            Decoration(leg, "Trouser reflector", new Vector3(0, -0.58f, -0.165f), new Vector3(0.3f, 0.06f, 0.015f), trim);
            Decoration(leg, "Work boot", new Vector3(0, -0.78f, 0.07f), new Vector3(0.35f, 0.19f, 0.51f), shoes);
            return leg;
        }

        public void Reset()
        {
            stride = 0;
            Root.localRotation = Quaternion.identity;
            leftLeg.localRotation = rightLeg.localRotation = Quaternion.identity;
            torso.localPosition = new Vector3(0, 0.82f, 0);
        }

        public void Tick(Vector3 velocity, float deltaTime, bool sprinting = false)
        {
            velocity.y = 0;
            float speed = velocity.magnitude;
            if (speed > 0.15f)
                Root.localRotation = Quaternion.Slerp(Root.localRotation, Quaternion.LookRotation(velocity), 1 - Mathf.Exp(-12 * deltaTime));
            stride += speed * deltaTime * 3.5f;
            float amount = Mathf.Clamp01(speed / 3);
            float swing = sprinting ? 40 : 27;
            leftLeg.localRotation = Quaternion.Euler(Mathf.Sin(stride) * swing * amount, 0, 0);
            rightLeg.localRotation = Quaternion.Euler(-Mathf.Sin(stride) * swing * amount, 0, 0);
            torso.localPosition = new Vector3(0, 0.82f + Mathf.Abs(Mathf.Sin(stride)) * 0.035f * amount, 0);
        }
    }
}
