using System;
using System.Collections;
using UnityEngine;

namespace RiskyDelivery
{
    // Opt-in integration check, executed only by a standalone player with this flag.
    public sealed class DeliverySmokeTest : MonoBehaviour
    {
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
            var game = FindFirstObjectByType<DeliveryGame>();
            if (game == null) { Fail("Game did not boot"); yield break; }
            game.Automated = true;
            game.SetControls(Vector2.up, false);
            float deadline = Time.realtimeSinceStartup + 15;
            while (game.Cart.position.z < 22.8f)
            {
                if (Time.realtimeSinceStartup > deadline) { Fail($"Cart did not reach destination approach: position={game.Cart.position}, velocity={game.Cart.linearVelocity}, elapsed={game.Elapsed}, state={game.State}, gravity={Physics.gravity}"); yield break; }
                yield return new WaitForFixedUpdate();
            }
            if (game.State != DeliveryGame.RunState.Playing) { Fail("Delivery completed before stopping"); yield break; }
            game.SetControls(Vector2.zero, true);
            deadline = Time.realtimeSinceStartup + 4;
            while (game.State == DeliveryGame.RunState.Playing && Time.realtimeSinceStartup < deadline)
                yield return new WaitForFixedUpdate();
            if (game.State != DeliveryGame.RunState.Delivered) { Fail("Stopping in destination did not complete delivery"); yield break; }
            game.Restart();
            if (game.State != DeliveryGame.RunState.Playing || game.Elapsed != 0 || game.Cart.position.z != -12 || game.Cart.linearVelocity.sqrMagnitude > 0.001f)
            { Fail("Restart did not reset state"); yield break; }
            Debug.Log("RISKY_DELIVERY_SMOKE_OK: movement, stop-to-deliver, restart");
            Application.Quit(0);
        }

        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                Application.Quit(1);
        }

        private static void Fail(string message)
        {
            Debug.LogError("RISKY_DELIVERY_SMOKE_FAILED: " + message);
            Application.Quit(1);
        }
    }
}
