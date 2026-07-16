using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class GlassProjectTemplateHandlerTests
{
    private readonly IGlassProjectTemplateRepository _repo = Substitute.For<IGlassProjectTemplateRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly Guid _userId = Guid.NewGuid();

    public GlassProjectTemplateHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
    }

    private SaveGlassProjectTemplateCommandHandler CreateSaveSut() => new(_repo, _currentUser);
    private DeleteGlassProjectTemplateCommandHandler CreateDeleteSut() => new(_repo, _currentUser);
    private GetGlassProjectTemplateByIdQueryHandler CreateGetSut() => new(_repo, _currentUser);
    private GetMyGlassProjectTemplatesQueryHandler CreateListSut() => new(_repo, _currentUser);

    [Fact]
    public async Task Saving_derives_counts_from_payload_and_owner_from_current_user()
    {
        GlassProjectTemplate? captured = null;
        await _repo.AddAsync(Arg.Do<GlassProjectTemplate>(t => captured = t), Arg.Any<CancellationToken>());
        var payload = """{"walls":[{},{},{}],"slabs":[{}],"runs":[{},{}]}""";

        var dto = await CreateSaveSut().Handle(
            new SaveGlassProjectTemplateCommand(new SaveGlassProjectTemplateDto("My room", payload)), default);

        captured.Should().NotBeNull();
        captured!.CreatedByUserId.Should().Be(_userId);
        captured.WallCount.Should().Be(3);
        captured.SlabCount.Should().Be(1);
        captured.RunCount.Should().Be(2);
        dto.Name.Should().Be("My room");
        dto.WallCount.Should().Be(3);
        await _repo.Received(1).AddAsync(Arg.Any<GlassProjectTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Saving_an_empty_scene_is_rejected()
    {
        var act = () => CreateSaveSut().Handle(
            new SaveGlassProjectTemplateCommand(new SaveGlassProjectTemplateDto("Empty", """{"walls":[],"slabs":[],"runs":[]}""")), default);

        await act.Should().ThrowAsync<GlassProjectTemplateInvalidException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<GlassProjectTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Saving_malformed_json_is_rejected()
    {
        var act = () => CreateSaveSut().Handle(
            new SaveGlassProjectTemplateCommand(new SaveGlassProjectTemplateDto("Bad", "not-json")), default);

        await act.Should().ThrowAsync<GlassProjectTemplateInvalidException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<GlassProjectTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleting_another_users_template_is_rejected_as_not_found()
    {
        var template = new GlassProjectTemplate("Theirs", Guid.NewGuid(), "{}", 1, 0, 0);
        _repo.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var act = () => CreateDeleteSut().Handle(new DeleteGlassProjectTemplateCommand(template.Id), default);

        await act.Should().ThrowAsync<GlassProjectTemplateNotFoundException>();
        _repo.DidNotReceive().Remove(Arg.Any<GlassProjectTemplate>());
    }

    [Fact]
    public async Task Deleting_own_template_removes_it()
    {
        var template = new GlassProjectTemplate("Mine", _userId, "{}", 1, 0, 0);
        _repo.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        await CreateDeleteSut().Handle(new DeleteGlassProjectTemplateCommand(template.Id), default);

        _repo.Received(1).Remove(template);
    }

    [Fact]
    public async Task Getting_another_users_template_returns_null()
    {
        var template = new GlassProjectTemplate("Theirs", Guid.NewGuid(), "{}", 1, 0, 0);
        _repo.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var result = await CreateGetSut().Handle(new GetGlassProjectTemplateByIdQuery(template.Id), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Listing_returns_only_the_current_users_templates()
    {
        _repo.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<GlassProjectTemplateListItem>
        {
            new(Guid.NewGuid(), "A", 2, 1, 0, DateTime.UtcNow, DateTime.UtcNow),
        });

        var result = await CreateListSut().Handle(new GetMyGlassProjectTemplatesQuery(), default);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("A");
        await _repo.Received(1).ListByUserAsync(_userId, Arg.Any<CancellationToken>());
    }
}
