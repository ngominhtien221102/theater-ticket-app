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
    Public Class frmSeatAssignment
        Inherits Form

        Private ReadOnly _bookingRepository As New BookingRepository()
        Private ReadOnly _seatButtons As New Dictionary(Of String, Button)(StringComparer.OrdinalIgnoreCase)

        Private _blockedSeats As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Private _selectedSeats As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Private _currentBooking As BookingSummary
        Private _isLoadingPerformances As Boolean
        Private _isLoadingBookings As Boolean

        Private ReadOnly cmbPerformance As New ComboBox()
        Private ReadOnly cmbBooking As New ComboBox()
        Private ReadOnly btnReloadPerformances As New Button()
        Private ReadOnly btnReloadBookings As New Button()
        Private ReadOnly btnRefreshSeatMap As New Button()
        Private ReadOnly lblBookingInfo As New Label()
        Private ReadOnly pnlSeatMap As New Panel()
        Private ReadOnly lstSelectedSeats As New ListBox()
        Private ReadOnly lblSelectedCount As New Label()
        Private ReadOnly btnCancelBooking As New Button()
        Private ReadOnly btnSave As New Button()
        Private ReadOnly btnBack As New Button()

        Public Sub New()
            InitializeLayout()
            WireEvents()
            CreateSeatButtons()
            LoadPerformances()
        End Sub

        Private Sub InitializeLayout()
            Text = "frmSeatAssignment - Gán ghế theo sơ đồ"
            StartPosition = FormStartPosition.CenterParent
            Width = 1020
            Height = 800
            MinimumSize = New Size(980, 740)

            Dim lblPerformance As New Label() With {.Text = "Ca diễn:", .Location = New Point(15, 20), .AutoSize = True}
            cmbPerformance.Location = New Point(80, 16)
            cmbPerformance.Width = 610
            cmbPerformance.DropDownStyle = ComboBoxStyle.DropDownList

            btnReloadPerformances.Text = "Tải ca diễn"
            btnReloadPerformances.Location = New Point(700, 14)
            btnReloadPerformances.Size = New Size(85, 30)

            Dim lblBooking As New Label() With {.Text = "Booking:", .Location = New Point(15, 56), .AutoSize = True}
            cmbBooking.Location = New Point(80, 52)
            cmbBooking.Width = 700
            cmbBooking.DropDownStyle = ComboBoxStyle.DropDownList

            btnReloadBookings.Text = "Tải booking"
            btnReloadBookings.Location = New Point(790, 50)
            btnReloadBookings.Size = New Size(90, 30)

            btnRefreshSeatMap.Text = "Tải lại ghế"
            btnRefreshSeatMap.Location = New Point(885, 50)
            btnRefreshSeatMap.Size = New Size(90, 30)

            lblBookingInfo.Text = "Chọn ca diễn để xem booking."
            lblBookingInfo.Location = New Point(15, 90)
            lblBookingInfo.Size = New Size(960, 35)
            lblBookingInfo.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)

            pnlSeatMap.Location = New Point(15, 130)
            pnlSeatMap.Size = New Size(760, 600)
            pnlSeatMap.BorderStyle = BorderStyle.FixedSingle
            pnlSeatMap.AutoScroll = True

            Dim legendAvailable As Label = BuildLegendLabel("Trắng: ghế trống", Color.WhiteSmoke, New Point(790, 140))
            Dim legendSelected As Label = BuildLegendLabel("Xanh: ghế chọn hiện tại", Color.DodgerBlue, New Point(790, 175))
            Dim legendBlocked As Label = BuildLegendLabel("Đỏ: ghế đã có booking khác", Color.IndianRed, New Point(790, 210))

            Dim lblSelected As New Label() With {.Text = "Ghế đã chọn:", .Location = New Point(790, 260), .AutoSize = True}
            lstSelectedSeats.Location = New Point(790, 285)
            lstSelectedSeats.Size = New Size(185, 320)

            lblSelectedCount.Location = New Point(790, 612)
            lblSelectedCount.Size = New Size(190, 22)
            lblSelectedCount.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)

            btnCancelBooking.Text = "Hủy booking"
            btnCancelBooking.Location = New Point(790, 640)
            btnCancelBooking.Size = New Size(185, 35)
            btnCancelBooking.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)

            btnSave.Text = "Lưu gán ghế"
            btnSave.Location = New Point(790, 680)
            btnSave.Size = New Size(185, 40)
            btnSave.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)

            btnBack.Text = "Quay lại"
            btnBack.Location = New Point(790, 725)
            btnBack.Size = New Size(185, 35)

            Controls.Add(lblPerformance)
            Controls.Add(cmbPerformance)
            Controls.Add(btnReloadPerformances)
            Controls.Add(lblBooking)
            Controls.Add(cmbBooking)
            Controls.Add(btnReloadBookings)
            Controls.Add(btnRefreshSeatMap)
            Controls.Add(lblBookingInfo)
            Controls.Add(pnlSeatMap)
            Controls.Add(legendAvailable)
            Controls.Add(legendSelected)
            Controls.Add(legendBlocked)
            Controls.Add(lblSelected)
            Controls.Add(lstSelectedSeats)
            Controls.Add(lblSelectedCount)
            Controls.Add(btnCancelBooking)
            Controls.Add(btnSave)
            Controls.Add(btnBack)

            CancelButton = btnBack
        End Sub

        Private Function BuildLegendLabel(text As String, backgroundColor As Color, location As Point) As Label
            Return New Label() With {
                .Text = text,
                .Location = location,
                .Size = New Size(185, 28),
                .BackColor = backgroundColor,
                .BorderStyle = BorderStyle.FixedSingle,
                .TextAlign = ContentAlignment.MiddleLeft
            }
        End Function

        Private Sub WireEvents()
            AddHandler btnReloadPerformances.Click, AddressOf btnReloadPerformances_Click
            AddHandler cmbPerformance.SelectedIndexChanged, AddressOf cmbPerformance_SelectedIndexChanged
            AddHandler btnReloadBookings.Click, AddressOf btnReloadBookings_Click
            AddHandler cmbBooking.SelectedIndexChanged, AddressOf cmbBooking_SelectedIndexChanged
            AddHandler btnSave.Click, AddressOf btnSave_Click
            AddHandler btnCancelBooking.Click, AddressOf btnCancelBooking_Click
            AddHandler btnRefreshSeatMap.Click, AddressOf btnRefreshSeatMap_Click
            AddHandler btnBack.Click, AddressOf btnBack_Click
        End Sub

        Private Sub CreateSeatButtons()
            pnlSeatMap.Controls.Clear()
            _seatButtons.Clear()

            Dim seatSize As Integer = 56
            Dim gap As Integer = 6
            Dim topOffset As Integer = 35
            Dim leftOffset As Integer = 45

            For rowIndex As Integer = 0 To 9
                Dim rowChar As Char = ChrW(AscW("A"c) + rowIndex)

                Dim rowLabel As New Label() With {
                    .Text = rowChar.ToString(),
                    .Location = New Point(10, topOffset + (rowIndex * (seatSize + gap)) + 18),
                    .AutoSize = True,
                    .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
                }
                pnlSeatMap.Controls.Add(rowLabel)

                For seatNumber As Integer = 1 To 10
                    If rowIndex = 0 Then
                        Dim colLabel As New Label() With {
                            .Text = seatNumber.ToString(),
                            .Location = New Point(leftOffset + ((seatNumber - 1) * (seatSize + gap)) + 22, 8),
                            .AutoSize = True,
                            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
                        }
                        pnlSeatMap.Controls.Add(colLabel)
                    End If

                    Dim seatKey As String = $"{rowChar}{seatNumber}"
                    Dim seatButton As New Button() With {
                        .Text = seatKey,
                        .Tag = seatKey,
                        .Size = New Size(seatSize, seatSize),
                        .Location = New Point(leftOffset + ((seatNumber - 1) * (seatSize + gap)), topOffset + (rowIndex * (seatSize + gap))),
                        .FlatStyle = FlatStyle.Flat,
                        .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
                        .BackColor = Color.WhiteSmoke
                    }
                    AddHandler seatButton.Click, AddressOf SeatButton_Click
                    _seatButtons.Add(seatKey, seatButton)
                    pnlSeatMap.Controls.Add(seatButton)
                Next
            Next
        End Sub

        Private Sub ResetSeatState(infoMessage As String)
            _currentBooking = Nothing
            _blockedSeats = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            _selectedSeats = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            lblBookingInfo.Text = infoMessage
            RefreshSeatVisuals()
        End Sub

        Private Sub LoadPerformances(Optional preferredPerformanceId As Nullable(Of Integer) = Nothing)
            Try
                _isLoadingPerformances = True

                Dim currentPerformanceId As Nullable(Of Integer) = preferredPerformanceId
                If Not currentPerformanceId.HasValue Then
                    Dim selectedPerformance As Performance = TryCast(cmbPerformance.SelectedItem, Performance)
                    If selectedPerformance IsNot Nothing Then
                        currentPerformanceId = selectedPerformance.Id
                    End If
                End If

                Dim performances As List(Of Performance) = _bookingRepository.GetPerformancesWithBookings()
                cmbPerformance.DataSource = Nothing

                If performances.Count = 0 Then
                    cmbPerformance.Items.Clear()
                    cmbBooking.DataSource = Nothing
                    cmbBooking.Items.Clear()
                    ResetSeatState("Chưa có ca diễn nào có booking.")
                    Return
                End If

                cmbPerformance.DataSource = performances
                cmbPerformance.DisplayMember = NameOf(Performance.DisplayLabel)
                cmbPerformance.ValueMember = NameOf(Performance.Id)

                Dim selectedIndex As Integer = 0
                If currentPerformanceId.HasValue Then
                    For index As Integer = 0 To performances.Count - 1
                        If performances(index).Id = currentPerformanceId.Value Then
                            selectedIndex = index
                            Exit For
                        End If
                    Next
                End If
                cmbPerformance.SelectedIndex = selectedIndex
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi tải ca diễn", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                _isLoadingPerformances = False
            End Try

            LoadBookingsForSelectedPerformance()
        End Sub

        Private Sub LoadBookingsForSelectedPerformance(Optional preferredBookingId As Nullable(Of Integer) = Nothing)
            Dim selectedPerformance As Performance = TryCast(cmbPerformance.SelectedItem, Performance)
            If selectedPerformance Is Nothing Then
                cmbBooking.DataSource = Nothing
                cmbBooking.Items.Clear()
                ResetSeatState("Chọn ca diễn để xem booking.")
                Return
            End If

            Try
                _isLoadingBookings = True

                Dim currentBookingId As Nullable(Of Integer) = preferredBookingId
                If Not currentBookingId.HasValue Then
                    Dim selectedBooking As BookingSummary = TryCast(cmbBooking.SelectedItem, BookingSummary)
                    If selectedBooking IsNot Nothing Then
                        currentBookingId = selectedBooking.BookingId
                    End If
                End If

                Dim bookings As List(Of BookingSummary) = _bookingRepository.GetBookingSummariesByPerformance(selectedPerformance.Id)
                cmbBooking.DataSource = Nothing

                If bookings.Count = 0 Then
                    cmbBooking.Items.Clear()
                    ResetSeatState($"Ca diễn {selectedPerformance.DisplayLabel} chưa có booking.")
                    Return
                End If

                cmbBooking.DataSource = bookings
                cmbBooking.DisplayMember = NameOf(BookingSummary.DisplayText)
                cmbBooking.ValueMember = NameOf(BookingSummary.BookingId)

                Dim selectedIndex As Integer = 0
                If currentBookingId.HasValue Then
                    For index As Integer = 0 To bookings.Count - 1
                        If bookings(index).BookingId = currentBookingId.Value Then
                            selectedIndex = index
                            Exit For
                        End If
                    Next
                End If
                cmbBooking.SelectedIndex = selectedIndex
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi tải booking", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                _isLoadingBookings = False
            End Try

            LoadSeatMapForSelectedBooking()
        End Sub

        Private Sub LoadSeatMapForSelectedBooking()
            Dim selected As BookingSummary = TryCast(cmbBooking.SelectedItem, BookingSummary)
            If selected Is Nothing Then
                ResetSeatState("Chọn booking để gán ghế.")
                Return
            End If

            Try
                _currentBooking = selected
                _blockedSeats = _bookingRepository.GetBookedSeatKeys(_currentBooking.PerformanceId, _currentBooking.BookingId)
                _selectedSeats = _bookingRepository.GetSeatKeysForBooking(_currentBooking.BookingId)

                If _selectedSeats.Count > _currentBooking.TicketQty Then
                    MessageBox.Show(
                        $"Booking #{_currentBooking.BookingId} đang có {_selectedSeats.Count} ghế vượt quá số vé ({_currentBooking.TicketQty}). Vui lòng điều chỉnh và lưu lại.",
                        "Cảnh báo dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                End If

                lblBookingInfo.Text = $"Booking #{_currentBooking.BookingId} | Khách: {_currentBooking.CustomerName} | Suất diễn: {_currentBooking.PerformanceName} {_currentBooking.PerformanceStartTime:dd/MM/yyyy HH:mm} | Vé: {_currentBooking.TicketQty}"
                If Not CanEditSeatAssignment() Then
                    lblBookingInfo.Text &= " | Đã kết thúc: chỉ xem, không được chỉnh sửa gán ghế"
                End If

                RefreshSeatVisuals()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi tải sơ đồ ghế", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub RefreshSeatVisuals()
            Dim canEdit As Boolean = CanEditSeatAssignment()
            btnSave.Enabled = canEdit

            For Each kvp As KeyValuePair(Of String, Button) In _seatButtons
                Dim seatKey As String = kvp.Key
                Dim buttonControl As Button = kvp.Value

                If _blockedSeats.Contains(seatKey) Then
                    buttonControl.BackColor = Color.IndianRed
                    buttonControl.ForeColor = Color.White
                    buttonControl.Enabled = False
                ElseIf _selectedSeats.Contains(seatKey) Then
                    buttonControl.BackColor = Color.DodgerBlue
                    buttonControl.ForeColor = Color.White
                    buttonControl.Enabled = canEdit
                Else
                    buttonControl.BackColor = Color.WhiteSmoke
                    buttonControl.ForeColor = Color.Black
                    buttonControl.Enabled = canEdit
                End If
            Next

            lstSelectedSeats.Items.Clear()
            For Each seatKey As String In SortSeatKeys(_selectedSeats)
                lstSelectedSeats.Items.Add(seatKey)
            Next

            If _currentBooking Is Nothing Then
                lblSelectedCount.Text = "0/0 ghế đã chọn"
            Else
                lblSelectedCount.Text = $"{_selectedSeats.Count}/{_currentBooking.TicketQty} ghế đã chọn"
            End If
        End Sub

        Private Function CanEditSeatAssignment() As Boolean
            If _currentBooking Is Nothing Then
                Return False
            End If

            Dim selectedPerformance As Performance = TryCast(cmbPerformance.SelectedItem, Performance)
            If selectedPerformance Is Nothing Then
                Return False
            End If

            Return Not String.Equals(selectedPerformance.Status, PerformanceStatuses.Ended, StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function SortSeatKeys(seatKeys As IEnumerable(Of String)) As IEnumerable(Of String)
            Return seatKeys.
                OrderBy(Function(x) x.Substring(0, 1)).
                ThenBy(Function(x) Integer.Parse(x.Substring(1)))
        End Function

        Private Sub SeatButton_Click(sender As Object, e As EventArgs)
            If _currentBooking Is Nothing Then
                MessageBox.Show("Vui lòng chọn booking trước khi chọn ghế.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If Not CanEditSeatAssignment() Then
                MessageBox.Show("Suất diễn đã kết thúc, không thể chỉnh sửa gán ghế.", "Không thể chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim buttonControl As Button = CType(sender, Button)
            Dim seatKey As String = CType(buttonControl.Tag, String)

            If _blockedSeats.Contains(seatKey) Then
                MessageBox.Show("Ghế này đã được booking khác giữ.", "Ghế không khả dụng", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If _selectedSeats.Contains(seatKey) Then
                _selectedSeats.Remove(seatKey)
                RefreshSeatVisuals()
                Return
            End If

            If _selectedSeats.Count >= _currentBooking.TicketQty Then
                MessageBox.Show($"Bạn chỉ được chọn tối đa {_currentBooking.TicketQty} ghế.", "Vượt số lượng ghế", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            _selectedSeats.Add(seatKey)
            RefreshSeatVisuals()
        End Sub

        Private Function ValidateBeforeSave() As Boolean
            If _currentBooking Is Nothing Then
                MessageBox.Show("Vui lòng chọn booking.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            If Not CanEditSeatAssignment() Then
                MessageBox.Show("Suất diễn đã kết thúc, không thể chỉnh sửa gán ghế.", "Không thể chỉnh sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            If _selectedSeats.Count <> _currentBooking.TicketQty Then
                MessageBox.Show(
                    $"Booking cần đúng {_currentBooking.TicketQty} ghế, hiện tại đang chọn {_selectedSeats.Count} ghế.",
                    "Sai số lượng ghế",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return False
            End If

            Return True
        End Function

        Private Sub btnSave_Click(sender As Object, e As EventArgs)
            If Not ValidateBeforeSave() Then
                Return
            End If

            Dim confirmResult As DialogResult = MessageBox.Show(
                $"Lưu gán ghế cho booking #{_currentBooking.BookingId}?",
                "Xác nhận lưu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmResult <> DialogResult.Yes Then
                Return
            End If

            Try
                Dim bookingId As Integer = _currentBooking.BookingId
                _bookingRepository.SaveSeatAssignments(bookingId, _selectedSeats)
                MessageBox.Show("Lưu gán ghế thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
                LoadBookingsForSelectedPerformance(bookingId)
            Catch ex As InvalidOperationException
                MessageBox.Show(ex.Message, "Không thể lưu ghế", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                LoadSeatMapForSelectedBooking()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi lưu ghế", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnCancelBooking_Click(sender As Object, e As EventArgs)
            If _currentBooking Is Nothing Then
                MessageBox.Show("Vui lòng chọn booking cần hủy.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim confirmResult As DialogResult = MessageBox.Show(
                $"Xác nhận hủy booking #{_currentBooking.BookingId}?{Environment.NewLine}Chỉ được hủy trước 24 giờ so với giờ diễn.",
                "Xác nhận hủy booking",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmResult <> DialogResult.Yes Then
                Return
            End If

            Try
                Dim bookingId As Integer = _currentBooking.BookingId
                _bookingRepository.CancelBooking(bookingId)
                MessageBox.Show($"Đã hủy booking #{bookingId}.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
                LoadBookingsForSelectedPerformance()
            Catch ex As InvalidOperationException
                MessageBox.Show(ex.Message, "Không thể hủy booking", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Lỗi hủy booking", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnReloadPerformances_Click(sender As Object, e As EventArgs)
            LoadPerformances()
        End Sub

        Private Sub btnReloadBookings_Click(sender As Object, e As EventArgs)
            LoadBookingsForSelectedPerformance()
        End Sub

        Private Sub cmbPerformance_SelectedIndexChanged(sender As Object, e As EventArgs)
            If _isLoadingPerformances Then
                Return
            End If

            LoadBookingsForSelectedPerformance()
        End Sub

        Private Sub cmbBooking_SelectedIndexChanged(sender As Object, e As EventArgs)
            If _isLoadingBookings Then
                Return
            End If

            LoadSeatMapForSelectedBooking()
        End Sub

        Private Sub btnRefreshSeatMap_Click(sender As Object, e As EventArgs)
            LoadSeatMapForSelectedBooking()
        End Sub

        Private Sub btnBack_Click(sender As Object, e As EventArgs)
            Close()
        End Sub
    End Class
End Namespace
