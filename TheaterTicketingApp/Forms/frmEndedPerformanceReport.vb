Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports TheaterTicketingApp.Models
Imports TheaterTicketingApp.Repositories

Namespace TheaterTicketingApp.Forms
    Public Class frmEndedPerformanceReport
        Inherits Form

        Private ReadOnly _performanceRepository As New PerformanceRepository()

        Private ReadOnly dgvReport As New DataGridView()
        Private ReadOnly btnReload As New Button()
        Private ReadOnly btnBack As New Button()
        Private ReadOnly lblSummary As New Label()

        Public Sub New()
            InitializeLayout()
            WireEvents()
            LoadReport()
        End Sub

        Private Sub InitializeLayout()
            Text = "frmEndedPerformanceReport - Báo cáo suất diễn đã kết thúc"
            StartPosition = FormStartPosition.CenterParent
            Width = 1020
            Height = 650
            MinimumSize = New Size(980, 600)

            btnReload.Text = "Tải báo cáo"
            btnReload.Location = New Point(15, 15)
            btnReload.Size = New Size(120, 32)

            btnBack.Text = "Quay lại"
            btnBack.Location = New Point(145, 15)
            btnBack.Size = New Size(100, 32)

            lblSummary.Location = New Point(260, 20)
            lblSummary.Size = New Size(730, 25)
            lblSummary.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            lblSummary.TextAlign = ContentAlignment.MiddleLeft

            dgvReport.Location = New Point(15, 60)
            dgvReport.Size = New Size(970, 530)
            dgvReport.ReadOnly = True
            dgvReport.AllowUserToAddRows = False
            dgvReport.AllowUserToDeleteRows = False
            dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            dgvReport.MultiSelect = False
            dgvReport.AutoGenerateColumns = False
            dgvReport.RowHeadersVisible = False
            dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            dgvReport.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(EndedPerformanceReportItem.PerformanceId),
                .HeaderText = "ID",
                .FillWeight = 10
            })
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(EndedPerformanceReportItem.PlayName),
                .HeaderText = "Tên vở diễn",
                .FillWeight = 28
            })
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(EndedPerformanceReportItem.StartTime),
                .HeaderText = "Bắt đầu",
                .FillWeight = 18,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "dd/MM/yyyy HH:mm"}
            })
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(EndedPerformanceReportItem.EndTime),
                .HeaderText = "Kết thúc",
                .FillWeight = 18,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "dd/MM/yyyy HH:mm"}
            })
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(EndedPerformanceReportItem.SeatUsageDisplay),
                .HeaderText = "Ghế đã booking / Tổng ghế",
                .FillWeight = 16
            })
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(EndedPerformanceReportItem.Revenue),
                .HeaderText = "Doanh thu (VND)",
                .FillWeight = 18,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N0"}
            })

            Controls.Add(btnReload)
            Controls.Add(btnBack)
            Controls.Add(lblSummary)
            Controls.Add(dgvReport)

            CancelButton = btnBack
        End Sub

        Private Sub WireEvents()
            AddHandler btnReload.Click, AddressOf btnReload_Click
            AddHandler btnBack.Click, AddressOf btnBack_Click
        End Sub

        Private Sub LoadReport()
            Try
                _performanceRepository.SyncPerformanceStatuses()

                Dim reportItems As List(Of EndedPerformanceReportItem) = _performanceRepository.GetEndedPerformanceReport()
                dgvReport.DataSource = reportItems

                Dim totalPerformances As Integer = reportItems.Count
                Dim totalBookedSeats As Integer = reportItems.Sum(Function(x) x.BookedSeats)
                Dim totalRevenue As Decimal = reportItems.Sum(Function(x) x.Revenue)
                lblSummary.Text = $"Tổng suất đã kết thúc: {totalPerformances} | Tổng ghế đã booking: {totalBookedSeats} | Tổng doanh thu: {totalRevenue:N0} VND"
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi tải báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnReload_Click(sender As Object, e As EventArgs)
            LoadReport()
        End Sub

        Private Sub btnBack_Click(sender As Object, e As EventArgs)
            Close()
        End Sub
    End Class
End Namespace
