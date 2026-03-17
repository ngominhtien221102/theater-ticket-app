Option Strict On
Option Explicit On

Namespace TheaterTicketingApp.Models
    Public Class Booking
        Public Property Id As Integer
        Public Property PerformanceId As Integer
        Public Property CustomerName As String = String.Empty
        Public Property SeatType As String = String.Empty
        Public Property TicketQty As Integer
        Public Property UnitPrice As Decimal
        Public Property TotalAmount As Decimal
    End Class
End Namespace
