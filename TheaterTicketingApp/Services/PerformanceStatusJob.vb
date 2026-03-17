Option Strict On
Option Explicit On

Imports System
Imports System.Threading
Imports TheaterTicketingApp.Repositories

Namespace TheaterTicketingApp.Services
    Public Class PerformanceStatusJob
        Implements IDisposable

        Private Const IntervalMilliseconds As Integer = 5 * 60 * 1000

        Private ReadOnly _performanceRepository As New PerformanceRepository()
        Private ReadOnly _timer As Timer
        Private _disposed As Boolean

        Public Sub New()
            _timer = New Timer(AddressOf Execute, Nothing, Timeout.Infinite, Timeout.Infinite)
        End Sub

        Public Sub Start()
            Execute(Nothing)
            _timer.Change(IntervalMilliseconds, IntervalMilliseconds)
        End Sub

        Private Sub Execute(state As Object)
            Try
                _performanceRepository.SyncPerformanceStatuses()
            Catch
                ' Keep background job non-blocking for UI flow.
            End Try
        End Sub

        Public Sub [Stop]()
            _timer.Change(Timeout.Infinite, Timeout.Infinite)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _disposed Then
                Return
            End If

            _disposed = True
            [Stop]()
            _timer.Dispose()
        End Sub
    End Class
End Namespace
