using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Interfaces;

public interface IAfterBlackCatSealExplode
{
    Task AfterBlackCatSealExplode(
        PlayerChoiceContext choiceContext,
        BlackCatSealExplodeContext context);
}

public sealed class BlackCatSealExplodeContext
{
    public required Creature Owner { get; init; }

    public Creature? Applier { get; init; }

    public required PlayerChoiceContext ChoiceContext { get; init; }
}
