using JanusSpire2.JanusSpire2Code.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Singleton;

[RegisterSingleton]
public class JanusSingleton : HookedSingletonModel
{
    public static JanusSingleton? Instance { get; private set; }

    public bool IsReversibleSideFlipped { get; private set; } = false;

    public JanusSingleton() : base(HookType.Combat)
    {
    }

    public override Task BeforeCombatStart()
    {
        Instance = this;
        IsReversibleSideFlipped = false;
        return base.BeforeCombatStart();
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);

        if (combatState != null && combatState.RoundNumber <= 1 && side == CombatSide.Player || combatState?.Players == null)
        {
            return;
        }

        IsReversibleSideFlipped = !IsReversibleSideFlipped;

        foreach (Player player in combatState.Players)
        {
            var playerPiles = player.PlayerCombatState?.AllPiles;
            if (playerPiles == null) continue;

            var janusCards = playerPiles
                .SelectMany(p => p.Cards)
                .OfType<JanusReversibleCardModel>()
                .ToList();

            foreach (var card in janusCards)
            {
                card.RequestVisualReload();
            }
        }
    }
}