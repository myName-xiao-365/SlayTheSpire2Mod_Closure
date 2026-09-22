using ClosureMod.Characters;
using ClosureMod.Patches;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Relics;

public abstract class WorkshopRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsAllowedInShops => false;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class GaulCheque : WorkshopRelic;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class GiftCard : WorkshopRelic
{
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal price)
    {
        if (!ReferenceEquals(Owner, player))
        {
            return price;
        }

        return WorkshopShopDiscounts.GetPrice(entry, price);
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class StructuralPrinciple : WorkshopRelic
{
    private bool _usedThisCombat;

    public override bool IsUsedUp => _usedThisCombat;

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public bool TryUse()
    {
        if (_usedThisCombat)
        {
            return false;
        }

        _usedThisCombat = true;
        Flash();
        return true;
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class SniperScope : WorkshopRelic
{
    private int _turns;

    public override bool ShowCounter => true;
    public override int DisplayAmount => _turns;

    public override Task BeforeCombatStart()
    {
        _turns = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (ReferenceEquals(Owner, player))
        {
            _turns++;
            Flash();
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return ReferenceEquals(dealer, Owner?.Creature) && props.IsPoweredAttack()
            ? 1m + _turns * 0.1m
            : 1m;
    }
}
