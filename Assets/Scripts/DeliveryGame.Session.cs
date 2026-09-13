using UnityEngine;

namespace RiskyDelivery
{
    public sealed partial class DeliveryGame
    {
        public enum ViewMode { Driving, Title, Paused }
        public ViewMode View { get; private set; }
        private float runningTimeScale = 1;

        public void ShowTitle() => Freeze(ViewMode.Title);

        public void Pause()
        {
            if (View == ViewMode.Driving && State == RunState.Playing) Freeze(ViewMode.Paused);
        }

        private void Freeze(ViewMode view)
        {
            if (Time.timeScale > 0) runningTimeScale = Time.timeScale;
            View = view;
            input = Vector2.zero;
            braking = true;
            Time.timeScale = 0;
        }

        public void Resume()
        {
            if (View == ViewMode.Paused) RestorePlayTime();
        }

        private void RestorePlayTime()
        {
            if (Time.timeScale == 0) Time.timeScale = runningTimeScale;
            View = ViewMode.Driving;
        }

        private void ReadPlayerInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (View == ViewMode.Paused) Resume();
                else if (View == ViewMode.Driving && State == RunState.Playing) Pause();
                else if (View == ViewMode.Driving) ShowTitle();
            }
            if (View == ViewMode.Paused)
            {
                if (Input.GetKeyDown(KeyCode.R)) Restart();
                return;
            }
            for (int chapter = 1; chapter <= ChapterCount; chapter++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + chapter - 1))) StartChapter(chapter);
            if (View == ViewMode.Title)
            {
                if (Input.GetKeyDown(KeyCode.Return)) StartChapter(1);
                return;
            }
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.Return)) TryNextChapter();
            SetControls(new Vector2(
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)), Input.GetKey(KeyCode.Space));
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !Automated) Pause();
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0) Time.timeScale = 1;
        }
    }
}
