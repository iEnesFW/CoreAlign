namespace CoreAlign.Domain.Enums;

public enum PlateKind
{
    Fresh = 0,
    Remnant = 1
}

public enum GlassPlateStatus
{
    Available = 0,
    Reserved = 1,
    InUse = 2,
    Consumed = 3,
    Scrapped = 4
}

public enum PlateCondition
{
    Good = 0,
    Chipped = 1,
    Cracked = 2,
    Scratched = 3
}

public enum StorageLocationKind
{
    Rack = 0,
    Shelf = 1,
    Pallet = 2,
    Floor = 3,
    Zone = 4
}

public enum GlassScrapMode
{
    Area = 0,
    Count = 1
}
