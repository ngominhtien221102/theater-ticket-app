Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms
Imports TheaterTicketingApp.Services

Namespace TheaterTicketingApp
    Friend Module Program
        <STAThread>
        Public Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Using statusJob As New PerformanceStatusJob()
                statusJob.Start()
                Application.Run(New MainForm())
            End Using
        End Sub
    End Module
End Namespace
