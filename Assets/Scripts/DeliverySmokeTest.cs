using System;
using System.Collections;
using UnityEngine;

namespace RiskyDelivery
{
    // Opt-in standalone integration tests exercise real PhysX contacts and level geometry.
    public sealed class DeliverySmokeTest : MonoBehaviour
    {
        private DeliveryGame game;
        private float measuredDistance, measuredLateralSpeed;
        private bool crossedWet, crossedDry;
        private static readonly WaitForFixedUpdate PhysicsFrame = new WaitForFixedUpdate();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-risky-smoke-test") >= 0)
                new GameObject("Delivery smoke test").AddComponent<DeliverySmokeTest>();
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            yield return null;
            game = FindFirstObjectByType<DeliveryGame>();
            Check(game != null, "Game boots");
            game.Automated = true;
            Time.timeScale = 3; // Keep the same fixed physics timestep, run the test faster.
            game.StartChapter(1);
            Check(!game.TryNextChapter(), "Cannot advance before delivery");
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.Cart.position.z >= 22.8f, 10, "Training movement");
            Check(game.State == DeliveryGame.RunState.Playing, "Must stop before delivery");
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Training delivery");
            Check(game.CargoHealth == 100 && game.Rating == 3, "Undamaged delivery rating");
            Check(game.TryNextChapter() && game.Chapter == 2, "Advance to roadworks");
            CheckReset();
            Debug.Log("RISKY_CHECK_OK: training, stop-to-deliver, chapter progression");

            // A braking-speed bump must not damage cargo, including sustained contact.
            game.SetControls(Vector2.up, true);
            yield return Until(() => game.Cart.position.z > -4.7f, 7, "Slow approach to actual barricade");
            yield return new WaitForSeconds(1);
            Check(game.Cart.position.z < -4.3f && game.CargoHealth == 100, "Barricade blocks cart; slow bump is safe");
            Debug.Log("RISKY_CHECK_OK: low-speed impact and blocking geometry");

