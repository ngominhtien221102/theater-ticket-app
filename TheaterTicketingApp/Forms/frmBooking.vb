Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports TheaterTicketingApp.Models
Imports TheaterTicketingApp.Repositories

Namespace TheaterTicketingApp.Forms
    Public Class frmBooking
        Inherits Form

        Private ReadOnly _bookingRepository As New BookingRepository()

        Private ReadOnly txtPerformanceSearch As New TextBox()
        Private ReadOnly btnLoadPerformances As New Button()
        Private ReadOnly cmbPerformance As New ComboBox()
        Private ReadOnly txtCustomerName As New TextBox()
        Private ReadOnly numTicketQty As New NumericUpDown()
        Private ReadOnly lblUnitPriceValue As New Label()
        Private ReadOnly lblTotalValue As New Label()
        Private ReadOnly btnBook As New Button()
        Private ReadOnly btnReset As New Button()
        Private ReadOnly btnBack As New Button()

        Public Sub New()
            InitializeLayout()
            WireEvents()
            LoadPerformances()
            UpdatePricePreview()
        End Sub

        Private Sub InitializeLayout()
            Text = "frmBooking - Đặt vé"
            StartPosition = FormStartPosition.CenterParent
            Width = 760
            Height = 430
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False

            Dim lblSearch As New Label() With {.Text = "Tìm suất diễn:", .Location = New Point(25, 25), .AutoSize = True}
            txtPerformanceSearch.Location = New Point(120, 20)
            txtPerformanceSearch.Width = 300

            btnLoadPerformances.Text = "Tải danh sách"
            btnLoadPerformances.Location = New Point(430, 18)
            btnLoadPerformances.Size = New Size(110, 30)

            Dim lblPerformance As New Label() With {.Text = "Suất diễn:", .Location = New Point(25, 75), .AutoSize = True}
            cmbPerformance.Location = New Point(120, 70)
            cmbPerformance.Width = 550
            cmbPerformance.DropDownStyle = ComboBoxStyle.DropDownList

            Dim lblCustomerName As New Label() With {.Text = "Khách hàng:", .Location = New Point(25, 125), .AutoSize = True}
            txtCustomerName.Location = New Point(120, 120)
            txtCustomerName.Width = 300

            Dim lblQty As New Label() With {.Text = "Số lượng vé:", .Location = New Point(25, 175), .AutoSize = True}
            numTicketQty.Location = New Point(120, 170)
            numTicketQty.Minimum = 1
            numTicketQty.Maximum = 10
            numTicketQty.Value = 1
            numTicketQty.Width = 100

            Dim lblUnitPrice As New Label() With {.Text = "Đơn giá:", .Location = New Point(25, 225), .AutoSize = True}
            lblUnitPriceValue.Location = New Point(120, 225)
            lblUnitPriceValue.AutoSize = True
            lblUnitPriceValue.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)

            Dim lblTotal As New Label() With {.Text = "Tổng tiền:", .Location = New Point(25, 255), .AutoSize = True}
            lblTotalValue.Location = New Point(120, 255)
            lblTotalValue.AutoSize = True
            lblTotalValue.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
            lblTotalValue.ForeColor = Color.DarkBlue

            btnBook.Text = "Đặt vé"
            btnBook.Location = New Point(25, 320)
            btnBook.Size = New Size(140, 40)

            btnReset.Text = "Làm mới"
            btnReset.Location = New Point(175, 320)
            btnReset.Size = New Size(140, 40)

            btnBack.Text = "Quay lại"
            btnBack.Location = New Point(325, 320)
            btnBack.Size = New Size(140, 40)

            Controls.Add(lblSearch)
            Controls.Add(txtPerformanceSearch)
            Controls.Add(btnLoadPerformances)
            Controls.Add(lblPerformance)
            Controls.Add(cmbPerformance)
            Controls.Add(lblCustomerName)
            Controls.Add(txtCustomerName)
            Controls.Add(lblQty)
            Controls.Add(numTicketQty)
            Controls.Add(lblUnitPrice)
            Controls.Add(lblUnitPriceValue)
            Controls.Add(lblTotal)
            Controls.Add(lblTotalValue)
            Controls.Add(btnBook)
            Controls.Add(btnReset)
            Controls.Add(btnBack)

            CancelButton = btnBack
        End Sub

        Private Sub WireEvents()
            AddHandler btnLoadPerformances.Click, AddressOf btnLoadPerformances_Click
            AddHandler cmbPerformance.SelectedIndexChanged, AddressOf PriceInputs_Changed
            AddHandler numTicketQty.ValueChanged, AddressOf PriceInputs_Changed
            AddHandler btnBook.Click, AddressOf btnBook_Click
            AddHandler btnReset.Click, AddressOf btnReset_Click
            AddHandler btnBack.Click, AddressOf btnBack_Click
        End Sub

        Private Sub LoadPerformances()
            Try
                Dim performances As List(Of Performance) = _bookingRepository.GetPerformancesForBooking(txtPerformanceSearch.Text)
                cmbPerformance.DataSource = Nothing
                cmbPerformance.DataSource = performances
                cmbPerformance.DisplayMember = NameOf(Performance.DisplayLabel)
                cmbPerformance.ValueMember = NameOf(Performance.Id)
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi tải suất diễn", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub UpdatePricePreview()
            Dim selectedPerformance As Performance = TryCast(cmbPerformance.SelectedItem, Performance)
            If selectedPerformance Is Nothing Then
                lblUnitPriceValue.Text = "N/A"
                lblTotalValue.Text = "N/A"
                Return
            End If

            Dim unitPrice As Decimal = selectedPerformance.TicketPrice
            Dim totalAmount As Decimal = unitPrice * numTicketQty.Value

            lblUnitPriceValue.Text = $"{unitPrice:N0} VND"
            lblTotalValue.Text = $"{totalAmount:N0} VND"
        End Sub

        Private Function ValidateInputs() As Boolean
            If cmbPerformance.SelectedItem Is Nothing Then
                MessageBox.Show("Vui lòng chọn suất diễn.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
                MessageBox.Show("Vui lòng nhập tên khách hàng.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCustomerName.Focus()
                Return False
            End If

            If numTicketQty.Value <= 0 Then
                MessageBox.Show("Số lượng vé phải lớn hơn 0.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numTicketQty.Focus()
                Return False
            End If

            Dim selectedPerformance As Performance = TryCast(cmbPerformance.SelectedItem, Performance)
            If selectedPerformance Is Nothing OrElse selectedPerformance.TicketPrice <= 0D Then
                MessageBox.Show("Giá vé của suất diễn không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            Return True
        End Function

        Private Sub ResetForm()
            txtCustomerName.Clear()
            numTicketQty.Value = 1
            UpdatePricePreview()
            txtCustomerName.Focus()
        End Sub

        Private Sub btnLoadPerformances_Click(sender As Object, e As EventArgs)
            LoadPerformances()
            UpdatePricePreview()
        End Sub

        Private Sub PriceInputs_Changed(sender As Object, e As EventArgs)
            UpdatePricePreview()
        End Sub

        Private Sub btnBook_Click(sender As Object, e As EventArgs)
            If Not ValidateInputs() Then
                Return
            End If

            Dim selectedPerformance As Performance = TryCast(cmbPerformance.SelectedItem, Performance)
            If selectedPerformance Is Nothing Then
                MessageBox.Show("Không lấy được thông tin suất diễn.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Dim unitPrice As Decimal = selectedPerformance.TicketPrice
            Dim totalAmount As Decimal = unitPrice * numTicketQty.Value

            Dim booking As New Booking With {
                .PerformanceId = selectedPerformance.Id,
                .CustomerName = txtCustomerName.Text.Trim(),
                .SeatType = SeatTypes.SingleType,
                .TicketQty = Convert.ToInt32(numTicketQty.Value),
                .UnitPrice = unitPrice,
                .TotalAmount = totalAmount
            }

            Dim confirmResult As DialogResult = MessageBox.Show(
                $"Xác nhận đặt {booking.TicketQty} vé cho khách {booking.CustomerName}?{Environment.NewLine}Đơn giá: {unitPrice:N0} VND{Environment.NewLine}Tổng tiền: {totalAmount:N0} VND",
                "Xác nhận lưu booking",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmResult <> DialogResult.Yes Then
                Return
            End If

            Try
                Dim newBookingId As Integer = _bookingRepository.CreateBooking(booking)
                MessageBox.Show(
                    $"Đặt vé thành công. Booking ID: {newBookingId}{Environment.NewLine}Tổng tiền: {totalAmount:N0} VND",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
                ResetForm()
            Catch ex As InvalidOperationException
                MessageBox.Show(ex.Message, "Không thể tạo booking", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi đặt vé", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnReset_Click(sender As Object, e As EventArgs)
            ResetForm()
        End Sub

        Private Sub btnBack_Click(sender As Object, e As EventArgs)
            Close()
        End Sub
    End Class
End Namespace
