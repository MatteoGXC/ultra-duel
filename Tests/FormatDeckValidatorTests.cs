using System;
using System.Collections.Generic;
using MDPro3.Duel.YGOSharp;

class FormatDeckValidatorTests
{
    static Dictionary<int, FormatDeckValidator.CardEntry> database;
    static List<int> main, extra, side;
    static int count;
    static void Reset()
    {
        database = new Dictionary<int, FormatDeckValidator.CardEntry>();
        main = new List<int>(); extra = new List<int>(); side = new List<int>();
        for (int i = 1; i <= 40; i++)
        {
            database[i] = new FormatDeckValidator.CardEntry { Id = i, Name = "Card " + i, Tcg = true };
            main.Add(i);
        }
    }
    static FormatDeckValidator.Result Check(bool genesys, int cap)
    {
        return FormatDeckValidator.Validate(main, extra, side,
            id => database.ContainsKey(id) ? database[id] : null, genesys, cap);
    }
    static void Assert(bool value, string name)
    {
        if (!value) throw new Exception("FAILED: " + name);
        count++; Console.WriteLine("PASS " + name);
    }
    static int Main()
    {
        Reset(); Assert(Check(true, 0).Valid, "A legal zero-point Deck is allowed at cap zero");
        Reset(); main.RemoveAt(0); Assert(!Check(true, 100).Valid, "Reject fewer than 40 Main Deck cards");
        Reset(); database[1].Points = 50; database[2].Points = 50;
        Assert(Check(true, 100).Valid, "Exactly 100 points is legal");
        database[3].Points = 1; Assert(!Check(true, 100).Valid, "Reject 101 points");
        Reset(); database[1].ExtraDeck = true; main[0] = 2; extra.Add(1); database[1].Points = 60;
        side.Add(3); database[3].Points = 30;
        Assert(Check(true, 100).Points == 120 && !Check(true, 100).Valid, "Count every copy in Main, Extra, and Side");
        Reset(); database[1].Link = true; Assert(!Check(true, 100).Valid, "Reject Link Monsters");
        Reset(); database[1].Pendulum = true; side.Add(1); Assert(!Check(true, 100).Valid, "Reject Pendulum Monsters including Side Deck");
        Reset(); database[1].Limit = 0; Assert(Check(true, 100).Valid, "Genesys ignores the traditional banlist");
        Assert(!Check(false, 100).Valid, "TCG rejects a forbidden card");
        Reset(); main[1] = 1; main[2] = 1; side.Add(1);
        Assert(!Check(true, 100).Valid, "Enforce three copies across all sections");
        Reset(); database[2].Alias = 1; database[3].Alias = 1; database[4].Alias = 1;
        Assert(!Check(true, 100).Valid, "Alternate artwork shares the copy limit");
        Reset(); database[1].Limit = 1; main[1] = 1;
        Assert(!Check(false, 100).Valid, "Enforce TCG limited cards");
        Reset(); database[1].Tcg = false; Assert(!Check(true, 100).Valid, "Reject unreleased and OCG-only cards");
        Reset(); main[0] = 999; Assert(!Check(true, 100).Valid, "Reject unknown IDs instead of treating them as free");
        Reset(); database[1].Token = true; Assert(!Check(false, 100).Valid, "Reject Tokens");
        Reset(); extra.Add(1); Assert(!Check(true, 100).Valid, "Reject Main Deck cards in the Extra Deck");
        Reset(); database[1].ExtraDeck = true; Assert(!Check(true, 100).Valid, "Reject Extra Deck cards in the Main Deck");
        Reset(); Assert(!Check(true, -1).Valid, "Reject a negative cap");
        Console.WriteLine(count + " checks passed."); return 0;
    }
}
