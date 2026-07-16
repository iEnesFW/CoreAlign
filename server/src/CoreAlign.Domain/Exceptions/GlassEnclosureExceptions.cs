namespace CoreAlign.Domain.Exceptions;

public class GlassProjectNotFoundException : NotFoundException
{
    public GlassProjectNotFoundException() : base("Glass enclosure project not found.") { }
}

public class GlassProjectCustomerMismatchException : ForbiddenException
{
    public GlassProjectCustomerMismatchException() : base("Customer does not belong to the current tenant.") { }
}

public class GlassProjectInvalidStatusTransitionException : DomainException
{
    public GlassProjectInvalidStatusTransitionException(string fromStatus, string toStatus)
        : base($"Cannot transition glass project from {fromStatus} to {toStatus}.")
    {
    }
}

public class GlassProjectTemplateNotFoundException : NotFoundException
{
    public GlassProjectTemplateNotFoundException() : base("Glass project template not found.") { }
}

public class GlassProjectTemplateInvalidException : DomainException
{
    public GlassProjectTemplateInvalidException(string message) : base(message) { }
}

public class GlassSystemNotCompatibleWithOpeningException : DomainException
{
    public GlassSystemNotCompatibleWithOpeningException(string systemType, string openingType)
        : base($"System '{systemType}' does not support opening type '{openingType}'.")
    {
    }
}

public class GlassPanelExceedsSystemMaxWidthException : DomainException
{
    public GlassPanelExceedsSystemMaxWidthException(int panelWidthMm, int maxWidthMm)
        : base($"Panel width {panelWidthMm} mm exceeds system maximum of {maxWidthMm} mm.")
    {
    }
}

public class GlassPanelExceedsSystemMaxWeightException : DomainException
{
    public GlassPanelExceedsSystemMaxWeightException(decimal panelKg, decimal maxKg)
        : base($"Panel weight {panelKg:F2} kg exceeds system maximum of {maxKg:F2} kg.")
    {
    }
}

public class GlassThicknessNotSupportedException : DomainException
{
    public GlassThicknessNotSupportedException(int thicknessMm, string supported)
        : base($"Glass thickness {thicknessMm} mm not supported by system. Supported: {supported}.")
    {
    }
}

public class GlassAreaExceedsMaxException : DomainException
{
    public GlassAreaExceedsMaxException(decimal areaM2, decimal maxM2)
        : base($"Glass panel area {areaM2:F2} m² exceeds maximum {maxM2:F2} m² for this glass type.")
    {
    }
}

public class GlassWindLoadInsufficientException : DomainException
{
    public GlassWindLoadInsufficientException(decimal pressurePa, int currentMm, int requiredMm)
        : base($"Wind load {pressurePa:F0} Pa requires {requiredMm} mm glass; current thickness {currentMm} mm is insufficient.")
    {
    }
}

public class GlassHingeCapacityExceededException : DomainException
{
    public GlassHingeCapacityExceededException(decimal panelKg, decimal hingeMaxKg)
        : base($"Panel weight {panelKg:F2} kg exceeds hinge maximum load {hingeMaxKg:F2} kg.")
    {
    }
}

public class GlassHardwareNotCompatibleException : DomainException
{
    public GlassHardwareNotCompatibleException(string hardwareName, string systemName)
        : base($"Hardware '{hardwareName}' is not compatible with system '{systemName}'.")
    {
    }
}

public class GlassRunConnectionAngleInvalidException : DomainException
{
    public GlassRunConnectionAngleInvalidException(decimal angleDeg)
        : base($"Run connection mitre angle {angleDeg}° is outside the allowed range [10°, 80°].")
    {
    }
}

public class GlassCuttingPlanGenerationFailedException : DomainException
{
    public GlassCuttingPlanGenerationFailedException(string reason)
        : base($"Cutting plan generation failed: {reason}")
    {
    }
}

public class GlassCatalogImportValidationException : DomainException
{
    public GlassCatalogImportValidationException(int errorCount)
        : base($"Catalog import contains {errorCount} validation error(s). Please review the dry-run report.")
    {
    }
}

public class GlassShareTokenExpiredException : DomainException
{
    public GlassShareTokenExpiredException() : base("Share token has expired or is invalid.") { }
}

