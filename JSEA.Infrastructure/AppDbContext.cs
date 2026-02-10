using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using JSEA_Application.Models;
using JSEA_Application.Enums;

namespace JSEA_Infrastructure;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Event> Events { get; set; }
    public virtual DbSet<EventOccurrence> EventOccurrences { get; set; }
    public virtual DbSet<ExperienceDetail> ExperienceDetails { get; set; }
    public virtual DbSet<ExperienceMetric> ExperienceMetrics { get; set; }
    public virtual DbSet<ExperiencePhoto> ExperiencePhotos { get; set; }
    public virtual DbSet<Feedback> Feedbacks { get; set; }
    public virtual DbSet<Journey> Journeys { get; set; }
    public virtual DbSet<JourneySuggestion> JourneySuggestions { get; set; }
    public virtual DbSet<JourneyWaypoint> JourneyWaypoints { get; set; }
    public virtual DbSet<MicroExperience> MicroExperiences { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Package> Packages { get; set; }
    public virtual DbSet<Rating> Ratings { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<RouteSegment> RouteSegments { get; set; }
    public virtual DbSet<SuggestionInteraction> SuggestionInteractions { get; set; }
    public virtual DbSet<SystemConfig> SystemConfigs { get; set; }
    public virtual DbSet<Transaction> Transactions { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserFavorite> UserFavorites { get; set; }
    public virtual DbSet<UserPackage> UserPackages { get; set; }
    public virtual DbSet<UserProfile> UserProfiles { get; set; }
    public virtual DbSet<Visit> Visits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<ActionType>("action_type");
        modelBuilder.HasPostgresEnum<ExperienceStatus>("experience_status");
        modelBuilder.HasPostgresEnum<InteractionType>("interaction_type");
        modelBuilder.HasPostgresEnum<JourneyStatus>("journey_status");
        modelBuilder.HasPostgresEnum<MoodType>("mood_type");
        modelBuilder.HasPostgresEnum<NotificationType>("notification_type");
        modelBuilder.HasPostgresEnum<PackageType>("package_type");
        modelBuilder.HasPostgresEnum<RecurrencePattern>("recurrence_pattern");
        modelBuilder.HasPostgresEnum<SeasonType>("season_type");
        modelBuilder.HasPostgresEnum<TimeOfDay>("time_of_day");
        modelBuilder.HasPostgresEnum<TransactionStatus>("transaction_status");
        modelBuilder.HasPostgresEnum<TransactionType>("transaction_type");
        modelBuilder.HasPostgresEnum<UserRole>("user_role");
        modelBuilder.HasPostgresEnum<UserStatus>("user_status");
        modelBuilder.HasPostgresEnum<VehicleType>("vehicle_type");
        modelBuilder.HasPostgresEnum<WeatherType>("weather_type");

        // Đăng ký Extension PostGIS
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("fuzzystrmatch");
        modelBuilder.HasPostgresExtension("pg_trgm");

        // CONFIG CÁC ENTITY

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs).HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("events_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.Experience).WithMany(p => p.Events).HasConstraintName("events_experience_id_fkey");
            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.Events).HasConstraintName("events_uploaded_by_user_id_fkey");
        });

        modelBuilder.Entity<EventOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_occurrences_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.Event).WithMany(p => p.EventOccurrences).HasConstraintName("event_occurrences_event_id_fkey");
        });

        modelBuilder.Entity<ExperienceDetail>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("experience_details_pkey");
            entity.Property(e => e.ExperienceId).ValueGeneratedNever();
            entity.HasOne(d => d.FeaturedByUser).WithMany(p => p.ExperienceDetailFeaturedByUsers).HasConstraintName("experience_details_featured_by_user_id_fkey");
            entity.HasOne(d => d.VerifiedByUser).WithMany(p => p.ExperienceDetailVerifiedByUsers).HasConstraintName("experience_details_verified_by_user_id_fkey");
        });

        modelBuilder.Entity<ExperienceMetric>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("experience_metrics_pkey");
            entity.Property(e => e.ExperienceId).ValueGeneratedNever();
            entity.Property(e => e.AvgRating).HasDefaultValueSql("0");
            entity.Property(e => e.TotalRatings).HasDefaultValue(0);
            entity.Property(e => e.TotalVisits).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<ExperiencePhoto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("experience_photos_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsCover).HasDefaultValue(false);
            entity.HasOne(d => d.Experience).WithMany(p => p.ExperiencePhotos).OnDelete(DeleteBehavior.Cascade).HasConstraintName("experience_photos_experience_id_fkey");
            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.ExperiencePhotos).HasConstraintName("experience_photos_uploaded_by_user_id_fkey");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("feedbacks_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsFlagged).HasDefaultValue(false);
            entity.HasOne(d => d.Experience).WithMany(p => p.Feedbacks).HasConstraintName("feedbacks_experience_id_fkey");
            entity.HasOne(d => d.Traveler).WithMany(p => p.Feedbacks).HasConstraintName("feedbacks_traveler_id_fkey");

            entity.HasOne(d => d.Visit)
                  .WithOne(p => p.Feedback)
                  .HasForeignKey<Feedback>(d => d.VisitId)
                  .IsRequired(false)
                  .HasConstraintName("feedbacks_visit_id_fkey");
        });

        modelBuilder.Entity<Journey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journeys_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Traveler).WithMany(p => p.Journeys).HasConstraintName("journeys_traveler_id_fkey");
        });

        modelBuilder.Entity<JourneySuggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journey_suggestions_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SuggestedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Experience).WithMany(p => p.JourneySuggestions).HasConstraintName("journey_suggestions_experience_id_fkey");
            entity.HasOne(d => d.Journey).WithMany(p => p.JourneySuggestions).HasConstraintName("journey_suggestions_journey_id_fkey");
            entity.HasOne(d => d.Segment).WithMany(p => p.JourneySuggestions).HasConstraintName("journey_suggestions_segment_id_fkey");
        });

        modelBuilder.Entity<JourneyWaypoint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journey_waypoints_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ExperienceId).HasComment("Có thể là 1 micro-experience hoặc điểm tùy chỉnh");
            entity.Property(e => e.StopOrder).HasComment("Thứ tự điểm dừng 1, 2, 3...");
            entity.HasOne(d => d.Experience).WithMany(p => p.JourneyWaypoints).HasConstraintName("journey_waypoints_experience_id_fkey");
            entity.HasOne(d => d.Journey).WithMany(p => p.JourneyWaypoints).OnDelete(DeleteBehavior.Cascade).HasConstraintName("journey_waypoints_journey_id_fkey");
        });

        modelBuilder.Entity<MicroExperience>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("micro_experiences_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Country).HasDefaultValueSql("'Vietnam'::character varying");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Category).WithMany(p => p.MicroExperiences).HasConstraintName("micro_experiences_category_id_fkey");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.MicroExperience).HasConstraintName("micro_experiences_id_fkey1");
            entity.HasOne(d => d.Id1).WithOne(p => p.MicroExperience).HasConstraintName("micro_experiences_id_fkey");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.MicroExperiences).HasConstraintName("micro_experiences_uploaded_by_user_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.HasOne(d => d.RelatedExperience).WithMany(p => p.Notifications).HasConstraintName("notifications_related_experience_id_fkey");
            entity.HasOne(d => d.RelatedJourney).WithMany(p => p.Notifications).HasConstraintName("notifications_related_journey_id_fkey");
            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("packages_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Benefit).HasComment("Lưu các quyền lợi của gói");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsPopular).HasDefaultValue(false);
            entity.Property(e => e.Km).HasComment("Giới hạn km hoặc thuộc tính liên quan");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ratings_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Rating1).HasComment("1-5 stars");
            entity.HasOne(d => d.Experience).WithMany(p => p.Ratings).HasConstraintName("ratings_experience_id_fkey");
            entity.HasOne(d => d.Traveler).WithMany(p => p.Ratings).HasConstraintName("ratings_traveler_id_fkey");

            entity.HasOne(d => d.Visit)
                  .WithOne(p => p.Rating)
                  .HasForeignKey<Rating>(d => d.VisitId)
                  .IsRequired(false)
                  .HasConstraintName("ratings_visit_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<RouteSegment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("route_segments_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.Journey).WithMany(p => p.RouteSegments).OnDelete(DeleteBehavior.Cascade).HasConstraintName("route_segments_journey_id_fkey");
        });

        modelBuilder.Entity<SuggestionInteraction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("suggestion_interactions_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.InteractedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Suggestion).WithMany(p => p.SuggestionInteractions).HasConstraintName("suggestion_interactions_suggestion_id_fkey");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("system_configs_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.SystemConfigs).HasConstraintName("system_configs_updated_by_user_id_fkey");
        });

    
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

          
            entity.HasIndex(e => e.OrderCode)
                  .IsUnique()
                  .HasDatabaseName("transactions_order_code_key");

        
            entity.Property(e => e.WebhookData).HasColumnType("jsonb");
            entity.Property(e => e.ItemSnapshot).HasColumnType("jsonb").HasComment("Lưu thông tin gói tại thời điểm mua");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions).HasConstraintName("transactions_user_id_fkey");
        });
      

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", tb => tb.HasComment("Quản lý authentication và phân quyền"));
            entity.HasKey(e => e.Id).HasName("users_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasDefaultValue(false);

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.User)
                .HasPrincipalKey<UserProfile>(p => p.UserId)
                .HasForeignKey<User>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_id_fkey");
        });

        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_favorites_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Experience).WithMany(p => p.UserFavorites).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("user_favorites_experience_id_fkey");
            entity.HasOne(d => d.User).WithMany(p => p.UserFavorites).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("user_favorites_user_id_fkey");
        });

        modelBuilder.Entity<UserPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_packages_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ActivatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(d => d.Package).WithMany(p => p.UserPackages).HasConstraintName("user_packages_package_id_fkey");
            entity.HasOne(d => d.User).WithMany(p => p.UserPackages).HasConstraintName("user_packages_user_id_fkey");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles", tb => tb.HasComment("Metadata chi tiết cho từng loại user"));
            entity.HasKey(e => e.Id).HasName("user_profiles_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("visits_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.VisitedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Experience).WithMany(p => p.Visits).HasConstraintName("visits_experience_id_fkey");
            entity.HasOne(d => d.Journey).WithMany(p => p.Visits).HasConstraintName("visits_journey_id_fkey");
            entity.HasOne(d => d.Traveler).WithMany(p => p.Visits).HasConstraintName("visits_traveler_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}