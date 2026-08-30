using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace JanusSpire2.JanusSpire2Code.Cards;

public abstract class JanusRecordCardModel : JanusCardModel
{
    private bool _canTake;

    [SavedProperty]
    public int RecordMappingKey { get; set; }

    [SavedProperty]
    public bool CanTake
    {
        get => _canTake;
        protected set
        {
            AssertMutable();

            _canTake = value;
        }
    }

    public JanusRecordCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task EnableTake()
    {
        CanTake = true;
        await RecordExtraHandManager.SyncFor(this);
    }

    public async Task DisableTake()
    {
        CanTake = false;
        await RecordExtraHandManager.SyncFor(this);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
        {
            await DisableTake();
        }
    }
}
