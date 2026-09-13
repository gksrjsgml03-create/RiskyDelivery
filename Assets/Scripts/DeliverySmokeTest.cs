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

        private IEnumerator CheckCourierView()
        {
            game.StartChapter(1);
            var rig = game.Cart.transform.Find("Courier facing");
            Check(rig != null && !game.Cart.GetComponentInChildren<MeshRenderer>().enabled, "Visible cart is replaced by courier rig");
            var parcel = rig.Find("Cargo balance pivot");
            Check(parcel != null && parcel.localPosition == CargoBalance.HoldPosition, "Parcel is carried in front of the courier");
            game.SetControls(Vector2.right, true);
            yield return new WaitForSeconds(0.8f);
            Check(Vector3.Dot(rig.forward, Vector3.right) > 0.9f, "Courier turns toward actual movement");
            Check(Vector3.Dot(parcel.position - rig.position, rig.forward) > 0.5f, "Carried parcel follows the courier's facing");
            Check(!Camera.main.orthographic && Camera.main.transform.position.z < game.Cart.position.z - 4, "Perspective third-person camera follows behind courier");
            var leg = rig.Find("Walking leg");
            Quaternion pose = leg.localRotation;
            yield return new WaitForSeconds(0.15f);
            Check(Quaternion.Angle(pose, leg.localRotation) > 1, "Walking animation responds to movement");
            game.Restart();
            Check(rig.localRotation == Quaternion.identity && parcel.localPosition == CargoBalance.HoldPosition, "Restart restores courier and held parcel");
            Debug.Log("RISKY_CHECK_OK: third-person camera, courier facing, walk animation and held parcel");
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            yield return null;
            game = FindFirstObjectByType<DeliveryGame>();
            Check(game != null, "Game boots");
            game.Automated = true;
            ProgressSmokeTests.Run(Check);
            Check(game.Progress.CompletedCount == 0, "Automated gameplay has an isolated empty profile");
            Time.timeScale = 3; // Keep the same fixed physics timestep, run the test faster.
            yield return CheckCourierView();
            yield return CheckSessionFlow();
            game.StartChapter(1);
            Check(!game.TryNextChapter(), "Cannot advance before delivery");
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.Cart.position.z >= 22.8f, 10, "Training movement");
            Check(game.State == DeliveryGame.RunState.Playing, "Must stop before delivery");
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Training delivery");
            Check(game.CargoHealth == 100 && game.Rating == 3, "Undamaged delivery rating");
            Check(game.Progress.BestStars(1) == 3 && game.Progress.NextChapter == 2, "Successful delivery updates campaign records");
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
            Check(game.Progress.BestStars(2) == 0, "Failed runs do not unlock a completed record");
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
            // Reproduce out-of-order saved clears: finishing route 3 fills the last missing record.
            game.Progress.Record(4, 3, 60);
            game.Progress.Record(5, 3, 60);
            Vector2[] rainRoute = {
                new Vector2(3.8f, -10), new Vector2(3.8f, 6.5f),
                new Vector2(-3.8f, 6.5f), new Vector2(-3.8f, 23), new Vector2(0, 25)
            };
            foreach (Vector2 waypoint in rainRoute) yield return DriveTo(waypoint);
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Rain delivery");
            Check(crossedWet && crossedDry, "Rain route crosses wet and dry surfaces");
            Check(game.CargoHealth == 100 && game.Rating == 3, "Rain route can be completed without damage");
            Check(game.Progress.IsComplete && game.View == DeliveryGame.ViewMode.Driving, "Completing saved records on route 3 does not end the campaign");
            Check(game.TryNextChapter() && game.Chapter == 4, "Advance from rain to hill delivery even with prior route 4 and 5 records");
            CheckReset();
            game.SetControls(Vector2.up, true);
            yield return Until(() => game.Cart.position.z > 5, 15, "Climb actual ramp at safe speed");
            Check(game.Cart.position.y > 2 && game.State == DeliveryGame.RunState.Playing && !game.Balance.HasFallen, "Hill raises cart and safe cargo stays aboard");
            yield return DriveTo(new Vector2(0, 25));
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Hill delivery");
            Check(game.CargoHealth == 100 && game.Rating == 3 && game.Cart.position.y < 0.7f, "Hill has damage-free ascent and descent");
            Check(game.TryNextChapter() && game.Chapter == 5, "Advance from hill to night shift");
            yield return CheckNightDelivery();
            game.StartChapter(4);
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
            Debug.Log("RISKY_DELIVERY_SMOKE_OK: five chapters, collisions, failure, restart, damage-free roadworks/rain/hill/night delivery, wet braking, counter-steering, hill ascent/descent, cargo drop, night signals, moving traffic contacts and lighting reset");
            Time.timeScale = 1;
            Application.Quit(0);
        }

        private IEnumerator CheckNightDelivery()
        {
            CheckReset();
            Check(game.Night.Active && game.Night.HeadlightEnabled && RenderSettings.fog, "Night traffic and lighting enabled");
            Vector3 initialVehicle = game.Night.VehiclePosition(0);
            Check(game.Night.GetSignal(0) == NightTraffic.Signal.Green, "Night starts with a safe green");
            yield return Until(() => game.Night.GetSignal(0) == NightTraffic.Signal.Amber, 4, "Green warns amber before moving traffic");
            Check(Vector3.Distance(initialVehicle, game.Night.VehiclePosition(0)) < 0.01f, "Tug remains parked during warning");
            yield return Until(() => game.Night.GetSignal(0) == NightTraffic.Signal.Red, 2, "Amber changes to red");
            yield return new WaitForSeconds(0.5f);
            Check(Vector3.Distance(initialVehicle, game.Night.VehiclePosition(0)) > 1, "Tug physically crosses during red");
            yield return Until(() => game.Night.GetSignal(0) == NightTraffic.Signal.Green, 3, "Crossing reopens after traffic clears");
            Check(game.Night.VehiclePosition(0).x > 5, "Tug clears central delivery lane before green");

            // A stationary cart receives damage from actual moving traffic, not a direct damage call.
            game.Restart();
            game.Cart.position = new Vector3(0, 0.5f, NightTraffic.CrossingZ[0]);
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.CargoHealth < 100, 6, "Moving tug hits waiting cart in crossing");
            Check(game.CargoHealth > 0 && game.State == DeliveryGame.RunState.Playing, "One moving traffic impact is survivable");
            game.Restart();
            CheckReset();
            Check(game.Night.Clock == 0 && Vector3.Distance(initialVehicle, game.Night.VehiclePosition(0)) < 0.01f, "Retry resets traffic clock and positions");
            for (int i = 0; i < NightTraffic.CrossingZ.Length; i++)
            {
                int crossing = i;
                yield return DriveTo(new Vector2(0, NightTraffic.CrossingZ[i] - 4));
                game.SetControls(Vector2.zero, true);
                yield return Until(() => game.Night.GreenRemaining(crossing) > 4.2f, 5, "Wait behind stop line for a fresh green");
                yield return DriveTo(new Vector2(0, NightTraffic.CrossingZ[i] + 3));
                Check(game.CargoHealth == 100, "Green crossing is damage free");
            }
            yield return DriveTo(new Vector2(0, 25));
            game.SetControls(Vector2.zero, true);
            yield return Until(() => game.State == DeliveryGame.RunState.Delivered, 3, "Night delivery");
            Check(game.Rating == 3 && game.CargoHealth == 100 && game.Night.NextCrossing(game.Cart.position.z) == -1, "Night has a complete safe route");
            Check(game.Progress.IsComplete && game.Progress.TotalStars == 15 && game.View == DeliveryGame.ViewMode.CampaignComplete, "Actual five-route completion opens campaign results");
            Check(!game.TryNextChapter(), "No nonexistent sixth chapter");
            float stoppedClock = game.Night.Clock;
            Vector3 stoppedVehicle = game.Night.VehiclePosition(0);
            yield return new WaitForSeconds(0.4f);
            Check(game.Night.Clock == stoppedClock && Vector3.Distance(stoppedVehicle, game.Night.VehiclePosition(0)) < 0.01f, "Traffic freezes on delivery result");
            game.StartChapter(3);
            Check(!game.Night.Active && !game.Night.HeadlightEnabled && RenderSettings.fog && Mathf.Approximately(RenderSettings.fogDensity, 0.012f), "Switching to rain disables night lights and restores rain fog");
            game.StartChapter(1);
            Check(!RenderSettings.fog && RenderSettings.ambientLight.maxColorComponent > 0.7f, "Switching to training restores daylight");
            yield return new WaitForSeconds(0.1f);
            Check(game.Night.Clock == 0, "Inactive night traffic does not advance");
            Debug.Log("RISKY_CHECK_OK: night signal cycle, actual moving collision, safe crossings, night delivery, traffic reset/freeze and daylight restoration");
        }

        private IEnumerator CheckSessionFlow()
        {
            game.StartChapter(5);
            game.SetControls(Vector2.up, true);
            yield return Until(() => game.Cart.position.z > -10, 3, "Drive before pausing");
            // Pause from the rendered frame, just like keyboard/UI input, after the physics step completes.
            yield return null;
            game.Pause();
            Vector3 position = game.Cart.position, velocity = game.Cart.linearVelocity;
            float elapsed = game.Elapsed, trafficClock = game.Night.Clock;
            Check(game.View == DeliveryGame.ViewMode.Paused && Time.timeScale == 0 && AudioListener.pause, "Pause freezes simulation time and gameplay audio");
            game.SetControls(Vector2.right, false);
            game.RegisterImpact(12);
            game.FailCargoDrop();
            yield return new WaitForSecondsRealtime(0.2f);
            Check(game.Cart.position == position && game.Cart.linearVelocity == velocity && game.Elapsed == elapsed && game.Night.Clock == trafficClock && game.CargoHealth == 100, "Paused cart, traffic, timer and cargo do not change");
            game.Resume();
            Check(game.View == DeliveryGame.ViewMode.Driving && Time.timeScale == 3 && !AudioListener.pause, "Resume restores previous simulation speed and audio");
            game.ToggleSound();
            Check(!game.Progress.SoundEnabled, "Sound can be muted during play");
            game.ToggleSound();
            game.SetControls(Vector2.up, true);
            yield return new WaitForSeconds(0.4f);
            Check(game.Cart.position.z > position.z, "Resumed cart moves again");
            game.Pause();
            game.Restart();
            CheckReset();
            Check(game.View == DeliveryGame.ViewMode.Driving && Time.timeScale == 3, "Retry from pause restores play");
            game.ShowTitle();
            game.SetControls(Vector2.up, false);
            yield return new WaitForSecondsRealtime(0.1f);
            Check(game.View == DeliveryGame.ViewMode.Title && game.Elapsed == 0 && game.Cart.position.z == -12, "Title menu does not advance a run");
            game.StartChapter(2);
            CheckReset();
            Check(game.View == DeliveryGame.ViewMode.Driving && Time.timeScale == 3 && game.Chapter == 2, "Title chapter selection starts a clean run");
            Debug.Log("RISKY_CHECK_OK: title, pause, frozen physics and input, resume, retry and chapter navigation");
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
