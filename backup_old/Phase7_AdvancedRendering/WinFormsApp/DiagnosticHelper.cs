using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CamViewerPOC
{
    /// <summary>
    /// Helper class for diagnosing common issues
    /// </summary>
    public static class DiagnosticHelper
    {
        /// <summary>
        /// Check if NativeRenderer.dll exists and can be loaded
        /// </summary>
        public static string CheckNativeRendererDLL()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Native Renderer DLL Diagnostic ===");
            report.AppendLine();

            // Get application directory
            string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            report.AppendLine("Application Directory: " + appDir);
            report.AppendLine();

            // Check if DLL exists
            string dllPath = Path.Combine(appDir, "NativeRenderer.dll");
            bool dllExists = File.Exists(dllPath);
            report.AppendLine("DLL Path: " + dllPath);
            report.AppendLine("DLL Exists: " + (dllExists ? "YES" : "NO"));
            report.AppendLine();

            if (dllExists)
            {
                FileInfo fi = new FileInfo(dllPath);
                report.AppendLine("DLL Size: " + fi.Length.ToString() + " bytes");
                report.AppendLine("DLL Modified: " + fi.LastWriteTime.ToString());
                report.AppendLine();

                // Check architecture
                try
                {
                    string arch = GetDllArchitecture(dllPath);
                    report.AppendLine("DLL Architecture: " + arch);
                }
                catch (Exception ex)
                {
                    report.AppendLine("Failed to read DLL architecture: " + ex.Message);
                }
                report.AppendLine();
            }

            // Check current process architecture
            string processArch = Environment.Is64BitProcess ? "x64 (64-bit)" : "x86 (32-bit)";
            string osArch = Environment.Is64BitOperatingSystem ? "x64 (64-bit)" : "x86 (32-bit)";
            report.AppendLine("Process Architecture: " + processArch);
            report.AppendLine("OS Architecture: " + osArch);
            report.AppendLine();

            // Check OpenGL support
            report.AppendLine("=== OpenGL Support ===");
            report.AppendLine("Checking for opengl32.dll...");
            
            string openglPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "opengl32.dll");
            bool openglExists = File.Exists(openglPath);
            string openglStatus = openglExists ? "FOUND" : "NOT FOUND";
            report.AppendLine("OpenGL DLL: " + openglStatus);
            report.AppendLine("Path: " + openglPath);
            report.AppendLine();

            // List all DLLs in application directory
            report.AppendLine("=== DLLs in Application Directory ===");
            try
            {
                string[] dlls = Directory.GetFiles(appDir, "*.dll");
                foreach (string dll in dlls)
                {
                    report.AppendLine("  - " + Path.GetFileName(dll));
                }
            }
            catch (Exception ex)
            {
                report.AppendLine("Failed to list DLLs: " + ex.Message);
            }

            return report.ToString();
        }

        /// <summary>
        /// Get DLL architecture (x86 or x64)
        /// </summary>
        private static string GetDllArchitecture(string dllPath)
        {
            const int IMAGE_FILE_MACHINE_I386 = 0x014c;
            const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;

            using (FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // DOS header
                fs.Seek(0x3c, SeekOrigin.Begin);
                int peOffset = br.ReadInt32();

                // PE header
                fs.Seek(peOffset, SeekOrigin.Begin);
                uint peHeader = br.ReadUInt32();

                if (peHeader != 0x00004550) // "PE\0\0"
                    return "Invalid PE header";

                // Machine type
                ushort machine = br.ReadUInt16();

                switch (machine)
                {
                    case IMAGE_FILE_MACHINE_I386:
                        return "x86 (32-bit)";
                    case IMAGE_FILE_MACHINE_AMD64:
                        return "x64 (64-bit)";
                    default:
                        return "Unknown (0x" + machine.ToString("X4") + ")";
                }
            }
        }

        /// <summary>
        /// Show diagnostic dialog
        /// </summary>
        public static void ShowDiagnosticDialog()
        {
            string report = CheckNativeRendererDLL();
            
            Form diagForm = new Form
            {
                Text = "CAM Viewer POC - Diagnostics",
                Width = 700,
                Height = 600,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            TextBox txtReport = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 9F),
                Text = report,
                ReadOnly = true,
                WordWrap = false
            };

            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            Button btnCopy = new Button
            {
                Text = "Copy to Clipboard",
                Width = 150,
                Height = 30,
                Left = 10,
                Top = 10
            };
            btnCopy.Click += delegate(object sender, EventArgs e)
            {
                Clipboard.SetText(report);
                MessageBox.Show("Diagnostic report copied to clipboard!", 
                                "Success", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Information);
            };

            Button btnClose = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 30,
                Left = 170,
                Top = 10
            };
            btnClose.Click += delegate(object sender, EventArgs e)
            {
                diagForm.Close();
            };

            buttonPanel.Controls.Add(btnCopy);
            buttonPanel.Controls.Add(btnClose);

            diagForm.Controls.Add(txtReport);
            diagForm.Controls.Add(buttonPanel);

            diagForm.ShowDialog();
        }
    }
}
