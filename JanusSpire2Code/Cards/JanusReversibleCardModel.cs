using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Singleton;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards;

public abstract class JanusReversibleCardModel : JanusCardModel{
    public JanusReversibleCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }
    
    public override bool ShouldReceiveCombatHooks => false;

    public override CardAssetProfile AssetProfile
    {
        get
        {
            JanusCardFrameMode globalMode = JanusConfigPage.CardFrameBinding.Read();
            bool useFrame2 = globalMode == JanusCardFrameMode.卡框二;
            
            if (this.IsMutable && this.CombatState != null)
            {
                var janusSingleton = JanusSingleton.Instance;
                if (janusSingleton != null && janusSingleton.IsReversibleSideFlipped)
                {
                    useFrame2 = !useFrame2;
                }
            }

            return new CardAssetProfile(
                PortraitPath: $"res://JanusSpire2/images/cards/{GetType().Name}.png",
                BannerTexturePath: "res://JanusSpire2/images/card_frames/janus_Banner.png",
                AncientBannerPath: "res://JanusSpire2/images/card_frames/janus_Banner.png",
                AncientBorderPath: "res://JanusSpire2/images/card_frames/janus_ancient.png",
                FramePath: Type switch
                {
                    CardType.Attack => useFrame2
                        ? "res://JanusSpire2/images/card_frames/janus_attack_2.png"
                        : "res://JanusSpire2/images/card_frames/janus_attack_1.png",
                    CardType.Skill => useFrame2
                        ? "res://JanusSpire2/images/card_frames/janus_skill_2.png"
                        : "res://JanusSpire2/images/card_frames/janus_skill_1.png",
                    CardType.Power => useFrame2
                        ? "res://JanusSpire2/images/card_frames/janus_power_2.png"
                        : "res://JanusSpire2/images/card_frames/janus_power_1.png",
                    _ => ""
                }
            );
        }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = this.CombatState;
        if (combatState == null) return;
        if (combatState.CurrentSide == CombatSide.Player)
        {
            await OnPlayerTurnPlay(choiceContext, cardPlay);
        }
        else if (combatState.CurrentSide == CombatSide.Enemy)
        {
            await OnEnemyTurnPlay(choiceContext, cardPlay);
        }
    }
    
    protected virtual Task OnPlayerTurnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;

    protected virtual Task OnEnemyTurnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}