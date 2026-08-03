using Godot;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace JanusSpire2.JanusSpire2Code.Orbs;

public sealed class CourageOrb : ModOrbTemplate
{
    public override decimal PassiveVal => 0m;

    public override decimal EvokeVal => ModifyOrbValue(4);
    
    public override ModOrbValueDisplayMode ValueDisplayMode => ModOrbValueDisplayMode.SingleEvoke;

    public override Color DarkenedColor => new(0.4f, 0.2f, 0.5f);

    // 对于图片，只要是godot支持的格式都可以，例如png,jpg,svg等等
    public override OrbAssetProfile AssetProfile => new(
        // 提示文本小图标路径
        IconPath: "res://icon.svg",
        // 充能球场景路径
        VisualsScenePath: "res://Test/scenes/test_orb.tscn"
    );
    
    protected override Node2D? TryCreateOrbSprite() => RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();
        await CreatureCmd.Heal(base.Owner.Creature, EvokeVal);
        return [Owner.Creature];
    }
    
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Creature.Player)
        {
            return Task.CompletedTask;
        }

        CardCmd.ApplyKeyword(cardPlay.Card, JanusKeywords.Counterattack);
        return Task.CompletedTask;
    }
}