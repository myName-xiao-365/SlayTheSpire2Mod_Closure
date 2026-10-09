using ClosureMod.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class CreditLimitExpansionPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CreditLimitExpansionPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CreditLimitExpansionPower.png");

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) ||
            Owner.Player is not { PlayerCombatState: { } combatState } player)
        {
            return;
        }

        int debt = Math.Clamp(-combatState.Energy, 0, Amount);
        debt = Math.Max(0, debt - ClosureModRelic.GetEndTurnDebtReduction(player));
        if (debt > 0)
        {
            await PowerCmd.Apply<EnergyDebtPower>(choiceContext, Owner, debt, Owner, null);
        }
    }
}
