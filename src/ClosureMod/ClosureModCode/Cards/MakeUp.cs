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
public sealed class MakeUp : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        // Temporary shared art until a dedicated MakeUp portrait is provided.
        PortraitPath: $"{Entry.ResPath}/images/cards/MakeUp.png");

    public MakeUp() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayerCombatState? playerCombatState = Owner.PlayerCombatState;
        CardPile? discardPile = playerCombatState?.DiscardPile;
        if (discardPile is null || discardPile.Cards.Count == 0)
        {
            return;
        }

        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_MAKE_UP.selectPrompt"),
            1,
            1)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            discardPile,
            Owner,
            selectorPrefs)).FirstOrDefault();
        if (selectedCard is null)
        {
            return;
        }

        if (IsUpgraded && selectedCard.IsUpgradable)
        {
            // The selected card is the combat copy, so this upgrade is not saved to the deck.
            CardCmd.Upgrade(selectedCard, CardPreviewStyle.None);
        }

        // This flag is consumed when the selected card is played and is combat-only.
        selectedCard.ExhaustOnNextPlay = true;
        selectedCard.RequestVisualReload();
        await CardPileCmd.Add(selectedCard, PileType.Hand, CardPilePosition.Top, this, false);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class MakeUpDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is MakeUp makeUp)
        {
            __result = new LocString(
                "cards",
                makeUp.IsUpgraded
                    ? "CLOSURE_MOD_CARD_MAKE_UP.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_MAKE_UP.description");
        }
    }
}
