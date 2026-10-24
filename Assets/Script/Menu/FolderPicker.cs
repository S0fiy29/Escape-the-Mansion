using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Opens the system "choose a folder" window. Returns null when cancelled or when the platform has none,
// in which case the player can still type a path into the save screen.
public static class FolderPicker
{
    public static bool CanBrowse
    {
        get
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return true;
#else
            return false;
#endif
        }
    }

    public static string Pick(string startFolder)
    {
#if UNITY_EDITOR
        string chosen = UnityEditor.EditorUtility.OpenFolderPanel("Choose a folder for your saves", startFolder, "");
        return string.IsNullOrEmpty(chosen) ? null : chosen;
#elif UNITY_STANDALONE_WIN
        try
        {
            return PickOnWindows();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Folder window is not available: " + ex.Message);
            return null;
        }
#else
        return null;
#endif
    }

#if UNITY_STANDALONE_WIN
    private const uint BIF_RETURNONLYFSDIRS = 0x0001;
    private const uint BIF_EDITBOX = 0x0010;
    private const uint BIF_NEWDIALOGSTYLE = 0x0040;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BrowseInfo
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public IntPtr pszDisplayName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHBrowseForFolderW(ref BrowseInfo info);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDListW(IntPtr pidl, StringBuilder path);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr pointer);

    [DllImport("ole32.dll")]
    private static extern int OleInitialize(IntPtr reserved);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private static string PickOnWindows()
    {
        // The new-style dialog needs COM. This does nothing if Unity already set it up.
        OleInitialize(IntPtr.Zero);

        IntPtr nameBuffer = Marshal.AllocHGlobal(520);
        try
        {
            var info = new BrowseInfo
            {
                hwndOwner = GetActiveWindow(),
                pszDisplayName = nameBuffer,
                lpszTitle = "Choose a folder for your saves",
                ulFlags = BIF_RETURNONLYFSDIRS | BIF_EDITBOX | BIF_NEWDIALOGSTYLE
            };

            IntPtr pidl = SHBrowseForFolderW(ref info);
            if (pidl == IntPtr.Zero) return null;

            try
            {
                var path = new StringBuilder(520);
                return SHGetPathFromIDListW(pidl, path) ? path.ToString() : null;
            }
            finally
            {
                CoTaskMemFree(pidl);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(nameBuffer);
        }
    }
#endif
}
