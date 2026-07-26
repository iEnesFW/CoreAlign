using CoreAlign.API.HostedServices;
using CoreAlign.Application.Feedback;
using CoreAlign.Application.Feedback.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Feedback;

public class FeedbackStatusFsmTests
{
    private static FeedbackTicket Ticket() =>
        new(FeedbackType.Bug, "t", "d", FeedbackPriority.Medium);

    private static FeedbackTicket At(FeedbackStatus status)
    {
        var t = Ticket();
        foreach (var step in PathTo(status))
        {
            t.ChangeStatus(step, null);
        }
        return t;
    }

    private static IEnumerable<FeedbackStatus> PathTo(FeedbackStatus status) => status switch
    {
        FeedbackStatus.Open => [],
        FeedbackStatus.InProgress => [FeedbackStatus.InProgress],
        FeedbackStatus.Resolved => [FeedbackStatus.Resolved],
        FeedbackStatus.Rejected => [FeedbackStatus.Rejected],
        FeedbackStatus.Closed => [FeedbackStatus.Resolved, FeedbackStatus.Closed],
        _ => [],
    };

    [Theory]
    [InlineData(FeedbackStatus.Open, FeedbackStatus.InProgress)]
    [InlineData(FeedbackStatus.Open, FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Open, FeedbackStatus.Rejected)]
    [InlineData(FeedbackStatus.InProgress, FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.InProgress, FeedbackStatus.Rejected)]
    [InlineData(FeedbackStatus.InProgress, FeedbackStatus.Open)]
    [InlineData(FeedbackStatus.Resolved, FeedbackStatus.Closed)]
    [InlineData(FeedbackStatus.Resolved, FeedbackStatus.InProgress)]
    [InlineData(FeedbackStatus.Rejected, FeedbackStatus.Open)]
    public void Allowed_transition_moves_the_ticket(FeedbackStatus from, FeedbackStatus to)
    {
        var ticket = At(from);
        ticket.ChangeStatus(to, null);
        ticket.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(FeedbackStatus.Open, FeedbackStatus.Closed)]
    [InlineData(FeedbackStatus.InProgress, FeedbackStatus.Closed)]
    [InlineData(FeedbackStatus.Resolved, FeedbackStatus.Open)]
    [InlineData(FeedbackStatus.Resolved, FeedbackStatus.Rejected)]
    [InlineData(FeedbackStatus.Rejected, FeedbackStatus.InProgress)]
    [InlineData(FeedbackStatus.Rejected, FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Closed, FeedbackStatus.Open)]
    [InlineData(FeedbackStatus.Closed, FeedbackStatus.InProgress)]
    [InlineData(FeedbackStatus.Closed, FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Closed, FeedbackStatus.Rejected)]
    public void Rejected_transition_throws_and_leaves_the_status_alone(
        FeedbackStatus from,
        FeedbackStatus to)
    {
        var ticket = At(from);
        var act = () => ticket.ChangeStatus(to, null);
        act.Should().Throw<InvalidFeedbackStatusTransitionException>();
        ticket.Status.Should().Be(from);
    }

    [Fact]
    public void Repeating_the_current_status_is_a_no_op_not_a_conflict()
    {
        var ticket = At(FeedbackStatus.Resolved);
        var resolvedAt = ticket.ResolvedAtUtc;
        var revision = ticket.StatusChangeCount;

        ticket.ChangeStatus(FeedbackStatus.Resolved, "note");

        ticket.Status.Should().Be(FeedbackStatus.Resolved);
        ticket.ResolvedAtUtc.Should().Be(resolvedAt);
        ticket.StatusChangeCount.Should().Be(revision);
        ticket.AdminResponse.Should().Be("note");
    }

    [Fact]
    public void Reopening_a_resolved_ticket_keeps_the_original_resolution_time()
    {
        var ticket = At(FeedbackStatus.Resolved);
        var resolvedAt = ticket.ResolvedAtUtc;
        resolvedAt.Should().NotBeNull();

        ticket.ChangeStatus(FeedbackStatus.InProgress, null);

        ticket.ResolvedAtUtc.Should().Be(resolvedAt);
    }

