Option Strict On
Option Explicit On

Imports System

Namespace TheaterTicketingApp.Models
    Public Class Performance
        Public Property Id As Integer
        Public Property PlayName As String = String.Empty
        Public Property StartTime As DateTime
        Public Property DurationMinutes As Integer
        Public Property TicketPrice As Decimal
        Public Property Status As String = PerformanceStatuses.NotStarted

        Public ReadOnly Property StatusDisplay As String
            Get
                Return PerformanceStatuses.ToDisplayText(Status)
            End Get
        End Property

        Public ReadOnly Property DisplayLabel As String
            Get
                Return $"{PlayName} - {StartTime:dd/MM/yyyy HH:mm} [{StatusDisplay}]"
            End Get
        End Property
    End Class
End Namespace
