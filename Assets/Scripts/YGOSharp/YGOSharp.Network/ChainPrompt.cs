using System;
using System.Collections.Generic;
using System.IO;

namespace YGOSharp.Network
{
    // Legacy ocgcore has one forced byte in the header. MDPro protocol has a
    // forced byte per candidate instead. Count=1 has equal packet lengths, so
    // that case must use the known server protocol rather than guessing.
    public sealed class ChainPrompt
    {
        public sealed class Entry
        {
            public byte Flag, Controller, Location, Sequence, Subsequence;
            public int Code, Description;
            public bool Forced;
        }
        public byte Player, SpecialCount;
        public int Hint0, Hint1;
        public readonly List<Entry> Entries=new List<Entry>();
        public static ChainPrompt Read(BinaryReader reader,bool nativeProtocol)
        {
            var result=new ChainPrompt();
            result.Player=reader.ReadByte();int count=reader.ReadByte();result.SpecialCount=reader.ReadByte();
            long remaining=reader.BaseStream.Length-reader.BaseStream.Position;
            bool legacy=remaining==9+13*count && (count!=1 || nativeProtocol);
            if(!legacy && remaining!=8+14*count)throw new InvalidDataException("Invalid chain prompt length.");
            bool forced=legacy && reader.ReadByte()!=0;
            result.Hint0=reader.ReadInt32();result.Hint1=reader.ReadInt32();
            for(int i=0;i<count;i++)
            {
                var e=new Entry{Flag=reader.ReadByte()};
                e.Forced=legacy?forced:reader.ReadByte()!=0;
                e.Code=reader.ReadInt32();e.Controller=reader.ReadByte();e.Location=reader.ReadByte();
                e.Sequence=reader.ReadByte();e.Subsequence=reader.ReadByte();e.Description=reader.ReadInt32();
                result.Entries.Add(e);
            }
            return result;
        }
    }
}
