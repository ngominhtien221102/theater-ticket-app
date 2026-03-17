Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Namespace TheaterTicketingApp
    Public Class MainForm
        Inherits Form

        Private ReadOnly btnPerformanceMaster As New Button()
        Private ReadOnly btnBooking As New Button()
        Private ReadOnly btnSeatAssignment As New Button()
        Private ReadOnly btnReport As New Button()
        Private ReadOnly lblConnectionHint As New Label()

        Public Sub New()
            InitializeLayout()
            WireEvents()
        End Sub

        Private Sub InitializeLayout()
            Text = "Hệ thống Bán Vé Nhà Hát"
            StartPosition = FormStartPosition.CenterScreen
            Width = 880
            Height = 360
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False

            Dim lblTitle As New Label() With {
                .Text = "Hệ thống Bán Vé Nhà Hát",
                .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
                .AutoSize = True,
                .Location = New Point(280, 30)
            }

            btnPerformanceMaster.Text = "Quản lý suất diễn"
            btnPerformanceMaster.Size = New Size(185, 55)
            btnPerformanceMaster.Location = New Point(30, 110)
            btnPerformanceMaster.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

            btnBooking.Text = "Đặt vé"
            btnBooking.Size = New Size(185, 55)
            btnBooking.Location = New Point(235, 110)
            btnBooking.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

            btnSeatAssignment.Text = "Gán ghế"
            btnSeatAssignment.Size = New Size(185, 55)
            btnSeatAssignment.Location = New Point(440, 110)
            btnSeatAssignment.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

            btnReport.Text = "Báo cáo"
            btnReport.Size = New Size(185, 55)
            btnReport.Location = New Point(645, 110)
            btnReport.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

            lblConnectionHint.Text = "Connection string: đặt biến môi trường THEATER_DB_CONNECTION nếu khác mặc định."
            lblConnectionHint.Font = New Font("Segoe UI", 9.0F, FontStyle.Italic)
            lblConnectionHint.AutoSize = True
            lblConnectionHint.Location = New Point(30, 225)

            Controls.Add(lblTitle)
            Controls.Add(btnPerformanceMaster)
            Controls.Add(btnBooking)
            Controls.Add(btnSeatAssignment)
            Controls.Add(btnReport)
            Controls.Add(lblConnectionHint)
        End Sub

        Private Sub WireEvents()
            AddHandler btnPerformanceMaster.Click, AddressOf btnPerformanceMaster_Click
            AddHandler btnBooking.Click, AddressOf btnBooking_Click
            AddHandler btnSeatAssignment.Click, AddressOf btnSeatAssignment_Click
            AddHandler btnReport.Click, AddressOf btnReport_Click
        End Sub

        Private Sub btnPerformanceMaster_Click(sender As Object, e As EventArgs)
            Using frm As New Forms.frmPerformanceMaster()
                frm.ShowDialog(Me)
            End Using
        End Sub

        Private Sub btnBooking_Click(sender As Object, e As EventArgs)
            Using frm As New Forms.frmBooking()
                frm.ShowDialog(Me)
            End Using
        End Sub

        Private Sub btnSeatAssignment_Click(sender As Object, e As EventArgs)
            Using frm As New Forms.frmSeatAssignment()
                frm.ShowDialog(Me)
            End Using
        End Sub

        Private Sub btnReport_Click(sender As Object, e As EventArgs)
            Using frm As New Forms.frmEndedPerformanceReport()
                frm.ShowDialog(Me)
            End Using
        End Sub
    End Class
End Namespace
