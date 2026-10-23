using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using UnityEngine.EventSystems;

namespace MDPro3
{
    public static class DuelFormat
    {
        public static bool Genesys { get { return Config.Get("DuelFormat", "TCG") == "Genesys"; } }
        public static int PointCap
        {
            get
            {
                int cap;
                return int.TryParse(Config.Get("GenesysPointCap", "100"), out cap) && cap >= 0 ? cap : 100;
            }
        }
        public static string DisplayName { get { return Genesys ? "Genesys · " + PointCap + " points" : "TCG Advanced"; } }
        public static void ApplyDeckFormat(string deckName)
        {
            string path = Program.PATH_DECK + deckName + Program.EXPANSION_YDK;
            if (!File.Exists(path)) return;
            var deck = new Deck(path);
            Set(deck.Format == "Genesys", deck.FormatPointCap);
        }

        public static void SaveDeckFormat(string deckName)
        {
            string path = Program.PATH_DECK + deckName + Program.EXPANSION_YDK;
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Where(line => !line.StartsWith("#matteo-format:")).ToList();
            lines.Insert(0, "#matteo-format:" + (Genesys ? "Genesys" : "TCG") + ":" + PointCap);
            File.WriteAllLines(path, lines);
        }
        // Private local-server format IDs; translated to MR2020 plus rule flags in native code.
        public static string MasterRule { get { return Genesys ? "8" : "7"; } }
        public static bool LocalSession { get { return !RoomServant.FromHandTest && (RoomServant.FromSolo || RoomServant.FromLocalHost || RoomServant.FromUltraHost); } }

