using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ClosureMod.Characters;

[RegisterCharacter]
public sealed class ClosureModCharacter : ModCharacterTemplate<ClosureModCardPool, ClosureModRelicPool, ClosureModPotionPool>
{
    public static readonly Color ThemeColor = new(0.78f, 0.35f, 0.48f);

    private const string SceneRoot = $"{Entry.ResPath}/scenes/characters";
    private const string ImageRoot = $"{Entry.ResPath}/images/characters";
    private const string CharacterScenePath = $"{SceneRoot}/ClosureMod_character.tscn";
    private const string EnergyCounterScenePath = $"{SceneRoot}/ClosureMod_energy_counter.tscn";
    private const string MerchantScenePath = $"{SceneRoot}/ClosureMod_merchant.tscn";
    private const string RestSiteScenePath = $"{SceneRoot}/ClosureMod_rest_site.tscn";
    private const string CharacterSelectBgScenePath = $"{SceneRoot}/ClosureMod_character_select_bg.tscn";

    // 角色名称颜色。
    public override Color NameColor => ThemeColor;
    // 能量图标轮廓颜色。
    public override Color EnergyLabelOutlineColor => new(0.08f, 0.18f, 0.24f);
    // 地图绘制颜色。
    public override Color MapDrawingColor => ThemeColor;

    // 人物性别（男女中立）。
    public override CharacterGender Gender => CharacterGender.Neutral;

    // 初始血量和金币。
    public override int StartingHp => 70;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: CharacterScenePath,
            EnergyCounterPath: EnergyCounterScenePath,
            MerchantAnimPath: MerchantScenePath,
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            IconTexturePath: $"{ImageRoot}/ClosureMod_character_icon.png",
            IconOutlineTexturePath: $"{ImageRoot}/ClosureMod_character_icon_outline.png",
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            CharacterSelectIconPath: $"{ImageRoot}/ClosureMod_avatar.png",
            CharacterSelectLockedIconPath: $"{ImageRoot}/ClosureMod_avatar_locked.png",
            MapMarkerPath: $"{ImageRoot}/ClosureMod_map_marker.png"));

    // 某个字段没写时，RitsuLib 会从占位角色配置里补齐。
    public override string? PlaceholderCharacterId => "ironclad";
    // 如果你的人物不需要时间线小故事，加上这句。
    public override bool RequiresEpochAndTimeline => false;
    // 攻击和施法动画延迟，以对齐动画。静态占位资源不需要延迟。
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            CharacterScenePath);
    }

    // 攻击建筑师的攻击特效列表。
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}

