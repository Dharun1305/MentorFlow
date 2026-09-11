using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Entities;

namespace MentorFlow.API.Data;

public sealed class MentorFlowDbContext(DbContextOptions<MentorFlowDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Mentor> Mentors => Set<Mentor>();
    public DbSet<MentorStudent> MentorStudents => Set<MentorStudent>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Goal> Goals => Set<Goal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(role => role.RoleId).HasName("PK_Roles");
            entity.Property(role => role.Name).HasMaxLength(50).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(250);
            entity.Property(role => role.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(role => role.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(role => role.Name).IsUnique().HasDatabaseName("UQ_Roles_Name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.UserId).HasName("PK_Users");
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(user => user.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            entity.HasIndex(user => new { user.RoleId, user.IsActive }).HasDatabaseName("IX_Users_RoleId_IsActive");
            entity.HasOne(user => user.Role).WithMany(role => role.Users).HasForeignKey(user => user.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(student => student.StudentId).HasName("PK_Students");
            entity.Property(student => student.StudentCode).HasMaxLength(50).IsRequired();
            entity.Property(student => student.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(student => student.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(student => student.UserId).IsUnique().HasDatabaseName("UQ_Students_UserId");
            entity.HasIndex(student => student.StudentCode).IsUnique().HasDatabaseName("UQ_Students_StudentCode");
            entity.HasOne(student => student.User).WithOne(user => user.Student).HasForeignKey<Student>(student => student.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Mentor>(entity =>
        {
            entity.ToTable("Mentors");
            entity.HasKey(mentor => mentor.MentorId).HasName("PK_Mentors");
            entity.Property(mentor => mentor.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(mentor => mentor.Specialization).HasMaxLength(150);
            entity.Property(mentor => mentor.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(mentor => mentor.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(mentor => mentor.UserId).IsUnique().HasDatabaseName("UQ_Mentors_UserId");
            entity.HasIndex(mentor => mentor.EmployeeCode).IsUnique().HasDatabaseName("UQ_Mentors_EmployeeCode");
            entity.HasOne(mentor => mentor.User).WithOne(user => user.Mentor).HasForeignKey<Mentor>(mentor => mentor.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MentorStudent>(entity =>
        {
            entity.ToTable("MentorStudents");
            entity.HasKey(assignment => assignment.MentorStudentId).HasName("PK_MentorStudents");
            entity.Property(assignment => assignment.AssignedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(assignment => assignment.UnassignedAt).HasColumnType("datetime2(0)");
            entity.Property(assignment => assignment.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(assignment => assignment.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(assignment => new { assignment.MentorId, assignment.StudentId }).IsUnique().HasDatabaseName("UQ_MentorStudents_Mentor_Student");
            entity.HasIndex(assignment => new { assignment.StudentId, assignment.IsActive }).HasDatabaseName("IX_MentorStudents_StudentId_IsActive");
            entity.HasIndex(assignment => new { assignment.MentorId, assignment.IsActive }).HasDatabaseName("IX_MentorStudents_MentorId_IsActive");
            entity.HasOne(assignment => assignment.Mentor).WithMany(mentor => mentor.MentorStudents).HasForeignKey(assignment => assignment.MentorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.Student).WithMany(student => student.MentorStudents).HasForeignKey(assignment => assignment.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.ToTable("Meetings");
            entity.HasKey(meeting => meeting.MeetingId).HasName("PK_Meetings");
            entity.Property(meeting => meeting.MeetingAt).HasColumnType("datetime2(0)").IsRequired();
            entity.Property(meeting => meeting.DurationMinutes).HasColumnType("smallint");
            entity.Property(meeting => meeting.Topic).HasMaxLength(200).IsRequired();
            entity.Property(meeting => meeting.Notes).HasColumnType("nvarchar(max)");
            entity.Property(meeting => meeting.Feedback).HasColumnType("nvarchar(max)");
            entity.Property(meeting => meeting.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(meeting => meeting.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(meeting => new { meeting.MentorStudentId, meeting.MeetingAt }).HasDatabaseName("IX_Meetings_MentorStudentId_MeetingAt");
            entity.HasOne(meeting => meeting.MentorStudent).WithMany(assignment => assignment.Meetings).HasForeignKey(meeting => meeting.MentorStudentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("Goals");
            entity.HasKey(goal => goal.GoalId).HasName("PK_Goals");
            entity.Property(goal => goal.Title).HasMaxLength(200).IsRequired();
            entity.Property(goal => goal.Description).HasColumnType("nvarchar(max)");
            entity.Property(goal => goal.TargetDate).HasColumnType("date");
            entity.Property(goal => goal.Status).HasMaxLength(20).IsRequired();
            entity.Property(goal => goal.Priority).HasMaxLength(20).IsRequired();
            entity.Property(goal => goal.CompletedAt).HasColumnType("datetime2(0)");
            entity.Property(goal => goal.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(goal => goal.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(goal => new { goal.MentorStudentId, goal.Status }).HasDatabaseName("IX_Goals_MentorStudentId_Status");
            entity.HasIndex(goal => new { goal.MentorStudentId, goal.TargetDate }).HasDatabaseName("IX_Goals_MentorStudentId_TargetDate");
            entity.HasOne(goal => goal.MentorStudent).WithMany(assignment => assignment.Goals).HasForeignKey(goal => goal.MentorStudentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
