using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// This class contains many automated build processes functions. They can only be run with UnityPro.
public class BuildCommands : MonoBehaviour
{
	[MenuItem("Build/All Platforms")]
	public static void Build_All(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.WebPlayer, BuildOptions.None);
		Build_Internal(AllScenes, ProjectName, BuildTarget.Android, BuildOptions.None);
		if (Application.platform == RuntimePlatform.WindowsEditor)
			Build_Internal(AllScenes, ProjectName, BuildTarget.StandaloneWindows, BuildOptions.None);
		if (Application.platform == RuntimePlatform.OSXEditor)
		{
			Build_Internal(AllScenes, ProjectName, BuildTarget.iOS, BuildOptions.None);
			Build_Internal(AllScenes, ProjectName, BuildTarget.StandaloneOSXIntel, BuildOptions.None);
		}
	}

	[MenuItem("Build/Android")]
	public static void Build_Android(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.Android, BuildOptions.None);
	}
	
	[MenuItem("Build/Android And Run")]
	public static void Build_Android_And_Run(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.Android, BuildOptions.AutoRunPlayer);
	}
	
	[MenuItem("Build/Android And Profile")]
	public static void Build_Android_And_Profile(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.Android, BuildOptions.Development | BuildOptions.AutoRunPlayer | BuildOptions.ConnectWithProfiler);
	}
	
	[MenuItem("Build/iPhone")]
	public static void Build_iPhone(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.iOS, BuildOptions.None);
	}
	
	[MenuItem("Build/iPhone and Run")]
	public static void Build_iPhone_And_Run(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.iOS, BuildOptions.AutoRunPlayer);
	}
	
	[MenuItem("Build/WebPlayer")]
	public static void Build_Web(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.WebPlayer, BuildOptions.None);
	}
	
	[MenuItem("Build/WebPlayer and Run")]
	public static void Build_Web_And_Run(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.WebPlayer, BuildOptions.AutoRunPlayer);
	}
	
	[MenuItem("Build/Windows and Run")]
	public static void Build_Windows64_And_Run(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.StandaloneWindows, BuildOptions.AutoRunPlayer);
	}
	
	[MenuItem("Build/OSXIntel and Run")]
	public static void Build_OSX_And_Run(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.StandaloneOSXIntel, BuildOptions.AutoRunPlayer);
	}
	
	[MenuItem("Build/Windows and Profile")]
	public static void Build_Windows_And_Profile(MenuCommand command)
	{
		Build_Internal(AllScenes, ProjectName, BuildTarget.StandaloneWindows, BuildOptions.Development | BuildOptions.AutoRunPlayer | BuildOptions.ConnectWithProfiler);
	}
	
	public static string[] AllScenes
	{
		get
		{
			List<string> list = new List<string>();
			foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
				list.Add(s.path);
			return list.ToArray();
		}
	}
	
	public static string ProjectName
	{
		get { return "ChutesAndLadders"; }
	}

	public static void Build_Internal(string[] scenes, string buildName, BuildTarget buildTarget, BuildOptions options)
	{
		string sceneName = buildName; // Path.GetFileNameWithoutExtension(scenePath);
		string root = Application.dataPath;
		string location = root + "/../Build/" + sceneName + "/" + buildTarget.ToString();
		if (Directory.Exists(location))
		{
			FileUtil.DeleteFileOrDirectory(location);
			if (Directory.Exists(location))
			{
				Debug.LogError("Abort. Old build could not be deleted: " + location);
				return;
			}
		}
		Directory.CreateDirectory(location);
		if (!Directory.Exists(location))
		{
			Debug.LogError("Abort. Directory could not be created: " + location);
			return;
		}
		string locationPathName;
		bool isWindows = buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64;
		if (isWindows)
		{
			locationPathName = location + "/" + sceneName + ".exe";
		}
		else if (buildTarget == BuildTarget.StandaloneOSXIntel)
		{
			locationPathName = location + "/" + sceneName + ".app";
		}
		else if (buildTarget == BuildTarget.WebPlayer || buildTarget == BuildTarget.WebPlayerStreamed)
		{
			locationPathName = location + "/" + sceneName;
		}
		else if (buildTarget == BuildTarget.Android)
		{
			locationPathName = location + "/" + sceneName + ".apk";
		}
		else
		{
			locationPathName = location;
		}
		BuildPipeline.BuildPlayer(scenes, locationPathName, buildTarget, options);
		
		// copy the runtime files (depending on the platform, on the corrsponding target directory)
		string targetDir = null;
		if (isWindows)
			targetDir = location + "/" + sceneName + "_Data/Assets";
		if (buildTarget == BuildTarget.WebPlayer)
			targetDir = location + "/" + sceneName + "/";
		
		if (!string.IsNullOrEmpty(targetDir))
		{
			string runtimeDir = root + "/Runtime/" + sceneName;
			if (Directory.Exists(runtimeDir))
			{
				Directory.CreateDirectory(targetDir);
				foreach (string path in Directory.GetFiles(runtimeDir))
				{
					if (path.EndsWith(".meta"))
						continue;
					int a = path.IndexOf(runtimeDir) + runtimeDir.Length;
					string pathRight = path.Substring(a);
					string targetPath = targetDir + pathRight;
					//Debug.Log ("Copying " + path + " to " + targetPath);
					string targetPathDir = Path.GetDirectoryName(targetPath);
					if (!Directory.Exists(targetPathDir))
						Directory.CreateDirectory(targetPathDir);
					FileUtil.CopyFileOrDirectory(path, targetPath);
				}
			}
		}
		
		// auto copy to shared dir
		if (buildTarget == BuildTarget.Android)
		{
			//Debug.Log(System.Environment.MachineName);
			if (System.Environment.MachineName == "AGUSTIN-PC")
			{
				AssetDatabase.Refresh();
				string fullPath = System.IO.Path.GetFullPath(locationPathName);
				string fileName = System.IO.Path.GetFileName(locationPathName);
				string targetPath = "C:/Users/Agustin/Dropbox/ChutesAndLadders/" + fileName;
				if (System.IO.File.Exists(fullPath))
				{
					System.IO.File.Copy(fullPath, targetPath, true);
				}
			}
		}
		
		Debug.Log("Build completed: " + sceneName + " " + buildTarget);
	}
}
