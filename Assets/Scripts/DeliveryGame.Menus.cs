using UnityEngine;

namespace RiskyDelivery
{
    public sealed partial class DeliveryGame
    {
        private GUIStyle menuButtonStyle, heroStyle;
        private static readonly Color Teal = new Color(0.12f, 0.83f, 0.7f);

        private void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private bool MenuButton(Rect rect, string label, bool primary = false)
        {
            if (menuButtonStyle == null)
            {
                menuButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 19, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 12, 8, 8) };
                menuButtonStyle.normal.background = menuButtonStyle.hover.background = menuButtonStyle.active.background = Texture2D.whiteTexture;
                menuButtonStyle.normal.textColor = menuButtonStyle.hover.textColor = menuButtonStyle.active.textColor = Color.white;
            }
            GUI.backgroundColor = primary ? new Color(0.08f, 0.48f, 0.4f) : new Color(0.15f, 0.23f, 0.32f);
            bool clicked = GUI.Button(rect, label, menuButtonStyle);
            if (clicked) Sound.Click();
            GUI.backgroundColor = Color.white;
            return clicked;
        }

        private void DrawTitle()
        {
            if (heroStyle == null) heroStyle = new GUIStyle(titleStyle) { fontSize = 46 };
            Fill(new Rect(48, 80, 6, 520), Teal);
            GUI.Label(new Rect(76, 90, 470, 120), "RISKY\nDELIVERY", heroStyle);
            GUI.Label(new Rect(76, 222, 450, 54), "Five deliveries. One fragile parcel.\nGet it there in one piece.", textStyle);
            GUI.Label(new Rect(76, 278, 450, 27), $"DELIVERIES {Progress.CompletedCount}/{ChapterCount}     STARS {Progress.TotalStars}/{ChapterCount * 3}", smallStyle);
            string startLabel = Progress.IsComplete ? "REPLAY FIRST DELIVERY  [ENTER]" : Progress.CompletedCount > 0 ? "CONTINUE DELIVERY  [ENTER]" : "START DELIVERY  [ENTER]";
            if (MenuButton(new Rect(76, 306, 430, 60), startLabel, true)) StartChapter(Progress.NextChapter);
            if (Progress.IsComplete)
            {
                if (MenuButton(new Rect(76, 379, 430, 42), "VIEW CAMPAIGN RESULTS")) ShowCampaignSummary();
            }
            else GUI.Label(new Rect(76, 380, 430, 48), Progress.Notice.Length > 0 ? Progress.Notice : "Your best results are saved automatically.", smallStyle);
            GUI.Label(new Rect(76, 442, 440, 90), "WASD / ARROWS   Move\nSPACE   Slow down / steady your grip\nR   Retry your delivery\nESC   Pause / return to menu", smallStyle);
            if (MenuButton(new Rect(76, 548, 206, 48), Progress.SoundEnabled ? "SOUND ON  [M]" : "SOUND OFF  [M]")) ToggleSound();
            if (MenuButton(new Rect(300, 548, 206, 48), "QUIT GAME")) Application.Quit();
            GUI.Label(new Rect(590, 90, 450, 48), "CHOOSE A DELIVERY", titleStyle);
            GUI.Label(new Rect(590, 142, 450, 30), "All routes are available for practice.", smallStyle);
            for (int chapter = 1; chapter <= ChapterCount; chapter++)
            {
                string record = Progress.BestStars(chapter) == 0 ? "Not delivered yet" : $"{Progress.BestStars(chapter)}/3 stars   /   Best: {Progress.BestSeconds(chapter):0.0}s";
                if (MenuButton(new Rect(590, 194 + (chapter - 1) * 76, 440, 60), $"{chapter:00}   {ChapterLabel(chapter)}\n{record}")) StartChapter(chapter);
            }
            GUI.Label(new Rect(76, 637, 950, 28), "Stop in the mint delivery zone to finish. A slower arrival beats a broken parcel.", smallStyle);
            if (Progress.Notice.Length > 0) GUI.Label(new Rect(76, 604, 950, 28), Progress.Notice, smallStyle);
        }

        private void DrawPause()
        {
            Fill(new Rect(290, 120, 520, 470), new Color(0.06f, 0.1f, 0.16f));
            Fill(new Rect(290, 120, 520, 5), Teal);
            GUI.Label(new Rect(320, 149, 460, 45), "DELIVERY PAUSED", titleStyle);
            GUI.Label(new Rect(320, 206, 460, 60), $"{ChapterName}  /  {Elapsed:0.0}s\nTake your time. Your delivery is safe.", textStyle);
            if (MenuButton(new Rect(320, 291, 460, 52), "RESUME  [ESC]", true)) Resume();
            if (MenuButton(new Rect(320, 356, 460, 52), "RESTART DELIVERY  [R]")) Restart();
            if (MenuButton(new Rect(320, 421, 460, 52), "MAIN MENU")) ShowTitle();
            if (MenuButton(new Rect(320, 486, 220, 52), Progress.SoundEnabled ? "SOUND ON  [M]" : "SOUND OFF  [M]")) ToggleSound();
            if (MenuButton(new Rect(558, 486, 222, 52), "QUIT GAME")) Application.Quit();
        }

        private void DrawCampaignSummary()
        {
            Fill(new Rect(80, 55, 940, 565), new Color(0.04f, 0.08f, 0.13f));
            Fill(new Rect(80, 55, 940, 6), Teal);
            GUI.Label(new Rect(120, 84, 850, 48), "ALL DELIVERIES COMPLETE!", titleStyle);
            GUI.Label(new Rect(120, 143, 850, 48), $"Five routes. Every parcel delivered.   /   {Progress.TotalStars}/{ChapterCount * 3} stars", textStyle);
            for (int chapter = 1; chapter <= ChapterCount; chapter++)
            {
                float y = 213 + (chapter - 1) * 50;
                GUI.Label(new Rect(120, y, 440, 34), $"{chapter:00}   {ChapterLabel(chapter)}", textStyle);
                GUI.Label(new Rect(590, y, 370, 34), $"{Progress.BestStars(chapter)}/3 stars   /   Best {Progress.BestSeconds(chapter):0.0}s", textStyle);
            }
            GUI.Label(new Rect(120, 477, 840, 30), Progress.TotalStars == ChapterCount * 3 ? "Perfect deliveries! Replay any route to improve your time." : "Replay your routes to earn all 15 stars.", smallStyle);
            if (Progress.Notice.Length > 0) GUI.Label(new Rect(120, 505, 840, 28), Progress.Notice, smallStyle);
            if (MenuButton(new Rect(120, 535, 400, 52), "MAIN MENU  [ENTER]", true)) ShowTitle();
            if (MenuButton(new Rect(550, 535, 430, 52), "REPLAY FIRST DELIVERY")) StartChapter(1);
        }
    }
}
