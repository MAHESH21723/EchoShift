using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace EchoShiftBootstrap
{
    // Compiled separately so UPM can repair template packages even when they fail to compile.
    // The resulting DLL is stored under Assets/Editor and never ships in the game.
    public static class PackageRepair
    {
        static AddAndRemoveRequest request;
        static double deadline;

        public static void Install()
        {
            try
            {
                var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                var input = packages.FirstOrDefault(p => p.name == "com.unity.inputsystem");
                bool hasVersionControl = packages.Any(p => p.name == "com.unity.collab-proxy" && p.isDirectDependency);
                if (input != null && input.version == "1.18.0" && !hasVersionControl)
                {
                    Debug.Log("[EchoShift Packages] Compatible packages already installed.");
                    EditorApplication.Exit(0);
                    return;
                }
                Debug.Log("[EchoShift Packages] Installing com.unity.inputsystem@1.18.0" + (hasVersionControl ? "; removing com.unity.collab-proxy." : "."));
                request = Client.AddAndRemove(new[] { "com.unity.inputsystem@1.18.0" }, hasVersionControl ? new[] { "com.unity.collab-proxy" } : new string[0]);
                deadline = EditorApplication.timeSinceStartup + 600;
                EditorApplication.update += Poll;
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        static void Poll()
        {
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[EchoShift Packages] Package resolution timed out.");
                    EditorApplication.Exit(2);
                }
                return;
            }
            EditorApplication.update -= Poll;
            if (request.Status != StatusCode.Success)
            {
                Debug.LogError("[EchoShift Packages] " + request.Error?.message);
                EditorApplication.Exit(1);
                return;
            }
            string report = string.Join("\n", request.Result.Select(p => p.name + "@" + p.version));
            Directory.CreateDirectory("BuildReports");
            File.WriteAllText("BuildReports/package-repair.txt", report);
            Debug.Log("[EchoShift Packages] Resolution succeeded. Restarting for a clean game compilation.");
            EditorApplication.Exit(0);
        }
    }
}
