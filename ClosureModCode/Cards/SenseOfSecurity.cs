using ClosureMod.Characters;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class SenseOfSecurity : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlockPerSupport", 4)
    ];

    public SenseOfSecurity() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SenseOfSecurityPower>(
            choiceContext,
            Owner.Creature,
            (int)DynamicVars["BlockPerSupport"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlockPerSupport"].UpgradeValueBy(2);
    }
}
