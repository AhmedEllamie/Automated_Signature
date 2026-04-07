namespace UUNATEK.Domain.Enums;

public enum RequestStatus
{
    New = 0,
    WaitingForApproval = 1,
    Approved = 2,
    Rejected = 3,
    Printing = 4,
    Printed = 5,
    Voided = 6,
    Completed = 7,
    Failed = 8
}
