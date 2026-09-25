using ClosureMod.Characters;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class EnhancedSupportModule : ModCardTemplate
{
    private const int BaseEnergyCost = 5;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/EnhancedSupportModule.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleHp", 2),
        new DynamicVar("SelfDamage", 1),
        new DynamicVar("Hits", 6)
    ];

    public EnhancedSupportModule() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DroneSwarmManager.Summon(
            choiceContext,
            Owner,
            DroneModuleKind.Support,
            (int)DynamicVars["ModuleHp"].BaseValue,
            this);

        for (int i = 0; i < (int)DynamicVars["Hits"].BaseValue; i++)
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature,
                DynamicVars["SelfDamage"].BaseValue,
                ValueProp.Unpowered,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(4);
    }
}
