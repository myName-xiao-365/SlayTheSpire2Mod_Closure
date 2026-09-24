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

[RegisterCard(typeof(ClosureSupportCardPool))]
public sealed class EmptyTheater : SupportCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/EmptyTheater.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar("HpLoss", 9m),
        new PowerVar<ParalysisPower>(2)
    ];

    public EmptyTheater() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await CreatureCmd.Damage(
            choiceContext,
            [cardPlay.Target],
            DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            Owner.Creature,
            this);

        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<ParalysisPower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars[nameof(ParalysisPower)].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HpLoss"].UpgradeValueBy(3);
        DynamicVars[nameof(ParalysisPower)].UpgradeValueBy(1);
    }
}
