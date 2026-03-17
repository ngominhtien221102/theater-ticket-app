Option Strict On
Option Explicit On

Imports System

Namespace TheaterTicketingApp.Models
    Public Class EndedPerformanceReportItem
        Public Property PerformanceId As Integer
        Public Property PlayName As String = String.Empty
        Public Property StartTime As DateTime
        Public Property EndTime As DateTime
        Public Property BookedSeats As Integer
        Public Property TotalSeats As Integer
        Public Property Revenue As Decimal

        Public ReadOnly Property SeatUsageDisplay As String
            Get
                Return $"{BookedSeats}/{TotalSeats}"
            End Get
        End Property
    End Class
End Namespace