        public static Banlist TcgBanlist
        {
            get
            {
                return BanlistManager.Banlists.FirstOrDefault(b => !b.isCredit && b.Name.StartsWith("TCG Advanced ·"))
                    ?? BanlistManager.Banlists.FirstOrDefault(b => !b.isCredit && b.Name.IndexOf("TCG", StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        public static void Select(Action next, string deckName = null)
        {
            UIManager.ShowPopupSelection(new List<string> {
                deckName == null ? "Duel format" : "Deck format",
                deckName == null ? "Choose the rules for this local duel. Current format — " + DisplayName
                    : "Save the format for " + deckName + ". Current format — " + DisplayName,
                "TCG Advanced", "Genesys · 100 points", "Genesys · 0 points", "Genesys · custom point cap"
            }, () => {
                var selected = EventSystem.current.currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
                if (selected == "Genesys · custom point cap")
                {
                    UIManager.ShowPopupInput(new List<string> {
                        "Genesys point cap — enter a whole number, zero or greater", PointCap.ToString()
                    }, value => {
                        int cap;
                        if (!int.TryParse(value, out cap) || cap < 0)
                        {
                            UIManager.ShowPopupConfirm(new List<string> { "Invalid point cap", "Enter a whole number, zero or greater." });
                            return;
                        }
                        Set(true, cap);
                        next();
                    }, null);
                    return;
                }
                Set(selected != "TCG Advanced", selected == "Genesys · 0 points" ? 0 : 100);
                next();
            });
        }

        public static void Set(bool genesys, int cap)
        {
            Config.Set("DuelFormat", genesys ? "Genesys" : "TCG");
            Config.Set("GenesysPointCap", cap.ToString());
            if (genesys && BanlistManager.currentGenesysBanList != null)
            {
                var list = BanlistManager.currentGenesysBanList;
                if (list.CreditLimits != null) list.CreditLimits["genesys"] = (uint)cap;
                list.Name = "Genesys · Official TCG · " + cap + " points";
            }
            if (!genesys && TcgBanlist != null) BanlistManager.currentBanList = TcgBanlist;
        }

        public static bool Validate(Deck deck, string owner)
        {
            var ban = Genesys ? BanlistManager.currentGenesysBanList : TcgBanlist;
            if (ban == null || (Genesys && (!ban.isCredit || ban.CardCredits == null || ban.CardCredits.Count == 0)))
            {
                UIManager.ShowPopupConfirm(new List<string> { "Rules data unavailable", "The " + DisplayName + " card list is missing. Refresh the rules data before starting a duel." });
                return false;
            }
            var result = InspectDeck(deck, ban);
            if (result.Valid) return true;
            UIManager.ShowPopupConfirm(new List<string> {
                owner + " — " + DisplayName,
                string.Join("\n", result.Errors.Distinct().Take(8)) + (result.Errors.Count > 8 ? "\nMore issues are present in this Deck." : string.Empty)
            });
            return false;
        }

        private static FormatDeckValidator.Result InspectDeck(Deck deck, Banlist ban)
        {
            return FormatDeckValidator.Validate(deck.Main, deck.Extra, deck.Side, id => {
                // Exact lookup: an unknown ID must never silently resolve to a neighbouring card.
                var card = CardsManager.GetCardRaw(id);
                if (card == null) return null;
                return new FormatDeckValidator.CardEntry {
                    Id = id, Alias = card.Alias, Name = card.Name,
                    Tcg = (card.Ot & 2) != 0, Token = card.HasType(CardType.Token),
                    Link = card.HasType(CardType.Link), Pendulum = card.HasType(CardType.Pendulum),
                    ExtraDeck = card.IsExtraCard(), Points = Genesys ? ban.GetCredit(id) : 0,
                    Limit = Genesys ? 3 : ban.GetQuantity(id)
                };
            }, Genesys, PointCap);
        }

        public static string SoloBotExecutor(string command)
        {
            var token = Tools.SplitWithPreservedQuotes(command.Replace("'", "\""))
                .FirstOrDefault(t => t.StartsWith("Deck=", StringComparison.Ordinal));
            return token == null ? null : token.Substring(5).Trim('"');
        }

        [Serializable]
        public class PublishedSoloDeck
        {
            public string name, executor, difficulty, mode, source, title, file;
            public int points;
        }
        [Serializable]
        private class PublishedSoloCatalog { public PublishedSoloDeck[] decks; }

        public static IEnumerable<PublishedSoloDeck> AvailableSoloDecks()
        {
            const string catalogPath = "Data/solo-published-decks.json";
            if (!File.Exists(catalogPath)) yield break;
            var catalog = UnityEngine.JsonUtility.FromJson<PublishedSoloCatalog>(File.ReadAllText(catalogPath));
            var ban = Genesys ? BanlistManager.currentGenesysBanList : TcgBanlist;
            if (catalog?.decks == null || ban == null) yield break;
            foreach (var row in catalog.decks)
            {
                if (row.mode != (Genesys ? "Genesys" : "TCG")) continue;
                string path = Path.Combine("Data", row.file);
                if (File.Exists(path) && InspectDeck(new Deck(path), ban).Valid) yield return row;
            }
        }

        public static string PublishedSoloCommand(PublishedSoloDeck row)
        {
            return "Deck=\"" + row.executor + "\" Name=\"" + row.name + "\" Dialog=Universal.EN DeckFile=\""
                + Path.GetFullPath(Path.Combine("Data", row.file)) + "\"";
        }

        public static bool PrepareSoloBot(string command, out string prepared)
        {
            prepared = command;
            if (command.StartsWith("Random=")) return true;
            string name = SoloBotExecutor(command);
            if (name == null) return false;
            var row = AvailableSoloDecks().FirstOrDefault(deck => deck.executor == name);
            if (row == null) return false;
            prepared += " DeckFile=\"" + Path.GetFullPath(Path.Combine("Data", row.file)) + "\"";
            return true;
        }

        public static bool ValidateSelectedDeck()
        {
            var path = Program.PATH_DECK + Config.GetConfigDeckName() + Program.EXPANSION_YDK;
            if (!File.Exists(path))
            {
                MessageManager.Toast("Select a Deck before starting a duel.");
                return false;
            }
            return Validate(new Deck(path), "Your Deck");
        }

        public static bool RequireNativeRules()
        {
            if (Net.YgoServer.SupportsTcgAndGenesys()) return true;
            UIManager.ShowPopupConfirm(new List<string> {
                "Rules engine needs rebuilding",
                "This build requires the matching TCG/Genesys ocgcore.dll and ygoserver.dll. The original MDPro3 engine cannot enforce these formats."
            });
            return false;
        }

        public static bool ValidateBotCommand(string command)
        {
            var tokens = Tools.SplitWithPreservedQuotes(command.Replace("'", "\""));
            var custom = tokens.LastOrDefault(t => t.StartsWith("DeckFile=", StringComparison.Ordinal));
            if (custom != null)
            {
                var customName = custom.Substring(9).Trim('"');
                var customPath = Path.IsPathRooted(customName) && customName.EndsWith(Program.EXPANSION_YDK, StringComparison.OrdinalIgnoreCase)
                    ? customName : Program.PATH_DECK + customName + Program.EXPANSION_YDK;
                if (!File.Exists(customPath))
                {
                    MessageManager.Toast("AI Deck unavailable — " + customName);
                    return false;
                }
                return Validate(new Deck(customPath), "AI Deck");
            }
            var token = tokens.FirstOrDefault(t => t.StartsWith("Deck=", StringComparison.Ordinal));
            if (token == null)
            {
                MessageManager.Toast("This AI does not specify a Deck; select another opponent.");
                return false;
            }
            var name = token.Substring(5).Trim('"');
            // WindBot accepts either a custom .ydk path or an internal Deck name.
            var path = name.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase)
                ? name : "Data/Windbot/Decks/AI_" + name + ".ydk";
            if (!File.Exists(path))
            {
                MessageManager.Toast("AI Deck unavailable — " + name);
                return false;
            }
            return Validate(new Deck(path), "AI Deck");
        }
    }
}
