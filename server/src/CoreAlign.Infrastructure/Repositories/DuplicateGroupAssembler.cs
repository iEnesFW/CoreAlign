using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Repositories;

internal static class DuplicateGroupAssembler
{
    public static IReadOnlyList<DuplicateGroupRow> Build(
        List<(string Key, int Count)> groups,
        List<(string Key, Guid Id, string Name)> members) =>
        groups
            .Select(g => new DuplicateGroupRow(
                g.Key,
                g.Count,
                members
                    .Where(m => m.Key == g.Key)
                    .Select(m => new DuplicateMemberRow(m.Id, m.Name))
                    .ToList()))
            .ToList();
}
