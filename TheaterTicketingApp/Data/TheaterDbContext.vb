Option Strict On
Option Explicit On

Imports Microsoft.EntityFrameworkCore
Imports TheaterTicketingApp.Models

Namespace TheaterTicketingApp.Data
    Public Class TheaterDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of TheaterDbContext))
            MyBase.New(options)
        End Sub

        Public Property Performances As DbSet(Of Performance)
        Public Property Bookings As DbSet(Of Booking)
        Public Property SeatAssignments As DbSet(Of SeatAssignment)

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Performance)(
                Sub(entity)
                    entity.ToTable("performances")
                    entity.HasKey(Function(x) x.Id)
                    entity.Property(Function(x) x.Id).HasColumnName("id")
                    entity.Property(Function(x) x.PlayName).HasColumnName("play_name").HasMaxLength(200).IsRequired()
                    entity.Property(Function(x) x.StartTime).HasColumnName("start_time").HasColumnType("timestamp without time zone").IsRequired()
                    entity.Property(Function(x) x.DurationMinutes).HasColumnName("duration_minutes").IsRequired()
                    entity.Property(Function(x) x.TicketPrice).HasColumnName("ticket_price").HasPrecision(12, 2).IsRequired()
                    entity.Property(Function(x) x.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
                End Sub)

            modelBuilder.Entity(Of Booking)(
                Sub(entity)
                    entity.ToTable("bookings")
                    entity.HasKey(Function(x) x.Id)
                    entity.Property(Function(x) x.Id).HasColumnName("id")
                    entity.Property(Function(x) x.PerformanceId).HasColumnName("performance_id").IsRequired()
                    entity.Property(Function(x) x.CustomerName).HasColumnName("customer_name").HasMaxLength(150).IsRequired()
                    entity.Property(Function(x) x.SeatType).HasColumnName("seat_type").HasMaxLength(20).IsRequired()
                    entity.Property(Function(x) x.TicketQty).HasColumnName("ticket_qty").IsRequired()
                    entity.Property(Function(x) x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2).IsRequired()
                    entity.Property(Function(x) x.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2).IsRequired()
                End Sub)

            modelBuilder.Entity(Of SeatAssignment)(
                Sub(entity)
                    entity.ToTable("seat_assignments")
                    entity.HasKey(Function(x) x.Id)
                    entity.Property(Function(x) x.Id).HasColumnName("id")
                    entity.Property(Function(x) x.BookingId).HasColumnName("booking_id").IsRequired()
                    entity.Property(Function(x) x.PerformanceId).HasColumnName("performance_id").IsRequired()
                    entity.Property(Function(x) x.SeatRow).HasColumnName("seat_row").HasMaxLength(1).IsRequired()
                    entity.Property(Function(x) x.SeatNumber).HasColumnName("seat_number").IsRequired()
                End Sub)

            MyBase.OnModelCreating(modelBuilder)
        End Sub
    End Class
End Namespace
