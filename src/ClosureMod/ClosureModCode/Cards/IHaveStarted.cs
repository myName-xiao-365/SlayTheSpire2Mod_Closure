using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class IHaveStarted : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Token;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    private int _attackModuleMaxHp;
    private int _defenseModuleMaxHp;
    private int _supportModuleMaxHp;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    public int DamageAmount => _attackModuleMaxHp * (IsUpgraded ? 12 : 10);
    public int BlockAmount => _defenseModuleMaxHp * (IsUpgraded ? 10 : 8);
    public int HealAmount => _supportModuleMaxHp * (IsUpgraded ? 3 : 2);

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/IHaveStarted.jpg");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.ComputedDamage("DroneDamage", 0, card => card is IHaveStarted started ? started.DamageAmount : 0, ValueProp.Unpowered),
        ModCardVars.ComputedBlock("DroneBlock", 0, card => card is IHaveStarted started ? started.BlockAmount : 0, ValueProp.Unpowered),
        ModCardVars.Computed("DroneHeal", 0, card => card is IHaveStarted started ? started.HealAmount : 0)
    ];

    public IHaveStarted() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    public void Configure(int attackModuleMaxHp, int defenseModuleMaxHp, int supportModuleMaxHp)
    {
        _attackModuleMaxHp = Math.Max(0, attackModuleMaxHp);
        _defenseModuleMaxHp = Math.Max(0, defenseModuleMaxHp);
        _supportModuleMaxHp = Math.Max(0, supportModuleMaxHp);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        if (DamageAmount > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                cardPlay.Target,
                DamageAmount,
                ValueProp.Unpowered,
                Owner.Creature,
                this);
        }

        if (BlockAmount > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, (decimal)BlockAmount, ValueProp.Unpowered, null);
        }

        if (HealAmount > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, HealAmount);
        }
    }
}
