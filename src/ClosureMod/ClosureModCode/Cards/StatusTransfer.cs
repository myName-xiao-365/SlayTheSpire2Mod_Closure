using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class StatusTransfer : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Common;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Sluggish];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/StatusTransfer.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("SluggishApply", 1),
        new DynamicVar("SluggishRemove", 1)
    ];

    public StatusTransfer() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PowerCmd.Apply<SluggishPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["SluggishApply"].BaseValue,
            Owner.Creature,
            this);

        SluggishPower? sluggish = Owner.Creature.Powers
            .OfType<SluggishPower>()
            .FirstOrDefault(power => power.Amount > 0);
        if (sluggish is null)
        {
            return;
        }

        decimal removeAmount = Math.Min(DynamicVars["SluggishRemove"].BaseValue, sluggish.Amount);
        await PowerCmd.ModifyAmount(choiceContext, sluggish, -removeAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SluggishApply"].UpgradeValueBy(1);
    }
}
