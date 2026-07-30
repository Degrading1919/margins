using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Margins.Editor
{
    public static class WindowsPlayerBuild
    {
        private const string RelativeExecutablePath =
            "Builds/FirstStoreValidation/MarginsFirstStoreValidation.exe";

        public static void BuildFirstStoreWindowsPlayer()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "No enabled scenes are configured for the Windows player.");
            }

            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            string executablePath = Path.Combine(
                projectRoot,
                RelativeExecutablePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(executablePath) ?? projectRoot);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows player build failed: {report.summary.result}; " +
                    $"{report.summary.totalErrors} errors.");
            }

            Debug.Log(
                $"Windows player built at '{executablePath}' " +
                $"({report.summary.totalSize} bytes).", null);
        }
    }
}
