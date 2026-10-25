using System;
using System.Collections.Generic;
using System.IO;
using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using System.Text;
using System.Linq;

namespace MDPro3.Duel.YGOSharp
{
    public class Deck
    {
        public List<int> Main;
        public List<int> Extra;
        public List<int> Side;
        public List<int> Pickup;
        public int Protector;
        public int Case;
        public int Field;
        public int Grave;
        public int Stand;
        public int Mate;

        public const string deckHint = "#created by Ultra Duel";
        public string Format = "TCG";
        public int FormatPointCap = 100;
        public string userId;
        public string deckId;

        public string type = string.Empty;

        public Deck()
        {
            Main = new List<int>();
            Extra = new List<int>();
            Side = new List<int>();
            Pickup = new List<int>() { 0, 0, 0 };
            Protector = 1070001;
            Case = 1080001;
            Field = 1090001;
            Grave = 1100001;
            Stand = 1110001;
            Mate = 1000001;
        }

        public Deck(string path) : this(File.ReadAllText(path), string.Empty, string.Empty)
        {
            type = Path.GetFileName(Path.GetDirectoryName(path));
            if(type == "Deck" && type == Path.GetDirectoryName(path))
                type = string.Empty;
        }

        public Deck(string ydk, string deckID = "", string userID = "")
        {
            deckId = deckID;
            userId = userID;

            Main = new List<int>();
            Extra = new List<int>();
            Side = new List<int>();
            Pickup = new List<int>();
            Protector = 1070001;
            Case = 1080001;
            Field = 1090001;
            Grave = 1100001;
            Stand = 1110001;
            Mate = 1000001;
            string st = ydk.Replace("\r", string.Empty).Replace("\\r", string.Empty).Replace("\\n", "\n");
            string[] lines = st.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            int flag = -1;
            bool hasUltraFormat = false;

            foreach (string line in lines)
            {
                if (line.StartsWith("###") && userId == string.Empty)
                {
                    userId = line.Replace("###", string.Empty);
                    continue;
                }
                if (line.StartsWith("##") && deckId == string.Empty)
                {
                    deckId = line.Replace("##", string.Empty);
                    continue;
                }

                if (DeckFormatMetadata.IsMetadata(line))
                {
                    bool legacy = line.StartsWith(DeckFormatMetadata.LegacyPrefix, StringComparison.OrdinalIgnoreCase);
                    if (legacy && hasUltraFormat) continue;
                    if (!legacy) hasUltraFormat = true;
                    var profile = line.Split(':');
                    Format = profile.Length > 1 && profile[1] == "Genesys" ? "Genesys" : "TCG";
                    if (profile.Length > 2 && int.TryParse(profile[2], out int cap) && cap >= 0)
                        FormatPointCap = cap;
                }
                else if (line.StartsWith("#main"))
                    flag = 1;
                else if (line.StartsWith( "#extra"))
                    flag = 2;
                else if (line.StartsWith("!side"))
                    flag = 3;
                else if (line.StartsWith("#pickup"))
                    flag = 4;
                else if (line.StartsWith("#protector"))
                    flag = 5;
                else if (line.StartsWith("#case"))
                    flag = 6;
                else if (line.StartsWith("#field"))
                    flag = 7;
                else if (line.StartsWith("#grave"))
                    flag = 8;
                else if (line.StartsWith("#stand"))
                    flag = 9;
                else if (line.StartsWith("#mate"))
                    flag = 10;
                else
                {
                    int code = 0;
                    try
                    {
                        code = int.Parse(line.Replace("#", ""));
                    }
                    catch
                    {
                        continue;
                    }

                    if (code > 100)
                    {
                        // Replace pre-release ids with official ids only for playable deck sections.
                        if (flag >= 1 && flag <= 3)
                            code = YdkIdHelper.ToOfficial(code);

                        switch (flag)
                        {
                            case 1:
                                Main.Add(code);
                                break;
                            case 2:
                                Extra.Add(code);
                                break;
                            case 3:
                                Side.Add(code);
                                break;
                            case 4:
                                Pickup.Add(code);
                                break;
                            case 5:
                                Protector = code;
                                break;
                            case 6:
                                Case = code;
                                break;
                            case 7:
                                Field = code;
                                break;
                            case 8:
                                Grave = code;
                                break;
                            case 9:
                                Stand = code;
                                break;
                            case 10:
                                Mate = code;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            if (Pickup.Count < 3)
                Pickup.AddRange(Enumerable.Repeat(0, 3 - Pickup.Count));
        }

        public Deck(List<int> main, List<int> extra, List<int> side)
        {
            Main = main;
            Extra = extra;
            Side = side;
            Pickup = new List<int>() { 0, 0, 0 };
            Protector = 1070001;
            Case = 1080001;
            Field = 1090001;
            Grave = 1100001;
            Stand = 1110001;
            Mate = 1000001;

            // Normalize any pre-release ids that may be in the provided lists.
            YdkIdHelper.NormalizeDeck(this);
        }

        public int Check(Banlist ban, bool ocg, bool tcg)
        {
            if (Main.Count < 40 ||
                Main.Count > 60 ||
                Extra.Count > 15 ||
                Side.Count > 15)
                return 1;

            Dictionary<int, int> cards = new Dictionary<int, int>();

            List<int>[] stacks = { Main, Extra, Side };
            foreach (List<int> stack in stacks)
            {
                foreach (int id in stack)
                {
                    Card card = CardsManager.Get(id);
                    AddToCards(cards, card);
                    if (!ocg && card.Ot == 1 || !tcg && card.Ot == 2)
                        return id;
                    if (card.HasType(CardType.Token))
                        return id;
                }
            }

            if (ban == null)
                return 0;

            foreach (var pair in cards)
            {
                int max = ban.GetQuantity(pair.Key);
                if (pair.Value > max)
                    return pair.Key;
            }
            return 0;
        }

        public int GetCardCount(int code)
        {
            int al = 0;
            try
            {
                al = CardsManager.Get(code).Alias;
            }
            catch (Exception)
            {
            }
            int returnValue = 0;
            return returnValue;
        }

        public bool Check(Deck deck)
        {
            if (deck.Main.Count != Main.Count || deck.Extra.Count != Extra.Count)
                return false;

            Dictionary<int, int> cards = new Dictionary<int, int>();
            Dictionary<int, int> ncards = new Dictionary<int, int>();
            List<int>[] stacks = { Main, Extra, Side };
            foreach (IList<int> stack in stacks)
            {
                foreach (int id in stack)
                {
                    if (!cards.ContainsKey(id))
                        cards.Add(id, 1);
                    else
                        cards[id]++;
                }
            }
            stacks = new[] { deck.Main, deck.Extra, deck.Side };
            foreach (var stack in stacks)
            {
                foreach (int id in stack)
                {
                    if (!ncards.ContainsKey(id))
                        ncards.Add(id, 1);
                    else
                        ncards[id]++;
                }
            }
            foreach (var pair in cards)
            {
                if (!ncards.ContainsKey(pair.Key))
                    return false;
                if (ncards[pair.Key] != pair.Value)
                    return false;
            }
            return true;
        }

        private static void AddToCards(Dictionary<int, int> cards, Card card)
        {
            int id = card.Id;
            if (card.Alias != 0)
                id = card.Alias;
            if (cards.ContainsKey(id))
                cards[id]++;
            else
                cards.Add(id, 1);
        }

        /// <summary>
        /// 直接保存即可，不需要考虑deckid，deckid会自动添加
        /// </summary>
        /// <param name="deckName"></param>
        /// <param name="saveTime"></param>
        /// <param name="upload"></param>
        /// <returns></returns>
        public bool Save(string deckName, DateTime saveTime, bool upload = true, bool showHint = true)
        {
            var ydk = GetYDK();
            try
            {
                deckName = NormalizeDeckFileName(deckName);
                if (!IsValidDeckFileName(deckName))
                    return false;

                var path = Program.PATH_DECK + (type == string.Empty ? string.Empty : $"{type}/") + deckName + Program.EXPANSION_YDK;
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, ydk, Encoding.UTF8);
                File.SetLastWriteTimeUtc(path, saveTime);

                if (MyCard.account != null && upload)
                    _ = OnlineDeck.SyncDeck(deckId, deckName, this, saveTime, showHint);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string NormalizeDeckFileName(string deckName)
        {
            deckName = deckName?.Trim() ?? string.Empty;
            if (deckName.EndsWith(Program.EXPANSION_YDK, StringComparison.OrdinalIgnoreCase))
                deckName = deckName[..^Program.EXPANSION_YDK.Length];
            return deckName;
        }

        public static bool IsValidDeckFileName(string deckName)
        {
            if (string.IsNullOrWhiteSpace(deckName))
                return false;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (deckName.EndsWith(" ", StringComparison.Ordinal) || deckName.EndsWith(".", StringComparison.Ordinal))
                return false;
#endif
            return deckName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        public string GetYDK()
        {
            var value = deckHint + "\r\n" + DeckFormatMetadata.Write(Format, FormatPointCap) + "\r\n#main";
            for (var i = 0; i < Main.Count; i++)
                value += $"\r\n{Main[i]}";
            value += "\r\n#extra";
            for (var i = 0; i < Extra.Count; i++)
                value += $"\r\n{Extra[i]}";
            value += "\r\n!side";
            for (var i = 0; i < Side.Count; i++)
                value += $"\r\n{Side[i]}";
            value += $"\r\n#pickup\r\n#{Pickup[0]}";
            value += $"\r\n#{Pickup[1]}";
            value += $"\r\n#{Pickup[2]}";

            value += $"\r\n#case\r\n#{Case}";
            value += $"\r\n#protector\r\n#{Protector}";
            value += $"\r\n#field\r\n#{Field}";
            value += $"\r\n#grave\r\n#{Grave}";
            value += $"\r\n#stand\r\n#{Stand}";
            value += $"\r\n#mate\r\n#{Mate}";

            if (!string.IsNullOrEmpty(deckId))
                value += $"\r\n##{deckId}";
            if (!string.IsNullOrEmpty(userId))
                value += $"\r\n###{userId}";

            return value;
        }
    }
}
