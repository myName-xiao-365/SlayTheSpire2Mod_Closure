using ArkBase.Api;
using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class SupportStrike : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move)
    ];

    public SupportStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (CombatState is not { } combatState)
        {
            return;
        }

        List<CardModel> options = PickRandom(
                SupportCards.GetCards(CardRarity.Common),
                3,
                Owner.RunState.Rng.CombatCardGeneration)
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

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
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
internal static class SupportStrikeDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is SupportStrike supportStrike)
        {
            __result = new LocString(
                "cards",
                supportStrike.IsUpgraded
                    ? "CLOSURE_MOD_CARD_SUPPORT_STRIKE.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_SUPPORT_STRIKE.description");
        }
    }
}
