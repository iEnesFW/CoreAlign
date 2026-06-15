using System.Text.RegularExpressions;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record NotificationRecipient(
    GlassNotificationRecipientKind Kind,
    string Address,
    string? DisplayName,
    string Locale);

public record NotificationDispatchRequest(
    Guid ProjectId,
    GlassNotificationEventCode EventCode,
    NotificationRecipient Recipient,
    GlassNotificationChannel Channel,
    IReadOnlyDictionary<string, string?> Placeholders);

public record NotificationDispatchResult(
    Guid LogId,
    GlassNotificationStatus Status,
    string? ProviderMessageId,
    string? ErrorMessage);

public interface INotificationChannelSender
{
    GlassNotificationChannel Channel { get; }
    Task<(string? ProviderMessageId, string? ErrorMessage)> SendAsync(
        string recipientAddress,
        string? subject,
        string body,
        CancellationToken cancellationToken);
}

public interface INotificationDispatcher
{
    Task<NotificationDispatchResult> DispatchAsync(
        NotificationDispatchRequest request,
        CancellationToken cancellationToken = default);

    Task RetryFailedAsync(int maxRetries, int batchSize, CancellationToken cancellationToken = default);
}

public class NotificationDispatcher : INotificationDispatcher
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{\s*([a-zA-Z0-9_\.]+)\s*\}\}", RegexOptions.Compiled);

    private readonly IGlassNotificationTemplateRepository _templateRepo;
    private readonly IGlassNotificationLogRepository _logRepo;
    private readonly IEnumerable<INotificationChannelSender> _senders;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IGlassNotificationTemplateRepository templateRepo,
        IGlassNotificationLogRepository logRepo,
        IEnumerable<INotificationChannelSender> senders,
        ILogger<NotificationDispatcher> logger)
    {
        _templateRepo = templateRepo;
        _logRepo = logRepo;
        _senders = senders;
        _logger = logger;
    }

    public async Task<NotificationDispatchResult> DispatchAsync(
        NotificationDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.ResolveAsync(
            request.EventCode, request.Channel, request.Recipient.Locale, cancellationToken);
        if (template is null)
        {
            return await PersistResultAsync(
                request,
                template: null,
                GlassNotificationStatus.Failed,
                providerMessageId: null,
                errorMessage: $"No template registered for {request.EventCode}/{request.Channel}/{request.Recipient.Locale}",
                cancellationToken);
        }

        var subject = string.IsNullOrWhiteSpace(template.SubjectTemplate)
            ? null
            : Render(template.SubjectTemplate!, request.Placeholders);
        var body = Render(template.BodyTemplate, request.Placeholders);

        var sender = _senders.FirstOrDefault(s => s.Channel == request.Channel);
        if (sender is null)
        {
            return await PersistResultAsync(
                request,
                template,
                GlassNotificationStatus.Failed,
                providerMessageId: null,
                errorMessage: $"No channel sender registered for {request.Channel}",
                cancellationToken);
        }

        try
        {
            var (providerId, error) = await sender.SendAsync(
                request.Recipient.Address, subject, body, cancellationToken);
            var status = string.IsNullOrEmpty(error)
                ? GlassNotificationStatus.Sent
                : GlassNotificationStatus.Failed;
            return await PersistResultAsync(request, template, status, providerId, error, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification dispatch failed for {Event}/{Channel}", request.EventCode, request.Channel);
            return await PersistResultAsync(
                request, template,
                GlassNotificationStatus.Failed,
                providerMessageId: null,
                errorMessage: ex.Message,
                cancellationToken);
        }
    }

    public async Task RetryFailedAsync(int maxRetries, int batchSize, CancellationToken cancellationToken = default)
    {
        var batch = await _logRepo.ListFailedForRetryAsync(maxRetries, batchSize, cancellationToken);
        foreach (var entry in batch)
        {
            var sender = _senders.FirstOrDefault(s => s.Channel == entry.Channel);
            if (sender is null)
            {
                entry.MarkFailed($"No sender for {entry.Channel}");
                _logRepo.Update(entry);
                continue;
            }
            try
            {
                var (providerId, error) = await sender.SendAsync(
                    entry.RecipientAddress, subject: null, body: entry.PayloadJson, cancellationToken);
                if (string.IsNullOrEmpty(error))
                {
                    entry.MarkSent(providerId);
                }
                else
                {
                    entry.MarkFailed(error);
                }
                _logRepo.Update(entry);
            }
            catch (Exception ex)
            {
                entry.MarkFailed(ex.Message);
                _logRepo.Update(entry);
            }
        }
    }

    private async Task<NotificationDispatchResult> PersistResultAsync(
        NotificationDispatchRequest request,
        GlassNotificationTemplate? template,
        GlassNotificationStatus status,
        string? providerMessageId,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(request.Placeholders);
        var log = new GlassNotificationLog(
            request.ProjectId,
            request.EventCode,
            request.Channel,
            request.Recipient.Kind,
            request.Recipient.Address,
            payload,
            template?.Id);

        if (status == GlassNotificationStatus.Sent)
        {
            log.MarkSent(providerMessageId);
        }
        else
        {
            log.MarkFailed(errorMessage ?? "unknown");
        }
        await _logRepo.AddAsync(log, cancellationToken);
        return new NotificationDispatchResult(log.Id, status, providerMessageId, errorMessage);
    }

    private static string Render(string template, IReadOnlyDictionary<string, string?> placeholders)
    {
        return PlaceholderPattern.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return placeholders.TryGetValue(key, out var value) ? value ?? string.Empty : match.Value;
        });
    }
}
