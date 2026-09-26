using ArkBase.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class BloodfiendPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/cards/Bloodfiend.jpg",
        BigIconPath: $"{Entry.ResPath}/images/cards/Bloodfiend.jpg");

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (!ReferenceEquals(Owner, player.Creature) || player.PlayerCombatState is not { } playerState)
        {
            return Task.CompletedTask;
        }

        List<SupportCardTemplate> supportCards = playerState.Hand.Cards
            .OfType<SupportCardTemplate>()
            .Where(card => !card.EnergyCost.CostsX && card.EnergyCost.GetResolved() > 0)
            .ToList();
        if (supportCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        foreach (SupportCardTemplate card in supportCards)
        {
            card.EnergyCost.AddThisCombat(-1, reduceOnly: true);
            card.RequestVisualReload();
        }

        return Task.CompletedTask;
    }
}
