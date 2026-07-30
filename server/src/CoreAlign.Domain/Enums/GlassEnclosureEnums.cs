namespace CoreAlign.Domain.Enums;

public enum GlassSystemType
{
    Folding = 0,
    Sliding = 1,
    HeatInsulatedSliding = 2,
    Guillotine = 3,
    Hinged = 4,
    Fixed = 5
}

public enum GlassOpeningType
{
    Fixed = 0,
    Folding = 1,
    SlidingLeft = 2,
    SlidingRight = 3,
    Hinged = 4,
    Guillotine = 5
}

public enum ProfileRole
{
    Top = 0,
    Bottom = 1,
    SideJamb = 2,
    Mullion = 3,
    Sash = 4,
    Adapter = 5,
    DripRail = 6,
    Corner = 7
}

public enum GlassStructure
{
    Tempered = 0,
    Laminated = 1,
    DoubleGlazed = 2,
    TripleGlazed = 3,
    LowE = 4
}

public enum HardwareCategoryKind
{
    Hinge = 0,
    Roller = 1,
    Lock = 2,
    Handle = 3,
    Gasket = 4,
    Brush = 5,
    Bumper = 6,
    WallBracket = 7,
    Chain = 8,
    DripCap = 9,
    CornerPost = 10,
    Other = 99
}

public enum ColorFinishType
{
    Anodized = 0,
    PowderCoated = 1,
    WoodLook = 2,
    Raw = 3
}

public enum CorrosionClass
{
    C1 = 0,
    C2 = 1,
    C3 = 2,
    C4 = 3,
    C5 = 4
}

public enum GlassProjectStatus
{
    Draft = 0,
    Surveyed = 1,
    Quoted = 2,
    Confirmed = 3,
    InProduction = 4,
    Ready = 5,
    InTransit = 6,
    Installed = 7,
    Defective = 8,
    Cancelled = 9
}

public enum GlassWorkOrderStatus
{
    Pending = 0,
    Cutting = 1,
    Assembling = 2,
    Ready = 3,
    InTransit = 4,
    Installed = 5,
    Defective = 6
}

public enum FieldSurveyStatus
{
    InProgress = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum DiscountScope
{
    CustomerGroup = 0,
    Coupon = 1,
    Volume = 2,
    DateRange = 3,
    Manual = 4
}

public enum DiscountKind
{
    Percent = 0,
    FixedAmount = 1
}

public enum GlassNotificationChannel
{
    Email = 0,
    Sms = 1,
    WhatsApp = 2,
    InApp = 3
}

public enum GlassNotificationStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3,
    Read = 4
}

public enum GlassNotificationRecipientKind
{
    Customer = 0,
    Designer = 1,
    Approver = 2,
    Producer = 3,
    Installer = 4,
    Salesperson = 5,
    Admin = 6
}

public enum GlassNotificationEventCode
{
    QuoteSent = 0,
    QuoteViewed = 1,
    QuoteAccepted = 2,
    QuoteRejected = 3,
    OrderConfirmed = 4,
    StockReserved = 5,
    ProductionStarted = 6,
    ProductionCompleted = 7,
    InTransit = 8,
    InstallationScheduled = 9,
    InstallationCompleted = 10,
    StockLow = 11,
    PaymentDue = 12
}

public enum GlassChangeLogKind
{
    RunAdded = 0,
    RunRemoved = 1,
    RunResized = 2,
    PanelAdded = 3,
    PanelRemoved = 4,
    OpeningTypeChanged = 5,
    GlassChanged = 6,
    SystemChanged = 7,
    ColorChanged = 8,
    HardwareChanged = 9,
    ConnectionChanged = 10
}

public enum GlassBOMLineKind
{
    ProfileCut = 0,
    GlassPiece = 1,
    HardwarePiece = 2,
    Labor = 3,
    Transport = 4,
    Installation = 5,
    Insurance = 6,
    Discount = 7
}

public enum GlassCuttingPlanType
{
    Profile1D = 0,
    Glass2D = 1,
    // WHY: nesting persists a different JSON shape than Glass2D; sharing one slot silently destroyed
    // the saved cutting report because the reader deserialises Glass2D as CuttingResult2DDto.
    Glass2DNesting = 2
}

public enum GlassAttachmentKind
{
    SiteSketch = 0,
    SitePhoto = 1,
    AnnotatedPhoto = 2,
    ApprovalDocument = 3,
    Other = 99
}

public enum GlassValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public enum GlassCatalogImportMode
{
    DryRun = 0,
    Commit = 1
}
