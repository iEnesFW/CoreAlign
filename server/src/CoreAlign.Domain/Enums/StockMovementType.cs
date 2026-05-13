namespace CoreAlign.Domain.Enums;

public enum StockMovementType
{
    OpeningBalance = 0,
    Receipt = 1,
    Issue = 2,
    TransferIn = 3,
    TransferOut = 4,
    AdjustmentPositive = 5,
    AdjustmentNegative = 6,
    CountVariancePositive = 7,
    CountVarianceNegative = 8,
    Reservation = 9,
    UnReservation = 10
}
