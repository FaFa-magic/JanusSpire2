using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

// public sealed class ForYourHeartOnly() : JanusCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
// {
//     public override int MaxUpgradeLevel => 999;
//
//     public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];
//     
//     protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6M, ValueProp.Move)];
//     
//     protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//     {
//         ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
//         
//         await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
//             .FromCard(this, cardPlay)
//             .Targeting(cardPlay.Target)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(choiceContext);
//
//         CardModel copy = CreateClone();
//         CardCmd.Upgrade(copy);
//         CardCmd.PreviewCardPileAdd(
//             await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner),
//             1.5F);
//     }
//     
//     protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6M);
// }
