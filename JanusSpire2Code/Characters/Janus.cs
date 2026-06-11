using Godot;
//using JanusSpire2.JanusSpire2Code.Config;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace JanusSpire2.JanusSpire2Code.Characters;

public sealed class JanusCharacter : ModCharacterTemplate<JanusCardPool, JanusRelicPool, JanusPotionPool>
{
	public override Color NameColor => new("#9A72A1");
	public override Color EnergyLabelOutlineColor => new Color("1E283CFF");
	public override Color MapDrawingColor => new("#9A72A1");

	public const string CharacterId = "Janus";
	public const string CharacterColor = "Janus_blue";
	public override CharacterGender Gender => CharacterGender.Feminine;
	
	public override int StartingHp => 70;
	public override int StartingGold => 99;
	
	public override Color DialogueColor => new("#9A72A1");
	public override Color RemoteTargetingLineColor => new("#AAAAAA");
	public override Color RemoteTargetingLineOutline => Colors.Black;
	
	 public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new(
            Scenes: new(
                // 人物模型tscn路径。
                VisualsPath: "res://JanusSpire2/scenes/characters/Janus.tscn",
                // 能量表盘tscn路径。
                EnergyCounterPath: "res://JanusSpire2/scenes/vfx/Janus_energy_counter.tscn",
                // 商店人物场景。
                MerchantAnimPath: "res://JanusSpire2/scenes/characters/Janus_merchant.tscn",
                // 篝火休息场景。
                RestSiteAnimPath: "res://JanusSpire2/scenes/characters/Janus_rest_site.tscn"
            ),
            Ui: new(
                // 人物头像路径。
                IconTexturePath: "res://JanusSpire2/scenes/characters/Janus_icon.tscn",
                // 人物头像2号带边框。
                //IconPath: "res://JanusSpire2/images/Janus/character_icon_Janus_outline.png",
                // 人物选择背景。
                CharacterSelectBgPath: "res://JanusSpire2/scenes/characters/char_select_bg_Janus.tscn",
                // 人物选择图标。
                CharacterSelectIconPath: "res://JanusSpire2/images/Janus/char_select_Janus.png",
                // 人物选择图标-锁定状态。
                CharacterSelectLockedIconPath: "res://JanusSpire2/images/Janus/char_select_Janus_locked.png",
                // 人物选择过渡动画。
                CharacterSelectTransitionPath: "res://JanusSpire2/materials/Janus_transition_mat.tres",
                // 地图上的角色标记图标、表情轮盘上的角色头像
                MapMarkerPath: "res://JanusSpire2/images/Janus/map_marker_Janus.png"
            ),
            Vfx: new(
                // 卡牌拖尾场景。
                TrailPath: "res://JanusSpire2/scenes/vfx/card_trail_Janus.tscn"
            ),
            Audio: new(
                // 攻击音效
                AttackSfx: "res://JanusSpire2/sfx/Janus_attacksfx.mp3",
                // 施法音效
                CastSfx: "res://JanusSpire2/sfx/Janus_castsfx.mp3",
                // 死亡音效
                DeathSfx: "res://JanusSpire2/sfx/Janus_deathsfx.mp3",
                // 角色选择音效
                CharacterSelectSfx: "res://JanusSpire2/sfx/Janus_character_select.mp3",
                // 过渡音效
                CharacterTransitionSfx: "res://JanusSpire2/sfx/Janus_character_transition.mp3"
            ),
            Multiplayer: new(
                // 多人模式-手指。
                // ArmPointingTexturePath: null,
                // 多人模式剪刀石头布-石头。
                // ArmRockTexturePath: null,
                // 多人模式剪刀石头布-布。
                // ArmPaperTexturePath: null,
                // 多人模式剪刀石头布-剪刀。
                // ArmScissorsTexturePath: null
            ),
            // Spine: null,
            // VisualCues: null, // 帧动画静态图人物使用，查看角色动画一章
            // WorldProceduralVisuals: null,
            // VanillaCardVisualOverrides: [],
            VanillaRelicVisualOverrides: [
                new (CharacterOwnedVanillaRelicModelId.YummyCookie, new(
	                "res://JanusSpire2/images/relics/packed/YummyCookie_Janus.png",
	                "res://JanusSpire2/images/relics/outline/YummyCookie_Janus.png",
	                "res://JanusSpire2/images/relics/big/YummyCookie_Janus.png"
	                )) // 美味饼干覆盖
            ]
            // VanillaPotionVisualOverrides: []
        ));
	
	 // 攻击和施法动画延迟，以对齐动画
	 public override float AttackAnimDelay => 0f;
	 public override float CastAnimDelay => 0f;

	 // 如果你的人物不需要时间线小故事，加上这句。
	 public override bool RequiresEpochAndTimeline => false;

	 // 自动转换人物场景，让你不需要手动挂脚本。
	 protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);
	 
	 public override List<string> GetArchitectAttackVfx() => [
		 "vfx/vfx_attack_blunt",
		 "vfx/vfx_heavy_blunt",
		 "vfx/vfx_attack_slash",
		 "vfx/vfx_bloody_impact",
		 "vfx/vfx_rock_shatter"
	 ];
	
	// public override string CustomArmPointingTexturePath =>
	// 	JanusConfig.多人模式使用哪种模型 == FjordMosaicMode.手部模型
	// 		? "res://JanusSpire2/images/Janus/hands/multiplayer_hand_Janus_point.png"
	// 		: "res://JanusSpire2/images/Janus/feet/multiplayer_foot_Janus_point.png";
	//
	// public override string CustomArmRockTexturePath =>
	// 	JanusConfig.多人模式使用哪种模型 == FjordMosaicMode.手部模型
	// 		? "res://JanusSpire2/images/Janus/hands/multiplayer_hand_Janus_rock.png"
	// 		: "res://JanusSpire2/images/Janus/feet/multiplayer_foot_Janus_rock.png";
	//
	// public override string CustomArmPaperTexturePath =>
	// 	JanusConfig.多人模式使用哪种模型 == FjordMosaicMode.手部模型
	// 		? "res://JanusSpire2/images/Janus/hands/multiplayer_hand_Janus_paper.png"
	// 		: "res://JanusSpire2/images/Janus/feet/multiplayer_foot_Janus_paper.png";
	//
	// public override string CustomArmScissorsTexturePath =>
	// 	JanusConfig.多人模式使用哪种模型 == FjordMosaicMode.手部模型
	// 		? "res://JanusSpire2/images/Janus/hands/multiplayer_hand_Janus_scissors.png"
	// 		: "res://JanusSpire2/images/Janus/feet/multiplayer_foot_Janus_scissors.png";
}
