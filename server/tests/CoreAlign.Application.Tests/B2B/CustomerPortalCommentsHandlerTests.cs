using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.CustomerPortal;
using CoreAlign.Application.B2B.PortalComments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class CustomerPortalCommentsHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid AuthorUserId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICommentRepository _comments = Substitute.For<ICommentRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrderCommentPostedOutbox _outbox = Substitute.For<IOrderCommentPostedOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public CustomerPortalCommentsHandlerTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _currentUser.UserIdOrThrow().Returns(AuthorUserId);
    }

    [Fact]
    public async Task List_returns_comments_for_an_order_the_customer_owns()
    {
        var order = BuildOrder(CustomerId, withDealer: false);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var existing = new Comment("Order", order.Id, AuthorUserId, "Hello world") { Id = Guid.NewGuid(), TenantId = TenantId };
        _comments.ListByEntityAsync("Order", order.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { existing });

        _users.GetByIdAsync(AuthorUserId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(AuthorUserId, "Ayşe", "Yılmaz"));

        var sut = new ListCustomerPortalOrderCommentsHandler(_scope, _orders, _comments, _users);

        var dtos = await sut.Handle(new ListCustomerPortalOrderCommentsQuery(order.Id), default);

        dtos.Should().HaveCount(1);
        dtos.Single().Body.Should().Be("Hello world");
        dtos.Single().AuthorName.Should().Be("Ayşe Yılmaz");
    }

    [Fact]
    public async Task List_returns_404_for_an_order_the_customer_does_not_own()
    {
        var foreign = BuildOrder(customerId: Guid.NewGuid(), withDealer: false);
        _orders.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        var sut = new ListCustomerPortalOrderCommentsHandler(_scope, _orders, _comments, _users);

        var act = async () => await sut.Handle(new ListCustomerPortalOrderCommentsQuery(foreign.Id), default);
        await act.Should().ThrowAsync<OrderNotFoundException>();

        await _comments.DidNotReceive().ListByEntityAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_creates_a_comment_with_author_id_and_enqueues_outbox_for_dealer_orders()
    {
        var order = BuildOrder(CustomerId, withDealer: true);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        Comment? captured = null;
        await _comments.AddAsync(Arg.Do<Comment>(c => captured = c), Arg.Any<CancellationToken>());

        OrderCommentPostedPayload? capturedPayload = null;
        await _outbox.EnqueueAsync(Arg.Do<OrderCommentPostedPayload>(p => capturedPayload = p), Arg.Any<CancellationToken>());

        var sut = new PostCustomerPortalOrderCommentHandler(_scope, _currentUser, _orders, _comments, _users, _outbox, _uow);

        var dto = await sut.Handle(new PostCustomerPortalOrderCommentCommand(order.Id, "Please confirm the spec."), default);

        captured.Should().NotBeNull();
        captured!.AuthorUserId.Should().Be(AuthorUserId);
        captured.Body.Should().Be("Please confirm the spec.");
        capturedPayload.Should().NotBeNull();
        capturedPayload!.AuthorPersona.Should().Be("customer");
        capturedPayload.OriginDealerAccountId.Should().Be(DealerAccountId);
        capturedPayload.CustomerId.Should().Be(CustomerId);
        dto.Body.Should().Be("Please confirm the spec.");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_returns_404_for_an_order_the_customer_does_not_own()
    {
        var foreign = BuildOrder(customerId: Guid.NewGuid(), withDealer: false);
        _orders.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        var sut = new PostCustomerPortalOrderCommentHandler(_scope, _currentUser, _orders, _comments, _users, _outbox, _uow);

        var act = async () => await sut.Handle(new PostCustomerPortalOrderCommentCommand(foreign.Id, "hi"), default);
        await act.Should().ThrowAsync<OrderNotFoundException>();

        await _comments.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<OrderCommentPostedPayload>(), Arg.Any<CancellationToken>());
    }

    private static Order BuildOrder(Guid customerId, bool withDealer)
    {
        var order = new Order("ORD-1", customerId, DateTime.UtcNow, "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        if (withDealer)
        {
            order.MarkOrigin(OrderOriginPersona.Dealer, null, DealerAccountId, Guid.NewGuid());
        }
        return order;
    }

    private static User BuildUser(Guid userId, string firstName, string lastName)
    {
        var user = new User(TenantId, "u" + Guid.NewGuid().ToString("N").Substring(0, 6), $"u{Guid.NewGuid():N}@x.test", "hash")
        {
            Id = userId,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
        };
        return user;
    }
}
