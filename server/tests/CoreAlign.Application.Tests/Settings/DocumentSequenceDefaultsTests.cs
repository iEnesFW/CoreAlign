using CoreAlign.Application.Settings;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Settings;

public class DocumentSequenceDefaultsTests
{
    [Fact]
    public async Task Listing_sequences_yields_a_real_prefix_for_every_document_type()
    {
        var repo = Substitute.For<IDocumentSequenceRepository>();
        repo.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<DocumentSequence>());
        var handler = new ListDocumentSequencesHandler(repo);

        var result = await handler.Handle(new ListDocumentSequencesQuery(), CancellationToken.None);

        var allTypes = Enum.GetValues<DocumentSequenceType>();
        result.Should().HaveCount(allTypes.Length);

        foreach (var dto in result)
        {
            dto.Prefix.Should().NotBeNullOrWhiteSpace();
            dto.Prefix.Should().NotBe(
                dto.Type.ToString(),
                "every DocumentSequenceType must have a real default prefix (not the enum name) so the numbering preview reads professionally before configuration");
            dto.Preview.Should().Contain(dto.Prefix);
        }
    }
}
