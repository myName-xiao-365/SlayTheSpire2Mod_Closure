using ClosureMod.Characters;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class PowerAllocation : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.AllEnemies;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnemyOffset", 0),
        new DynamicVar("SelfOffset", 1)
    ];

    public PowerAllocation() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int currentEnergy = Owner.PlayerCombatState?.Energy ?? 0;
        decimal enemyStrength = currentEnergy + DynamicVars["EnemyOffset"].BaseValue;
        decimal selfStrength = currentEnergy + DynamicVars["SelfOffset"].BaseValue;

        foreach (Creature enemy in CombatState?.HittableEnemies ?? [])
        {
            await ApplyTemporaryStrength(choiceContext, enemy, enemyStrength);
        }

        await ApplyTemporaryStrength(choiceContext, Owner.Creature, selfStrength);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EnemyOffset"].UpgradeValueBy(-1);
        DynamicVars["SelfOffset"].UpgradeValueBy(1);
    }

    private async Task ApplyTemporaryStrength(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount)
    {
        if (amount == 0)
        {
            return;
        }

        await PowerCmd.Apply<TemporaryStrengthFromPowerAllocationPower>(
            choiceContext,
            target,
            amount,
            Owner.Creature,
            this);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class PowerAllocationDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not PowerAllocation powerAllocation)
        {
            return;
        }

        __result = new LocString(
            "cards",
            powerAllocation.IsUpgraded
                ? "CLOSURE_MOD_CARD_POWER_ALLOCATION.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_POWER_ALLOCATION.description");
    }
}
