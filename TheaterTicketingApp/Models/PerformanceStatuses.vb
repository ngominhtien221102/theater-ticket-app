Option Strict On
Option Explicit On

Namespace TheaterTicketingApp.Models
    Public NotInheritable Class PerformanceStatuses
        Public Const NotStarted As String = "NOT_STARTED"
        Public Const InProgress As String = "IN_PROGRESS"
        Public Const Ended As String = "ENDED"

        Private Sub New()
        End Sub

        Public Shared Function ToDisplayText(status As String) As String
            Select Case If(status, String.Empty).Trim().ToUpperInvariant()
                Case NotStarted
                    Return "Chưa diễn ra"
                Case InProgress
                    Return "Đang diễn ra"
                Case Ended
                    Return "Đã kết thúc"
                Case Else
                    Return "Không xác định"
            End Select
        End Function
    End Class
End Namespace
