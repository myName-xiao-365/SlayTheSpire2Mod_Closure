using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class AdaptiveResponse : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Common;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;
    private const int UpgradedBonus = 2;

    private Creature? _previewTarget;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Sluggish, ClosureKeywords.Block];

    public bool HasPreviewTarget => _previewTarget is not null;
    public int PreviewAmount => _previewTarget is null ? 0 : CalculateAmount(_previewTarget);

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/AdaptiveResponse.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Computed("PreviewAmount", 0, card => card is AdaptiveResponse adaptiveResponse ? adaptiveResponse.PreviewAmount : 0)
    ];

    public AdaptiveResponse() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    internal void SetPreviewTarget(Creature? target)
    {
        if (ReferenceEquals(_previewTarget, target))
        {
            return;
        }

        _previewTarget = target;
        this.RequestVisualReload();
    }

    internal int CalculateAmount(Creature target)
    {
        return GetTargetSluggish(target) + (IsUpgraded ? UpgradedBonus : 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        int amount = CalculateAmount(cardPlay.Target);
        if (amount > 0)
        {
            await DamageCmd.Attack(amount)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        await CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Unpowered, null);
    }

    private static int GetTargetSluggish(Creature target)
    {
        return target.Powers
            .OfType<SluggishPower>()
            .Where(power => power.Amount > 0)
            .Sum(power => power.Amount);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class AdaptiveResponseDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not AdaptiveResponse adaptiveResponse)
        {
            return;
        }

        __result = new LocString(
            "cards",
            adaptiveResponse.HasPreviewTarget
                ? "CLOSURE_MOD_CARD_ADAPTIVE_RESPONSE.descriptionPreview"
                : adaptiveResponse.IsUpgraded
                    ? "CLOSURE_MOD_CARD_ADAPTIVE_RESPONSE.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_ADAPTIVE_RESPONSE.description");
    }
}

[HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.OnCreatureHover))]
internal static class AdaptiveResponseCreatureHoverPatch
{
    private static void Prefix(NCardPlay __instance, NCreature creature)
    {
        if (__instance.Card is AdaptiveResponse adaptiveResponse)
        {
            adaptiveResponse.SetPreviewTarget(creature.Entity);
        }
    }
}

[HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.OnCreatureUnhover))]
internal static class AdaptiveResponseCreatureUnhoverPatch
{
    private static void Prefix(NCardPlay __instance)
    {
        ClearPreview(__instance);
    }

    internal static void ClearPreview(NCardPlay cardPlay)
    {
        if (cardPlay.Card is AdaptiveResponse adaptiveResponse)
        {
            adaptiveResponse.SetPreviewTarget(null);
        }
    }
}

[HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.ClearTarget))]
internal static class AdaptiveResponseClearTargetPatch
{
    private static void Postfix(NCardPlay __instance)
    {
        AdaptiveResponseCreatureUnhoverPatch.ClearPreview(__instance);
    }
}

[HarmonyPatch(typeof(NCardPlay), "OnCancelPlayCard")]
internal static class AdaptiveResponseCancelPlayCardPatch
{
    private static void Prefix(NCardPlay __instance)
    {
        AdaptiveResponseCreatureUnhoverPatch.ClearPreview(__instance);
    }
}

[HarmonyPatch(typeof(NCardPlay), "Cleanup")]
internal static class AdaptiveResponseCleanupPatch
{
    private static void Prefix(NCardPlay __instance)
    {
        AdaptiveResponseCreatureUnhoverPatch.ClearPreview(__instance);
    }
}
