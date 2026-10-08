using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace EchoShift.Editor
{
    [InitializeOnLoad]
    public static class EchoBuild
    {
        const string ScenePath="Assets/EchoShift/Scenes/EchoShift.unity";
        static Action afterResources;
        static bool exitWhenFinished, resourcesImported;
        static double resourceDeadline;
        static EchoBuild()
        {
            if(!Application.isBatchMode)EditorApplication.delayCall+=FirstOpen;
        }
        static void FirstOpen()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=FirstOpen; return; }
            if(!File.Exists(ScenePath)) Prepare();
        }
        [MenuItem("Echo Shift/1 - Prepare game")]
        public static void Prepare()
        {
            WithResources(PrepareCore,false);
        }
        static void WithResources(Action continuation,bool exit)
        {
            if(afterResources!=null)throw new InvalidOperationException("Echo Shift is already preparing resources.");
            afterResources=continuation; exitWhenFinished=exit;
            if(TMP_Settings.instance!=null) { FinishResources(); return; }
            resourcesImported=false; resourceDeadline=EditorApplication.timeSinceStartup+180;
            AssetDatabase.importPackageCompleted+=ResourcesImported;
            AssetDatabase.importPackageFailed+=ResourcesFailed;
            EditorApplication.update+=WaitForResources;
            Debug.Log("[EchoShift] Importing TMP Essentials; waiting for the asynchronous import to finish.");
            try { TMP_PackageResourceImporter.ImportResources(true,false,false); }
            catch(Exception error) { FailResources(error); }
        }
        static void ResourcesImported(string name)
        {
            if(name=="TMP Essential Resources")resourcesImported=true;
        }
        static void ResourcesFailed(string name,string error)
        {
            FailResources(new Exception("Resource import failed: "+name+" / "+error));
        }
        static void WaitForResources()
        {
            if(EditorApplication.timeSinceStartup>resourceDeadline) { FailResources(new TimeoutException("TMP Essentials import did not finish within 180 seconds.")); return; }
            if(resourcesImported&&!EditorApplication.isUpdating&&!EditorApplication.isCompiling&&TMP_Settings.instance!=null)FinishResources();
        }
        static void ClearResourceCallbacks()
        {
            EditorApplication.update-=WaitForResources;
            AssetDatabase.importPackageCompleted-=ResourcesImported;
            AssetDatabase.importPackageFailed-=ResourcesFailed;
        }
        static void FailResources(Exception error)
        {
            ClearResourceCallbacks(); afterResources=null;
            Debug.LogException(error);
            if(exitWhenFinished)EditorApplication.Exit(1);
        }
        static void FinishResources()
        {
            ClearResourceCallbacks(); var action=afterResources; afterResources=null;
            try
            {
                action();
                if(exitWhenFinished)EditorApplication.Exit(0);
            }
            catch(Exception error) { FailResources(error); }
        }
        static void PrepareCore()
        {
            Directory.CreateDirectory("Assets/EchoShift/Scenes"); Directory.CreateDirectory("Assets/EchoShift/Generated"); Directory.CreateDirectory("BuildReports");
            if(TMP_Settings.instance==null)throw new InvalidOperationException("TMP Essentials must finish importing before fonts are generated.");
            var normal=Font("Barlow-Regular","Barlow-Regular SDF"); var bold=Font("Barlow-SemiBold","Barlow-SemiBold SDF");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Main Camera"); cam.tag="MainCamera"; cam.AddComponent<Camera>(); cam.AddComponent<AudioListener>();
            var game=new GameObject("Echo Shift").AddComponent<EchoGame>(); game.regularFont=normal; game.boldFont=bold;
            EditorSceneManager.SaveScene(scene,ScenePath); EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="Echo Shift Studio"; PlayerSettings.productName="Echo Shift"; PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900; PlayerSettings.runInBackground=false;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.WebGL.template="PROJECT:EchoShift"; PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true; PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL,ManagedStrippingLevel.Low);
            QualitySettings.vSyncCount=0; QualitySettings.antiAliasing=0;
            // Runtime-created SpriteRenderers still need the sprite shader in the exported player.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders"); var shader=Shader.Find("Sprites/Default"); bool found=false;
            for(int i=0;i<shaders.arraySize;i++)found|=shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader;
            if(!found){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;graphics.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.SaveAssets(); File.WriteAllText("BuildReports/prepared.txt","Echo Shift scene and settings prepared with Unity "+Application.unityVersion);
            Debug.Log("[EchoShift] Scene, font atlases and Web settings prepared.");
        }
        static TMP_FontAsset Font(string filename,string assetName)
        {
            string path="Assets/EchoShift/Generated/"+assetName+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if(existing!=null)return existing;
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/EchoShift/Fonts/"+filename+".ttf");
            if(source==null)throw new Exception("Missing font: "+filename);
            var font=TMP_FontAsset.CreateFontAsset(source,64,7,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            font.name=assetName; string characters=""; for(int i=32;i<=126;i++)characters+=(char)i;
            if(!font.TryAddCharacters(characters,out string missing)) Debug.LogWarning("[EchoShift] Font missing: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font,path);
            font.material.name=assetName+" Material"; AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures) { texture.name=assetName+" Atlas"; AssetDatabase.AddObjectToAsset(texture,font); }
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssets(); return font;
        }
        [MenuItem("Echo Shift/2 - Validate puzzles")]
        public static void Validate()
        {
            Directory.CreateDirectory("BuildReports"); string report=EchoValidation.Run();
            File.WriteAllText("BuildReports/validation.txt",report); Debug.Log("[EchoShift] "+report);
        }
        [MenuItem("Echo Shift/3 - Build browser version")]
        public static void BuildWeb()
        {
            WithResources(BuildWebCore,Application.isBatchMode);
        }
        static void BuildWebCore()
        {
            PrepareCore(); Validate(); Directory.CreateDirectory("Builds/WebGL");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None });
            string summary="Result: "+result.summary.result+"\nBytes: "+result.summary.totalSize+"\nDuration: "+result.summary.totalTime+"\nUnity: "+Application.unityVersion;
            File.WriteAllText("BuildReports/build-summary.txt",summary); Debug.Log("[EchoShift] "+summary);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Echo Shift Web build failed. See BuildReports/build.log.");
        }
    }
}
