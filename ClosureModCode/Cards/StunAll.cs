using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class StunAll : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AllEnemies;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        // Temporary shared art until a dedicated portrait is provided.
        PortraitPath: $"{Entry.ResPath}/images/cards/StunAll.png");

    public StunAll() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<Creature> targets = [Owner.Creature];
        targets.AddRange(CombatState?.Enemies.Where(enemy => enemy.IsAlive) ?? []);

        foreach (Creature target in targets.Distinct())
        {
            await SluggishPower.ClearSluggishAndStun(
                choiceContext,
                target,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(2);
    }
}
