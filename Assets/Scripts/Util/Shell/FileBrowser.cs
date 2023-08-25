using UnityEngine;
using SFB;
using UnityEngine.Networking;


namespace Arcade.Util.Shell
{
	public static class FileBrowser
	{
		public static string OpenFolderDialog(string Title = "", string InitPath = "")
		{
			string[] strs = StandaloneFileBrowser.OpenFolderPanel(Title, InitPath, false);
			if (strs.Length > 0 && strs[0] != "")
			{
				string str = strs[0];
				if (str.StartsWith("file://"))
				{
					str = UnityWebRequest.UnEscapeURL(str.Replace("file://", ""));
				}
				return str;
			}
			else
			{
				return null;
			}
		}
		public static void OpenExplorer(string SelectPath)
		{
			if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
			{
				System.Diagnostics.Process.Start(SelectPath?.Replace("/", "\\"));
			}
			else
			{
				var p = new System.Diagnostics.Process();
				p.StartInfo.FileName = (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer) ? "xdg-open" : "open";
				p.StartInfo.Arguments = "\"" + SelectPath + "\"";
				p.StartInfo.UseShellExecute = false;
				p.Start();
				p.WaitForExit();
				p.Close();
			}
		}
	}
}
