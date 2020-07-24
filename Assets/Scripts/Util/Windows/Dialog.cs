using System;
using System.Text;
using Arcade.Util.Windows.Internal;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Arcade.Util.Windows
{
	namespace Internal
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string filter = null;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string file = null;
            public int maxFile = 0;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string fileTitle = null;
            public int maxFileTitle = 0;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string initialDir = null;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class BrowserInfo
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszDisplayName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszTitle;
            public uint ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public int iImage;
        }
        public class OpenFolderDialog
        {
            public string InitPath;
            public string SelectedFolder;
            public string Title;

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
            [DllImport("user32.dll")]
            private static extern IntPtr GetActiveWindow();
            [DllImport("shell32.dll")]
            private static extern IntPtr SHBrowseForFolder([In, Out] BrowserInfo bi);
            [DllImport("shell32.dll")]
            public static extern bool SHGetPathFromIDListW(IntPtr pidl, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder pszPath);

            public int ShowDialog()
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    return ShowVistaDialog();
                }
                else
                {
                    return ShowLegacyDialog();
                }
            }
            private int ShowVistaDialog()
            {
                IFileDialog dialog = new FileOpenDialogRCW() as IFileDialog;
                uint options = 0;
                dialog.GetOptions(out options);
                options |= (uint)FileOpenDialogOptions.PickFolders | (uint)FileOpenDialogOptions.ForceFileSystem;
                dialog.SetOptions(options);
                if (InitPath != null)
                {
                    Guid guid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                    IShellItem shellItem = null;
                    if (SHCreateItemFromParsingName(InitPath, IntPtr.Zero, ref guid, out shellItem) == 0)
                    {
                        dialog.SetFolder(shellItem);
                    }
                }
                if (Title != null)
                {
                    dialog.SetTitle(Title);
                }
                if (dialog.Show(GetActiveWindow()) == 0)
                {
                    IShellItem shellItem = null;
                    if (dialog.GetResult(out shellItem) == 0)
                    {
                        IntPtr result = IntPtr.Zero;
                        if (shellItem.GetDisplayName((uint)Constants.FileSysPath, out result) == 0)
                        {
                            if (result != IntPtr.Zero)
                            {
                                try
                                {
                                    SelectedFolder = Marshal.PtrToStringAuto(result);
                                    return 1;
                                }
                                catch (Exception)
                                {
                                    Marshal.FreeCoTaskMem(result);
                                    return 0;
                                }
                            }
                        }
                    }
                }
                return 0;
            }
            private int ShowLegacyDialog()
            {
                BrowserInfo bi = new BrowserInfo
                {
                    hwndOwner = GetActiveWindow(),
                    lpszTitle = Title,
                    ulFlags = 0x00000001
                };
                IntPtr Result = SHBrowseForFolder(bi);
                if (Result == IntPtr.Zero) return 0;
                StringBuilder sb = new StringBuilder(1024);
                if (!SHGetPathFromIDListW(Result, sb)) return 0;
                SelectedFolder = sb.ToString();
                return 1;
            }
        }
        public enum FileOpenDialogOptions : uint
        {
            PickFolders = 0x00000020,
            ForceFileSystem = 0x00000040,
            NoValidate = 0x00000100,
            NoTestFileCreate = 0x00010000,
            DontAddToRecent = 0x02000000
        }
        public enum Constants : uint
        {
            FileSysPath = 0x80058000
        }
        [ComImport, ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate), Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        internal class FileOpenDialogRCW { }
        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItem
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint BindToHandler([In] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IntPtr ppvOut);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetDisplayName([In] uint sigdnName, out IntPtr ppszName);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
        }
        [ComImport(), Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IFileDialog
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig()]
            uint Show([In, Optional] IntPtr hwndOwner);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypeIndex([In] uint iFileType);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileTypeIndex(out uint piFileType);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Unadvise([In] uint dwCookie);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOptions([In] uint fos);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetOptions(out uint fos);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Close([MarshalAs(UnmanagedType.Error)] uint hr);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetClientGuid([In] ref Guid guid);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint ClearClientData();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        }
    }
    public static class Dialog
    {
        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSaveFileName([In, Out] OpenFileName ofn);
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
        public static string OpenFileDialog(string Title = null, string Filename = null, string InitPath = null, string Filter = null)
        {
#if UNITY_EDITOR
            return EditorUtility.OpenFilePanel(Title, InitPath, Filter);
#endif
            string filename = (Filename ?? "") + new string(new char[1024]);
            OpenFileName ofn = new OpenFileName
            {
                dlgOwner = GetActiveWindow(),
                filter = Filter?.Replace("|", "\0") + "\0",
                file = filename,
                maxFile = 1024,
                initialDir = InitPath?.Replace("/", "\\"),
                title = Title,
                flags = 0x00000008,
                structSize = Marshal.SizeOf(typeof(OpenFileName))
            };
            bool result = GetOpenFileName(ofn);
            if (result) return ofn.file.ToString();
            else return null;
        }
        public static string SaveFileDialog(string Title = null, string Filename = null, string InitPath = null, string Filter = null)
        {
#if UNITY_EDITOR
            return EditorUtility.SaveFilePanel(Title, InitPath, Filename, Filter);
#endif
            string filename = (Filename ?? "") + new string(new char[1024]);
            OpenFileName ofn = new OpenFileName
            {
                dlgOwner = GetActiveWindow(),
                filter = Filter?.Replace("|", "\0") + "\0",
                file = filename,
                maxFile = 1024,
                initialDir = InitPath?.Replace("/", "\\"),
                title = Title,
                flags = 0x00000008,
                structSize = Marshal.SizeOf(typeof(OpenFileName))
            };
            bool result = GetSaveFileName(ofn);
            if (result) return ofn.file.ToString();
            else return null;
        }
        public static string OpenFolderDialog(string Title = null, string InitPath = null)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                InitPath = InitPath,
                Title = Title
            };
            if (openFolderDialog.ShowDialog() == 0) return null;
            return openFolderDialog.SelectedFolder;
        }
        public static void OpenExplorer(string SelectPath)
        {
            // UnityEngine.Debug.Log(SelectPath);
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) {
                Process.Start(SelectPath?.Replace("/", "\\"));
            } else {
                Process p = new Process ();
                p.StartInfo.FileName = "open";
                p.StartInfo.Arguments = SelectPath;
                p.Start();
            }
        }
    }
}
