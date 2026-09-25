namespace RideMatchingSystem.Models;

public enum RideStatus
{
    Searching = 0,
    DriverOffered = 1,
    DriverAssigned = 2,
    DriverArrived = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    NoDriverFound = 7
}