using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class Incense : JanusRelicModel, IModRightClickableRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    [SavedProperty]
    public bool UsedThisCombat
    {
        get => _usedThisCombat;
        set
        {
            AssertMutable();
            _usedThisCombat = value;
            if (value)
            {
                Status = RelicStatus.Normal;
            }
        }
    }

    public override Task BeforeCombatStart()
    {
        UsedThisCombat = false;
        Status = RelicStatus.Active;
        return Task.CompletedTask;
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        if (context.Model != this || context.Player != Owner || context.PlayerChoiceContext is null)
        {
            return false;
        }

        if (!CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsOverOrEnding ||
            Owner.Creature.CombatState is null ||
            UsedThisCombat)
        {
            return false;
        }

        return PileType.Hand.GetPile(Owner).Cards.Count > 0;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (!CanExecuteRightClick(context) || context.PlayerChoiceContext is null)
        {
            return;
        }

        List<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        int cardsToDraw = handCards.Count;

        UsedThisCombat = true;
        Flash();

        foreach (CardModel card in handCards)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }

        await CardPileCmd.Shuffle(context.PlayerChoiceContext, Owner);
        await CardPileCmd.Draw(context.PlayerChoiceContext, cardsToDraw, Owner);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        UsedThisCombat = false;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
