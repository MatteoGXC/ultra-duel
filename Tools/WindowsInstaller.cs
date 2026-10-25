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
using System.Threading;
using System.Linq;
using System.Diagnostics;

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
    CancellationTokenSource cancellation;
    bool closing;

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
        destination.Text = MDPro3.UltraInstallMigration.DefaultInstallationDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        browse.Text = "Browse…";
        browse.SetBounds(485, 123, 90, 30);
        browse.Click += delegate { using (var dialog = new FolderBrowserDialog()) { dialog.SelectedPath = destination.Text; if (dialog.ShowDialog() == DialogResult.OK) destination.Text = dialog.SelectedPath; } };
        status.SetBounds(25, 162, 550, 28);
        status.Text = "Requires about " + Math.Ceiling(manifest.installedBytes / 1073741824.0) + " GB for the game, plus download space.";
        progress.SetBounds(25, 196, 420, 22);
        install.Text = "Install";
        install.SetBounds(465, 192, 110, 34);
        install.Click += async delegate { if (installed) Close(); else if(installing) cancellation.Cancel(); else await InstallGame(); };
        Controls.AddRange(new Control[] { heading, description, destination, browse, status, progress, install });
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (installing) { closing=true; cancellation.Cancel(); e.Cancel=true; } };
    }

    static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    async Task InstallGame()
    {
        installing = true;
        cancellation=new CancellationTokenSource();
        browse.Enabled = destination.Enabled = false;
        install.Text="Pause";
        MDPro3.UltraInstallMigration migration=null;
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string target = Path.GetFullPath(destination.Text.Trim());
            Directory.CreateDirectory(target);
            MDPro3.UltraInstallMigration.AssertNotRunning(target);
            string personalStore=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Ultra Duel","UserData");
            var personalBackup=new MDPro3.UltraUserDataBackup(target,personalStore);
            status.Text="Backing up existing decks and settings…";
            await Task.Run(()=>personalBackup.Capture());
            string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UltraDuelInstaller", "Downloads");
            Directory.CreateDirectory(cache);
            long[] received=new long[manifest.parts.Length];
            long total=manifest.parts.Sum(p=>p.size);
            var elapsed=Stopwatch.StartNew();
            long initial=0;
            for(int i=0;i<manifest.parts.Length;i++)
            {
                string partial=Path.Combine(cache,manifest.parts[i].sha256+".zip.download");
                if(File.Exists(partial))initial+=Math.Min(new FileInfo(partial).Length,manifest.parts[i].size);
            }
            var updates=new Progress<int>(delegate(int ignored) {
                long done=received.Sum();double seconds=elapsed.Elapsed.TotalSeconds;
                double speed=seconds>0?Math.Max(0,done-initial)/seconds:0;
                progress.Value=(int)Math.Min(90,done*90/Math.Max(1,total));
                status.Text=String.Format("Downloading {0:N1} / {1:N1} GB · {2:N1} MB/s",done/1073741824.0,total/1073741824.0,speed/1048576.0);
            });
            using(var slots=new SemaphoreSlim(3))
            {
                var tasks=manifest.parts.Select(async (part,index)=>
                {
                    await slots.WaitAsync(cancellation.Token);
                    try
                    {
                        string zip=Path.Combine(cache,part.sha256+".zip");
                        bool cached=File.Exists(zip) && new FileInfo(zip).Length==part.size && await Task.Run(()=>Hash(zip))==part.sha256;
                        if(!cached)
                        {
                            string partial=zip+".download";
                            await DownloadPart(part,partial,delegate(long bytes){Interlocked.Exchange(ref received[index],bytes);((IProgress<int>)updates).Report(index);},cancellation.Token);
                            cancellation.Token.ThrowIfCancellationRequested();
                            if(new FileInfo(partial).Length!=part.size || await Task.Run(()=>Hash(partial))!=part.sha256)
                            {
                                File.Delete(partial);
                                throw new IOException("Download verification failed for "+part.name+". Click Install to retry.");
                            }
                            if(File.Exists(zip))File.Delete(zip);
                            File.Move(partial,zip);
                        }
                        Interlocked.Exchange(ref received[index],part.size);((IProgress<int>)updates).Report(index);
                    }
                    catch { cancellation.Cancel();throw; }
                    finally { slots.Release(); }
                }).ToArray();
                await Task.WhenAll(tasks);
            }
            MDPro3.UltraInstallMigration.AssertNotRunning(target);
            migration=new MDPro3.UltraInstallMigration(target);
            migration.Prepare();
            // Extract in manifest order: the final cumulative update replaces baseline files.
            for (int i=0;i<manifest.parts.Length;i++)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                Part part=manifest.parts[i];string zip=Path.Combine(cache,part.sha256+".zip");
                status.Text = "Installing part " + (i+1) + " of " + manifest.parts.Length + "…";
                await Task.Run(() => ExtractPackage(zip, target, migration));
                progress.Value=90+(i+1)*10/manifest.parts.Length;
            }
            MDPro3.UltraInstallMigration.ValidateRuntime(target);
            migration.Complete();
            string exe = Path.Combine(target, MDPro3.UltraInstallMigration.Executable);
            status.Text="Restoring your decks and settings…";
            await Task.Run(()=>{personalBackup.Restore();personalBackup.Capture();});
            try
            {
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                foreach(string link in Directory.GetFiles(desktop,"*.lnk"))
                {
                    dynamic existing=shell.CreateShortcut(link);
                    string previous=(string)existing.TargetPath;
                    if(MDPro3.UltraInstallMigration.LegacyExecutables.Any(n=>string.Equals(previous,Path.Combine(target,n),StringComparison.OrdinalIgnoreCase)))
                    { existing.TargetPath=exe;existing.WorkingDirectory=target;existing.Save(); }
                }
                dynamic shortcut = shell.CreateShortcut(Path.Combine(desktop, "Ultra Duel.lnk"));
                shortcut.TargetPath = exe;
                shortcut.WorkingDirectory = target;
                shortcut.Save();
            }
            catch { /* The portable game still works if desktop shortcuts are restricted. */ }
            foreach (Part part in manifest.parts) { try { File.Delete(Path.Combine(cache, part.sha256+".zip")); } catch { } }
            progress.Value = 100;
            status.Text = "Installed. Open Ultra Duel from your desktop or installation folder.";
            install.Text = "Done";
            installed = true;
            install.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            if(migration!=null)migration.Rollback();
            status.Text="Paused. Click Install to continue the saved download.";
            install.Text="Install";install.Enabled=browse.Enabled=destination.Enabled=true;
        }
        catch (Exception error)
        {
            if(migration!=null)migration.Rollback();
            MessageBox.Show(this, error.Message + "\n\nClose Ultra Duel before updating. Downloads are reused when you retry.", "Installation could not finish", MessageBoxButtons.OK, MessageBoxIcon.Error);
            status.Text = "Installation stopped. Your existing decks and settings are preserved.";
            install.Enabled = browse.Enabled = destination.Enabled = true;
            install.Text="Install";
        }
        finally { installing = false;cancellation.Dispose();if(closing)Close(); }
    }

    public static async Task DownloadPart(Part part,string partial,Action<long> report,CancellationToken token)
    {
        ServicePointManager.DefaultConnectionLimit=6;
        for(int attempt=0;attempt<5;attempt++)
        {
            token.ThrowIfCancellationRequested();
            long offset=File.Exists(partial)?new FileInfo(partial).Length:0;
            if(offset>part.size){File.Delete(partial);offset=0;}
            report(offset);
            if(offset==part.size)return;
            var request=(HttpWebRequest)WebRequest.Create(part.url);
            request.UserAgent="Ultra-Duel-Installer";request.Timeout=request.ReadWriteTimeout=30000;
            if(offset>0)request.AddRange(offset);
            try
            {
                using(token.Register(request.Abort))
                using(var response=(HttpWebResponse)await request.GetResponseAsync())
                {
                    if(response.StatusCode==HttpStatusCode.PartialContent)
                    {
                        string expected="bytes "+offset+"-";
                        string range=response.Headers[HttpResponseHeader.ContentRange];
                        if(range==null || !range.StartsWith(expected,StringComparison.Ordinal) || !range.EndsWith("/"+part.size,StringComparison.Ordinal))
                            throw new IOException("The download server returned an invalid resume range.");
                    }
                    else if(response.StatusCode==HttpStatusCode.OK)offset=0;
                    else throw new IOException("Download HTTP "+(int)response.StatusCode);
                    using(var output=new FileStream(partial,offset>0?FileMode.Append:FileMode.Create,FileAccess.Write,FileShare.Read,262144,true))
                    using(var input=response.GetResponseStream())
                    {
                        byte[] buffer=new byte[262144];int count;long sent=offset;var tick=Stopwatch.StartNew();
                        while((count=await input.ReadAsync(buffer,0,buffer.Length,token))>0)
                        {
                            if(sent+count>part.size)throw new IOException("Download exceeded its expected size.");
                            await output.WriteAsync(buffer,0,count,token);sent+=count;
                            if(tick.ElapsedMilliseconds>=200){report(sent);tick.Restart();}
                        }
                        report(sent);
                        if(sent!=part.size)throw new IOException("Download was interrupted.");
                    }
                }
                return;
            }
            catch(Exception error)
            {
                token.ThrowIfCancellationRequested();
                if(!(error is WebException || error is IOException) || attempt==4)throw;
            }
            await Task.Delay(1000*(1<<attempt),token);
        }
    }

    static void Extract(string zip, string target)
    { ExtractPackage(zip,target,null); }

    static void ExtractPackage(string zip, string target, MDPro3.UltraInstallMigration migration)
    {
        string root = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using (var archive = ZipFile.OpenRead(zip))
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string relative=MDPro3.UltraInstallMigration.CanonicalArchivePath(entry.FullName);
            if(relative==null)continue;
            string path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new IOException("Invalid archive path.");
            if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(path); continue; }
            bool preserve = relative.Equals("Data/config.conf", StringComparison.OrdinalIgnoreCase)
                || relative.Equals("Data/Settings.json", StringComparison.OrdinalIgnoreCase)
                || relative.Equals("Data/mycard_apps.json", StringComparison.OrdinalIgnoreCase)
                || relative.Equals("Data/profile.json", StringComparison.OrdinalIgnoreCase)
                || relative.Equals("Data/friends-session.json", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("Deck/", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("Replay/", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("Progress/", StringComparison.OrdinalIgnoreCase);
            if (preserve && File.Exists(path)) continue;
            // Reinstalling or upgrading can reuse immutable runtime content.
            // This also avoids allocating a second copy of unchanged game assets.
            if(File.Exists(path) && new FileInfo(path).Length==entry.Length)
            {
                using(var content=entry.Open())
                using(var sha=SHA256.Create())
                    if(BitConverter.ToString(sha.ComputeHash(content)).Replace("-", "").ToLowerInvariant()==Hash(path))continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if(migration!=null)migration.PrepareReplacement(relative);
            string temporary=path+".ultra-install-"+Guid.NewGuid().ToString("N");
            try { entry.ExtractToFile(temporary);MDPro3.UltraInstallMigration.ReplaceFile(temporary,path); }
            finally { if(File.Exists(temporary))File.Delete(temporary); }
        }
    }
}
