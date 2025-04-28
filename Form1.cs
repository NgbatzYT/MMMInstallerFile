using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MMMInstallerFileMaker
{
    public partial class Form1 : Form
    {
        private string dllPath;
        private string zipPath;
        
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect, 
            int nWidthEllipse,
            int nHeightEllipse 
        );
        
        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
        public Form1()
        {
            InitializeComponent();
            IntPtr hRegion = CreateRoundRectRgn(0, 0, Width, Height, 20, 20);
            SetWindowRgn(this.Handle, hRegion, true);
            this.MouseDown += Form_MouseDown;
            StartPosition = FormStartPosition.CenterScreen;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedFolder = PickFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    CreateMMMFile(selectedFolder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string PickFolder()
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
            const uint FOS_PICKFOLDERS = 0x00000020;
            const uint FOS_FORCEFILESYSTEM = 0x00000040;
            const uint FOS_PATHMUSTEXIST = 0x00000800;

            dialog.SetOptions(FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST);
            dialog.SetTitle("Select Folder to Create MMM Installer");
            
            dialog.Show(IntPtr.Zero);
            
            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr pszString);
            string path = Marshal.PtrToStringAuto(pszString);
            Marshal.FreeCoTaskMem(pszString);

            return path;
        }
        
        private void CreateMMMFile(string folderPath)
        {
            string dllName = textBox1.Text;
            string zipName = textBox2.Text;
            string downloadUrl = textBox3.Text;
            
            if (string.IsNullOrEmpty(dllName) && string.IsNullOrEmpty(zipName) && string.IsNullOrEmpty(downloadUrl))
            {
                MessageBox.Show("Please provide at least one input (DLL, ZIP, or Download URL).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (!IsValidFileName(dllName) || !IsValidFileName(zipName))
            {
                MessageBox.Show("DLL or ZIP name contains invalid characters.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var jsonContent = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(dllName) && !string.IsNullOrEmpty(dllPath))
            {
                if (File.Exists(dllPath))
                {
                    File.Copy(dllPath, Path.Combine(folderPath, "mod.dll"), true);
                    jsonContent.Add("dll", dllName);
                }
            }
            if (!string.IsNullOrEmpty(zipName) && !string.IsNullOrEmpty(zipPath))
            {
                if (File.Exists(zipPath))
                {
                    File.Copy(zipPath, Path.Combine(folderPath, "mod.zip"), true);
                    jsonContent.Add("zip", zipName);
                }
                
            }
            if (!string.IsNullOrEmpty(downloadUrl))
                jsonContent.Add("download", downloadUrl);
            
            string infoJsonPath = Path.Combine(folderPath, "Info.json");

            try
            {
                File.WriteAllText(infoJsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(jsonContent, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create Info.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            string parentFolder = Path.GetDirectoryName(folderPath);
            string folderName = Path.GetFileName(folderPath);

            string zipTempPath = Path.Combine(parentFolder, folderName + ".zip");
            string mmmFinalPath = Path.Combine(folderPath, folderName + ".mmm");

            if (File.Exists(zipTempPath))
            {
                File.Delete(zipTempPath);
            }
            
            try
            {
                ZipFile.CreateFromDirectory(folderPath, zipTempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create zip file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            

            if (File.Exists(mmmFinalPath))
            {
                File.Delete(mmmFinalPath);
            }
                

            File.Move(zipTempPath, mmmFinalPath);
            MessageBox.Show("MMM File created successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process.Start("explorer.exe", folderPath);
        }
        private bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return true;

            string invalidChars = new string(Path.GetInvalidFileNameChars());
            return !fileName.Any(c => invalidChars.Contains(c));
        }
        
        // Ignore all this!!!
        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        [ClassInterface(ClassInterfaceType.None)]
        private class FileOpenDialogRCW { }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            void Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(uint fos);
            void GetOptions(out uint fos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, uint fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
        }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000,
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "DLL files (*.dll)|*.dll";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = false;
            ofd.FilterIndex = 1;
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                dllPath = ofd.FileName; 
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Zip files (*.zip)|*.zip";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = false;
            ofd.FilterIndex = 1;
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                zipPath = ofd.FileName; 
            }
        }
    }
}
