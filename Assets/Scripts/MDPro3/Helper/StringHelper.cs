using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;

namespace MDPro3
{
    public static class StringHelper
    {
        private class HashedString
        {
            public string content = string.Empty;
            public int hashCode;
            public string region = string.Empty;
        }

        private const string PATH_CONF_FILE = "/strings.conf";

        private static readonly List<HashedString> hashedStrings = new();
        private static readonly List<HashedString> hashedStringsForRender = new();
        private static readonly List<HashedString> hashedStringsForPrerelease = new();
        private static readonly List<HashedString> setNames = new();
        private static readonly HashSet<uint> missingDescriptions = new();

        public static void Initialize()
        {
            hashedStrings.Clear();
            hashedStringsForRender.Clear();
            hashedStringsForPrerelease.Clear();
            missingDescriptions.Clear();

            var text = File.ReadAllText(Program.PATH_LOCALES + Language.GetConfig() + PATH_CONF_FILE);
            AddExpansionStrings(ref text);
            InitializeContent(text, 0);

            text = File.ReadAllText(Program.PATH_LOCALES + Language.GetCardConfig() + PATH_CONF_FILE);
            AddExpansionStrings(ref text);
            InitializeContent(text, 1);

            text = File.ReadAllText(Program.PATH_LOCALES + Language.GetPrereleaseConfig() + PATH_CONF_FILE);
            AddExpansionStrings(ref text);
            InitializeContent(text, 2);
        }

        private static void AddExpansionStrings(ref string text)
        {
            if (Config.GetBool("Expansions", true))
            {
                foreach (var conf in Directory.GetFiles(Program.PATH_EXPANSIONS, "*.conf"))
                    if (!conf.ToLower().EndsWith(BanlistManager.FILE_NAME))
                        text += Program.STRING_LINE_BREAK + File.ReadAllText(conf);
                foreach (var zip in ZipHelper.zips)
                {
                    if (zip.Name.ToLower().EndsWith(Program.SCRIPT_ZIP))
                        continue;
                    foreach (var file in zip.EntryFileNames)
                    {
                        if (file.ToLower().EndsWith(Program.EXPANSION_CONF)
                            && !file.ToLower().EndsWith(BanlistManager.FILE_NAME))
                        {
                            var ms = new MemoryStream();
                            var e = zip[file];
                            e.Extract(ms);
                            text += Program.STRING_LINE_BREAK + Encoding.UTF8.GetString(ms.ToArray());
                        }
                    }
                }
            }
        }

        private static void InitializeContent(string text, int type)
        {
            var st = text.Replace("\r", string.Empty);
            var lines = st.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var targetStrings = GetHashedStrings(type);
            targetStrings.Clear();
            if (type == 0)
                setNames.Clear();
            foreach (var line in lines)
                if (line.Length > 1 && line[..1] == "!")
                {
                    var mats = line[1..].Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (mats.Length > 2)
                    {
                        var a = new HashedString
                        {
                            region = mats[0]
                        };
                        try
                        {
                            a.hashCode = StringToInt(mats[1]);
                        }
                        catch (Exception e)
                        {
                            MessageManager.Cast(e.ToString());
                        }

                        a.content = string.Empty;
                        for (var i = 2; i < mats.Length; i++) a.content += mats[i] + " ";
                        a.content = a.content[..^1];
                        if (Get(a.region, a.hashCode, type) == string.Empty)
                        {
                            targetStrings.Add(a);
                            if (a.region == "setname" && type == 0)
                                setNames.Add(a);
                        }
                    }
                }
        }

        private static int StringToInt(string str)
        {
            var return_value = 0;
            try
            {
                if (str.Length > 2 && str[..2] == "0x")
                    return_value = Convert.ToInt32(str, 16);
                else
                    return_value = int.Parse(str);
            }
            catch { }
            return return_value;
        }

        private static List<HashedString> GetHashedStrings(int type)
        {
            return type switch
            {
                1 => hashedStringsForRender,
                2 => hashedStringsForPrerelease,
                _ => hashedStrings,
            };
        }

        internal static string Get(string region, int hashCode, int type = 0)
        {
            var re = string.Empty;
            foreach (var s in GetHashedStrings(type))
                if (s.region == region && s.hashCode == hashCode)
                {
                    re = s.content;
                    break;
                }
            return re;
        }

        internal static string GetUnsafe(int hashCode, int type = 0)
        {
            var re = string.Empty;
            foreach (var s in GetHashedStrings(type))
                if (s.region == "system" && s.hashCode == hashCode)
                {
                    re = s.content;
                    break;
                }
            return re;
        }

        internal static string Get(int description)
        {
            var a = string.Empty;
            uint packed=unchecked((uint)description);
            Card card=null;
            int index=0;
            if (packed < 10000)
            {
                a = Get("system", description);
            }
            else
            {
                var code = (int)(packed >> 4);
                index = (int)(packed & 0xf);
                card = CardsManager.GetCardRaw(code);
                if(card?.Str!=null && index<card.Str.Length)a=card.Str[index];
            }

            if (string.IsNullOrWhiteSpace(a))
            {
                // Unknown/custom descriptions must not throw another popup on
                // top of the active choice, or pretend to describe an action.
                if(missingDescriptions.Add(packed))Debug.LogWarning("Missing effect description: "+packed);
                a=card==null?InterString.Get("发动效果"):card.Name+" — "+InterString.Get("效果选择")+" "+(index+1);
            }
            return a;
        }

