using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class BigDiscount : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.jpg");

    public BigDiscount() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool CanDiscount(CardModel card) =>
            !ReferenceEquals(card, this) &&
            !card.EnergyCost.CostsX &&
            card.Type is not CardType.Status and not CardType.Curse;

        List<CardModel> targets = Owner.PlayerCombatState?.Hand.Cards
            .Where(CanDiscount)
            .ToList() ?? [];
        if (targets.Count <= 0)
        {
            return;
        }

        CardModel selectedCard = targets[Random.Shared.Next(targets.Count)];

        selectedCard.EnergyCost.SetCustomBaseCost(0);
        if (IsUpgraded && selectedCard.IsUpgradable)
        {
            CardCmd.Upgrade(selectedCard, CardPreviewStyle.None);
        }

        selectedCard.RequestVisualReload();
        await Task.CompletedTask;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class BigDiscountDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not BigDiscount bigDiscount)
        {
            return;
        }

        __result = new LocString(
            "cards",
            bigDiscount.IsUpgraded
                ? "CLOSURE_MOD_CARD_BIG_DISCOUNT.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_BIG_DISCOUNT.description");
    }
}
