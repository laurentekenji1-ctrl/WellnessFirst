using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WellnessPlatform.Models;

public enum UserRole { Client, Staff, Admin }
public enum AppointmentStatus { Pending, Confirmed, CheckedIn, InProgress, Completed, Cancelled, Rescheduled, NoShow }
public enum PaymentStatus { Pending, Processing, Paid, Failed, Refunded, PartiallyRefunded }
public enum PaymentRefundStatus { Pending, Succeeded, Failed, Canceled }
public enum GoalStatus { Active, Completed, Paused, Archived }
public enum ActionPlanStatus { Active, Completed }



public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Client;

    [NotMapped]
    public bool EmailVerified
    {
        get => EmailConfirmed;
        set => EmailConfirmed = value;
    }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserPermission> UserPermissions { get; set; }
        = new List<UserPermission>();
}

public class Permission
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public ICollection<UserPermission> UserPermissions { get; set; }
        = new List<UserPermission>();
}

public class UserPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public AppUser? User { get; set; }
    public Permission? Permission { get; set; }
}

public class ServiceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool Active { get; set; } = true;
}

public class Appointment
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int ServiceItemId { get; set; }
    public int? StaffId { get; set; }
    public DateTime StartAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string Notes { get; set; } = "";

    public AppUser? Client { get; set; }
    public AppUser? Staff { get; set; }
    public ServiceItem? ServiceItem { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class BusinessHours
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeOnly OpensAt { get; set; } = new(9, 0);
    public TimeOnly ClosesAt { get; set; } = new(17, 0);
}

public class AvailabilitySettings
{
    public int Id { get; set; } = 1;
    public string TimeZoneId { get; set; } = "Asia/Manila";
    public int SlotIntervalMinutes { get; set; } = 30;
    public int MaxConcurrentAppointments { get; set; } = 1;
}

public class WellnessRecord
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class Goal
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? TargetDate { get; set; }
    public int ProgressPercent { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
}

public class ActionPlan
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int StaffId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TargetDate { get; set; }
    public ActionPlanStatus Status { get; set; } = ActionPlanStatus.Active;

    public AppUser? Client { get; set; }
    public AppUser? Staff { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string Method { get; set; } = "";
    public string ReceiptNumber { get; set; } = "";
    public DateTime? PaidAt { get; set; }
    public string Currency { get; set; } = "php";
    public string? CheckoutSessionId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? CheckoutExpiresAt { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public long? ChargedAmountMinor { get; set; }
    public string? ChargedCurrency { get; set; }
    public ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();

    public Appointment? Appointment { get; set; }
}

public class PaymentRefund
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "";
    public PaymentRefundStatus Status { get; set; } = PaymentRefundStatus.Pending;
    public string? StripeRefundId { get; set; }
    public string Reason { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Payment? Payment { get; set; }
    public AppUser? CreatedBy { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Details { get; set; } = "";
}

public class Notification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}