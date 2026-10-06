using Pnioyot.Web.Models;

namespace Pnioyot.Web.Services;

public static class InquiryStatusRules
{
    public static readonly IReadOnlyDictionary<InquiryStatus, InquiryStatus[]> Allowed =
        new Dictionary<InquiryStatus, InquiryStatus[]>
        {
            [InquiryStatus.New] = [InquiryStatus.InProgress, InquiryStatus.Waiting],
            [InquiryStatus.InProgress] = [InquiryStatus.Waiting, InquiryStatus.Completed],
            [InquiryStatus.Waiting] = [InquiryStatus.InProgress, InquiryStatus.Completed],
            [InquiryStatus.Completed] = []
        };

    public static bool IsDefined(InquiryStatus status) => Enum.IsDefined(status);

    public static bool CanTransition(InquiryStatus from, InquiryStatus to)
        => from == to || (Allowed.TryGetValue(from, out var next) && next.Contains(to));

    public static string Label(InquiryStatus status) => status switch
    {
        InquiryStatus.New => "חדש",
        InquiryStatus.InProgress => "בטיפול",
        InquiryStatus.Waiting => "ממתין",
        InquiryStatus.Completed => "הושלם",
        _ => status.ToString()
    };

    public static IReadOnlyList<InquiryStatus> OptionsFor(InquiryStatus current)
    {
        var next = Allowed.GetValueOrDefault(current) ?? [];
        return new[] { current }.Concat(next).Distinct().ToArray();
    }
}
