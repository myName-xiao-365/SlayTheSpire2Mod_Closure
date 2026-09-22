using ClosureMod.Characters;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class EnhancedAttackModule : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.RandomEnemy;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/EnhancedAttackModule.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleHp", 5),
        new DamageVar(2, ValueProp.Move),
        new DynamicVar("Hits", 6)
    ];

    public EnhancedAttackModule() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DroneSwarmManager.Summon(
            choiceContext,
            Owner,
            DroneModuleKind.Attack,
            (int)DynamicVars["ModuleHp"].BaseValue,
            this);

        for (int i = 0; i < (int)DynamicVars["Hits"].BaseValue; i++)
        {
            Creature? target = Owner.RunState.Rng.CombatTargets.NextItem(
                CombatState?.HittableEnemies.ToList() ?? []);
            if (target is null)
            {
                return;
            }

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(2);
    }
}
