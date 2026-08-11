namespace FEMS.Domain.Enums;

/// <summary>Section 8.1: Field visit status workflow.</summary>
public enum VisitStatus
{
    Assigned = 0,
    Accepted = 1,
    Started = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Missed = 6
}
