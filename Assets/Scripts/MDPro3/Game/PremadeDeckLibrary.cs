using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;

namespace MDPro3
{
    public static class PremadeDeckLibrary
    {
        public sealed class Entry
        {
            public string id, archetype, format, label, file, source, description, reason, sha256;
            public int cap, points, mainCount, extraCount;
            public int[] covers;
            public bool available;
            public string DeckName => archetype + " - " + label;
            public string Summary => available
                ? mainCount + " Main · " + extraCount + " Extra" + (format == "Genesys" ? " · " + points + "/" + cap + " points" : "")
                : "No compatible starter recipe";
        }
        sealed class Catalog { public Entry[] entries; }
        static Entry[] entries;
        static readonly Dictionary<int, Banlist> previewLists = new();
        public static Entry Current { get; private set; }
        public static IReadOnlyList<Entry> Entries => entries ??= Load();
        static Entry[] Load()
        {
            const string path = "Data/premade-decks.json";
            if (!File.Exists(path)) throw new IOException("The premade deck library is missing. Reinstall its game data.");
            var catalog = JsonConvert.DeserializeObject<Catalog>(File.ReadAllText(path));
            if (catalog?.entries == null || catalog.entries.Length == 0) throw new IOException("The premade deck library is empty.");
            if (catalog.entries.Select(e => e.id).Distinct().Count() != catalog.entries.Length)
                throw new IOException("Duplicate premade deck identifiers.");
            return catalog.entries;
        }
        public static Entry Find(string id) => Entries.FirstOrDefault(e => e.id == id);
        static string SearchKey(string text) => new string((text ?? "").Normalize(NormalizationForm.FormKC)
            .Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        public static Entry[] Search(string query, string label) => Entries
            .Where(e => e.label == label && SearchKey(e.archetype).Contains(SearchKey(query)))
            .OrderBy(e => e.archetype, StringComparer.OrdinalIgnoreCase).ToArray();
        static string FilePath(Entry entry)
        {
            string root = Path.GetFullPath("Data/PremadeDecks") + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine("Data", entry.file ?? ""));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !path.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Invalid premade deck path.");
            return path;
        }
        public static Deck Read(Entry entry)
        {
            if (entry == null || !entry.available) throw new InvalidOperationException(entry?.reason ?? "Premade deck not found.");
            var bytes = File.ReadAllBytes(FilePath(entry));
            using var sha = SHA256.Create();
            string hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (hash != entry.sha256) throw new IOException("The premade deck file did not match its catalog: " + entry.archetype);
            return new Deck(Encoding.UTF8.GetString(bytes), "", "");
        }
        public static FormatDeckValidator.Result Validate(Entry entry, Deck deck)
        {
            bool genesys = entry.format == "Genesys";
            var ban = genesys ? BanlistManager.currentGenesysBanList : DuelFormat.TcgBanlist;
            if (ban == null) throw new InvalidOperationException("The format rules are not ready.");
            return FormatDeckValidator.Validate(deck.Main, deck.Extra, deck.Side, id => {
                var c = CardsManager.GetCardRaw(id);
                if (c == null) return null;
                return new FormatDeckValidator.CardEntry {
                    Id = id, Alias = c.Alias, Name = c.Name, Tcg = (c.Ot & 2) != 0,
                    Token = c.HasType(CardType.Token), Link = c.HasType(CardType.Link), Pendulum = c.HasType(CardType.Pendulum),
                    ExtraDeck = c.IsExtraCard(), Points = genesys ? ban.GetCredit(id) : 0, Limit = genesys ? 3 : ban.GetQuantity(id)
                };
            }, genesys, entry.cap);
        }
        public static Deck Prepare(Entry entry)
        {
            var deck = Read(entry);
            var validation = Validate(entry, deck);
            if (!validation.Valid) throw new InvalidOperationException("This recipe no longer meets the installed rules:\n" + string.Join("\n", validation.Errors.Take(6)));
            if (deck.Format != entry.format || deck.FormatPointCap != entry.cap || validation.Points != entry.points)
                throw new InvalidOperationException("The premade deck format or points do not match its catalog.");
            Current = entry;
            return deck;
        }
        public static Banlist PreviewBanlist
        {
            get
            {
                if (Current == null || Current.format == "TCG") return DuelFormat.TcgBanlist;
                if (previewLists.TryGetValue(Current.cap, out var existing)) return existing;
                var list = new Banlist { Name = "Genesys · " + Current.cap + " points" };
                list.AddCreditLimit("genesys", (uint)Current.cap);
                foreach (var card in BanlistManager.currentGenesysBanList.CardCredits)
                    foreach (var credit in card.Value) list.AddCardCredit(card.Key, credit.Key, credit.Value);
                previewLists[Current.cap] = list;
                return list;
            }
        }
        public static string SaveCopy(Entry entry)
        {
            Prepare(entry);
            string text = File.ReadAllText(FilePath(entry));
            string name = Deck.NormalizeDeckFileName(entry.DeckName);
            if (!Deck.IsValidDeckFileName(name)) throw new IOException("Invalid premade deck name.");
            Directory.CreateDirectory("Deck");
            string candidate = name;
            for (int suffix = 2; ; suffix++)
            {
                string path = Path.Combine("Deck", candidate + ".ydk");
                if (File.Exists(path))
                {
                    if (File.ReadAllText(path).TrimStart('\ufeff').Replace("\r\n", "\n") == text.Replace("\r\n", "\n")) return candidate;
                    candidate = name + " (" + suffix + ")";
                    continue;
                }
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                var bytes = new UTF8Encoding(false).GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
                return candidate;
            }
        }
    }
}
