using UnityEngine;
using SFB;
using UnityEngine.Networking;


namespace Arcade.Util.Shell
{
	public class FileBrowser
	{
		public static string OpenFolderDialog(string Title = null, string InitPath = null)
		{
			string[] strs = StandaloneFileBrowser.OpenFolderPanel("选择您的 Arcaea 自制谱文件夹 (包含 0/1/2.aff, base.mp3/ogg/wav, base.jpg)", "%HOMEDRIVE/Desktop%", false);
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
			UnityEngine.Debug.Log(SelectPath);
			if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
			{
				System.Diagnostics.Process.Start(SelectPath?.Replace("/", "\\"));
			}
			else
			{
				var p = new System.Diagnostics.Process();
				p.StartInfo.FileName = "open";
				p.StartInfo.Arguments = "\"" + SelectPath + "\"";
				p.StartInfo.UseShellExecute = false;
				p.Start();
				p.WaitForExit();
				p.Close();
			}
		}
	}
}
