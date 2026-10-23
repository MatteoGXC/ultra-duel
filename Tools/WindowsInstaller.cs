using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;

// Small portable installer; all game files come from this project's versioned GitHub Release.
public class WindowsInstaller : Form
{
    public class Part { public string name, url, sha256; public long size; }
    public class Manifest { public string version; public long installedBytes; public Part[] parts; }
    readonly TextBox destination = new TextBox();
    readonly Label status = new Label();
    readonly ProgressBar progress = new ProgressBar();
    readonly Button install = new Button();
    readonly Button browse = new Button();
    bool installing;
    bool installed;
    Manifest manifest;

    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new WindowsInstaller());
    }

    public WindowsInstaller()
    {
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("release-manifest.json"))
        using (var reader = new StreamReader(stream))
            manifest = new JavaScriptSerializer().Deserialize<Manifest>(reader.ReadToEnd());
        Text = "Install Ultra Duel";
        ClientSize = new Size(600, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var heading = new Label { Text = "Ultra Duel", Font = new Font("Segoe UI", 21, FontStyle.Bold), Bounds = new Rectangle(22, 15, 540, 45) };
        var description = new Label { Text = "Windows · TCG and Genesys · Solo practice\nDownloads the complete game and creates a desktop shortcut.", Bounds = new Rectangle(25, 67, 550, 48) };
        destination.SetBounds(25, 124, 450, 28);
        destination.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UltraDuel");
        browse.Text = "Browse…";
        browse.SetBounds(485, 123, 90, 30);
        browse.Click += delegate { using (var dialog = new FolderBrowserDialog()) { dialog.SelectedPath = destination.Text; if (dialog.ShowDialog() == DialogResult.OK) destination.Text = dialog.SelectedPath; } };
        status.SetBounds(25, 162, 550, 28);
        status.Text = "Requires about " + Math.Ceiling(manifest.installedBytes / 1073741824.0) + " GB for the game, plus download space.";
        progress.SetBounds(25, 196, 420, 22);
        install.Text = "Install";
        install.SetBounds(465, 192, 110, 34);
        install.Click += async delegate { if (installed) Close(); else await InstallGame(); };
        Controls.AddRange(new Control[] { heading, description, destination, browse, status, progress, install });
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (installing) e.Cancel = true; };
    }

    static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    async Task InstallGame()
    {
        installing = true;
        install.Enabled = browse.Enabled = destination.Enabled = false;
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string target = Path.GetFullPath(destination.Text.Trim());
            Directory.CreateDirectory(target);
            string cache = Path.Combine(Path.GetTempPath(), "UltraDuel-" + manifest.version);
            Directory.CreateDirectory(cache);
            for (int i = 0; i < manifest.parts.Length; i++)
            {
                Part part = manifest.parts[i];
                string zip = Path.Combine(cache, part.name);
                status.Text = "Checking download " + (i+1) + " of " + manifest.parts.Length + "…";
                bool cached = File.Exists(zip) && new FileInfo(zip).Length == part.size && await Task.Run(() => Hash(zip)) == part.sha256;
                if (!cached)
                {
                    string partial = zip + ".download";
                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "Ultra-Duel-Installer/" + manifest.version;
                        int partIndex = i;
                        client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e) {
                            progress.Value = Math.Min(100, (partIndex * 100 + e.ProgressPercentage) / manifest.parts.Length);
                            status.Text = "Downloading part " + (partIndex+1) + " of " + manifest.parts.Length + " · " + e.ProgressPercentage + "%";
                        };
                        await client.DownloadFileTaskAsync(new Uri(part.url), partial);
                    }
                    status.Text = "Verifying download " + (i+1) + "…";
                    if (new FileInfo(partial).Length != part.size || await Task.Run(() => Hash(partial)) != part.sha256)
                        throw new IOException("Download verification failed. Click Install to retry.");
                    if (File.Exists(zip)) File.Delete(zip);
                    File.Move(partial, zip);
                }
                status.Text = "Installing part " + (i+1) + " of " + manifest.parts.Length + "…";
                await Task.Run(() => Extract(zip, target));
            }
            string exe = Path.Combine(target, "Ultra-Duel.exe");
            if (!File.Exists(exe)) exe = Path.Combine(target, "Matteo-Duel.exe");
            if (!File.Exists(exe)) throw new IOException("The game executable was not installed.");
            try
            {
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                dynamic shortcut = shell.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Ultra Duel.lnk"));
                shortcut.TargetPath = exe;
                shortcut.WorkingDirectory = target;
                shortcut.Save();
            }
            catch { /* The portable game still works if desktop shortcuts are restricted. */ }
            foreach (Part part in manifest.parts) { try { File.Delete(Path.Combine(cache, part.name)); } catch { } }
            progress.Value = 100;
            status.Text = "Installed. Open Ultra Duel from your desktop or installation folder.";
            install.Text = "Done";
            installed = true;
            install.Enabled = true;
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message + "\n\nClose Ultra Duel before updating. Downloads are reused when you retry.", "Installation could not finish", MessageBoxButtons.OK, MessageBoxIcon.Error);
            status.Text = "Installation stopped. Your existing decks and settings are preserved.";
            install.Enabled = browse.Enabled = destination.Enabled = true;
        }
        finally { installing = false; }
    }

    static void Extract(string zip, string target)
    {
        string root = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using (var archive = ZipFile.OpenRead(zip))
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string path = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new IOException("Invalid archive path.");
            if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(path); continue; }
            bool preserve = entry.FullName.Equals("Data/config.conf", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.StartsWith("Deck/", StringComparison.OrdinalIgnoreCase);
            if (preserve && File.Exists(path)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            entry.ExtractToFile(path, true);
        }
    }
}