    [Fact]
    public void Every_status_change_advances_the_revision_and_the_concurrency_token()
    {
        var ticket = Ticket();
        ticket.ChangeStatus(FeedbackStatus.InProgress, null);
        ticket.ChangeStatus(FeedbackStatus.Open, null);
        ticket.ChangeStatus(FeedbackStatus.InProgress, null);

        // Open -> InProgress -> Open -> InProgress must NOT hash identically, or the dispatcher
        // swallows the repeat notification as a duplicate.
        ticket.StatusChangeCount.Should().Be(3);
        ticket.ConcurrencyToken.Should().Be(3);
    }

    [Fact]
    public void Allowed_next_statuses_never_include_the_current_one()
    {
        foreach (var status in Enum.GetValues<FeedbackStatus>())
        {
            FeedbackTicket.IsTransitionAllowed(status, status).Should().BeFalse();
        }
    }
}

public class FeedbackNotificationTemplateSeedTests
{
    [Fact]
    public void Every_dispatched_template_key_is_seeded()
    {
        // The dispatcher creates NOTHING when a template is missing and swallows the error, so an
        // unseeded key is a notification that never fires and never complains.
        FeedbackNotificationTemplateSeeder.SeededKeys
            .Should()
            .BeEquivalentTo(FeedbackTemplateKeys.All);
    }
}

public class AddFeedbackCommentHandlerTests
{
    private static FeedbackTicket Ticket(Guid? reporter = null)
    {
        var t = new FeedbackTicket(
            FeedbackType.Bug,
            "t",
            "d",
            FeedbackPriority.Medium,
            createdByUserId: reporter);
        return t;
    }