        internal static string FormatLocation(uint location, uint sequence)
        {
            if (location == 0x8)
            {
                if (sequence < 5)
                    return Get(1003);
                if (sequence == 5)
                    return Get(1008);
                return Get(1009);
            }

            uint filter = 1;
            var i = 1000;
            for (; filter != 0x100 && filter != location; filter <<= 1)
                ++i;
            if (filter == location)
                return Get(i);
            return "???";
        }

        internal static string FormatLocation(GPS gps)
        {
            return FormatLocation(gps.location, gps.sequence);
        }

        internal static string Attribute(long attribute, int type = 0)
        {
            var r = string.Empty;
            var passFirst = false;
            for (int i = 0; i < 7; i++)
                if ((attribute & (1u << i)) > 0)
                {
                    if (passFirst) r += Program.STRING_SLASH;
                    r += GetUnsafe(1010 + i, type);
                    passFirst = true;
                }
            return r;
        }

        internal static string Race(long race, int type = 0)
        {
            var r = string.Empty;
            var passFirst = false;
            for (var i = 0; i < 26; i++)
                if ((race & (1 << i)) > 0)
                {
                    if (passFirst) r += Program.STRING_SLASH;
                    r += GetUnsafe(1020 + i, type);
                    passFirst = true;
                }
            return r;
        }

        internal static string MainType(long cardType, int type = 0)
        {
            var r = string.Empty;
            var passFirst = false;
            for (var i = 0; i < 3; i++)
                if ((cardType & (1 << i)) > 0)
                {
                    if (passFirst) r += Program.STRING_SLASH;
                    r += GetUnsafe(1050 + i, type);
                    passFirst = true;
                }
            return r;
        }

        internal static string SecondType(long a, int type = 0)
        {
            var start = string.Empty;
            var end = string.Empty;
            if ((a & 0x68020C0) > 0)
            {
                for (var i = 4; i < 27; i++)
                    if (((a & 0x68020C0) & (1 << i)) > 0)
                    {
                        if (type == 0 && i == 25 && !Language.NeedSpSummonString(Language.GetConfig()))
                            continue;
                        if (type == 1 && i == 25 && !Language.NeedSpSummonString(Language.GetCardConfig()))
                            continue;
                        if (type == 2 && i == 25 && !Language.NeedSpSummonString(Language.GetPrereleaseConfig()))
                            continue;

                        start += Program.STRING_SLASH + GetUnsafe(1050 + i, type);
                        break;
                    }
                a -= a & 0x68020C0;
            }
            if ((a & (long)CardType.Pendulum) > 0)
            {
                start += Program.STRING_SLASH + GetUnsafe(1074, type);
                a -= (long)CardType.Pendulum;
            }
            if ((a & 0x30) > 0)
            {
                for (var i = 4; i < 6; i++)
                    if ((a & (1 << i)) > 0)
                    {
                        end += Program.STRING_SLASH + GetUnsafe(1050 + i, type);
                        break;
                    }
                a -= a & 0x30;
            }
            for (var i = 4; i < 27; i++)
                if ((a & (1 << i)) > 0)
                    start += Program.STRING_SLASH + GetUnsafe(1050 + i, type);
            var returnValue = start + end;
            if (returnValue == string.Empty)
                returnValue = GetUnsafe(1054, type);
            else
                returnValue = returnValue[1..];

            return returnValue;
        }

        internal static string Zone(long data)
        {
            var strs = new List<string>();
            for (var filter = 0x1L; filter <= 0x1L << 32; filter <<= 1)
            {
                var str = string.Empty;
                var s = filter & data;
                if (s != 0)
                {
                    if ((s & 0x60) != 0)
                    {
                        str += GetUnsafe(1081);
                        data &= ~0x600000;
                    }
                    else if ((s & 0xffff) != 0)
                    {
                        str += GetUnsafe(102);
                    }
                    else if ((s & 0xffff0000) != 0)
                    {
                        str += GetUnsafe(103);
                        s >>= 16;
                    }

                    if ((s & 0x1f) != 0)
                    {
                        str += GetUnsafe(1002);
                    }
                    else if ((s & 0xff00) != 0)
                    {
                        s >>= 8;
                        if ((s & 0x1f) != 0)
                            str += GetUnsafe(1003);
                        else if ((s & 0x20) != 0)
                            str += GetUnsafe(1008);
                        else if ((s & 0xc0) != 0)
                            str += GetUnsafe(1009);
                    }

                    var seq = 1;
                    for (var i = 0x1; i < 0x100; i <<= 1)
                    {
                        if ((s & i) != 0)
                            break;
                        ++seq;
                    }

                    str += "(" + seq + ")";
                    strs.Add(str);
                }
            }

            return string.Join(", ", strs.ToArray());

        }

