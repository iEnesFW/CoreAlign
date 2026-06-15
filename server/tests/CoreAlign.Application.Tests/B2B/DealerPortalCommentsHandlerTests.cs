using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Application.B2B.PortalComments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerPortalCommentsHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid AuthorUserId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICommentRepository _comments = Substitute.For<ICommentRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrderCommentPostedOutbox _outbox = Substitute.For<IOrderCommentPostedOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public DealerPortalCommentsHandlerTests()
    {
        _scope.GetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerAccountId);
        _currentUser.UserIdOrThrow().Returns(AuthorUserId);
    }

    [Fact]
    public async Task List_returns_comments_for_an_order_the_dealer_submitted()
    {
        var order = BuildOrder(DealerAccountId);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var existing = new Comment("Order", order.Id, AuthorUserId, "Spec confirmed") { Id = Guid.NewGuid(), TenantId = TenantId };
        _comments.ListByEntityAsync("Order", order.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { existing });

        _users.GetByIdAsync(AuthorUserId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(AuthorUserId));

        var sut = new ListDealerPortalOrderCommentsHandler(_scope, _orders, _comments, _users);

        var dtos = await sut.Handle(new ListDealerPortalOrderCommentsQuery(order.Id), default);

        dtos.Should().HaveCount(1);
        dtos.Single().Body.Should().Be("Spec confirmed");
    }

    [Fact]
    public async Task List_returns_404_for_an_order_not_submitted_by_this_dealer()
    {
        var foreignDealerId = Guid.NewGuid();
        var foreign = BuildOrder(foreignDealerId);
        _orders.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        var sut = new ListDealerPortalOrderCommentsHandler(_scope, _orders, _comments, _users);

        var act = async () => await sut.Handle(new ListDealerPortalOrderCommentsQuery(foreign.Id), default);
        await act.Should().ThrowAsync<OrderNotFoundException>();

        await _comments.DidNotReceive().ListByEntityAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_creates_comment_and_enqueues_outbox_for_customer_fan_out()
    {
        var order = BuildOrder(DealerAccountId);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        Comment? captured = null;
        await _comments.AddAsync(Arg.Do<Comment>(c => captured = c), Arg.Any<CancellationToken>());

        OrderCommentPostedPayload? capturedPayload = null;
        await _outbox.EnqueueAsync(Arg.Do<OrderCommentPostedPayload>(p => capturedPayload = p), Arg.Any<CancellationToken>());

        var sut = new PostDealerPortalOrderCommentHandler(_scope, _currentUser, _orders, _comments, _users, _outbox, _uow);

        var dto = await sut.Handle(new PostDealerPortalOrderCommentCommand(order.Id, "Can you confirm the quantity?"), default);

        captured.Should().NotBeNull();
        captured!.AuthorUserId.Should().Be(AuthorUserId);
        capturedPayload.Should().NotBeNull();
        capturedPayload!.AuthorPersona.Should().Be("dealer");
        capturedPayload.CustomerId.Should().Be(CustomerId);
        capturedPayload.OriginDealerAccountId.Should().Be(DealerAccountId);
        dto.Body.Should().Be("Can you confirm the quantity?");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_returns_404_for_an_order_not_owned_by_this_dealer()
    {
        var foreign = BuildOrder(Guid.NewGuid());
        _orders.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        var sut = new PostDealerPortalOrderCommentHandler(_scope, _currentUser, _orders, _comments, _users, _outbox, _uow);

        var act = async () => await sut.Handle(new PostDealerPortalOrderCommentCommand(foreign.Id, "hi"), default);
        await act.Should().ThrowAsync<OrderNotFoundException>();

        await _comments.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<OrderCommentPostedPayload>(), Arg.Any<CancellationToken>());
    }

    private static Order BuildOrder(Guid originDealerId)
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        order.MarkOrigin(OrderOriginPersona.Dealer, null, originDealerId, Guid.NewGuid());
        return order;
    }

    private static User BuildUser(Guid userId)
    {
        return new User(TenantId, "u" + Guid.NewGuid().ToString("N").Substring(0, 6), $"u{Guid.NewGuid():N}@x.test", "hash")
        {
            Id = userId,
            FirstName = "Bayi",
            LastName = "Kullanıcı",
            IsActive = true,
        };
    }
}
