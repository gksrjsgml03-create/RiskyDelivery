using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace RiskyDelivery
{
    // Explicit screenshot mode avoids OS-window capture races and never runs in normal play.
    public sealed class DeliveryPreview : MonoBehaviour
    {
        private string outputDirectory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-risky-preview");
            if (flag < 0) return;
            if (flag + 1 >= args.Length) { Debug.LogError("Preview requires an output directory"); Application.Quit(1); return; }
            new GameObject("Preview capture").AddComponent<DeliveryPreview>().outputDirectory = args[flag + 1];
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            var game = FindFirstObjectByType<DeliveryGame>();
            game.Automated = true;
            while (!SplashScreen.isFinished) yield return null;
            Directory.CreateDirectory(outputDirectory);
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                game.StartChapter(chapter);
                if (chapter == 3) game.Cart.position = new Vector3(3.8f, 0.4f, -4);
                yield return new WaitForSeconds(0.8f);
                yield return Capture("chapter" + chapter);
            }
            game.RegisterImpact(12);
            yield return new WaitForSeconds(0.5f);
            game.RegisterImpact(12);
            yield return Capture("failed");
            game.Restart();
            game.Cart.position = game.Destination + Vector3.up * 0.4f;
            yield return new WaitForSeconds(0.8f);
            yield return Capture("delivered");
            Debug.Log("RISKY_DELIVERY_PREVIEW_OK");
            Application.Quit(0);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null) throw new InvalidOperationException("Preview capture needs a visible graphics window; do not use hidden or nographics mode.");
            File.WriteAllBytes(Path.Combine(outputDirectory, name + ".png"), screenshot.EncodeToPNG());
            Destroy(screenshot);
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Application.Quit(1);
        }
    }
}
