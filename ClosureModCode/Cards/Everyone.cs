using ArkBase.Api;
using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
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
public sealed class Everyone : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/Everyone.png");

    public Everyone() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState is null)
        {
            return;
        }

        List<CardModel> candidates = SupportCards.GetCards(CardRarity.Rare).ToList();

        List<CardModel> options = PickRandom(candidates, 3, Owner.RunState.Rng.CombatCardGeneration)
            .Select(card => combatState.CreateCard(card, Owner))
            .ToList();
        if (options.Count == 0)
        {
            return;
        }

        if (IsUpgraded)
        {
            foreach (CardModel option in options.Where(option => option.IsUpgradable))
            {
                CardCmd.Upgrade(option, CardPreviewStyle.None);
            }
        }

        CardModel? selected;
        try
        {
            selected = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                options,
                Owner,
                canSkip: false);
        }
        finally
        {
            foreach (CardModel option in options)
            {
                combatState.RemoveCard(option);
            }
        }

        if (selected is null || CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
        {
            return;
        }

        bool shouldUpgrade = selected.IsUpgraded;
        CardModel canonicalCard = selected.CanonicalInstance ?? selected;
        CardModel generatedCard = combatState.CreateCard(canonicalCard, Owner);
        if (shouldUpgrade && generatedCard.IsUpgradable)
        {
            CardCmd.Upgrade(generatedCard, CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
    }

    private static List<CardModel> PickRandom(
        IReadOnlyList<CardModel> source,
        int count,
        MegaCrit.Sts2.Core.Random.Rng rng)
    {
        List<CardModel> remaining = source.ToList();
        List<CardModel> picked = [];
        while (picked.Count < count && remaining.Count > 0)
        {
            int index = rng.NextInt(remaining.Count);
            picked.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        return picked;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class EveryoneDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is Everyone everyone)
        {
            __result = new LocString(
                "cards",
                everyone.IsUpgraded
                    ? "CLOSURE_MOD_CARD_EVERYONE.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_EVERYONE.description");
        }
    }
}
