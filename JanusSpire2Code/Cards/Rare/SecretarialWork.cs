using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class SecretarialWork() : JanusCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private CardModel? _pendingTransformation;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Pile?.Type != PileType.Play)
        {
            return;
        }

        CardModel? cardModel = (await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            pile: PileType.Draw.GetPile(Owner),
            player: Owner,
            filter: card => card is not SecretarialWork &&
                            (card.Type is CardType.Skill or CardType.Attack))).FirstOrDefault();
        if (cardModel != null)
        {
            CardModel cardClone = cardModel.CreateClone();
            cardClone.SetToFreeThisCombat();
            _pendingTransformation = cardClone;
        }
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (card != this ||
            oldPileType != PileType.Play ||
            Pile?.Type == PileType.Play ||
            _pendingTransformation is not { } replacement)
        {
            return;
        }

        // Transforming the card while OnPlay is still running leaves its active table node
        // behind. Wait until vanilla has moved it out of Play, then transform from that pile.
        _pendingTransformation = null;
        CardPileAddResult? transformResult = await CardCmd.Transform(
            this,
            replacement,
            CardPreviewStyle.None);
        if (transformResult?.cardAdded is { } transformedCard)
        {
            await CardPileCmd.Add(transformedCard, PileType.Hand);
        }
    }
    
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
