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
            GUI.backgroundColor = Color.white;
            return clicked;
        }

        private void DrawTitle()
        {
            if (heroStyle == null) heroStyle = new GUIStyle(titleStyle) { fontSize = 46 };
            Fill(new Rect(48, 80, 6, 520), Teal);
            GUI.Label(new Rect(76, 90, 470, 120), "RISKY\nDELIVERY", heroStyle);
            GUI.Label(new Rect(76, 222, 450, 54), "Five deliveries. One fragile parcel.\nGet it there in one piece.", textStyle);
            if (MenuButton(new Rect(76, 306, 430, 60), "START DELIVERY  [ENTER]", true)) StartChapter(1);
            GUI.Label(new Rect(76, 400, 440, 120), "WASD / ARROWS   Move\nSPACE   Brake / drive slowly\nR   Retry your delivery\nESC   Pause / return to menu", textStyle);
            if (MenuButton(new Rect(76, 548, 430, 48), "QUIT GAME")) Application.Quit();
            GUI.Label(new Rect(590, 90, 450, 48), "CHOOSE A DELIVERY", titleStyle);
            GUI.Label(new Rect(590, 142, 450, 30), "All routes are available for practice.", smallStyle);
            for (int chapter = 1; chapter <= ChapterCount; chapter++)
                if (MenuButton(new Rect(590, 194 + (chapter - 1) * 76, 440, 60), $"{chapter:00}   {ChapterLabel(chapter)}")) StartChapter(chapter);
            GUI.Label(new Rect(76, 637, 950, 28), "Stop in the mint delivery zone to finish. A slower arrival beats a broken parcel.", smallStyle);
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
            if (MenuButton(new Rect(320, 486, 460, 52), "QUIT GAME")) Application.Quit();
        }
    }
}
