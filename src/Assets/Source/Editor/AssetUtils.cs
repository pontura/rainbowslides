using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// This class contains editor utilities to help in the development process.
public class AssetUtils : MonoBehaviour
{
	[MenuItem("Assets/Lower AudioClip Settings")]
	public static void LowerAudioSettings()
	{
		foreach (AudioClip clip in Selection.GetFiltered(typeof(AudioClip), SelectionMode.DeepAssets))
		{
			string path = AssetDatabase.GetAssetPath(clip);
			AudioImporter ai = AssetImporter.GetAtPath(path) as AudioImporter;
			if (ai)
			{
				//ai.format = AudioImporterFormat.Compressed;
				ai.forceToMono = true;
				//ai.threeD = false;
				//ai.compressionBitrate = 32000;
				AssetDatabase.WriteImportSettingsIfDirty(path);
			}
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	
	public static string pngoutPath = "Assets/Programs/pngout.exe";
	
	[MenuItem("Assets/Convert images with PNGOUT")]
	public static void ConvertImagesWithPNGOUTSettings()
	{

		foreach (Texture t in Selection.GetFiltered(typeof(Texture), SelectionMode.DeepAssets))
		{
			string path = AssetDatabase.GetAssetPath(t);
			TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
			if (!ti)
				return;
			string fullPath = Path.GetFullPath(path);
			string directoryName = Path.GetDirectoryName(fullPath);
			string pngoutFullPath = Path.GetFullPath(pngoutPath);
			
			string outPath = Path.GetDirectoryName(fullPath) + "/" + Path.GetFileNameWithoutExtension(fullPath) + "_opt.png";
			
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = pngoutFullPath;
			process.StartInfo.Arguments = fullPath + " " + outPath + " /y";
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(directoryName);
			
			process.Start();
			float p = 0f;
			while (!process.HasExited)
			{
				p += 0.0001f;
				if (p > 1)
					p = 0f;
				EditorUtility.DisplayProgressBar("PNGOUT working...", path, p);
			}
			EditorUtility.ClearProgressBar();
			process.WaitForExit();
			AssetDatabase.Refresh();

			string outPathShort = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_opt.png";
			AssetDatabase.ImportAsset(outPathShort, ImportAssetOptions.ForceUpdate);
			TextureImporter tiOut = AssetImporter.GetAtPath(outPathShort) as TextureImporter;
			if (tiOut)
			{
				TextureImporterSettings tis = new TextureImporterSettings();
				ti.ReadTextureSettings(tis);
				tiOut.SetTextureSettings(tis);
				AssetDatabase.WriteImportSettingsIfDirty(outPathShort);
			}
			else
			{
				Debug.LogWarning("PNGOUT texture output not found: " + outPathShort);
			}
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	
	[MenuItem("CONTEXT/Transform/Fit scale Y to match texture aspect")]
	public static void FitScaleYToMatchAspect(MenuCommand command)
	{
		Transform t = (Transform)command.context;
		Renderer r = t.GetComponent<Renderer>();
		if (r)
		{
			Material m = r.sharedMaterial;
			if (m)
			{
				Texture texture = m.mainTexture;
				if (texture)
				{
					float a = (float)texture.width / (float)texture.height;
					Vector3 scale = t.localScale;
					scale.y = scale.x / a;
					t.localScale = scale;
				}
			}
		}
	}
	
	[MenuItem("CONTEXT/Transform/Fit scale Z to match texture aspect")]
	public static void FitScaleZToMatchAspect(MenuCommand command)
	{
		Transform t = (Transform)command.context;
		Renderer r = t.GetComponent<Renderer>();
		if (r)
		{
			Material m = r.sharedMaterial;
			if (m)
			{
				Texture texture = m.mainTexture;
				if (texture)
				{
					float a = (float)texture.width / (float)texture.height;
					Vector3 scale = t.localScale;
					scale.z = scale.x / a;
					t.localScale = scale;
				}
			}
		}
	}
	
	[MenuItem("Assets/Move Mesh and Renderer to child")]
	public static void MoveMeshAndRendererToChild()
	{
		foreach (Transform t in Selection.GetFiltered(typeof(Transform), SelectionMode.TopLevel))
		{
			GameObject copy = (GameObject)Instantiate(t.gameObject, t.position, t.rotation);
			copy.transform.parent = t;
			copy.transform.localScale = Vector3.one;
			copy.name = "model";
			MeshFilter mf = t.GetComponent<MeshFilter>();
			if (mf)
				MeshFilter.DestroyImmediate(mf, false);
			MeshRenderer mr = t.GetComponent<MeshRenderer>();
			if (mr)
				MeshRenderer.DestroyImmediate(mr, false);
		}
	}
	
	[MenuItem("Assets/Find Shaders Use")]
	public static void FindShadersUse()
	{
		List<string> shaderNames = new List<string>();
		foreach (Shader shader in Selection.GetFiltered(typeof(Shader), SelectionMode.DeepAssets))
		{
			if (!shaderNames.Contains(shader.name))
				shaderNames.Add(shader.name);
		}
		
		if (shaderNames.Count == 0)
		{
			Debug.Log("No shaders selected.");
			return;
		}
		
		string[] allAssetsPaths = AssetDatabase.GetAllAssetPaths();
		int useCount = 0;
		for (int i = 0; i < allAssetsPaths.Length; i++)
		{
			if (EditorUtility.DisplayCancelableProgressBar("Find Shaders Use", "Searching...", (float)i / allAssetsPaths.Length))
				break;
			string assetPath = allAssetsPaths[i];
			if (!assetPath.EndsWith(".prefab") && !assetPath.EndsWith(".mat"))
				continue;
			Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			if (objects.Length == 0)
				continue;
			Object found = null;
			foreach (Object o in objects)
			{
				if (o is GameObject)
				{
					GameObject go = (GameObject)o;
					foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
					{
						Material m = r.sharedMaterial;
						if (m && m.shader && shaderNames.Contains(m.shader.name))
							found = o;
						foreach (Material m2 in r.sharedMaterials)
						{
							if (m2 && m2.shader && shaderNames.Contains(m2.shader.name))
								found = o;
						}
					}
				}
				else if (o is Material)
				{
					Material m = (Material)o;
					if (m && m.shader && shaderNames.Contains(m.shader.name))
						found = o;
				}
			}
			if (found != null)
			{
				Debug.Log("Match found at: " + assetPath, found);
				useCount++;
			}
		}
		EditorUtility.ClearProgressBar();
		Debug.Log("" + useCount + " matches found");
	}	
}
