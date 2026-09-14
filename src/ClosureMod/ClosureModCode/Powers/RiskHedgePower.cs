using ClosureMod.Keywords;
using ClosureMod.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class RiskHedgePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.DebtId];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/characters/energy.png",
        BigIconPath: $"{Entry.ResPath}/images/characters/energy.png");

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        int debtStacks = ResolveDebtStacks();
        if (debtStacks <= 0 || Amount <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, debtStacks * Amount, ValueProp.Unpowered, null);
    }

    private int ResolveDebtStacks()
    {
        int appliedDebt = Owner.GetPowerAmount<EnergyDebtPower>();
        int pendingDebt = Math.Max(0, -(Owner.Player?.PlayerCombatState?.Energy ?? 0));
        if (Owner.Player is { } player)
        {
            pendingDebt = Math.Min(pendingDebt, ClosureModRelic.GetMaxEnergyDebt(player));
        }

        return Math.Max(appliedDebt, pendingDebt);
    }
}
