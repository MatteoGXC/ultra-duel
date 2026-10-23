using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

class NativeFormatSmokeTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr ScriptReader([MarshalAs(UnmanagedType.LPStr)] string name, out int length);
    [DllImport("ocgcore", CallingConvention=CallingConvention.Cdecl)] static extern void set_script_reader(ScriptReader reader);
    [DllImport("ocgcore", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr create_duel(UIntPtr seed);
    [DllImport("ocgcore", CallingConvention=CallingConvention.Cdecl)] static extern void end_duel(IntPtr duel);
    [DllImport("ocgcore", CallingConvention=CallingConvention.Cdecl)] static extern int preload_script(IntPtr duel, [MarshalAs(UnmanagedType.LPStr)] string script);
    [DllImport("ocgcore", CallingConvention=CallingConvention.Cdecl)] static extern uint ocg_format_capabilities();
    [DllImport("ygoserver", CallingConvention=CallingConvention.Cdecl)] static extern int get_server_format_capabilities();
    static ScriptReader reader = Read;
    static Dictionary<string,IntPtr> buffers = new Dictionary<string,IntPtr>();
    static Dictionary<string,int> sizes = new Dictionary<string,int>();
    static IntPtr Read(string name, out int length)
    {
        string key = name.Replace("\\", "/");
        if (buffers.ContainsKey(key)) { length=sizes[key]; return buffers[key]; }
        // These field-only checks use the registered engine API and no card scripts.
        length=0; return IntPtr.Zero;
    }
    static void Run(string name, string code)
    {
        byte[] data=Encoding.UTF8.GetBytes(code);
        IntPtr memory=Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data,0,memory,data.Length);
        buffers[name]=memory; sizes[name]=data.Length;
        IntPtr duel=create_duel(new UIntPtr(123u));
        try {
            if (preload_script(duel,name)==0) throw new Exception("Native assertion failed: "+name);
            Console.WriteLine("PASS "+name);
        } finally { end_duel(duel); buffers.Remove(name); sizes.Remove(name); Marshal.FreeHGlobal(memory); }
    }
    static int Main()
    {
        if (ocg_format_capabilities()!=3 || get_server_format_capabilities()!=3)
            throw new Exception("Native client/server format versions do not match.");
        set_script_reader(reader);
        Run("genesys-five-zones.lua", "Debug.ReloadFieldBegin(0x600,5)\nassert(Duel.GetLocationCountFromEx(0,0,nil,0x40)==5)\nassert(Duel.GetLocationCountFromEx(1,1,nil,0x2000)==5)\nassert(Duel.GetLocationCountFromEx(0,0,nil,0x800000)==5)");
        Run("tcg-extra-zones-preserved.lua", "Debug.ReloadFieldBegin(0x200,5)\nassert(Duel.GetLocationCountFromEx(0,0,nil,0x40)==6)");
        Run("original-engine-mode-preserved.lua", "Debug.ReloadFieldBegin(0,5)\nassert(Duel.GetLocationCountFromEx(1,1,nil,0x800000)==6)");
        Console.WriteLine("Native capability and field checks passed. Card-effect regression testing is still required.");
        return 0;
    }
}
