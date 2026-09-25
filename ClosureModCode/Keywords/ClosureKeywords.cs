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
public sealed class ClosureKeywords
{
    public const string SluggishId = ArkKeywords.SluggishId;
    public const string DebtId = "CLOSURE_MOD_KEYWORD_DEBT";
    public const string OverflowDamageId = "CLOSURE_MOD_KEYWORD_OVERFLOW_DAMAGE";
    public const string BlockId = "CLOSURE_MOD_KEYWORD_BLOCK";
    public const string VulnerableId = "CLOSURE_MOD_KEYWORD_VULNERABLE";
    public const string WeakId = "CLOSURE_MOD_KEYWORD_WEAK";
    public const string ArtifactId = "CLOSURE_MOD_KEYWORD_ARTIFACT";
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

    public static readonly CardKeyword Shiver = ArkKeywords.Shiver;

    public static readonly CardKeyword Paralysis = ArkKeywords.Paralysis;

    public static readonly CardKeyword Poison = ArkKeywords.Poison;

    public static readonly CardKeyword Regen = ArkKeywords.Regen;

    public static readonly CardKeyword ForcedExit = ArkKeywords.ForcedExit;
}
