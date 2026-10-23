using System;
using System.Collections.Generic;

namespace MDPro3.Duel.YGOSharp
{
    // Kept independent of Unity so tournament deck construction can be tested directly.
    public static class FormatDeckValidator
    {
        public sealed class CardEntry
        {
            public int Id;
            public int Alias;
            public string Name;
            public bool Tcg;
            public bool Token;
            public bool Link;
            public bool Pendulum;
            public bool ExtraDeck;
            public int Points;
            public int Limit = 3;
        }

        public sealed class Result
        {
            public readonly List<string> Errors = new List<string>();
            public long Points;
            public bool Valid { get { return Errors.Count == 0; } }
        }

        public static Result Validate(IList<int> main, IList<int> extra, IList<int> side,
            Func<int, CardEntry> resolve, bool genesys, int pointCap)
        {
            var result = new Result();
            if (pointCap < 0) result.Errors.Add("The point cap cannot be negative.");
            if (main.Count < 40 || main.Count > 60)
                result.Errors.Add("Main Deck must contain 40–60 cards.");
            if (extra.Count > 15) result.Errors.Add("Extra Deck cannot exceed 15 cards.");
            if (side.Count > 15) result.Errors.Add("Side Deck cannot exceed 15 cards.");
            var quantities = new Dictionary<int, int>();
            var limits = new Dictionary<int, int>();
            var names = new Dictionary<int, string>();
            IList<int>[] sections = { main, extra, side };
            for (int section = 0; section < sections.Length; section++)
            {
                foreach (int id in sections[section])
                {
                    var card = resolve(id);
                    if (card == null)
                    {
                        result.Errors.Add("Unknown card ID " + id + "; update the card database.");
                        continue;
                    }
                    var name = string.IsNullOrEmpty(card.Name) ? id.ToString() : card.Name;
                    if (!card.Tcg) result.Errors.Add(name + " is not available in the TCG card pool.");
                    if (card.Token) result.Errors.Add(name + " is a Token and cannot be in a Deck.");
                    if (section == 0 && card.ExtraDeck) result.Errors.Add(name + " belongs in the Extra Deck.");
                    if (section == 1 && !card.ExtraDeck) result.Errors.Add(name + " cannot be in the Extra Deck.");
                    if (genesys && (card.Link || card.Pendulum))
                        result.Errors.Add(name + " is a Link or Pendulum Monster; Genesys excludes both.");
                    int key = card.Alias > 0 ? card.Alias : card.Id;
                    if (!quantities.ContainsKey(key)) quantities[key] = 0;
                    quantities[key]++;
                    int limit = genesys ? 3 : Math.Min(3, Math.Max(0, card.Limit));
                    if (!limits.ContainsKey(key) || limit < limits[key]) limits[key] = limit;
                    names[key] = name;
                    if (genesys)
                    {
                        if (card.Points < 0) result.Errors.Add("Invalid point value for " + name + ".");
                        else result.Points += card.Points;
                    }
                }
            }
            foreach (var pair in quantities)
                if (pair.Value > limits[pair.Key])
                    result.Errors.Add(names[pair.Key] + " exceeds the combined copy limit (" + limits[pair.Key] + ").");
            if (genesys && result.Points > pointCap)
                result.Errors.Add("Genesys total is " + result.Points + " / " + pointCap + " points across Main, Extra, and Side Decks.");
            return result;
        }
    }
}