        internal static string GetSetName(long Setcode)
        {
            var setcodes = new int[4];
            for (var j = 0; j < 4; j++)
            {
                setcodes[j] = (int)((Setcode >> j * 16) & 0xffff);
                // Unnamed subfamilies still belong to their named parent archetype.
                if (setcodes[j] != 0 && !setNames.Exists(n => n.hashCode == setcodes[j]))
                    setcodes[j] &= 0x0fff;
            }
            var returnValue = new List<string>();
            for (var i = 0; i < setNames.Count; i++)
            {
                var currentHash = setNames[i].hashCode;
                for (var j = 0; j < 4; j++)
                {
                    if (currentHash == setcodes[j])
                    {
                        var setArray = setNames[i].content.Split('\t');
                        var setString = setArray[0];
                        returnValue.Add(setString);
                    }
                }
            }
            if (returnValue.Count > 0)
                return string.Join("|", returnValue.ToArray());
            else
                return string.Empty;
        }

        internal static int GetSetNameCode(string setName)
        {
            int returnValue = 0;
            for (var i = 0; i < setNames.Count; i++)
            {
                var setArray = setNames[i].content.Split('\t');
                var setString = setArray[0];
                if (setName == setString)
                    returnValue = setNames[i].hashCode;
            }
            return returnValue;
        }

        public static string GetType(Card data, bool render = false, bool rushDuel = false)
        {
            var re = string.Empty;
            if (data.Id == 0)
                return re;

            var bracketLeft = "【";
            var bracketRight = "】";
            if (render && Language.NeedSmallBracket(data.isPre ? Language.GetPrereleaseConfig() : Language.GetCardConfig()))
            {
                bracketLeft = "[";
                bracketRight = "]";
            }

            var origin = render ? CardsManager.GetRenderCard(data.Id) : CardsManager.Get(data.Id);
            try
            {
                if (CardDescription.CardIsMonster(data))
                {
                    if (data.Race != origin.Race)
                        re = bracketLeft + "<color=#FD3E08>" + InterString.Get("[?]族", Race(data.Race)) + "</color>" + Program.STRING_SLASH + SecondType(data.Type) + bracketRight;
                    else
                        re = bracketLeft + InterString.Get("[?]族", Race(data.Race, 2), 2) + Program.STRING_SLASH + SecondType(data.Type, 2) + bracketRight;
                }
                else
                {
                    if (rushDuel)
                    {
                        int translationType = 0;
                        if (render)
                        {
                            translationType = 1;
                            if (data.isPre)
                                translationType = 2;
                        }

                        re = bracketLeft;
                        if (data.HasType(CardType.Spell))
                            re += InterString.Get("魔法卡", translationType);
                        else
                            re += InterString.Get("陷阱卡", translationType);

                        re = re.Replace("SPELL CARD", "Spell Card")
                                    .Replace("TRAP CARD", "Trap Card")
                                    .Replace("CARTA MÁGICA", "Carta Mágica")
                                    .Replace("CARTA TRAMPA", "Carta Trampa");

                        var secondType = SecondType(data.Type, 2);
                        if (secondType != GetUnsafe(1054, 2))
                        {
                            re += Program.STRING_SLASH + secondType;

                            if (data.HasType(CardType.Equip))
                                re += "<Sprite=0>";
                            if (data.HasType(CardType.QuickPlay))
                                re += "<Sprite=1>";
                            if (data.HasType(CardType.Field))
                                re += "<Sprite=2>";
                            if (data.HasType(CardType.Ritual))
                                re += "<Sprite=3>";
                            if (data.HasType(CardType.Continuous))
                                re += "<Sprite=4>";
                            if (data.HasType(CardType.Counter))
                                re += "<Sprite=5>";
                        }
                        re += bracketRight;
                    }
                    else
                    {
                        if (render)
                        {
                            int translationType = 1;
                            if (data.isPre)
                                translationType = 2;

                            re = bracketLeft;
                            if (data.HasType(CardType.Spell))
                                re += InterString.Get("魔法卡", translationType);
                            else
                                re += InterString.Get("陷阱卡", translationType);

                            var secondType = SecondType(data.Type, 2);
                            if (secondType != GetUnsafe(1054, 2))
                            {
                                if (data.HasType(CardType.Equip))
                                    re += "<Sprite=0>";
                                if (data.HasType(CardType.QuickPlay))
                                    re += "<Sprite=1>";
                                if (data.HasType(CardType.Field))
                                    re += "<Sprite=2>";
                                if (data.HasType(CardType.Ritual))
                                    re += "<Sprite=3>";
                                if (data.HasType(CardType.Continuous))
                                    re += "<Sprite=4>";
                                if (data.HasType(CardType.Counter))
                                    re += "<Sprite=5>";
                            }
                            re += bracketRight;
                        }
                        else
                            re = bracketLeft + MainType(data.Type, 2) + bracketRight;
                    }
                }
            }
            catch (Exception e)
            { Debug.LogError(e); }
            return re;
        }


    }
}
