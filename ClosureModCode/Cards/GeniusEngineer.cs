using ClosureMod.Characters;
using ClosureMod.Powers;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class GeniusEngineer : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Power;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/GeniusEngineer.jpg");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleHp", 2)
    ];

    public GeniusEngineer() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DroneSwarmPower? swarm = await DroneSwarmManager.Summon(
            choiceContext,
            Owner,
            DroneModuleKind.Attack,
            (int)DynamicVars["ModuleHp"].BaseValue,
            this);

        if (swarm is not null)
        {
            swarm.AttackModuleHitsAllEnemies = true;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ModuleHp"].UpgradeValueBy(1);
    }
}
