namespace Atlas.Application.Common;

/// <summary>Unit of measure for a service and (feature 5) for a work item. Serialized as a string.</summary>
public enum Unit
{
    /// <summary>„м.л.“: linear meter.</summary>
    LinearMeter,

    /// <summary>„м²“: square meter.</summary>
    SquareMeter,

    /// <summary>„ден“: a day of work.</summary>
    Day,

    /// <summary>„бр.“: a piece.</summary>
    Piece,
}
