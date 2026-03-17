Option Strict On
Option Explicit On

Namespace TheaterTicketingApp.Models
    Public Class SeatAssignment
        Public Property Id As Integer
        Public Property BookingId As Integer
        Public Property PerformanceId As Integer
        Public Property SeatRow As String = String.Empty
        Public Property SeatNumber As Integer
    End Class
End Namespace
