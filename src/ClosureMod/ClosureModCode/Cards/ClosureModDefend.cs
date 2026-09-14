using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using ClosureMod.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

// 防御牌和打击一样注册到角色卡池，并作为 4 张初始卡加入角色卡组。
[RegisterCard(typeof(ClosureModCardPool))]
[RegisterCharacterStarterCard(typeof(ClosureModCharacter), 4)]
public sealed class ClosureModDefend : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;
    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;
    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Basic;
    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;
    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    // 卡牌基础数值。
    // BlockVar 会绑定到本地化里的 {Block:diff()}，升级时文本会自动显示差值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move)
    ];

    public ClosureModDefend() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

