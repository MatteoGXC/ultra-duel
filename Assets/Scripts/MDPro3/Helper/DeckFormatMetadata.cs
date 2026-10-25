using System;

namespace MDPro3
{
    public static class DeckFormatMetadata
    {
        public const string Prefix = "#ultra-format:";
        // Existing decks remain readable; saving migrates this header without changing cards.
        public const string LegacyPrefix = "#matteo-format:";
        public static bool IsMetadata(string line)
        {
            return line.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) || line.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase);
        }
        public static string Write(string format, int cap)
        {
            return Prefix + (format == "Genesys" ? "Genesys" : "TCG") + ":" + Math.Max(0, cap);
        }
    }
}
