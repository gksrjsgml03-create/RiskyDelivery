using System;
using System.Collections;
using UnityEngine;

namespace RiskyDelivery
{
    public static class MovementSmokeTests
    {
        public static IEnumerator Run(DeliveryGame game, Action<bool, string> check)
        {
            game.StartChapter(1);
            game.SetControls(Vector2.up, false);
            yield return new WaitForSeconds(1.5f);
            float walk = game.Cart.linearVelocity.z;
            check(walk > 3.5f && walk < 4.2f && !game.IsSprinting, "Default movement is walking");
            game.SetControls(Vector2.up, false, true);
            yield return new WaitForSeconds(1.2f);
            check(game.IsSprinting && game.Cart.linearVelocity.z > walk * 1.8f, "Holding sprint increases actual movement speed");
            game.SetControls(Vector2.up, false, false);
            check(!game.IsSprinting, "Releasing sprint immediately clears running intent");
            yield return new WaitForSeconds(1);
            check(game.Cart.linearVelocity.z < 4.3f, "Releasing sprint returns to walking speed");
            game.SetControls(Vector2.up, true, true);
            yield return new WaitForSeconds(0.6f);
            check(!game.IsSprinting && game.Cart.linearVelocity.z < 2.2f, "Careful movement takes priority over sprint");
            game.Restart();
            game.SetControls(Vector2.zero, false, true);
            yield return new WaitForSeconds(0.2f);
            check(!game.IsSprinting && game.Cart.linearVelocity.magnitude < 0.1f, "Shift alone does not move the courier");
            game.SetControls(Vector2.up, false, true);
            yield return null;
            game.Pause();
            game.Resume();
            check(!game.IsSprinting, "Pause clears held sprint intent");
            game.Restart();
            check(!game.IsSprinting, "Retry clears sprint intent");
            Debug.Log("RISKY_CHECK_OK: walking, held sprint, release, careful movement priority and sprint reset");
        }
    }
}
