using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ProjectTemplateReview : TenantEntity, IHasConcurrencyToken
{
    public Guid TemplateId { get; private set; }
    public Guid ReviewerUserId { get; private set; }
    public int RatingStars { get; private set; }
    public string? CommentMd { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; } = DateTime.UtcNow;
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected ProjectTemplateReview() { }

    public ProjectTemplateReview(
        Guid templateId,
        Guid reviewerUserId,
        int ratingStars,
        string? commentMd)
    {
        if (ratingStars is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(ratingStars), "GlassEnclosure.Marketplace.Review.RatingOutOfRange");
        }
        TemplateId = templateId;
        ReviewerUserId = reviewerUserId;
        RatingStars = ratingStars;
        CommentMd = commentMd;
        ReviewedAtUtc = DateTime.UtcNow;
    }

    public void UpdateRating(int ratingStars, string? commentMd)
    {
        if (ratingStars is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(ratingStars), "GlassEnclosure.Marketplace.Review.RatingOutOfRange");
        }
        RatingStars = ratingStars;
        CommentMd = commentMd;
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
