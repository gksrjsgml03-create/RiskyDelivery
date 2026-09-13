using System;
using System.IO;
using UnityEngine;

namespace RiskyDelivery
{
    public sealed class DeliveryProgress
    {
        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public int[] stars;
            public float[] seconds;
            public bool soundEnabled;
        }

        private sealed class NewerSaveException : Exception { }
        private readonly string path;
        private readonly int[] stars = new int[ChapterCatalog.Count];
        private readonly float[] seconds = new float[ChapterCatalog.Count];
        private bool writable = true;
        public string Notice { get; private set; } = "";
        public bool SoundEnabled { get; private set; } = true;
        public int CompletedCount { get { int total = 0; foreach (int value in stars) if (value > 0) total++; return total; } }
        public int TotalStars { get { int total = 0; foreach (int value in stars) total += value; return total; } }
        public bool IsComplete => CompletedCount == ChapterCatalog.Count;
        public int NextChapter { get { for (int i = 0; i < stars.Length; i++) if (stars[i] == 0) return i + 1; return 1; } }
        public int BestStars(int chapter) => chapter >= 1 && chapter <= stars.Length ? stars[chapter - 1] : 0;
        public float BestSeconds(int chapter) => chapter >= 1 && chapter <= seconds.Length ? seconds[chapter - 1] : 0;

        // A null path is an isolated in-memory profile for automation and previews.
        public DeliveryProgress(string path)
        {
            this.path = path;
            if (path == null) return;
            if (TryLoad(path)) return;
            if (!writable) return;
            if (TryLoad(path + ".bak")) Notice = "Recovered your previous saved records.";
        }

        private bool TryLoad(string source)
        {
            if (!File.Exists(source)) return false;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(source));
                if (data != null && data.version > 2) throw new NewerSaveException();
                if (data == null || data.version < 1 || data.stars == null || data.seconds == null) throw new InvalidDataException();
                SoundEnabled = data.version == 1 || data.soundEnabled;
                for (int i = 0; i < stars.Length; i++)
                {
                    if (i >= data.stars.Length || i >= data.seconds.Length || data.stars[i] < 1 || data.stars[i] > 3 || !ValidSeconds(data.seconds[i])) continue;
                    stars[i] = data.stars[i];
                    seconds[i] = data.seconds[i];
                }
                return true;
            }
            catch (NewerSaveException)
            {
                writable = false;
                Notice = "A newer save was found. Playing without changing it.";
                return false;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Notice = "Saved records could not be read. Starting a fresh session.";
                return false;
            }
        }

        private static bool ValidSeconds(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);

        public void SetSoundEnabled(bool value)
        {
            if (SoundEnabled == value) return;
            SoundEnabled = value;
            Save();
        }

        public bool Record(int chapter, int rating, float time)
        {
            if (chapter < 1 || chapter > stars.Length || rating < 1 || rating > 3 || !ValidSeconds(time)) return false;
            int index = chapter - 1;
            bool better = rating > stars[index] || seconds[index] == 0 || time < seconds[index];
            if (!better) return false;
            stars[index] = Mathf.Max(stars[index], rating);
            seconds[index] = seconds[index] == 0 ? time : Mathf.Min(seconds[index], time);
            Save();
            return true;
        }

        private void Save()
        {
            if (path == null || !writable) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var data = new SaveData { version = 2, stars = stars, seconds = seconds, soundEnabled = SoundEnabled };
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                Notice = "";
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Notice = "Unable to save to disk. Records are kept for this session.";
            }
        }
    }
}
