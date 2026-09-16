using ClosureMod.Characters;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/EnhancedSupportModule.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleHp", 2)
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
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ModuleHp"].UpgradeValueBy(1);
    }
}
