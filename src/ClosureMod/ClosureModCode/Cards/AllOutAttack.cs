using ClosureMod.Characters;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class AllOutAttack : ModCardTemplate
{
    private const int BaseEnergyCost = 5;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/AllOutAttack.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AttackModuleHp", 3),
        new DynamicVar("DefenseModuleHp", 3),
        new DynamicVar("SupportModuleHp", 1)
    ];

    public AllOutAttack() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DroneSwarmManager.Summon(choiceContext, Owner, DroneModuleKind.Attack, (int)DynamicVars["AttackModuleHp"].BaseValue, this);
        await DroneSwarmManager.Summon(choiceContext, Owner, DroneModuleKind.Defense, (int)DynamicVars["DefenseModuleHp"].BaseValue, this);
        await DroneSwarmManager.Summon(choiceContext, Owner, DroneModuleKind.Support, (int)DynamicVars["SupportModuleHp"].BaseValue, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(4);
    }
}
