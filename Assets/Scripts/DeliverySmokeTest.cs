using System;
using System.Collections;
using UnityEngine;

namespace RiskyDelivery
{
    // Opt-in standalone integration tests exercise real PhysX contacts and level geometry.
    public sealed class DeliverySmokeTest : MonoBehaviour
    {
        private DeliveryGame game;
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
            Check(!game.TryNextChapter(), "No nonexistent third chapter");
            game.StartChapter(1);
            CheckReset();
            Check(game.Chapter == 1, "Chapter selection resets state");
            Debug.Log("RISKY_DELIVERY_SMOKE_OK: training, chapters, low/high-speed collisions, failure, restart, damage-free roadworks delivery");
            Time.timeScale = 1;
            Application.Quit(0);
        }

        private IEnumerator DriveTo(Vector2 target)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (Vector2.Distance(new Vector2(game.Cart.position.x, game.Cart.position.z), target) > 0.22f)
            {
                Check(Time.realtimeSinceStartup < deadline, "Route waypoint " + target);
                Check(game.State == DeliveryGame.RunState.Playing || (target.y == 25 && game.State == DeliveryGame.RunState.Delivered), "Route remains playable");
                if (game.State == DeliveryGame.RunState.Delivered) yield break;
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
