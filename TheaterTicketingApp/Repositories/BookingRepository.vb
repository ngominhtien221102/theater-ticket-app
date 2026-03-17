Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports Npgsql
Imports TheaterTicketingApp.Data
Imports TheaterTicketingApp.Models

Namespace TheaterTicketingApp.Repositories
    Public Class BookingRepository
        Private Const TotalSeatsPerPerformance As Integer = 100

        Public Function GetPerformancesForBooking(nameKeyword As String) As List(Of Performance)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim safeName As String = If(nameKeyword, String.Empty).Trim()
                Dim now As DateTime = DateTime.Now
                Dim performances As List(Of Performance) = db.Performances.
                    AsNoTracking().
                    OrderBy(Function(x) x.StartTime).
                    ToList()

                If safeName <> String.Empty Then
                    performances = performances.
                        Where(Function(x) x.PlayName.IndexOf(safeName, StringComparison.OrdinalIgnoreCase) >= 0).
                        ToList()
                End If

                For Each performance As Performance In performances
                    performance.Status = CalculateStatus(performance.StartTime, performance.DurationMinutes, now)
                Next

                Return performances.
                    Where(Function(x) x.Status <> PerformanceStatuses.Ended).
                    ToList()
            End Using
        End Function

        Public Function GetPerformancesWithBookings() As List(Of Performance)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim performanceIds = db.Bookings.
                    AsNoTracking().
                    Select(Function(x) x.PerformanceId).
                    Distinct()

                Dim performances As List(Of Performance) = db.Performances.
                    AsNoTracking().
                    Where(Function(x) performanceIds.Contains(x.Id)).
                    OrderBy(Function(x) x.StartTime).
                    ThenBy(Function(x) x.Id).
                    ToList()

                Dim now As DateTime = DateTime.Now
                For Each performance As Performance In performances
                    performance.Status = CalculateStatus(performance.StartTime, performance.DurationMinutes, now)
                Next

                Return performances
            End Using
        End Function

        Public Function CreateBooking(booking As Booking) As Integer
            If booking Is Nothing Then
                Throw New ArgumentNullException(NameOf(booking))
            End If

            Dim performanceId As Integer = booking.PerformanceId
            Dim ticketQty As Integer = booking.TicketQty
            Dim customerName As String = booking.CustomerName.Trim()
            Dim seatType As String = booking.SeatType.Trim().ToUpperInvariant()
            Dim unitPrice As Decimal = booking.UnitPrice
            Dim totalAmount As Decimal = booking.TotalAmount

            If ticketQty <= 0 Then
                Throw New InvalidOperationException("Số lượng vé phải lớn hơn 0.")
            End If

            Using db As TheaterDbContext = Database.CreateDbContext()
                Using transaction = db.Database.BeginTransaction(IsolationLevel.Serializable)
                    Try
                        Dim performance As Performance = db.Performances.Find(performanceId)
                        If performance Is Nothing Then
                            Throw New InvalidOperationException($"Không tìm thấy suất diễn ID {performanceId}.")
                        End If

                        Dim now As DateTime = DateTime.Now
                        Dim performanceEndTime As DateTime = performance.StartTime.AddMinutes(performance.DurationMinutes)
                        If performanceEndTime <= now Then
                            Throw New InvalidOperationException(
                                $"Suất diễn đã kết thúc lúc {performanceEndTime:dd/MM/yyyy HH:mm}, không thể tạo booking.")
                        End If

                        Dim totalBookedQty As Integer = db.Bookings.
                            ToList().
                            Where(Function(x) x.PerformanceId = performanceId).
                            Select(Function(x) x.TicketQty).
                            Sum()

                        Dim remainingSeats As Integer = TotalSeatsPerPerformance - totalBookedQty
                        If ticketQty > remainingSeats Then
                            If remainingSeats <= 0 Then
                                Throw New InvalidOperationException("Suất diễn đã hết ghế, không thể tạo booking mới.")
                            End If

                            Throw New InvalidOperationException(
                                $"Không đủ ghế trống để đặt {ticketQty} vé. Hiện chỉ còn {remainingSeats} ghế.")
                        End If

                        Dim newBooking As New Booking With {
                            .PerformanceId = performanceId,
                            .CustomerName = customerName,
                            .SeatType = seatType,
                            .TicketQty = ticketQty,
                            .UnitPrice = unitPrice,
                            .TotalAmount = totalAmount
                        }

                        db.Bookings.Add(newBooking)
                        db.SaveChanges()
                        transaction.Commit()

                        Return newBooking.Id
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Sub CancelBooking(bookingId As Integer)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Using transaction = db.Database.BeginTransaction(IsolationLevel.Serializable)
                    Try
                        Dim bookingEntity As Booking = db.Bookings.Find(bookingId)
                        If bookingEntity Is Nothing Then
                            Throw New InvalidOperationException($"Không tìm thấy booking #{bookingId}.")
                        End If

                        Dim performance As Performance = db.Performances.Find(bookingEntity.PerformanceId)
                        If performance Is Nothing Then
                            Throw New InvalidOperationException($"Không tìm thấy suất diễn cho booking #{bookingId}.")
                        End If

                        Dim remaining As TimeSpan = performance.StartTime - DateTime.Now
                        If remaining < TimeSpan.FromHours(24) Then
                            Throw New InvalidOperationException(
                                $"Booking chỉ được hủy trước 24 giờ. Suất diễn bắt đầu lúc {performance.StartTime:dd/MM/yyyy HH:mm}.")
                        End If

                        db.Bookings.Remove(bookingEntity)
                        db.SaveChanges()
                        transaction.Commit()
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Function GetBookingSummaries() As List(Of BookingSummary)
            Return GetBookingSummariesInternal(Nothing)
        End Function

        Public Function GetBookingSummariesByPerformance(performanceId As Integer) As List(Of BookingSummary)
            Return GetBookingSummariesInternal(New Nullable(Of Integer)(performanceId))
        End Function

        Private Function GetBookingSummariesInternal(performanceId As Nullable(Of Integer)) As List(Of BookingSummary)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim bookings As List(Of Booking) = db.Bookings.AsNoTracking().ToList()
                Dim performances As List(Of Performance) = db.Performances.AsNoTracking().ToList()
                Dim seatAssignments As List(Of SeatAssignment) = db.SeatAssignments.AsNoTracking().ToList()

                Dim result As List(Of BookingSummary) = (From b In bookings
                                                         Join p In performances On b.PerformanceId Equals p.Id
                                                         Group Join sa In seatAssignments On b.Id Equals sa.BookingId Into bookingSeats = Group
                                                         Select New BookingSummary With {
                                                             .BookingId = b.Id,
                                                             .PerformanceId = b.PerformanceId,
                                                             .PerformanceName = p.PlayName,
                                                             .PerformanceStartTime = p.StartTime,
                                                             .CustomerName = b.CustomerName,
                                                             .SeatType = b.SeatType,
                                                             .TicketQty = b.TicketQty,
                                                             .AssignedSeatCount = bookingSeats.Count()
                                                         }).
                    OrderByDescending(Function(x) x.BookingId).
                    ToList()

                If performanceId.HasValue Then
                    result = result.
                        Where(Function(x) x.PerformanceId = performanceId.Value).
                        ToList()
                End If

                Return result
            End Using
        End Function

        Public Function GetBookedSeatKeys(performanceId As Integer, excludeBookingId As Integer) As HashSet(Of String)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim seatKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                Dim seats = db.SeatAssignments.
                    AsNoTracking().
                    Select(Function(x) New With {
                        .SeatRow = x.SeatRow,
                        .SeatNumber = x.SeatNumber,
                        .PerformanceId = x.PerformanceId,
                        .BookingId = x.BookingId
                    }).
                    ToList()

                For Each seat In seats
                    If seat.PerformanceId = performanceId AndAlso seat.BookingId <> excludeBookingId Then
                        seatKeys.Add(BuildSeatKey(seat.SeatRow, seat.SeatNumber))
                    End If
                Next

                Return seatKeys
            End Using
        End Function

        Public Function GetSeatKeysForBooking(bookingId As Integer) As HashSet(Of String)
            Using db As TheaterDbContext = Database.CreateDbContext()
                Dim seatKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                Dim seats = db.SeatAssignments.
                    AsNoTracking().
                    Select(Function(x) New With {
                        .SeatRow = x.SeatRow,
                        .SeatNumber = x.SeatNumber,
                        .BookingId = x.BookingId
                    }).
                    ToList()

                For Each seat In seats
                    If seat.BookingId = bookingId Then
                        seatKeys.Add(BuildSeatKey(seat.SeatRow, seat.SeatNumber))
                    End If
                Next

                Return seatKeys
            End Using
        End Function

        Public Sub SaveSeatAssignments(bookingId As Integer, seatKeys As IEnumerable(Of String))
            If seatKeys Is Nothing Then
                Throw New ArgumentNullException(NameOf(seatKeys))
            End If

            Dim normalizedSeatKeys As List(Of String) = seatKeys.
                Select(Function(x) x.Trim().ToUpperInvariant()).
                Where(Function(x) x <> String.Empty).
                Distinct(StringComparer.OrdinalIgnoreCase).
                ToList()

            Using db As TheaterDbContext = Database.CreateDbContext()
                Using transaction = db.Database.BeginTransaction(IsolationLevel.Serializable)
                    Try
                        Dim bookingInfo = GetBookingInfoForUpdate(db, bookingId)

                        Dim performanceEndTime As DateTime = bookingInfo.PerformanceStartTime.AddMinutes(bookingInfo.PerformanceDurationMinutes)
                        If performanceEndTime <= DateTime.Now Then
                            Throw New InvalidOperationException(
                                $"Suất diễn đã kết thúc lúc {performanceEndTime:dd/MM/yyyy HH:mm}, không thể chỉnh sửa gán ghế cho booking #{bookingId}.")
                        End If

                        If normalizedSeatKeys.Count <> bookingInfo.TicketQty Then
                            Throw New InvalidOperationException($"Booking #{bookingId} yêu cầu đúng {bookingInfo.TicketQty} ghế.")
                        End If

                        Dim existingAssignments As List(Of SeatAssignment) = db.SeatAssignments.
                            ToList().
                            Where(Function(x) x.BookingId = bookingId).
                            ToList()

                        If existingAssignments.Count > 0 Then
                            db.SeatAssignments.RemoveRange(existingAssignments)
                        End If

                        For Each seatKey As String In normalizedSeatKeys
                            Dim parsed = ParseSeatKey(seatKey)
                            db.SeatAssignments.Add(New SeatAssignment With {
                                .BookingId = bookingId,
                                .PerformanceId = bookingInfo.PerformanceId,
                                .SeatRow = parsed.SeatRow,
                                .SeatNumber = parsed.SeatNumber
                            })
                        Next

                        db.SaveChanges()
                        transaction.Commit()
                    Catch ex As DbUpdateException
                        transaction.Rollback()
                        Dim postgresException As PostgresException = FindPostgresException(ex)
                        If postgresException IsNot Nothing AndAlso postgresException.SqlState = PostgresErrorCodes.UniqueViolation Then
                            Throw New InvalidOperationException("Một hoặc nhiều ghế đã bị booking khác giữ trước đó. Vui lòng tải lại sơ đồ ghế.")
                        End If

                        Throw
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Shared Function GetBookingInfoForUpdate(db As TheaterDbContext,
                                                        bookingId As Integer) As (PerformanceId As Integer,
                                                                                 TicketQty As Integer,
                                                                                 PerformanceStartTime As DateTime,
                                                                                 PerformanceDurationMinutes As Integer)
            Dim bookingEntity As Booking = db.Bookings.Find(bookingId)
            If bookingEntity Is Nothing Then
                Throw New InvalidOperationException($"Không tìm thấy booking #{bookingId}.")
            End If

            Dim performanceEntity As Performance = db.Performances.Find(bookingEntity.PerformanceId)
            If performanceEntity Is Nothing Then
                Throw New InvalidOperationException($"Không tìm thấy suất diễn cho booking #{bookingId}.")
            End If

            Return (bookingEntity.PerformanceId,
                    bookingEntity.TicketQty,
                    performanceEntity.StartTime,
                    performanceEntity.DurationMinutes)
        End Function

        Private Shared Function FindPostgresException(ex As Exception) As PostgresException
            Dim current As Exception = ex
            While current IsNot Nothing
                Dim postgresException As PostgresException = TryCast(current, PostgresException)
                If postgresException IsNot Nothing Then
                    Return postgresException
                End If

                current = current.InnerException
            End While

            Return Nothing
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

        Private Shared Function BuildSeatKey(seatRow As String, seatNumber As Integer) As String
            Return $"{seatRow.Trim().ToUpperInvariant()}{seatNumber}"
        End Function

        Private Shared Function ParseSeatKey(seatKey As String) As (SeatRow As String, SeatNumber As Integer)
            If String.IsNullOrWhiteSpace(seatKey) Then
                Throw New ArgumentException("Mã ghế rỗng.", NameOf(seatKey))
            End If

            Dim normalized As String = seatKey.Trim().ToUpperInvariant()
            If normalized.Length < 2 Then
                Throw New ArgumentException($"Mã ghế không hợp lệ: {seatKey}", NameOf(seatKey))
            End If

            Dim seatRow As String = normalized.Substring(0, 1)
            Dim rowChar As Char = seatRow(0)
            If rowChar < "A"c OrElse rowChar > "J"c Then
                Throw New ArgumentException($"Hàng ghế không hợp lệ: {seatRow}", NameOf(seatKey))
            End If

            Dim seatNumberText As String = normalized.Substring(1)
            Dim seatNumber As Integer
            If Not Integer.TryParse(seatNumberText, seatNumber) Then
                Throw New ArgumentException($"Cột ghế không hợp lệ: {seatNumberText}", NameOf(seatKey))
            End If

            If seatNumber < 1 OrElse seatNumber > 10 Then
                Throw New ArgumentException($"Cột ghế phải từ 1 đến 10. Giá trị nhận: {seatNumber}.", NameOf(seatKey))
            End If

            Return (seatRow, seatNumber)
        End Function
    End Class
End Namespace
