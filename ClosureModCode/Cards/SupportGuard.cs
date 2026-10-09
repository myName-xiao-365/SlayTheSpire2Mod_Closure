using ArkBase.Api;
using ClosureMod.Characters;
using ClosureMod.Keywords;
using HarmonyLib;
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
public sealed class SupportGuard : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Block];

    public override bool GainsBlock => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/SupportGuard.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    public SupportGuard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (CombatState is not { } combatState)
        {
            return;
        }

        IReadOnlyList<CardModel> candidates = SupportCards.GetCards(CardRarity.Common);
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel generatedCard = combatState.CreateCard(
            candidates[Owner.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)],
            Owner);
        if (IsUpgraded && generatedCard.IsUpgradable)
        {
            CardCmd.Upgrade(generatedCard, CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class SupportGuardDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is SupportGuard guard)
        {
            __result = new LocString(
                "cards",
                guard.IsUpgraded
                    ? "CLOSURE_MOD_CARD_SUPPORT_GUARD.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_SUPPORT_GUARD.description");
        }
    }
}
