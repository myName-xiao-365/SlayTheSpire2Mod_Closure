using ClosureMod.Characters;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class TimelyRescue : ModCardTemplate
{
    private const int BaseEnergyCost = 2;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override bool GainsBlock => true;
    public override bool ShouldReceiveCombatHooks => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/TimelyRescue.png");

    private bool _putIntoHandAfterPlay;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(15m, ValueProp.Move)
    ];

    public TimelyRescue() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        _putIntoHandAfterPlay = SluggishStunLimiterPower.IsStunned(Owner.Creature);
    }

    public override async Task AfterCardChangedPilesLate(
        CardModel card,
        PileType previousPileType,
        AbstractModel? source)
    {
        if (!_putIntoHandAfterPlay ||
            !ReferenceEquals(card, this) ||
            previousPileType != PileType.Play ||
            Pile?.Type == PileType.Hand)
        {
            return;
        }

        _putIntoHandAfterPlay = false;
        await CardPileCmd.Add(this, PileType.Hand, CardPilePosition.Top, this, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
