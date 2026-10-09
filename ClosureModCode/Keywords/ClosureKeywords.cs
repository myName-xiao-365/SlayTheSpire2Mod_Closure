using ArkBase.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ClosureMod.Keywords;

[RegisterOwnedCardKeyword(nameof(Debt), IconPath = "res://ClosureMod/images/powers/EnergyDebtPower.png")]
[RegisterOwnedCardKeyword(nameof(OverflowDamage))]
[RegisterOwnedCardKeyword(nameof(Block), IncludeInCardHoverTip = false)]
[RegisterOwnedCardKeyword(nameof(Vulnerable))]
[RegisterOwnedCardKeyword(nameof(Weak))]
[RegisterOwnedCardKeyword(nameof(Artifact))]
[RegisterOwnedCardKeyword(nameof(EliteTwo))]
[RegisterOwnedCardKeyword(nameof(UpgradeAction))]
[RegisterOwnedCardKeyword(nameof(AttackModule))]
[RegisterOwnedCardKeyword(nameof(DefenseModule))]
[RegisterOwnedCardKeyword(nameof(SupportModule))]
[RegisterOwnedCardKeyword(nameof(Inherit))]
[RegisterOwnedCardKeyword(nameof(Stun))]
[RegisterOwnedCardKeyword(nameof(Daze))]
[RegisterOwnedCardKeyword(nameof(HandRetain))]
public sealed class ClosureKeywords
{
    public const string SluggishId = ArkKeywords.SluggishId;
    public const string DebtId = "CLOSURE_MOD_KEYWORD_DEBT";
    public const string OverflowDamageId = "CLOSURE_MOD_KEYWORD_OVERFLOW_DAMAGE";
    public const string BlockId = "CLOSURE_MOD_KEYWORD_BLOCK";
    public const string VulnerableId = "CLOSURE_MOD_KEYWORD_VULNERABLE";
    public const string WeakId = "CLOSURE_MOD_KEYWORD_WEAK";
    public const string ArtifactId = "CLOSURE_MOD_KEYWORD_ARTIFACT";
    public const string EliteTwoId = "CLOSURE_MOD_KEYWORD_ELITE_TWO";
    public const string UpgradeActionId = "CLOSURE_MOD_KEYWORD_UPGRADE_ACTION";
    public const string AttackModuleId = "CLOSURE_MOD_KEYWORD_ATTACK_MODULE";
    public const string DefenseModuleId = "CLOSURE_MOD_KEYWORD_DEFENSE_MODULE";
    public const string SupportModuleId = "CLOSURE_MOD_KEYWORD_SUPPORT_MODULE";
    public const string InheritId = "CLOSURE_MOD_KEYWORD_INHERIT";
    public const string StunId = "CLOSURE_MOD_KEYWORD_STUN";
    public const string DazeId = "CLOSURE_MOD_KEYWORD_DAZE";
    public const string HandRetainId = "CLOSURE_MOD_KEYWORD_HAND_RETAIN";
    public const string ShiverId = ArkKeywords.ShiverId;
    public const string ParalysisId = ArkKeywords.ParalysisId;
    public const string PoisonId = ArkKeywords.PoisonId;
    public const string RegenId = ArkKeywords.RegenId;
    public const string ForcedExitId = ArkKeywords.ForcedExitId;

    public static readonly CardKeyword Sluggish = ArkKeywords.Sluggish;

    public static readonly CardKeyword Debt =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Debt)).GetModCardKeyword();

    public static readonly CardKeyword OverflowDamage =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(OverflowDamage)).GetModCardKeyword();

    public static readonly CardKeyword Block =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Block)).GetModCardKeyword();

    public static readonly CardKeyword Vulnerable =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Vulnerable)).GetModCardKeyword();

    public static readonly CardKeyword Weak =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Weak)).GetModCardKeyword();

    public static readonly CardKeyword Artifact =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Artifact)).GetModCardKeyword();

    public static readonly CardKeyword EliteTwo =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(EliteTwo)).GetModCardKeyword();

    public static readonly CardKeyword UpgradeAction =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(UpgradeAction)).GetModCardKeyword();

    public static readonly CardKeyword AttackModule =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(AttackModule)).GetModCardKeyword();

    public static readonly CardKeyword DefenseModule =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(DefenseModule)).GetModCardKeyword();

    public static readonly CardKeyword SupportModule =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(SupportModule)).GetModCardKeyword();

    public static readonly CardKeyword Inherit =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Inherit)).GetModCardKeyword();

    public static readonly CardKeyword Stun =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Stun)).GetModCardKeyword();

    public static readonly CardKeyword Daze =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Daze)).GetModCardKeyword();

    public static readonly CardKeyword HandRetain =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(HandRetain)).GetModCardKeyword();

    public static readonly CardKeyword Shiver = ArkKeywords.Shiver;

    public static readonly CardKeyword Paralysis = ArkKeywords.Paralysis;

    public static readonly CardKeyword Poison = ArkKeywords.Poison;

    public static readonly CardKeyword Regen = ArkKeywords.Regen;

    public static readonly CardKeyword ForcedExit = ArkKeywords.ForcedExit;
}
