using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiskyDelivery.Editor
{
    public static class ProjectTools
    {
        [MenuItem("Risky Delivery/Prepare Project")]
        public static void Prepare()
        {
            PlayerSettings.companyName = "RiskyDelivery";
            PlayerSettings.productName = "Risky Delivery";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (!System.IO.File.Exists("Assets/Scenes/Delivery.unity"))
            {
                System.IO.Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Risky Delivery").AddComponent<DeliveryGame>();
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/Delivery.unity");
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Delivery.unity", true) };
            System.IO.Directory.CreateDirectory("Assets/Resources");
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/WorldMaterial.mat") == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/Resources/WorldMaterial.mat");
            AssetDatabase.SaveAssets();
            Debug.Log("RISKY_DELIVERY_PREPARE_OK");
        }

        [MenuItem("Risky Delivery/Build Windows")]
        public static void BuildWindows()
        {
            Build("Builds/Windows/RiskyDelivery.exe", BuildOptions.Development);
        }

        [MenuItem("Risky Delivery/Build Release Windows")]
        public static void BuildReleaseWindows()
        {
            Build("Builds/Release/RiskyDelivery.exe", BuildOptions.None);
        }

        private static void Build(string output, BuildOptions options)
        {
            Prepare();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Delivery.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("RISKY_DELIVERY_BUILD_OK");
        }
    }
}
