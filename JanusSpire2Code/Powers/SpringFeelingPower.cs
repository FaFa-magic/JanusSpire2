using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class SpringFeelingPower : JanusPowerModel
{
    private sealed class Data
    {
        public CardModel? SelectedCard;
    }

    private const string CardKey = "Card";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar(CardKey)];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner.Player)
        {
            return;
        }

        CardModel? card = GetInternalData<Data>().SelectedCard;
        if (card == null)
        {
            return;
        }

        for (int i = 0; i < Amount; i++)
        {
            CardModel copy = card.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner.Player);
        }

        await PowerCmd.Remove(this);
    }

    public void SetSelectedCard(CardModel card)
    {
        CardModel cardModel = card.CreateClone();
        CardCmd.ClearAffliction(cardModel);
        GetInternalData<Data>().SelectedCard = cardModel;
        ((StringVar)DynamicVars[CardKey]).StringValue = cardModel.Title;
    }
}
