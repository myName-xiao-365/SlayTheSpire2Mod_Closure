using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class Overload : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;
    private bool _doubleCurrentHit;

    protected override bool HasEnergyCostX => true;
    public override bool ShouldReceiveCombatHooks => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/RecursiveAlgorithm.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move)
    ];

    public Overload() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        int hitCount = Math.Max(0, EnergyCost.CapturedXValue);
        if (hitCount == 0)
        {
            return;
        }

        int doubleDamageFromHit = IsUpgraded ? 3 : 4;
        int nextHit = 0;
        AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .Targeting(cardPlay.Target);
        attack.BeforeDamage(() =>
        {
            _doubleCurrentHit = ++nextHit >= doubleDamageFromHit;
            return Task.CompletedTask;
        });

        try
        {
            await attack.Execute(choiceContext);
        }
        finally
        {
            _doubleCurrentHit = false;
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return _doubleCurrentHit && ReferenceEquals(cardSource, this) &&
               ReferenceEquals(dealer, Owner.Creature) && props.IsPoweredAttack()
            ? 2m
            : 1m;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class OverloadDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is Overload overload)
        {
            __result = new LocString(
                "cards",
                overload.IsUpgraded
                    ? "CLOSURE_MOD_CARD_OVERLOAD.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_OVERLOAD.description");
        }
    }
}
