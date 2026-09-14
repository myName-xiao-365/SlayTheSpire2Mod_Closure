using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
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

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Sluggish];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ClosureModStrike.png");

    public AdaptiveResponse() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        int amount = GetTargetSluggish(cardPlay.Target) + (IsUpgraded ? UpgradedBonus : 0);
        if (amount > 0)
        {
            await DamageCmd.Attack(amount)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        await CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Unpowered, null);
    }

    private static int GetTargetSluggish(MegaCrit.Sts2.Core.Entities.Creatures.Creature target)
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
            adaptiveResponse.IsUpgraded
                ? "CLOSURE_MOD_CARD_ADAPTIVE_RESPONSE.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_ADAPTIVE_RESPONSE.description");
    }
}
