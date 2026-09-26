using ClosureMod.Characters;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class Friend : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/Friend.jpg");

    public Friend() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        FriendPower? power = await PowerCmd.Apply<FriendPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        if (power is not null)
        {
            power.UpgradeGeneratedCards = IsUpgraded;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class FriendDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is Friend friend)
        {
            __result = new LocString(
                "cards",
                friend.IsUpgraded
                    ? "CLOSURE_MOD_CARD_FRIEND.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_FRIEND.description");
        }
    }
}
