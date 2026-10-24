using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

public static class InstallerDownloadProbe
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static string Hash(string path){using(var s=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s));}
    public static void Main(string[] args){Run(args).GetAwaiter().GetResult();}
    static async Task Run(string[] args)
    {
        byte[] expected=File.ReadAllBytes(args[1]);string root=args[2];Directory.CreateDirectory(root);
        foreach(string route in new[]{"drop","ignore","resume"})
        {
            string file=Path.Combine(root,route+".download");
            if(route!="drop")File.WriteAllBytes(file,new ArraySegment<byte>(expected,0,131072).ToArray());
            await WindowsInstaller.DownloadPart(new WindowsInstaller.Part{url=args[0]+"/"+route,size=expected.Length},file,bytes=>{},CancellationToken.None);
            Require(Hash(file)==Hash(args[1]),route+" corrupted the resumed download");
        }
        string bad=Path.Combine(root,"bad.download");File.WriteAllBytes(bad,new byte[]{expected[0]});
        bool rejected=false;
        try{await WindowsInstaller.DownloadPart(new WindowsInstaller.Part{url=args[0]+"/bad",size=expected.Length},bad,bytes=>{},CancellationToken.None);}
        catch(IOException){rejected=true;}
        Require(rejected,"Invalid Content-Range was accepted");
        string paused=Path.Combine(root,"paused.download");
        using(var cancel=new CancellationTokenSource())
        {
            cancel.CancelAfter(180);
            try{await WindowsInstaller.DownloadPart(new WindowsInstaller.Part{url=args[0]+"/slow",size=expected.Length},paused,bytes=>{},cancel.Token);throw new Exception("Pause was ignored");}
            catch(OperationCanceledException){}
        }
        Require(File.Exists(paused) && new FileInfo(paused).Length>0 && new FileInfo(paused).Length<expected.Length,"Pause did not retain partial data");
        await WindowsInstaller.DownloadPart(new WindowsInstaller.Part{url=args[0]+"/resume",size=expected.Length},paused,bytes=>{},CancellationToken.None);
        Require(Hash(paused)==Hash(args[1]),"Pause/resume changed the file");
        Console.WriteLine("PASS: interrupted download, HTTP range resume, ignored range restart, invalid range rejection, pause/resume and exact hashes.");
    }
    static byte[] ToArray(this ArraySegment<byte> value){var output=new byte[value.Count];Array.Copy(value.Array,value.Offset,output,0,value.Count);return output;}
}
