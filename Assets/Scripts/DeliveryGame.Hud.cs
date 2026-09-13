using UnityEngine;

namespace RiskyDelivery
{
    public sealed partial class DeliveryGame
    {
        private GUIStyle titleStyle, textStyle, smallStyle;
        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };
                smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            }
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 700f);
            GUI.matrix = Matrix4x4.identity;
            if (View != ViewMode.Driving)
                Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.05f, 0.085f, View == ViewMode.Title ? 0.96f : 0.8f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1100 * scale) / 2, (Screen.height - 700 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            if (View == ViewMode.Title) { DrawTitle(); return; }
            if (View == ViewMode.Paused) { DrawPause(); return; }
            GUI.Box(new Rect(18, 18, 420, 146), GUIContent.none);
            GUI.Label(new Rect(34, 28, 400, 42), "RISKY DELIVERY", titleStyle);
            GUI.Label(new Rect(34, 72, 400, 30), $"{ChapterName}   /   {Elapsed:0.0}s", textStyle);
            GUI.Label(new Rect(34, 108, 400, 48), ChapterCatalog.Get(Chapter).Instructions, smallStyle);
            if (GUI.Button(new Rect(18, 176, 150, 32), State == RunState.Playing ? "PAUSE  [ESC]" : "MENU  [ESC]"))
            {
                if (State == RunState.Playing) Pause(); else ShowTitle();
            }
            GUI.Box(new Rect(18, 610, 660, 68), GUIContent.none);
            GUI.Label(new Rect(34, 620, 630, 28), "WASD / ARROWS  Move     SPACE  Brake     R  Restart", textStyle);
            GUI.Label(new Rect(34, 651, 620, 24), "Release movement keys to stop. Keep your parcel steady!", smallStyle);
            GUI.Box(new Rect(774, 18, 308, 118), GUIContent.none);
            GUI.Label(new Rect(792, 30, 275, 30), $"CARGO CONDITION   {CargoHealth}%", textStyle);
            Color healthColor = CargoHealth > 50 ? new Color(0.2f, 0.9f, 0.7f) : new Color(1, 0.35f, 0.2f);
            GUI.color = new Color(0.16f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(792, 70, 270, 16), Texture2D.whiteTexture);
            GUI.color = healthColor;
            GUI.DrawTexture(new Rect(792, 70, 270 * CargoHealth / 100f, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(792, 98, 280, 25), "Hard impacts damage your delivery.", smallStyle);
            if (impactFlash > 0 && State == RunState.Playing)
                GUI.Label(new Rect(430, 178, 340, 32), $"IMPACT!  -{lastDamage}% CARGO", textStyle);
            if (Chapter == 3)
            {
                GUI.Box(new Rect(774, 150, 308, 80), GUIContent.none);
                GUI.color = OnWetRoad ? new Color(0.5f, 0.85f, 1) : Color.white;
                GUI.Label(new Rect(792, 160, 280, 30), OnWetRoad ? "LOW GRIP / BRAKE EARLY" : "DRY ROAD / NORMAL GRIP", textStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(792, 196, 275, 25), $"Speed: {new Vector2(Cart.linearVelocity.x, Cart.linearVelocity.z).magnitude:0.0} m/s", smallStyle);
            }
            if (Chapter == 4)
            {
                GUI.Box(new Rect(774, 150, 308, 138), GUIContent.none);
                GUI.color = Balance.Risk > 0.55f ? new Color(1, 0.5f, 0.2f) : healthColor;
                GUI.Label(new Rect(792, 160, 280, 30), Balance.HasFallen ? "CARGO LOST" : Balance.Risk > 0.55f ? "LOAD SLIDING! SLOW DOWN" : "LOAD SECURE", textStyle);
                GUI.DrawTexture(new Rect(792, 198, 270 * Balance.Risk, 12), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(792, 224, 275, 25), $"Load shift: {Balance.Risk * 100:0}%   /   Speed: {new Vector2(Cart.linearVelocity.x, Cart.linearVelocity.z).magnitude:0.0} m/s", smallStyle);
                float grade = HillRoad.Grade(Cart.position.z);
                GUI.Label(new Rect(792, 254, 275, 25), grade > 0 ? "UPHILL / KEEP A STEADY PACE" : grade < 0 ? "DOWNHILL / CONTROL YOUR SPEED" : "LEVEL ROAD / TURN GENTLY", smallStyle);
            }
            if (Chapter == 5)
            {
                GUI.Box(new Rect(774, 150, 308, 132), GUIContent.none);
                int crossing = Night.NextCrossing(Cart.position.z);
                GUI.Label(new Rect(792, 160, 275, 26), crossing < 0 ? "ALL CROSSINGS CLEAR" : $"CROSSING {crossing + 1} / 2", textStyle);
                if (crossing >= 0)
                {
                    var signal = Night.GetSignal(crossing);
                    GUI.color = signal == NightTraffic.Signal.Green ? new Color(0.2f, 0.9f, 0.7f) : signal == NightTraffic.Signal.Amber ? new Color(1, 0.7f, 0.2f) : new Color(1, 0.35f, 0.25f);
                    GUI.Label(new Rect(792, 196, 275, 26), signal == NightTraffic.Signal.Green ? $"GREEN / {Night.GreenRemaining(crossing):0.0}s LEFT" : signal == NightTraffic.Signal.Amber ? "AMBER / DO NOT ENTER" : "CROSS TRAFFIC / WAIT", textStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(792, 232, 275, 30), signal == NightTraffic.Signal.Green ? Night.GreenRemaining(crossing) >= 4.2f ? "Fresh green: cross at a steady pace." : "Short green: wait for the next cycle." : $"Next green in {Night.UntilGreen(crossing):0.0}s", smallStyle);
                }
                else GUI.Label(new Rect(792, 204, 275, 48), "Head for the mint delivery zone.\nStop inside it to finish.", smallStyle);
            }
            for (int chapter = 1; chapter <= ChapterCount; chapter++)
                if (GUI.Button(new Rect(850, 648 - (ChapterCount - chapter) * 38, 232, 30), $"{chapter}  /  {ChapterLabel(chapter)}")) StartChapter(chapter);
            if (State != RunState.Playing)
            {
                GUI.Box(new Rect(310, 225, 480, 270), GUIContent.none);
                GUI.Label(new Rect(334, 242, 440, 45), State == RunState.Delivered ? "DELIVERY COMPLETE!" : Balance.HasFallen ? "PARCEL FELL OFF!" : "PARCEL BROKEN!", titleStyle);
                GUI.Label(new Rect(334, 296, 440, 30), State == RunState.Delivered ? $"{Elapsed:0.0}s   /   Cargo {CargoHealth}%   /   Rating {Rating}/3" : Balance.HasFallen ? "Hold SPACE from the start. Turn gently." : Chapter == 5 ? "Wait behind the line for a fresh green." : "Slow down before hitting a barrier.", textStyle);
                if (State == RunState.Delivered && Chapter < ChapterCount)
                {
                    if (GUI.Button(new Rect(334, 350, 432, 48), $"CHAPTER {Chapter + 1}: {ChapterLabel(Chapter + 1)}  [ENTER]")) TryNextChapter();
                }
                else
                    GUI.Label(new Rect(334, 351, 432, 40), State == RunState.Delivered ? "Try again for a faster, safer delivery." : "Your cargo is lost. Try a safer route.", smallStyle);
                if (GUI.Button(new Rect(334, 418, 432, 48), "RETRY CHAPTER  [R]")) Restart();
            }
        }
    }
}
