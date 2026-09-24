using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(EventCardPool))]
public sealed class DrawGame : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Event;
    private const TargetType CardTarget = TargetType.AllEnemies;
    private const bool ShowInCardLibrary = true;
    private const int DamagePerExhaustedCard = 5;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ClosureModStrike.png");

    public override int MaxUpgradeLevel => 0;
    public override bool ShouldReceiveCombatHooks => true;

    private bool _transformAfterEnteringResultPile;

    public DrawGame() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayerCombatState? combatState = Owner.PlayerCombatState;
        if (combatState is null)
        {
            return;
        }

        _transformAfterEnteringResultPile = true;

        List<CardModel> visibleHandCards = combatState.Hand.Cards
            .Where(card => !ReferenceEquals(card, this))
            .Distinct()
            .ToList();

        List<CardModel> hiddenPileCards = [.. combatState.DrawPile.Cards, .. combatState.DiscardPile.Cards];
        hiddenPileCards = hiddenPileCards.Distinct().ToList();

        int exhaustedCardCount = visibleHandCards.Count + hiddenPileCards.Count;

        foreach (CardModel card in visibleHandCards)
        {
            await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false, skipVisuals: false);
        }

        foreach (CardModel card in hiddenPileCards)
        {
            await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false, skipVisuals: false);
        }

        await ClearBuffsAndBlock(Owner.Creature);
        foreach (Creature enemy in CombatState?.HittableEnemies ?? [])
        {
            await ClearBuffsAndBlock(enemy);
        }

        await PlayerCmd.SetGold(0, Owner);
        foreach (PotionModel potion in Owner.Potions.ToList())
        {
            await PotionCmd.Discard(potion);
        }

        int newMaxHp = Math.Max(1, Owner.Creature.MaxHp / 2);
        await CreatureCmd.SetMaxHp(Owner.Creature, newMaxHp);
        await CreatureCmd.SetCurrentHp(Owner.Creature, 1);

        List<Creature> enemies = CombatState?.HittableEnemies.ToList() ?? [];
        int damage = exhaustedCardCount * DamagePerExhaustedCard;
        if (damage > 0 && enemies.Count > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemies,
                damage,
                ValueProp.Unpowered,
                Owner.Creature,
                this);
        }
    }

    public override async Task AfterCardChangedPilesLate(
        CardModel card,
        PileType previousPileType,
        AbstractModel? source)
    {
        if (!_transformAfterEnteringResultPile ||
            !ReferenceEquals(card, this) ||
            previousPileType != PileType.Play ||
            Pile?.Type != PileType.Discard)
        {
            return;
        }

        _transformAfterEnteringResultPile = false;
        await TransformDeckVersionIfPresent();
        await CardCmd.TransformTo<Trauma>(this, CardPreviewStyle.None);
    }

    private async Task TransformDeckVersionIfPresent()
    {
        if (DeckVersion is { HasBeenRemovedFromState: false } deckVersion &&
            !ReferenceEquals(deckVersion, this))
        {
            await CardCmd.TransformTo<Trauma>(deckVersion, CardPreviewStyle.None);
        }
    }

    private static async Task ClearBuffsAndBlock(Creature creature)
    {
        foreach (PowerModel power in creature.Powers
                     .Where(power => power.Type == PowerType.Buff || power.TypeForCurrentAmount == PowerType.Buff)
                     .ToList())
        {
            await PowerCmd.Remove(power);
        }

        if (creature.Block > 0)
        {
            await CreatureCmd.LoseBlock(creature, creature.Block);
        }
    }
}
