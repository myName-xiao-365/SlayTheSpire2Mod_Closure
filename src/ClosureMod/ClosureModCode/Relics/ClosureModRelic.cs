using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Relics;

// RegisterRelic 会把遗物注册进指定遗物池。
// RegisterCharacterStarterRelic 会把它作为 ClosureModCharacter 的初始遗物。
[RegisterRelic(typeof(ClosureModRelicPool))]
[RegisterCharacterStarterRelic(typeof(ClosureModCharacter))]
[RegisterTouchOfOrobasRefinement(typeof(AncientClosureModRelic))]
public class ClosureModRelic : ModRelicTemplate
{
    public const int MaxEnergyDebt = 2;

    // 稀有度。
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.DebtId];

    // 遗物的数值。这里会替换本地化中的 {EnergyDebt}。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnergyDebt", MaxEnergyDebt)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    public static bool PlayerHasEnergyDebtRelic(Player player)
    {
        return player.Relics.Any(relic => relic is ClosureModRelic);
    }

    public static int GetMaxEnergyDebt(Player? player = null)
    {
        return GetBaseMaxEnergyDebt(player) + (player?.Creature.GetPowerAmount<CreditLimitExpansionPower>() ?? 0);
    }

    private static int GetBaseMaxEnergyDebt(Player? player)
    {
        if (player?.Relics.Any(relic => relic is AncientClosureModRelic) == true)
        {
            return AncientClosureModRelic.MaxEnergyDebt;
        }

        return MaxEnergyDebt;
    }

    private static int GetEndTurnDebtReduction(Player player)
    {
        return player.Relics.Any(relic => relic is AncientClosureModRelic)
            ? AncientClosureModRelic.EndTurnDebtReduction
            : 0;
    }

    public static bool CanPlayWithEnergyDebt(CardModel card)
    {
        Player? owner = card.Owner;
        var combatState = owner?.PlayerCombatState;
        if (owner is null || combatState is null || !PlayerHasEnergyDebtRelic(owner))
        {
            return false;
        }

        int energyCost = Math.Max(0, card.EnergyCost.GetAmountToSpend());
        int currentEnergy = combatState.Energy;
        return currentEnergy - energyCost >= -GetMaxEnergyDebt(owner);
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        Player? owner = Owner;
        var combatState = owner?.PlayerCombatState;
        if (owner is null || combatState is null || side != CombatSide.Player)
        {
            return;
        }

        int debt = Math.Clamp(-combatState.Energy, 0, GetMaxEnergyDebt(owner));
        debt = Math.Max(0, debt - GetEndTurnDebtReduction(owner));
        if (debt <= 0)
        {
            return;
        }

        await PowerCmd.Apply<EnergyDebtPower>(choiceContext, owner.Creature, debt, owner.Creature, null);
    }
}

[RegisterRelic(typeof(ClosureModRelicPool))]
public sealed class AncientClosureModRelic : ClosureModRelic
{
    public new const int MaxEnergyDebt = 3;
    public const int EndTurnDebtReduction = 1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnergyDebt", MaxEnergyDebt),
        new DynamicVar("DebtReduction", EndTurnDebtReduction)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{nameof(ClosureModRelic)}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{nameof(ClosureModRelic)}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{nameof(ClosureModRelic)}.png");
}