    private static (AddFeedbackCommentHandler Handler,
        IFeedbackCommentRepository Comments,
        IFeedbackNotificationOutbox Outbox) Build(FeedbackTicket ticket)
    {
        var tickets = Substitute.For<IFeedbackRepository>();
        tickets.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ticket);
        var comments = Substitute.For<IFeedbackCommentRepository>();
        var outbox = Substitute.For<IFeedbackNotificationOutbox>();
        var uow = Substitute.For<IUnitOfWork>();
        return (new AddFeedbackCommentHandler(tickets, comments, outbox, uow), comments, outbox);
    }

    [Fact]
    public async Task Comment_is_stored_and_the_reporter_is_notified()
    {
        var reporter = Guid.NewGuid();
        var (handler, comments, outbox) = Build(Ticket(reporter));

        var dto = await handler.Handle(
            new AddFeedbackCommentCommand(Guid.NewGuid(), " hello ", Guid.NewGuid(), "Admin", false, true),
            CancellationToken.None);

        dto.Body.Should().Be("hello");
        await comments.Received(1).AddAsync(Arg.Any<FeedbackTicketComment>(), Arg.Any<CancellationToken>());
        await outbox.Received(1).EnqueueAsync(
            Arg.Is<FeedbackNotificationPayload>(p => p.Kind == FeedbackNotificationKinds.CommentAdded),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Internal_note_never_notifies_the_reporter()
    {
        var (handler, _, outbox) = Build(Ticket(Guid.NewGuid()));

        await handler.Handle(
            new AddFeedbackCommentCommand(Guid.NewGuid(), "internal", Guid.NewGuid(), "Admin", true, true),
            CancellationToken.None);

        await outbox.DidNotReceive().EnqueueAsync(
            Arg.Any<FeedbackNotificationPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_non_platform_admin_cannot_write_an_internal_note()
    {
        var (handler, comments, _) = Build(Ticket(Guid.NewGuid()));

        var act = async () => await handler.Handle(
            new AddFeedbackCommentCommand(Guid.NewGuid(), "sneaky", Guid.NewGuid(), "User", true, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<FeedbackCommentForbiddenException>();
        await comments.DidNotReceive().AddAsync(
            Arg.Any<FeedbackTicketComment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commenting_on_your_own_ticket_does_not_notify_you()
    {
        var reporter = Guid.NewGuid();
        var (handler, _, outbox) = Build(Ticket(reporter));

        await handler.Handle(
            new AddFeedbackCommentCommand(Guid.NewGuid(), "my own note", reporter, "Me", false, false),
            CancellationToken.None);

        await outbox.DidNotReceive().EnqueueAsync(
            Arg.Any<FeedbackNotificationPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_ticket_is_a_404()
    {
        var tickets = Substitute.For<IFeedbackRepository>();
        tickets.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((FeedbackTicket?)null);
        var handler = new AddFeedbackCommentHandler(
            tickets,
            Substitute.For<IFeedbackCommentRepository>(),
            Substitute.For<IFeedbackNotificationOutbox>(),
            Substitute.For<IUnitOfWork>());

        var act = async () => await handler.Handle(
            new AddFeedbackCommentCommand(Guid.NewGuid(), "x", Guid.NewGuid(), "u", false, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<FeedbackNotFoundException>();
    }

    [Fact]
    public void An_empty_body_is_rejected_by_the_aggregate()
    {
        var act = () => new FeedbackTicketComment(Guid.NewGuid(), "   ", null, null, false);
        act.Should().Throw<ArgumentException>();
    }
}

public class AddFeedbackAttachmentsHandlerTests
{
    private static FeedbackUploadedFile File(string name) =>
        new($"feedback-attachments/x/{name}", name, "image/png", 1024);

    private static (AddFeedbackAttachmentsHandler Handler, IFeedbackAttachmentRepository Repo) Build(
        FeedbackTicket ticket,
        int existing)
    {
        var tickets = Substitute.For<IFeedbackRepository>();
        tickets.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ticket);
        var attachments = Substitute.For<IFeedbackAttachmentRepository>();
        attachments
            .CountByTicketAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        var uow = Substitute.For<IUnitOfWork>();
        return (new AddFeedbackAttachmentsHandler(tickets, attachments, uow), attachments);
    }

    [Fact]
    public async Task Multiple_files_are_stored_in_order()
    {
        var ticket = new FeedbackTicket(FeedbackType.Bug, "t", "d", FeedbackPriority.Low);
        var (handler, repo) = Build(ticket, 0);

        var result = await handler.Handle(
            new AddFeedbackAttachmentsCommand(
                Guid.NewGuid(),
                [File("a.png"), File("b.png")],
                Guid.NewGuid()),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].FileName.Should().Be("a.png");
        await repo.Received(2).AddAsync(Arg.Any<FeedbackAttachment>(), Arg.Any<CancellationToken>());
        // The first upload also fills the legacy single-attachment columns for backward compatibility.
        ticket.AttachmentFileName.Should().Be("a.png");
    }

    [Fact]
    public async Task The_per_ticket_cap_is_enforced()
    {
        var (handler, repo) = Build(new FeedbackTicket(FeedbackType.Bug, "t", "d", FeedbackPriority.Low), 4);

        var act = async () => await handler.Handle(
            new AddFeedbackAttachmentsCommand(Guid.NewGuid(), [File("a.png"), File("b.png")], null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FeedbackAttachmentLimitExceededException>();
        await repo.DidNotReceive().AddAsync(Arg.Any<FeedbackAttachment>(), Arg.Any<CancellationToken>());
    }
}

public class GetFeedbackAttachmentFileHandlerTests
{
    [Fact]
    public async Task An_attachment_belonging_to_another_ticket_is_not_served()
    {
        var attachment = new FeedbackAttachment(
            Guid.NewGuid(),
            "path",
            "a.png",
            "image/png",
            10,
            null,
            0);
        var repo = Substitute.For<IFeedbackAttachmentRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(attachment);
        var handler = new GetFeedbackAttachmentFileHandler(repo);

        var result = await handler.Handle(
            new GetFeedbackAttachmentFileQuery(Guid.NewGuid(), attachment.Id),
            CancellationToken.None);

        result.Should().BeNull();
    }
}
