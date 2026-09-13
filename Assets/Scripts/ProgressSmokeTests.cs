using System;
using System.IO;
using UnityEngine;

namespace RiskyDelivery
{
    public static class ProgressSmokeTests
    {
        public static void Run(Action<bool, string> check)
        {
            // These artifacts stay inside the built player's data, never in the user's save folder.
            string root = Path.Combine(Application.dataPath, "TestArtifacts", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "progress.json");
            var progress = new DeliveryProgress(path);
            check(progress.CompletedCount == 0 && progress.NextChapter == 1, "New progress starts empty");
            check(!progress.Record(0, 3, 10) && !progress.Record(1, 4, 10) && !progress.Record(1, 3, float.NaN), "Invalid results cannot enter saved records");
            check(progress.Record(1, 2, 40), "First completed delivery saves");
            check(progress.Record(1, 3, 45), "Better stars preserve independent best time");
            check(!progress.Record(1, 1, 50), "Worse replay does not overwrite records");
            var loaded = new DeliveryProgress(path);
            check(loaded.BestStars(1) == 3 && loaded.BestSeconds(1) == 40 && loaded.NextChapter == 2, "Disk round-trip preserves stars, time and continuation");
            File.WriteAllText(path, "not valid json");
            var recovered = new DeliveryProgress(path);
            check(recovered.BestStars(1) == 2 && recovered.BestSeconds(1) == 40, "Corrupt primary save recovers last atomic backup");
            for (int chapter = 1; chapter <= ChapterCatalog.Count; chapter++) recovered.Record(chapter, 3, 30 + chapter);
            check(new DeliveryProgress(path).IsComplete && recovered.TotalStars == ChapterCatalog.Count * 3, "Complete campaign survives reload");

            string futurePath = Path.Combine(root, "future.json");
            string future = "{\"version\":99,\"stars\":[],\"seconds\":[]}";
            File.WriteAllText(futurePath, future);
            new DeliveryProgress(futurePath).Record(1, 3, 20);
            check(File.ReadAllText(futurePath) == future, "Newer save formats are not overwritten");
            string blockedDirectory = Path.Combine(root, "not-a-directory");
            File.WriteAllText(blockedDirectory, "block");
            var unavailable = new DeliveryProgress(Path.Combine(blockedDirectory, "progress.json"));
            unavailable.Record(1, 3, 20);
            check(unavailable.BestStars(1) == 3 && unavailable.Notice.Length > 0, "Disk failure retains session records without crashing");
            Debug.Log("RISKY_CHECK_OK: persistent records, independent personal bests, backup recovery, campaign completion, future saves and disk failure");
        }
    }
}
