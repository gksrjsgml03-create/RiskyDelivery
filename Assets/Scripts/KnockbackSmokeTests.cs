using System;
using System.Collections;
using UnityEngine;

namespace RiskyDelivery
{
    public static class KnockbackSmokeTests
    {
        public static IEnumerator Run(DeliveryGame game, Action<bool, string> check)
        {
            game.StartChapter(2);
            game.SetControls(Vector2.up, false);
            yield return Until(() => game.IsRecoiling, check, "Walking collision produces knockback");
            float slowRebound = -game.Cart.linearVelocity.z;
            int slowDamage = 100 - game.CargoHealth;
            float hitZ = game.Cart.position.z;
            check(slowRebound > 1.5f, "Walking collision throws courier away from the wall");
            yield return new WaitForSeconds(0.12f);
            check(game.Cart.position.z < hitZ - 0.12f && game.Cart.linearVelocity.z < 0, "Held forward input cannot cancel the initial recoil");

            game.Restart();
            game.SetControls(Vector2.up, false, true);
            yield return Until(() => game.IsRecoiling, check, "Running collision produces knockback");
            check(-game.Cart.linearVelocity.z > slowRebound * 1.5f && 100 - game.CargoHealth > slowDamage, "Running collision has stronger recoil and damage than walking");
            yield return null;
            game.Pause();
            Vector3 pausedPosition = game.Cart.position, pausedVelocity = game.Cart.linearVelocity;
            yield return new WaitForSecondsRealtime(0.15f);
            check(game.IsRecoiling && game.Cart.position == pausedPosition && game.Cart.linearVelocity == pausedVelocity, "Pause freezes knockback physics and recovery");
            game.Resume();
            game.SetControls(Vector2.zero, true);
            yield return new WaitForSeconds(0.7f);
            check(!game.IsRecoiling && game.State == DeliveryGame.RunState.Playing, "Courier recovers after the recoil interval");
            game.RegisterImpact(12, Vector3.back);
            hitZ = game.Cart.position.z;
            check(game.State == DeliveryGame.RunState.Failed && game.IsRecoiling, "Fatal impact also starts recoil");
            yield return new WaitForSeconds(0.15f);
            check(game.Cart.position.z < hitZ - 0.3f, "Fatal recoil continues after failure instead of freezing instantly");
            game.Restart();
            check(!game.IsRecoiling && game.Cart.linearVelocity == Vector3.zero, "Restart clears recoil and momentum");
            game.SetControls(Vector2.up, true);
            yield return Until(() => game.IsRecoiling, check, "Careful walking still produces a small bump");
            check(game.CargoHealth == 100, "Small bump recoil preserves cargo health");
            game.StartChapter(1);
            check(!game.IsRecoiling, "Chapter selection clears recoil");
            Debug.Log("RISKY_CHECK_OK: actual collision knockback, speed scaling, input lockout, paused recovery, fatal recoil and reset");
        }

        private static IEnumerator Until(Func<bool> ready, Action<bool, string> check, string label)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (!ready())
            {
                check(Time.realtimeSinceStartup < deadline, label);
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
