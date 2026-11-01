using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks
{
    // Reuse the Branded executor's fusion trees, graveyard resources and interruption
    // timing; add Dracotail decisions ahead of generic main-phase/set handlers.
    [Deck("Dracotail", "AI_Dracotail")]
    public sealed class DracotailExecutor : AlbazExecutor
    {
        private static readonly int[] Monsters = {75003700,1498449,7375867,44482554,70871153,84477320};
        private static readonly int[] Fusions = {33760966,79755671,42125140};
        private static readonly int[] Spells = {6153210,32548318,69932023,5431722,80208225};
        private bool fusionStarted;

        public DracotailExecutor(GameAI ai, Duel duel) : base(ai, duel)
        {
            var rules = new List<CardExecutor>();
            foreach (int id in Monsters.Concat(Fusions).Concat(Spells))
                rules.Add(new CardExecutor(ExecutorType.Activate, id, DracotailActivate));
            rules.Add(new CardExecutor(ExecutorType.Summon,75003700,()=>!Bot.HasInMonstersZone(75003700,true)));
            rules.Add(new CardExecutor(ExecutorType.Summon,84477320,()=>Bot.Graveyard.Any(c=>Monsters.Contains(c.Id))));
            rules.Add(new CardExecutor(ExecutorType.Summon,7375867,()=>Bot.Hand.Any(c=>c.IsMonster() && c.Id!=7375867)));
            for (int i=0;i<rules.Count;i++) Executors.Insert(i,rules[i]);
        }

        public override void OnNewTurn() { base.OnNewTurn(); fusionStarted=false; }
        public override void OnChainEnd() { base.OnChainEnd(); fusionStarted=false; }

        private bool DracotailActivate()
        {
            if (DefaultCheckWhetherCardIsNegated(Card)) return false;
            if (Card.Location==CardLocation.Grave || Card.Location==CardLocation.Removed)
            {
                if (Card.Id==70871153) return Bot.Graveyard.Any(c=>c.Id!=70871153 && Monsters.Contains(c.Id));
                return true;
            }
            switch(Card.Id)
            {
                case 6153210: return Duel.Player==0; // Search before committing a fusion.
                case 32548318: return Duel.Player==0 && Bot.Hand.Any(c=>c.IsMonster()) && Bot.ExtraDeck.Any(c=>Fusions.Contains(c.Id));
                case 1498449:
                case 7375867:
                    if (fusionStarted || !Bot.ExtraDeck.Any(c=>Fusions.Contains(c.Id))) return false;
                    int resources=Bot.Hand.Count(c=>c.IsMonster())+Bot.GetMonsters().Count;
                    if (resources<(Card.Id==1498449?3:2) || !Bot.Hand.Any(c=>c.IsMonster() && c!=Card)) return false;
                    if (Duel.Player!=0 && Duel.LastChainPlayer!=1 && !Util.IsChainTarget(Card)) return false;
                    fusionStarted=true;
                    return true;
                case 69932023: return Enemy.GetMonsters().Any(c=>c.IsAttack()) && Duel.LastChainPlayer!=0;
                case 5431722: return Enemy.GetSpells().Any(c=>c.IsFaceup()) && Duel.LastChainPlayer==1;
                case 80208225: return Enemy.Graveyard.Count>0 && Duel.LastChainPlayer==1;
                case 79755671: return Enemy.GetMonsterCount()+Enemy.GetSpellCount()>0;
                case 42125140: return Bot.Graveyard.Count(c=>Monsters.Contains(c.Id) || Spells.Contains(c.Id))>=2 && Enemy.GetMonsterCount()+Enemy.GetSpellCount()>0;
                case 84477320: return Bot.Graveyard.Any(c=>Monsters.Contains(c.Id) && c.Id!=84477320);
                default: return true;
            }
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards,int min,int max,int hint,bool cancelable)
        {
            var resolving=Duel.GetCurrentSolvingChainCard();
            if (resolving==null || resolving.Controller!=0 || !(Monsters.Contains(resolving.Id) || Fusions.Contains(resolving.Id) || Spells.Contains(resolving.Id)))
                return base.OnSelectCard(cards,min,max,hint,cancelable);
            IEnumerable<ClientCard> ordered=null;
            if (hint==HintMsg.SpSummon && cards.Any(c=>c.Location==CardLocation.Extra))
                ordered=cards.OrderByDescending(c=>c.Id==(Enemy.GetMonsterCount()>0?33760966:79755671)?100: c.Id==33760966?80: Fusions.Contains(c.Id)?60:0);
            else if (hint==HintMsg.FusionMaterial)
                // Material triggers set interaction. Use deck resources first for Rahu,
                // preserve established Fusion bosses and the next normal summon.
                ordered=cards.OrderByDescending(c=>(c.Id==7375867?90:c.Id==44482554 && Enemy.GetMonsterCount()>0?85:c.Id==70871153 && Enemy.GetSpellCount()>0?85:Monsters.Contains(c.Id)?60:0)+(c.Location==CardLocation.Deck?30:0)-(c.HasType(CardType.Fusion)?100:0));
            else if (hint==HintMsg.AddToHand)
                ordered=cards.OrderByDescending(c=>c.Id==1498449 && !Bot.Hand.Any(h=>h.Id==1498449)?100:c.Id==75003700?90:c.Id==7375867?80:0);
            else if (hint==HintMsg.Set)
                ordered=cards.OrderByDescending(c=>c.Id==32548318 && Duel.Player==0 && !Bot.Hand.Any(h=>h.Id==32548318) && !Bot.GetSpells().Any(h=>h.Id==32548318)?100:c.Id==69932023 && !Bot.GetSpells().Any(h=>h.Id==69932023)?90:c.Id==6153210?80:c.Id==5431722?70:0);
            else if (hint==HintMsg.Destroy || hint==HintMsg.ReturnToHand || hint==HintMsg.Disable || hint==HintMsg.Remove || hint==HintMsg.ToDeck)
                ordered=cards.OrderByDescending(c=>(c.Controller==1?10000:0)+(c.IsFaceup()?500:0)+c.Attack);
            if (ordered!=null) return Util.CheckSelectCount(ordered.ToList(),cards,min,max);
            return base.OnSelectCard(cards,min,max,hint,cancelable);
        }
    }
}
