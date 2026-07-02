using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Dunning;

public class DunningSettingDto
{
    public DunningType Type { get; set; }
    public bool IsEnabled { get; set; }
    public bool SendInApp { get; set; }
    public bool SendEmail { get; set; }
    public List<Guid> RecipientUserIds { get; set; } = new();
}

public record ListDunningSettingsQuery : IRequest<IReadOnlyList<DunningSettingDto>>;

public record UpsertDunningSettingCommand(
    DunningType Type,
    bool IsEnabled,
    bool SendInApp,
    bool SendEmail,
    List<Guid> RecipientUserIds) : IRequest<DunningSettingDto>, ITransactionalRequest;

public class UpsertDunningSettingCommandValidator : AbstractValidator<UpsertDunningSettingCommand>
{
    public UpsertDunningSettingCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x)
            .Must(x => !x.IsEnabled || x.SendInApp || x.SendEmail)
            .WithMessage("Validation.AtLeastOneChannelRequired");
    }
}