            game.Restart();
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.CargoHealth < 100, 5, "Fast barricade collision causes damage");
            int afterImpact = game.CargoHealth;
            Check(afterImpact > 0, "One ordinary collision is survivable");
            yield return new WaitForSeconds(1);
            Check(game.CargoHealth == afterImpact, "Holding against wall does not repeatedly damage cargo");
            game.SetControls(Vector2.down, false);
            yield return Until(() => game.Cart.position.z < -11, 5, "Reverse away from barrier");
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.State == DeliveryGame.RunState.Failed, 6, "Repeated hard collision breaks parcel");
            Check(game.CargoHealth == 0 && game.Rating == 0, "Failed parcel cannot earn a rating");
            game.Cart.position = game.Destination + Vector3.up * 0.4f;
            game.SetControls(Vector2.zero, true);
            yield return new WaitForSeconds(0.5f);
            Check(game.State == DeliveryGame.RunState.Failed, "Broken parcel cannot complete delivery");
            game.Restart();
            CheckReset();
            Check(game.Chapter == 2, "Retry preserves chapter");
            Debug.Log("RISKY_CHECK_OK: impact damage, contact debounce, failure, failure guard, restart");

            // Navigate the real alternating gaps rather than teleporting through obstacles.
            Vector2[] route = {
                new Vector2(3.8f, -8), new Vector2(3.8f, 0.5f),
                new Vector2(-3.8f, 3), new Vector2(-3.8f, 10.5f),
                new Vector2(3.8f, 13), new Vector2(3.8f, 20.5f),
                new Vector2(0, 25)
            };
            foreach (Vector2 waypoint in route) yield return DriveTo(waypoint);
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Roadworks delivery");
            Check(game.CargoHealth == 100 && game.Rating == 3, "Roadworks has a damage-free route");
            Check(game.TryNextChapter() && game.Chapter == 3, "Advance from roadworks to rain run");
            CheckReset();
            yield return MeasureBraking(1);
            float dryDistance = measuredDistance;
            yield return MeasureBraking(3);
            float wetDistance = measuredDistance;
            Check(wetDistance > dryDistance * 3 && wetDistance > 3, "Wet road has meaningfully longer braking distance");
            yield return MeasureSteering(1);
            float dryLateral = measuredLateralSpeed;
            yield return MeasureSteering(3);
            Check(measuredLateralSpeed > dryLateral + 1.2f, "Wet road preserves lateral momentum when counter-steering");
            Debug.Log($"RISKY_CHECK_OK: wet braking {wetDistance:0.00}m vs dry {dryDistance:0.00}m; wet counter-steer {measuredLateralSpeed:0.00}m/s vs dry {dryLateral:0.00}m/s");
            game.Restart();
            CheckReset();
            Check(game.Chapter == 3 && !game.OnWetRoad, "Rain retry starts on dry ground");
            Vector2[] rainRoute = {
                new Vector2(3.8f, -10), new Vector2(3.8f, 6.5f),
                new Vector2(-3.8f, 6.5f), new Vector2(-3.8f, 23), new Vector2(0, 25)
            };
            foreach (Vector2 waypoint in rainRoute) yield return DriveTo(waypoint);
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Rain delivery");
            Check(crossedWet && crossedDry, "Rain route crosses wet and dry surfaces");
            Check(game.CargoHealth == 100 && game.Rating == 3, "Rain route can be completed without damage");
            Check(game.TryNextChapter() && game.Chapter == 4, "Advance from rain to hill delivery");
            CheckReset();
            game.SetControls(Vector2.up, true);
            yield return Until(() => game.Cart.position.z > 5, 15, "Climb actual ramp at safe speed");
            Check(game.Cart.position.y > 2 && game.State == DeliveryGame.RunState.Playing && !game.Balance.HasFallen, "Hill raises cart and safe cargo stays aboard");
            yield return DriveTo(new Vector2(0, 25));
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Hill delivery");
            Check(game.CargoHealth == 100 && game.Rating == 3 && game.Cart.position.y < 0.7f, "Hill has damage-free ascent and descent");
            Check(!game.TryNextChapter(), "No nonexistent fifth chapter");
            game.Restart();
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.Cart.position.z > 0, 5, "Approach hill at speed");
            Check(game.State == DeliveryGame.RunState.Playing && game.CargoHealth == 100, "Fast approach has no wall impact");
            game.SetControls(Vector2.down, false);
            yield return Until(() => game.Balance.HasFallen, 4, "Abrupt reversal drops unsecured cargo on the hill");
            Check(game.State == DeliveryGame.RunState.Failed && game.CargoHealth == 0 && game.Rating == 0, "Fallen cargo fails delivery");
            Check(game.Balance.FallenBody != null && game.Balance.FallenBody.transform.parent == null && !game.Balance.FallenBody.isKinematic, "Dropped parcel is an independent physical body");
            Vector3 dropPosition = game.Balance.FallenBody.position;
            game.Cart.position = game.Destination + Vector3.up * 0.4f;
            yield return new WaitForSeconds(0.4f);
            Check(Vector3.Distance(dropPosition, game.Balance.FallenBody.position) > 0.1f, "Dropped parcel keeps moving after cart failure");
            Check(game.State == DeliveryGame.RunState.Failed, "Fallen parcel cannot deliver");
            game.Restart();
            CheckReset();
            Check(!game.Balance.HasFallen && game.Balance.FallenBody == null && game.Balance.Risk == 0 && game.Chapter == 4, "Retry restores secured cargo in hill chapter");
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.Cart.position.z > 0, 5, "Approach hill again after restart");
            game.SetControls(Vector2.down, false);
            yield return Until(() => game.Balance.HasFallen, 4, "Cargo can fall again after restart");
            game.StartChapter(1);
            CheckReset();
            Check(!game.Balance.HasFallen && game.Balance.Risk == 0, "Chapter selection clears fallen cargo");
            Check(game.Chapter == 1 && !game.OnWetRoad && !RenderSettings.fog, "Chapter selection clears rain and resets state");
            Debug.Log("RISKY_DELIVERY_SMOKE_OK: training, chapters, collisions, failure, restart, damage-free roadworks/rain/hill delivery, wet braking, counter-steering, physical hill ascent/descent, cargo drop and recovery");
            Time.timeScale = 1;
            Application.Quit(0);
        }

        private IEnumerator MeasureBraking(int chapter)
        {
            game.StartChapter(chapter);
            game.Cart.position = new Vector3(0, 0.4f, -6);
            game.SetControls(Vector2.zero, true);
            yield return new WaitForSeconds(0.15f);
            Check(game.OnWetRoad == (chapter == 3), "Surface detection for braking comparison");
            float startZ = game.Cart.position.z;
            game.Cart.linearVelocity = Vector3.forward * 6;
            yield return Until(() => game.Cart.linearVelocity.magnitude < 0.25f, 5, "Brake to stop");
            measuredDistance = game.Cart.position.z - startZ;
            Check(game.CargoHealth == 100, "Braking comparison does not hit obstacles");
        }

        private IEnumerator MeasureSteering(int chapter)
        {
            game.StartChapter(chapter);
            game.Cart.position = new Vector3(0, 0.4f, -4);
            game.SetControls(Vector2.zero, true);
            yield return new WaitForSeconds(0.15f);
            game.Cart.linearVelocity = Vector3.right * 4;
            game.SetControls(Vector2.left, false);
            float end = Time.fixedTime + 0.25f;
            while (Time.fixedTime < end) yield return PhysicsFrame;
            measuredLateralSpeed = game.Cart.linearVelocity.x;
        }

        private IEnumerator DriveTo(Vector2 target)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (Vector2.Distance(new Vector2(game.Cart.position.x, game.Cart.position.z), target) > 0.22f)
            {
                Check(Time.realtimeSinceStartup < deadline, "Route waypoint " + target);
                Check(game.State == DeliveryGame.RunState.Playing || (target.y == 25 && game.State == DeliveryGame.RunState.Delivered), "Route remains playable");
                if (game.State == DeliveryGame.RunState.Delivered) yield break;
                if (game.Chapter == 3)
                {
                    crossedWet |= game.OnWetRoad;
                    crossedDry |= !game.OnWetRoad;
                }
                Vector2 delta = target - new Vector2(game.Cart.position.x, game.Cart.position.z);
                game.SetControls(Vector2.ClampMagnitude(delta * 1.5f, 1), true);
                yield return PhysicsFrame;
            }
        }

        private IEnumerator Until(Func<bool> predicate, float timeout, string label)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate())
            {
                Check(Time.realtimeSinceStartup < deadline, label);
                yield return PhysicsFrame;
            }
        }

        private void CheckReset()
        {
            Check(game.State == DeliveryGame.RunState.Playing && game.CargoHealth == 100 && game.Elapsed == 0 && game.Cart.position.z == -12 && game.Cart.linearVelocity.sqrMagnitude < 0.001f, "Reset state");
        }

        private void Check(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException($"RISKY_DELIVERY_SMOKE_FAILED: {label}; chapter={game?.Chapter}, state={game?.State}, health={game?.CargoHealth}, position={game?.Cart.position}, velocity={game?.Cart.linearVelocity}");
        }

        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                Application.Quit(1);
        }
    }
}
