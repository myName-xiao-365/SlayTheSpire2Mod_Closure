using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace ClosureMod.Utils;

internal static class RelatedPowerHoverTips
{
    public static IEnumerable<IHoverTip> For<TPower>(PowerModel source, string keywordId)
        where TPower : PowerModel
    {
        TPower? relatedPower = source.IsMutable
            ? source.Owner?.Powers.OfType<TPower>().FirstOrDefault()
            : null;

        // Native creature tooltips merge matching IDs and retain the live stack values.
        return relatedPower is not null
            ? relatedPower.HoverTips
            : new[] { keywordId }.ToHoverTips();
    }
}
