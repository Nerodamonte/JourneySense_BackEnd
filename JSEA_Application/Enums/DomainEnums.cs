namespace JSEA_Application.Enums;

public enum NotificationType { ExperienceVerified, Featured, Rejected, TimeBudgetWarning }
public enum TransactionType { Purchase, Renewal, Upgrade }
public enum TransactionStatus { Pending, Completed, Failed, Refunded }
public enum ExperienceStatus { ActiveUnverified, Verified, Featured, NeedsUpdate, Inactive, Rejected }
public enum PackageType { Basic, Pro, Ultra }
public enum InteractionType { Accepted, Skipped, Saved, ViewedDetails }
public enum UserRole { Traveler, Staff, Admin }
public enum UserStatus { PendingVerification, Active, Suspended, Deleted }
public enum MoodType { Happy, Normal, Sad, Stressed }
public enum VibeType { Chill, Relax, Explorer, Foodie, LocalVibes, Adventure, Photographer }
public enum CrowdLevel { All, Quiet, Normal, Busy }
public enum VehicleType { Walking, Bicycle, Motorbike, Car }
public enum JourneyStatus { Planning, InProgress, Completed, Cancelled }
public enum ActionType
{
    Create,
    Update,
    Delete,
    Verify,
    Feature,
    Reject,
    Login,
    Logout,
    AdminUserStatusChanged,
    AdminStaffCreated,
    StaffFeedbackModerated,
    StaffJourneyFeedbackModerated,
    StaffUserReported,
    AdminEmbeddingBatchRun
}
public enum RecurrencePattern { Once, Daily, Weekly, Monthly, Yearly, Custom }
public enum TimeOfDay { Morning, Afternoon, Evening, Night }
public enum WeatherType { Sunny, Cloudy, Rainy }
public enum SeasonType { YearRound, Summer, Autumn, Winter, Spring }