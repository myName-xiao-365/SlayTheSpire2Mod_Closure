using ClosureMod.Characters;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class TimelyStop : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ClearDebt.png");

    public TimelyStop() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.PlayerCombatState is null)
        {
            return;
        }

        await PowerCmd.Apply<RetainHandPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);

        Owner.PlayerCombatState.Energy = 0;

        if (IsUpgraded)
        {
            await PowerCmd.Apply<TimelyStopNextTurnDrawPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this);
        }

        PlayerCmd.EndTurn(Owner, false, static () => Task.CompletedTask);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class TimelyStopDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not TimelyStop timelyStop)
        {
            return;
        }

        __result = new LocString(
            "cards",
            timelyStop.IsUpgraded
                ? "CLOSURE_MOD_CARD_TIMELY_STOP.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_TIMELY_STOP.description");
    }
}
