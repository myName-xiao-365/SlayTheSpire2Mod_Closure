using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class ForceBuySell : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        // Temporary shared art until a dedicated ForceBuySell portrait is provided.
        PortraitPath: $"{Entry.ResPath}/images/cards/ForceBuySell.png");

    public ForceBuySell() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayerCombatState? playerCombatState = Owner.PlayerCombatState;
        CardPile? drawPile = playerCombatState?.DrawPile;
        if (drawPile is null || drawPile.Cards.Count == 0)
        {
            return;
        }

        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_FORCE_BUY_SELL.selectPrompt"),
            1,
            1)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            drawPile,
            Owner,
            selectorPrefs)).FirstOrDefault();
        if (selectedCard is null)
        {
            return;
        }

        CardPileAddResult? transformResult = await CardCmd.TransformTo<DerivedCard>(
            selectedCard,
            CardPreviewStyle.HorizontalLayout);
        if (IsUpgraded && transformResult is { } result && result.cardAdded.IsUpgradable)
        {
            CardCmd.Upgrade(result.cardAdded, CardPreviewStyle.HorizontalLayout);
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class ForceBuySellDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is ForceBuySell forceBuySell)
        {
            __result = new LocString(
                "cards",
                forceBuySell.IsUpgraded
                    ? "CLOSURE_MOD_CARD_FORCE_BUY_SELL.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_FORCE_BUY_SELL.description");
        }
    }
}
