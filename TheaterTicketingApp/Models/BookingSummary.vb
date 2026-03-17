Option Strict On
Option Explicit On

Imports System

Namespace TheaterTicketingApp.Models
    Public Class BookingSummary
        Public Property BookingId As Integer
        Public Property PerformanceId As Integer
        Public Property PerformanceName As String = String.Empty
        Public Property PerformanceStartTime As DateTime
        Public Property CustomerName As String = String.Empty
        Public Property SeatType As String = String.Empty
        Public Property TicketQty As Integer
        Public Property AssignedSeatCount As Integer

        Public ReadOnly Property DisplayText As String
            Get
                Return $"#{BookingId} | {PerformanceName} ({PerformanceStartTime:dd/MM HH:mm}) | {CustomerName} | {AssignedSeatCount}/{TicketQty} ghế"
            End Get
        End Property
    End Class
End Namespace
