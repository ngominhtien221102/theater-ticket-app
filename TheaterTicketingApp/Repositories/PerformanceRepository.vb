Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports TheaterTicketingApp.Data
Imports TheaterTicketingApp.Models

Namespace TheaterTicketingApp.Repositories
    Public Class PerformanceRepository
        Private Const TotalSeatsPerPerformance As Integer = 100

        Public Function GetPerformances(nameKeyword As String,
                                        startFrom As Nullable(Of DateTime),
                                        startTo As Nullable(Of DateTime)) As List(Of Performance)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim safeName As String = If(nameKeyword, String.Empty).Trim()
                Dim results As List(Of Performance) = db.Performances.
                    AsNoTracking().
                    OrderBy(Function(x) x.StartTime).
                    ToList()

                If safeName <> String.Empty Then
                    results = results.
                        Where(Function(x) x.PlayName.IndexOf(safeName, StringComparison.OrdinalIgnoreCase) >= 0).
                        ToList()
                End If

                If startFrom.HasValue Then
                    results = results.
                        Where(Function(x) x.StartTime >= startFrom.Value).
                        ToList()
                End If

                If startTo.HasValue Then
                    results = results.
                        Where(Function(x) x.StartTime <= startTo.Value).
                        ToList()
                End If

                Return results
            End Using
        End Function

        Public Function InsertPerformance(performance As Performance) As Integer
            Using db As TheaterDbContext = Database.CreateDbContext()
                db.Performances.Add(performance)
                db.SaveChanges()
                Return performance.Id
            End Using
        End Function

        Public Sub UpdatePerformance(performance As Performance)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Using transaction = db.Database.BeginTransaction()
                    Dim currentPerformance As Performance = db.Performances.Find(performance.Id)
                    If currentPerformance Is Nothing Then
                        Throw New InvalidOperationException($"Không tìm thấy suất diễn ID {performance.Id}.")
                    End If

                    Dim hasBooking As Boolean = db.Bookings.
                        AsNoTracking().
                        ToList().
                        Any(Function(x) x.PerformanceId = performance.Id)
                    If hasBooking Then
                        Dim changedLockedFields As Boolean =
                            currentPerformance.StartTime <> performance.StartTime OrElse
                            currentPerformance.DurationMinutes <> performance.DurationMinutes OrElse
                            currentPerformance.TicketPrice <> performance.TicketPrice

                        If changedLockedFields Then
                            Throw New InvalidOperationException(
                                "Suất diễn đã có booking, không thể chỉnh sửa thời gian, thời lượng hoặc giá vé.")
                        End If
                    End If

                    currentPerformance.PlayName = performance.PlayName.Trim()
                    currentPerformance.StartTime = performance.StartTime
                    currentPerformance.DurationMinutes = performance.DurationMinutes
                    currentPerformance.TicketPrice = performance.TicketPrice

                    db.SaveChanges()
                    transaction.Commit()
                End Using
            End Using
        End Sub

        Public Function SyncPerformanceStatuses() As Integer
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim now As DateTime = DateTime.Now
                Dim performances As List(Of Performance) = db.Performances.ToList()
                Dim changedCount As Integer = 0

                For Each performance As Performance In performances
                    Dim newStatus As String = CalculateStatus(performance.StartTime, performance.DurationMinutes, now)
                    If Not String.Equals(performance.Status, newStatus, StringComparison.OrdinalIgnoreCase) Then
                        performance.Status = newStatus
                        changedCount += 1
                    End If
                Next

                If changedCount > 0 Then
                    db.SaveChanges()
                End If

                Return changedCount
            End Using
        End Function

        Public Sub DeletePerformance(performanceId As Integer)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim performance As Performance = db.Performances.Find(performanceId)
                If performance Is Nothing Then
                    Return
                End If

                db.Performances.Remove(performance)
                db.SaveChanges()
            End Using
        End Sub

        Public Function GetEndedPerformanceReport() As List(Of EndedPerformanceReportItem)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim now As DateTime = DateTime.Now
                Dim performances As List(Of Performance) = db.Performances.
                    AsNoTracking().
                    ToList()
                Dim bookings As List(Of Booking) = db.Bookings.
                    AsNoTracking().
                    ToList()

                Dim reports As New List(Of EndedPerformanceReportItem)()
                For Each performance As Performance In performances
                    Dim status As String = CalculateStatus(performance.StartTime, performance.DurationMinutes, now)
                    If Not String.Equals(status, PerformanceStatuses.Ended, StringComparison.OrdinalIgnoreCase) Then
                        Continue For
                    End If

                    Dim performanceBookings = bookings.
                        Where(Function(x) x.PerformanceId = performance.Id).
                        ToList()

                    Dim bookedSeats As Integer = performanceBookings.Sum(Function(x) x.TicketQty)
                    Dim revenue As Decimal = performanceBookings.Sum(Function(x) x.TotalAmount)
                    reports.Add(New EndedPerformanceReportItem With {
                        .PerformanceId = performance.Id,
                        .PlayName = performance.PlayName,
                        .StartTime = performance.StartTime,
                        .EndTime = performance.StartTime.AddMinutes(performance.DurationMinutes),
                        .BookedSeats = bookedSeats,
                        .TotalSeats = TotalSeatsPerPerformance,
                        .Revenue = revenue
                    })
                Next

                Return reports.
                    OrderByDescending(Function(x) x.StartTime).
                    ToList()
            End Using
        End Function

        Private Shared Function CalculateStatus(startTime As DateTime,
                                                durationMinutes As Integer,
                                                currentTime As DateTime) As String
            Dim endTime As DateTime = startTime.AddMinutes(durationMinutes)
            If currentTime >= endTime Then
                Return PerformanceStatuses.Ended
            End If

            If currentTime >= startTime Then
                Return PerformanceStatuses.InProgress
            End If

            Return PerformanceStatuses.NotStarted
        End Function
    End Class
End Namespace
