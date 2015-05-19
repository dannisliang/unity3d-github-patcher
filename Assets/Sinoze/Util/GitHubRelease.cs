using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

namespace Sinoze.Util
{
	public class GitHubRelease : EditorWindow 
	{
		#region PUBLIC API
		public static void Patch(string owner, string repo, string id, string access_token)
		{
			var window = EditorWindow.GetWindow<GitHubRelease>(true);
			window.DoPatch(owner, repo, id, access_token);
		}
		#endregion

		string urlbase;
		WWW www;

		void DoPatch(string owner, string repo, string id, string access_token)
		{
			if(www != null)
				www.Dispose ();

			urlbase = "https://api.github.com/repos/"+owner+"/"+repo+"/zipball/"+id;
			var urlpermit = urlbase +"?access_token="+access_token;
			www = new WWW(urlpermit);
		}

		void OnGUI()
		{
			if(www != null)
			{
				GUILayout.Label("Patching...");
			}
		}

		void Update()
		{
			if(www != null && www.isDone)
			{		
				var dir = Application.temporaryCachePath + "/githubtmp";
				if(Directory.Exists(dir))
					Directory.Delete(dir, true);
				Directory.CreateDirectory(dir);
				
				var path = dir + "/githubtmp.zip";
				var fs = File.Create(path);
				fs.Write(www.bytes, 0, www.size);
				fs.Close();
				fs.Dispose();
				
				UniZip.Unzip(path, dir);
				
				
				var getdirs = Directory.GetDirectories(dir, "Assets", SearchOption.AllDirectories);
				foreach(var getd in getdirs)
				{
					DirectoryCopy(getd, Application.dataPath, true);
				}
				
				
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

				Close ();

				EditorUtility.DisplayDialog("GitHub Patch Success!", urlbase, "Ok");

				www.Dispose ();
				www = null;
			}

		}

		
		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] dirs = dir.GetDirectories();
			
			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}
			
			// If the destination directory doesn't exist, create it. 
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}
			
			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}
			
			// If copying subdirectories, copy them and their contents to new location. 
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
	}
}