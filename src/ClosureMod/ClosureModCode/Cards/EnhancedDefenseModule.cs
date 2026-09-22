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
public sealed class EnhancedDefenseModule : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/EnhancedDefenseModule.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleHp", 5),
        new BlockVar(2m, ValueProp.Move),
        new DynamicVar("Hits", 6)
    ];

    public EnhancedDefenseModule() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DroneSwarmManager.Summon(
            choiceContext,
            Owner,
            DroneModuleKind.Defense,
            (int)DynamicVars["ModuleHp"].BaseValue,
            this);

        for (int i = 0; i < (int)DynamicVars["Hits"].BaseValue; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(2);
    }
}
