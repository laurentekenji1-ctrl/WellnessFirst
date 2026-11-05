namespace WellnessPlatform.Models;

public sealed record StaffAssignedClient(
    int Id,
    string FullName,
    int AssignedAppointmentCount);

public sealed record StaffWellnessRecordView(
    int Id,
    int ClientId,
    string ClientName,
    string Title,
    string Notes,
    DateTime RecordedAt);
