using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace BundleToolGUIReplacement
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private const string BundleToolVersion = "1.18.3";
        private const string BundleToolUrl = "https://github.com/google/bundletool/releases/download/1.18.3/bundletool-all-1.18.3.jar";
        private const string JavaUrl = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jre/hotspot/normal/eclipse";
        private const string PlatformToolsUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";

        private readonly string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BundleToolGUI");
        private readonly TextBox aabBox = new TextBox();
        private readonly TextBox outputBox = new TextBox();
        private readonly RadioButton debugRadio = new RadioButton();
        private readonly RadioButton customRadio = new RadioButton();
        private readonly TextBox keyBox = new TextBox();
        private readonly TextBox passwordBox = new TextBox();
        private readonly TextBox aliasBox = new TextBox();
        private readonly Button fetchAliasButton = new Button();
        private readonly Button buildButton = new Button();
        private readonly Button installButton = new Button();
        private readonly Button browseKeyButton = new Button();
        private readonly Button openOutputButton = new Button();
        private readonly TextBox logBox = new TextBox();
        private readonly Label keyPathLabel = new Label();
        private readonly Label keyPasswordLabel = new Label();
        private readonly Label keyAliasLabel = new Label();

        public MainForm()
        {
            Text = "AAB 2 APK Converter";
            Width = 760;
            Height = 570;
            MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();
            UpdateKeyInputs();
            Log("Python-free replacement. First build downloads Temurin Java 17, Google Bundletool, and Android platform-tools.");
            Log("Installing requires USB debugging enabled and an authorized Android device.");
        }

        private void BuildUi()
        {
            Label aabLabel = new Label { Text = "AAB file", AutoSize = true, Left = 18, Top = 20 };
            aabBox.SetBounds(18, 42, 590, 26);
            Button browseAab = new Button { Text = "Browse...", Left = 620, Top = 40, Width = 105, Height = 29 };
            browseAab.Click += delegate { SelectAab(); };

            Label outputLabel = new Label { Text = "Output folder", AutoSize = true, Left = 18, Top = 82 };
            outputBox.SetBounds(18, 104, 590, 26);
            Button browseOutput = new Button { Text = "Browse...", Left = 620, Top = 102, Width = 105, Height = 29 };
            browseOutput.Click += delegate { SelectOutput(); };

            GroupBox signGroup = new GroupBox { Text = "Signing key", Left = 18, Top = 145, Width = 707, Height = 178 };
            debugRadio.Text = "Use a generated debug keystore";
            debugRadio.SetBounds(15, 25, 260, 24);
            debugRadio.Checked = true;
            debugRadio.CheckedChanged += delegate { UpdateKeyInputs(); };
            customRadio.Text = "Use a custom keystore";
            customRadio.SetBounds(15, 54, 220, 24);
            customRadio.CheckedChanged += delegate { UpdateKeyInputs(); };

            keyPathLabel.Text = "Keystore path";
            keyPathLabel.SetBounds(15, 87, 100, 22);
            keyBox.SetBounds(120, 84, 430, 25);
            browseKeyButton.Text = "Browse...";
            browseKeyButton.SetBounds(560, 82, 125, 28);
            browseKeyButton.Click += delegate { SelectKeystore(); };

            keyPasswordLabel.Text = "Password";
            keyPasswordLabel.SetBounds(15, 119, 100, 22);
            passwordBox.SetBounds(120, 116, 220, 25);
            passwordBox.UseSystemPasswordChar = true;

            keyAliasLabel.Text = "Alias";
            keyAliasLabel.SetBounds(15, 150, 100, 22);
            aliasBox.SetBounds(120, 147, 220, 25);
            fetchAliasButton.Text = "Fetch alias";
            fetchAliasButton.SetBounds(350, 145, 125, 28);
            fetchAliasButton.Click += delegate { FetchAlias(); };

            signGroup.Controls.Add(debugRadio);
            signGroup.Controls.Add(customRadio);
            signGroup.Controls.Add(keyPathLabel);
            signGroup.Controls.Add(keyBox);
            signGroup.Controls.Add(browseKeyButton);
            signGroup.Controls.Add(keyPasswordLabel);
            signGroup.Controls.Add(passwordBox);
            signGroup.Controls.Add(keyAliasLabel);
            signGroup.Controls.Add(aliasBox);
            signGroup.Controls.Add(fetchAliasButton);

            buildButton.Text = "Build APK";
            buildButton.SetBounds(18, 338, 150, 36);
            buildButton.Click += delegate { StartBuild(false); };
            installButton.Text = "Build and Install";
            installButton.SetBounds(180, 338, 170, 36);
            installButton.Click += delegate { StartBuild(true); };
            openOutputButton.Text = "Open Output Folder";
            openOutputButton.SetBounds(365, 338, 170, 36);
            openOutputButton.Click += delegate { OpenOutput(); };

            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.SetBounds(18, 390, 707, 125);
            logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logBox.Font = new Font("Consolas", 9);

            Controls.Add(aabLabel);
            Controls.Add(aabBox);
            Controls.Add(browseAab);
            Controls.Add(outputLabel);
            Controls.Add(outputBox);
            Controls.Add(browseOutput);
            Controls.Add(signGroup);
            Controls.Add(buildButton);
            Controls.Add(installButton);
            Controls.Add(openOutputButton);
            Controls.Add(logBox);
        }

        private void SelectAab()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Android App Bundle (*.aab)|*.aab|All files (*.*)|*.*";
                dialog.Title = "Select AAB file";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    aabBox.Text = dialog.FileName;
                    outputBox.Text = Path.GetDirectoryName(dialog.FileName);
                }
            }
        }

        private void SelectOutput()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose where the APK files will be saved.";
                if (Directory.Exists(outputBox.Text))
                    dialog.SelectedPath = outputBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    outputBox.Text = dialog.SelectedPath;
            }
        }

        private void SelectKeystore()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Keystore files (*.keystore;*.jks)|*.keystore;*.jks|All files (*.*)|*.*";
                dialog.Title = "Select keystore file";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    keyBox.Text = dialog.FileName;
            }
        }

        private void UpdateKeyInputs()
        {
            bool enabled = customRadio.Checked;
            keyPathLabel.Enabled = enabled;
            keyBox.Enabled = enabled;
            browseKeyButton.Enabled = enabled;
            keyPasswordLabel.Enabled = enabled;
            passwordBox.Enabled = enabled;
            keyAliasLabel.Enabled = enabled;
            aliasBox.Enabled = enabled;
            fetchAliasButton.Enabled = enabled;
        }

        private void FetchAlias()
        {
            string key = keyBox.Text.Trim();
            string password = passwordBox.Text;
            if (!File.Exists(key) || password.Length == 0)
            {
                MessageBox.Show(this, "Choose a keystore and enter its password first.", "Keystore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SetBusy(true);
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string keytool = EnsureJavaAndGetKeytool();
                    string output;
                    int code = RunProcess(keytool, new string[] { "-list", "-keystore", key, "-storepass", password }, out output);
                    Match match = Regex.Match(output, @"(?im)^\s*Alias name:\s*(.+?)\s*$");
                    if (code == 0 && match.Success)
                    {
                        string found = match.Groups[1].Value.Trim();
                        BeginInvoke((MethodInvoker)delegate { aliasBox.Text = found; });
                        Log("Alias found: " + found);
                    }
                    else
                    {
                        Log("Alias not found. Check the keystore path and password, or enter the alias manually.");
                    }
                }
                catch (Exception ex) { Log("Error: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void StartBuild(bool install)
        {
            string aab = aabBox.Text.Trim();
            string output = outputBox.Text.Trim();
            if (!File.Exists(aab))
            {
                MessageBox.Show(this, "Please select an AAB file first.", "AAB file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (output.Length == 0)
                output = Path.GetDirectoryName(aab);
            if (customRadio.Checked && (!File.Exists(keyBox.Text.Trim()) || passwordBox.Text.Length == 0 || aliasBox.Text.Trim().Length == 0))
            {
                MessageBox.Show(this, "For a custom keystore, choose the file and enter its password and alias.", "Keystore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string key = keyBox.Text.Trim();
            string password = passwordBox.Text;
            string alias = aliasBox.Text.Trim();
            bool custom = customRadio.Checked;
            SetBusy(true);
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { BuildAndMaybeInstall(aab, output, custom, key, password, alias, install); }
                catch (Exception ex) { Log("Error: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void BuildAndMaybeInstall(string aab, string outputDir, bool custom, string customKey, string customPassword, string customAlias, bool install)
        {
            Directory.CreateDirectory(outputDir);
            string java = EnsureJava();
            string jar = EnsureBundleTool();
            string adb = install ? EnsureAdb() : null;
            string key;
            string password;
            string alias;

            if (custom)
            {
                key = customKey;
                password = customPassword;
                alias = customAlias;
            }
            else
            {
                key = Path.Combine(appData, "debug.keystore");
                password = "android";
                alias = "androiddebugkey";
                if (!File.Exists(key))
                {
                    Log("Creating a debug keystore...");
                    string keytool = Path.Combine(Path.GetDirectoryName(java), "keytool.exe");
                    string keytoolOutput;
                    int keyCode = RunProcess(keytool, new string[] {
                        "-genkeypair", "-keystore", key, "-storepass", password,
                        "-alias", alias, "-keypass", password, "-dname",
                        "CN=Android Debug,O=Android,C=US", "-keyalg", "RSA", "-validity", "10000"
                    }, out keytoolOutput);
                    if (keyCode != 0 || !File.Exists(key))
                        throw new Exception("Could not create the debug keystore. " + keytoolOutput);
                }
            }

            string baseName = Path.GetFileNameWithoutExtension(aab);
            string apks = Path.Combine(outputDir, baseName + ".apks");
            string apk = Path.Combine(outputDir, baseName + ".apk");
            if (File.Exists(apks))
                File.Delete(apks);
            Log("Building a universal APK set...");
            string ignored;
            int result = RunProcess(java, new string[] {
                "-jar", jar, "build-apks",
                "--bundle=" + aab,
                "--output=" + apks,
                "--mode=universal",
                "--ks=" + key,
                "--ks-pass=pass:" + password,
                "--ks-key-alias=" + alias,
                "--key-pass=pass:" + password
            }, out ignored);
            if (result != 0 || !File.Exists(apks))
                throw new Exception("Bundletool could not build the APK set. Review the log for details.");

            using (ZipArchive archive = ZipFile.OpenRead(apks))
            {
                ZipArchiveEntry universal = archive.GetEntry("universal.apk");
                if (universal == null)
                    throw new Exception("Bundletool produced an APK set without universal.apk.");
                if (File.Exists(apk))
                    File.Delete(apk);
                using (Stream input = universal.Open())
                using (FileStream target = new FileStream(apk, FileMode.CreateNew, FileAccess.Write))
                    input.CopyTo(target);
            }
            Log("Created: " + apk);

            if (install)
            {
                Log("Installing APK. Confirm the device is connected and USB debugging is authorized...");
                int installCode = RunProcess(adb, new string[] { "install", "-r", apk }, out ignored);
                if (installCode != 0)
                    throw new Exception("ADB install failed. Check the device connection and USB debugging authorization.");
                Log("Installation completed.");
            }
            Log("Task completed.");
            OpenOutputDirectory(outputDir);
        }

        private string EnsureJavaAndGetKeytool()
        {
            string java = EnsureJava();
            return Path.Combine(Path.GetDirectoryName(java), "keytool.exe");
        }

        private string EnsureJava()
        {
            string root = Path.Combine(appData, "java");
            string found = FindFile(root, "java.exe");
            if (found != null)
                return found;

            Directory.CreateDirectory(root);
            string zip = Path.Combine(appData, "temurin-jre-17.zip");
            DownloadFile(JavaUrl, zip, "Temurin Java 17 runtime");
            Log("Extracting Java runtime...");
            ZipFile.ExtractToDirectory(zip, root);
            File.Delete(zip);
            found = FindFile(root, "java.exe");
            if (found == null)
                throw new Exception("The Java runtime download did not contain java.exe.");
            return found;
        }

        private string EnsureBundleTool()
        {
            string path = Path.Combine(appData, "bundletool-all-" + BundleToolVersion + ".jar");
            if (!File.Exists(path))
                DownloadFile(BundleToolUrl, path, "Google Bundletool");
            return path;
        }

        private string EnsureAdb()
        {
            string root = Path.Combine(appData, "platform-tools");
            string found = FindFile(root, "adb.exe");
            if (found != null)
                return found;
            string zip = Path.Combine(appData, "platform-tools.zip");
            DownloadFile(PlatformToolsUrl, zip, "Android platform-tools (ADB)");
            Log("Extracting Android platform-tools...");
            ZipFile.ExtractToDirectory(zip, appData);
            File.Delete(zip);
            found = FindFile(root, "adb.exe");
            if (found == null)
                throw new Exception("The platform-tools download did not contain adb.exe.");
            return found;
        }

        private void DownloadFile(string url, string destination, string description)
        {
            Directory.CreateDirectory(appData);
            Log("Downloading " + description + "...");
            // This app targets the .NET Framework version available on Windows 10.
            // Its legacy default may negotiate TLS 1.0, which current download hosts reject.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "BundleToolGUIReplacement/1.0");
                client.DownloadFile(url, destination);
            }
        }

        private static string FindFile(string directory, string fileName)
        {
            if (!Directory.Exists(directory))
                return null;
            try { return Directory.GetFiles(directory, fileName, SearchOption.AllDirectories).FirstOrDefault(); }
            catch { return null; }
        }

        private int RunProcess(string executable, IEnumerable<string> arguments, out string output)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = executable;
            info.Arguments = String.Join(" ", arguments.Select(QuoteArgument).ToArray());
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            StringBuilder captured = new StringBuilder();
            object sync = new object();
            using (Process process = new Process())
            {
                process.StartInfo = info;
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        lock (sync) captured.AppendLine(e.Data);
                        Log(e.Data);
                    }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        lock (sync) captured.AppendLine(e.Data);
                        Log(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.WaitForExit();
                output = captured.ToString();
                return process.ExitCode;
            }
        }

        private static string QuoteArgument(string value)
        {
            if (value == null)
                return "\"\"";
            if (value.Length > 0 && value.IndexOfAny(new char[] { ' ', '\t', '\n', '\v', '"' }) < 0)
                return value;
            StringBuilder result = new StringBuilder();
            result.Append('"');
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    slashes++;
                }
                else if (c == '"')
                {
                    result.Append('\\', slashes * 2 + 1);
                    result.Append('"');
                    slashes = 0;
                }
                else
                {
                    if (slashes > 0)
                        result.Append('\\', slashes);
                    slashes = 0;
                    result.Append(c);
                }
            }
            if (slashes > 0)
                result.Append('\\', slashes * 2);
            result.Append('"');
            return result.ToString();
        }

        private void OpenOutput()
        {
            string path = outputBox.Text.Trim();
            if (path.Length == 0)
            {
                string aab = aabBox.Text.Trim();
                path = File.Exists(aab) ? Path.GetDirectoryName(aab) : appData;
            }
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = QuoteArgument(path), UseShellExecute = true });
        }

        private void OpenOutputDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Log("Output folder is missing: " + path);
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { OpenOutputDirectory(path); });
                return;
            }
            try
            {
                Log("Opening output folder...");
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "/n," + QuoteArgument(path), UseShellExecute = false, CreateNoWindow = true });
            }
            catch (Exception ex)
            {
                Log("Could not open the output folder automatically: " + ex.Message);
            }
        }

        private void SetBusy(bool busy)
        {
            if (IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { SetBusy(busy); });
                return;
            }
            buildButton.Enabled = !busy;
            installButton.Enabled = !busy;
            fetchAliasButton.Enabled = !busy && customRadio.Checked;
        }

        private void Log(string text)
        {
            if (IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { Log(text); });
                return;
            }
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        }
    }
}
