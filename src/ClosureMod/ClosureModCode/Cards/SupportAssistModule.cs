using ClosureMod.Characters;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class SupportAssistModule : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/SupportAssistModule.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleHp", 1)
    ];

    public SupportAssistModule() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
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
        EnergyCost.SetCustomBaseCost(2);
    }
}
