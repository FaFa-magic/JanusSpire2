using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class RestPower : JanusPowerModel
{
    private sealed class Data
    {
        public List<CardModel> Cards { get; set; } = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public void SetCards(IEnumerable<CardModel> cards)
    {
        GetInternalData<Data>().Cards = cards.Select(card =>
        {
            CardModel template = card.CreateClone();
            CardCmd.ClearAffliction(template);
            return template;
        }).ToList();
    }

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner.Player)
        {
            return;
        }

        List<CardModel> templates = GetInternalData<Data>().Cards;
        await PowerCmd.Remove(this);

        if (templates.Count == 0 || Owner.IsDead)
        {
            return;
        }

        List<CardModel> copies = templates
            .Select(card => card.CreateClone())
            .ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(copies, PileType.Hand, Owner.Player);
    }
}
