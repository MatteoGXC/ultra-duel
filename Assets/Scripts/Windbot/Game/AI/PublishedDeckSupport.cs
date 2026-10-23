using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI
{
    public abstract partial class DefaultExecutor
    {
        bool publishedSupport;
        HashSet<string> dedicatedActions;
        bool Missing(ExecutorType type) => !dedicatedActions.Contains(type+":"+Card.Id);

        // Published lists evolve independently of the older built-in recipes.
        // Keep every dedicated decision, then cover new cards before the final
        // set/pass rules. Never replace the user's published recipe.
        public void EnablePublishedDeckSupport()
        {
            if(publishedSupport)return;
            publishedSupport=true;
            dedicatedActions=new HashSet<string>(Executors.Where(e=>e.CardId>0).Select(e=>e.Type+":"+e.CardId));
            int index=Executors.ToList().FindIndex(e=>e.Type==ExecutorType.MonsterSet || e.Type==ExecutorType.SpellSet || e.Type==ExecutorType.GoToEndPhase);
            if(index<0)index=Executors.Count;
            var support=new[] {
                new CardExecutor(ExecutorType.Activate,-1,()=>Missing(ExecutorType.Activate) && PublishedActivate()),
                new CardExecutor(ExecutorType.SpSummon,-1,()=>Missing(ExecutorType.SpSummon) && PublishedSpecialSummon()),
                new CardExecutor(ExecutorType.Summon,-1,()=>Missing(ExecutorType.Summon) && !dedicatedActions.Contains(ExecutorType.SummonOrSet+":"+Card.Id) && DefaultMonsterSummon()),
                new CardExecutor(ExecutorType.Repos,-1,()=>Missing(ExecutorType.Repos) && DefaultMonsterRepos()),
            };
            foreach(var handler in support)Executors.Insert(index++,handler);
            AddExecutor(ExecutorType.MonsterSet,-1,()=>Missing(ExecutorType.MonsterSet) && Card.Level<=4 && (Card.Defense>Card.Attack || Bot.GetMonsterCount()==0));
            AddExecutor(ExecutorType.SpellSet,-1,()=>Missing(ExecutorType.SpellSet) && DefaultSpellSet());
        }
        bool PublishedActivate()
        {
            if(DefaultCheckWhetherCardIsNegated(Card) || Util.IsChainTargetOnly(Card))return false;
            // Reuse tested timing for defensive staples rather than firing them
            // into our own searches/summons.
            switch(Card.Id)
            {
                case 14558129:return DefaultAshBlossomAndJoyousSpring();
                case 97268403:return DefaultEffectVeiler();
                case 23434538:return DefaultMaxxC();
                case 59438930:return DefaultGhostOgreAndSnowRabbit();
                case 24224830:return DefaultCalledByTheGrave();
                case 10045474:return DefaultInfiniteImpermanence();
                case 18144506:return DefaultHarpiesFeatherDusterFirst();
                case 12580477:return DefaultRaigeki();
                case 53129443:return DefaultDarkHole();
                case 44095762:return Enemy.GetMonsterCount()>0 && DefaultTrap();
                case 53582587:return Enemy.GetMonsterCount()>0 && DefaultTrap();
                // Modern Blue-Eyes starter and recovery cards absent from the
                // legacy executor: use engine-offered legal effects, keep traps
                // that summon/search usable on our own turn as well.
                case 17947697: case 54332792: case 80326401: case 17725109:
                case 43219114: case 64202399: case 30576089: case 71143015:
                case 62089826: case 24382602: case 33907039: case 51405049:
                    return Duel.Player==0 || Card.Location==CardLocation.Grave || (Duel.LastChainPlayer!=0 && Duel.CurrentChain.Count>0);
            }
            if(Card.Location==CardLocation.Grave || Card.Location==CardLocation.Removed)return true;
            if(Card.IsTrap())
            {
                // Continuous engines (Labrynth/Apophis/True Light hybrids) must
                // be allowed to start; one-shot traps retain opponent timing.
                if(Card.HasType(CardType.Continuous))return !Util.HasChainedTrap(0);
                return DefaultTrap();
            }
            if(Duel.LastChainPlayer==0)return false;
            if(Card.HasType(CardType.Counter))return Duel.LastChainPlayer==1;
            return Duel.Player==0;
        }
        bool PublishedSpecialSummon()
        {
            if(Card.Location!=CardLocation.Extra)return true;
            // Avoid turning a finished board into a weaker generic Extra Deck
            // monster. One-material Link starters are permitted as extenders.
            if(Card.HasType(CardType.Link) && Card.LinkCount==1)return Bot.GetMonsterCount()==1;
            int strongest=Bot.GetMonsters().Select(c=>c.Attack).DefaultIfEmpty(0).Max();
            return Card.Attack>=strongest || (Card.Level<=4 && Card.HasType(CardType.Synchro));
        }
    }
}
