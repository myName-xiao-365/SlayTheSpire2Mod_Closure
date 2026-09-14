using ClosureMod.Characters;
using ClosureMod.Keywords;
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
public sealed class SlowerStill : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Common;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, ClosureKeywords.Sluggish];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ClearDebt.png");

    public SlowerStill() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        if (IsUpgraded)
        {
            await PowerCmd.Apply<SluggishPower>(
                choiceContext,
                cardPlay.Target,
                1,
                Owner.Creature,
                this);
        }

        SluggishPower? sluggish = cardPlay.Target.Powers
            .OfType<SluggishPower>()
            .FirstOrDefault(power => power.Amount > 0);
        if (sluggish is null)
        {
            return;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            sluggish,
            sluggish.Amount,
            Owner.Creature,
            this);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class SlowerStillDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not SlowerStill slowerStill)
        {
            return;
        }

        __result = new LocString(
            "cards",
            slowerStill.IsUpgraded
                ? "CLOSURE_MOD_CARD_SLOWER_STILL.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_SLOWER_STILL.description");
    }
}
