using CoreAlign.Application.Collaboration;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.B2B.PortalComments;

internal static class PortalCommentMapper
{
    private const int ExcerptLength = 100;

    public static CommentDto ToDto(Comment comment, IReadOnlyDictionary<Guid, User> authorLookup)
    {
        authorLookup.TryGetValue(comment.AuthorUserId, out var author);
        return new CommentDto(
            comment.Id,
            comment.EntityType,
            comment.EntityId,
            comment.AuthorUserId,
            DisplayNameFor(author),
            comment.Body,
            comment.ParentCommentId,
            comment.CreatedAtUtc,
            comment.EditedAtUtc);
    }

    public static string BuildExcerpt(string body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        var trimmed = body.Trim();
        return trimmed.Length <= ExcerptLength ? trimmed : trimmed[..ExcerptLength] + "...";
    }

    private static string DisplayNameFor(User? user)
    {
        if (user is null) return string.Empty;
        var first = user.FirstName?.Trim();
        var last = user.LastName?.Trim();
        if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
        {
            return string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrEmpty(s)));
        }
        return string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username;
    }
}
