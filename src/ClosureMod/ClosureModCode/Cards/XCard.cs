using ClosureMod.Characters;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class XCard : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;
    private const int CandidateCount = 3;

    protected override bool HasEnergyCostX => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.jpg");

    public XCard() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        ICombatState? combatState = CombatState;
        if (combatState is null)
        {
            return;
        }

        int transferredX = Math.Max(0, ResolveEnergyXValue() + (IsUpgraded ? 1 : 0));

        List<CardModel> canonicalOptions = ModelDb.AllCards
            .Where(IsEligibleXCard)
            .DistinctBy(card => card.Id)
            .ToList();
        if (canonicalOptions.Count <= 0)
        {
            return;
        }

        List<CardModel> pickedCanonicalOptions = PickRandomCards(canonicalOptions, CandidateCount);
        List<CardModel> choiceOptions = pickedCanonicalOptions
            .Select(card => combatState.CreateCard(card, Owner))
            .ToList();
        if (choiceOptions.Count <= 0)
        {
            return;
        }

        CardModel? selected;
        try
        {
            // Match the base game's colorless Discovery card: use the large
            // choose-a-card screen instead of the generic combat grid.
            selected = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                choiceOptions,
                Owner,
                canSkip: true);
        }
        finally
        {
            // Selection previews are registered in combat, but never enter a card pile.
            foreach (CardModel option in choiceOptions)
            {
                combatState.RemoveCard(option);
            }
        }

        if (selected is null)
        {
            return;
        }

        CardModel selectedCanonical = selected.CanonicalInstance;
        bool selectedWasUpgraded = selected.IsUpgraded;

        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
        {
            return;
        }

        CardModel cardToPlay = combatState.CreateCard(selectedCanonical, Owner);
        if (selectedWasUpgraded && cardToPlay.IsUpgradable)
        {
            CardCmd.Upgrade(cardToPlay, CardPreviewStyle.None);
        }

        cardToPlay.EnergyCost.CapturedXValue = transferredX;
        XCardTransferredXValuePatch.Mark(cardToPlay);
        cardToPlay.ExhaustOnNextPlay = true;
        Creature? target = ResolveAutoplayTarget(cardToPlay, cardPlay.Target);
        await CardCmd.AutoPlay(
            choiceContext,
            cardToPlay,
            target,
            AutoPlayType.Default,
            skipXCapture: true,
            skipCardPileVisuals: true);
    }

    private static bool IsEligibleXCard(CardModel card)
    {
        return card.EnergyCost.CostsX &&
            card.Type is not CardType.Status and not CardType.Curse &&
            card.Pool is not ClosureSupportCardPool &&
            card.GetType() != typeof(XCard);
    }

    private Creature? ResolveAutoplayTarget(CardModel card, Creature chosenTarget)
    {
        if (card.IsValidTarget(null))
        {
            return null;
        }

        if (card.IsValidTarget(chosenTarget))
        {
            return chosenTarget;
        }

        if (card.IsValidTarget(Owner.Creature))
        {
            return Owner.Creature;
        }

        Creature? fallbackEnemy = CombatState?.HittableEnemies.FirstOrDefault(card.IsValidTarget);
        return fallbackEnemy;
    }

    private static List<CardModel> PickRandomCards(IReadOnlyList<CardModel> source, int count)
    {
        List<CardModel> remaining = source.ToList();
        List<CardModel> picked = [];
        while (picked.Count < count && remaining.Count > 0)
        {
            int index = Random.Shared.Next(remaining.Count);
            picked.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        return picked;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ResolveEnergyXValue))]
internal static class XCardTransferredXValuePatch
{
    private static readonly ConditionalWeakTable<CardModel, object> TransferredCards = new();

    internal static void Mark(CardModel card)
    {
        TransferredCards.Add(card, new object());
    }

    private static bool Prefix(CardModel __instance, ref int __result)
    {
        if (!TransferredCards.TryGetValue(__instance, out _))
        {
            return true;
        }

        __result = __instance.EnergyCost.CapturedXValue;
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.TitleLocString), MethodType.Getter)]
internal static class XCardTitlePatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is XCard)
        {
            __result = new LocString("cards", "CLOSURE_MOD_CARD_X_CARD.title");
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class XCardDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not XCard xCard)
        {
            return;
        }

        __result = new LocString(
            "cards",
            xCard.IsUpgraded
                ? "CLOSURE_MOD_CARD_X_CARD.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_X_CARD.description");
    }
}