public class GlassShareTokenRateLimitException : DomainException
{
    public GlassShareTokenRateLimitException()
        : base("Too many requests from this address. Please try again later.")
    {
    }
}

public class GlassFieldSurveyNotApplicableException : DomainException
{
    public GlassFieldSurveyNotApplicableException()
        : base("Field survey can only be applied to a project in Draft or Surveyed status.")
    {
    }
}

public class GlassStockInsufficientForOrderException : ConflictException
{
    public GlassStockInsufficientForOrderException(int shortageCount)
        : base($"Cannot convert project to order: {shortageCount} stock shortage(s) detected. Review the suggested purchase orders.")
    {
    }
}

public class GlassWorkOrderScheduleConflictException : ConflictException
{
    public GlassWorkOrderScheduleConflictException(DateTime suggestedDate)
        : base($"Workshop capacity is full for the requested date. Next available slot: {suggestedDate:yyyy-MM-dd}.")
    {
    }
}

public class GlassFormulaEvaluationException : DomainException
{
    public GlassFormulaEvaluationException(string formula, string reason)
        : base($"Failed to evaluate formula '{formula}': {reason}")
    {
    }
}

public class GlassNotificationDeliveryException : DomainException
{
    public GlassNotificationDeliveryException(string channel, string reason)
        : base($"Failed to deliver notification via {channel}: {reason}")
    {
    }
}

public class GlassQuoteAlreadyAcceptedException : ConflictException
{
    public GlassQuoteAlreadyAcceptedException()
        : base("This quote has already been accepted and cannot be modified.")
    {
    }
}

public class GlassKvkkAnonymizeFailedException : DomainException
{
    public GlassKvkkAnonymizeFailedException(string reason)
        : base($"Failed to anonymize project data: {reason}")
    {
    }
}

public class GlassWindZoneNotFoundForAddressException : NotFoundException
{
    public GlassWindZoneNotFoundForAddressException()
        : base("Could not determine wind zone for the provided address. Please select manually.")
    {
    }
}

public class GlassTenantOnboardingIncompleteException : DomainException
{
    public GlassTenantOnboardingIncompleteException()
        : base("Glass enclosure module setup is incomplete. Please finish the onboarding wizard before creating projects.")
    {
    }
}

public class GlassEnclosureNotFoundException : NotFoundException
{
    public GlassEnclosureNotFoundException(string resource)
        : base($"{resource} not found.")
    {
    }
}

public class GlassEnclosureDuplicateCodeException : ConflictException
{
    public GlassEnclosureDuplicateCodeException(string resource, string code)
        : base($"{resource} with code '{code}' already exists.")
    {
    }
}

public class GlassBomLineNotFoundException : NotFoundException
{
    public GlassBomLineNotFoundException() : base("BOM line not found for this project.") { }
}

public class GlassBomLineNotManualException : ConflictException
{
    public GlassBomLineNotManualException()
        : base("Only manually added BOM lines can be deleted.") { }
}

public class GlassBomLinePushNotAllowedException : ConflictException
{
    public GlassBomLinePushNotAllowedException(string reason)
        : base($"Cannot push BOM line price to catalog: {reason}") { }
}

public class ProjectTemplateNotFoundException : NotFoundException
{
    public ProjectTemplateNotFoundException() : base("Project template not found.") { }
}

public class MarketplaceTemplateNotPublishedException : ConflictException
{
    public MarketplaceTemplateNotPublishedException()
        : base("Marketplace template is not in published state.") { }
}

public class MarketplaceTemplateInvalidStateException : ConflictException
{
    public MarketplaceTemplateInvalidStateException(string action, string visibility)
        : base($"Marketplace template cannot be {action} from state '{visibility}'.") { }
}

public class MarketplaceCannotSubmitGlobalTemplateException : ConflictException
{
    public MarketplaceCannotSubmitGlobalTemplateException()
        : base("Global system templates cannot be submitted to marketplace.") { }
}

public class MarketplaceCannotInstallOwnSubmissionException : ConflictException
{
    public MarketplaceCannotInstallOwnSubmissionException()
        : base("Tenant cannot install its own marketplace submission.") { }
}
