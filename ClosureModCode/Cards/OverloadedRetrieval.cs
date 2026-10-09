using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class OverloadedRetrieval : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Common;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Sluggish];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Draw", 1),
        new DynamicVar("ExtraDrawPerSluggish", 1),
        ModCardVars.Computed("TotalDraw", 1,
            card => card is OverloadedRetrieval retrieval ? retrieval.CalculateDraw() : 1)
    ];

    public OverloadedRetrieval() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    private int CalculateDraw()
    {
        int sluggishAmount = (IsMutable ? Owner?.Creature : null)?.Powers
            .OfType<SluggishPower>()
            .Where(power => power.Amount > 0)
            .Sum(power => (int)power.Amount) ?? 0;
        return (int)DynamicVars["Draw"].BaseValue
            + sluggishAmount * (int)DynamicVars["ExtraDrawPerSluggish"].BaseValue;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, CalculateDraw(), Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
        DynamicVars["TotalDraw"].UpgradeValueBy(1);
    }
}
