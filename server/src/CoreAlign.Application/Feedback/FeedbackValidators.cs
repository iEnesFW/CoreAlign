using FluentValidation;

namespace CoreAlign.Application.Feedback;

public class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    public CreateFeedbackCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.StepsToReproduce).MaximumLength(2000);
        RuleFor(x => x.Module).MaximumLength(100);
        RuleFor(x => x.PageUrl).MaximumLength(500);
    }
}

public class AddFeedbackCommentCommandValidator : AbstractValidator<AddFeedbackCommentCommand>
{
    public AddFeedbackCommentCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.AuthorName).MaximumLength(200);
    }
}

public class AddFeedbackAttachmentsCommandValidator : AbstractValidator<AddFeedbackAttachmentsCommand>
{
    public AddFeedbackAttachmentsCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Files).NotEmpty();
        RuleForEach(x => x.Files).ChildRules(f =>
        {
            f.RuleFor(x => x.RelativePath).NotEmpty().MaximumLength(500);
            f.RuleFor(x => x.DisplayFileName).NotEmpty().MaximumLength(255);
            f.RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
            f.RuleFor(x => x.SizeBytes).GreaterThanOrEqualTo(0);
        });
    }
}

public class DeleteFeedbackAttachmentCommandValidator : AbstractValidator<DeleteFeedbackAttachmentCommand>
{
    public DeleteFeedbackAttachmentCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.AttachmentId).NotEmpty();
    }
}

public class UpdateFeedbackStatusCommandValidator : AbstractValidator<UpdateFeedbackStatusCommand>
{
    public UpdateFeedbackStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.AdminResponse).MaximumLength(4000);
    }
}
