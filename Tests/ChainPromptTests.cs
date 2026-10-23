using System;
using System.IO;
using YGOSharp.Network;
class ChainPromptTests
{
    static void Main()
    {
        int checks=0;
        foreach(bool native in new[]{true,false})foreach(int count in new[]{0,1,3})foreach(bool forced in new[]{true,false})
        {
            var bytes=new MemoryStream();var w=new BinaryWriter(bytes);
            w.Write((byte)1);w.Write((byte)count);w.Write((byte)count);
            if(native)w.Write((byte)(forced?1:0));
            w.Write(0x100);w.Write(0x200);
            for(int i=0;i<count;i++)
            {
                w.Write((byte)0);if(!native)w.Write((byte)(forced?1:0));
                w.Write(8240199+i);w.Write((byte)1);w.Write((byte)4);w.Write((byte)i);w.Write((byte)0);w.Write(8240199*16+i);
            }
            bytes.Position=0;var r=ChainPrompt.Read(new BinaryReader(bytes),native);
            if(r.Player!=1 || r.Entries.Count!=count || r.Hint0!=0x100 || r.Hint1!=0x200 || bytes.Position!=bytes.Length)throw new Exception("Chain header misread");
            for(int i=0;i<count;i++)if(r.Entries[i].Forced!=forced || r.Entries[i].Code!=8240199+i || r.Entries[i].Sequence!=i)throw new Exception("Chain candidate misread");
            checks++;
        }
        Console.WriteLine("PASS "+checks+" native/MDPro chain packets including forced Sage prompts, zero, one and multiple candidates.");
    }
}
