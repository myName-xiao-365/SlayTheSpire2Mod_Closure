using HarmonyLib;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WaitForStartup : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Common;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/WaitForStartup.jpg");

    public WaitForStartup() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DroneSwarmPower? swarm = Owner.Creature.Powers.OfType<DroneSwarmPower>().FirstOrDefault();
        if (swarm is null)
        {
            return;
        }

        int attackHp = swarm.AttackModuleMaxHp;
        int defenseHp = swarm.DefenseModuleMaxHp;
        int supportHp = swarm.SupportModuleMaxHp;

        swarm.ClearModules();
        await PowerCmd.Remove(swarm);

        IHaveStarted started = Owner.Creature.CombatState?.CreateCard(ModelDb.Card<IHaveStarted>(), Owner) as IHaveStarted
                               ?? Owner.RunState.CreateCard<IHaveStarted>(Owner);
        started.Configure(attackHp, defenseHp, supportHp);
        if (IsUpgraded && started.IsUpgradable)
        {
            CardCmd.Upgrade(started, CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardToCombat(started, PileType.Hand, Owner);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class WaitForStartupDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not WaitForStartup waitForStartup)
        {
            return;
        }

        __result = new LocString(
            "cards",
            waitForStartup.IsUpgraded
                ? "CLOSURE_MOD_CARD_WAIT_FOR_STARTUP.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_WAIT_FOR_STARTUP.description");
    }
}
