Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports Npgsql
Imports TheaterTicketingApp.Models
Imports TheaterTicketingApp.Repositories

Namespace TheaterTicketingApp.Forms
    Public Class frmPerformanceMaster
        Inherits Form

        Private Const MinHoursBeforeStartForNewPerformance As Integer = 2

        Private ReadOnly _performanceRepository As New PerformanceRepository()
        Private _selectedPerformanceId As Nullable(Of Integer)

        Private ReadOnly txtSearchName As New TextBox()
        Private ReadOnly dtpSearchFrom As New DateTimePicker()
        Private ReadOnly dtpSearchTo As New DateTimePicker()
        Private ReadOnly btnSearch As New Button()
        Private ReadOnly btnClearSearch As New Button()

        Private ReadOnly dgvPerformances As New DataGridView()

        Private ReadOnly txtPlayName As New TextBox()
        Private ReadOnly dtpStartTime As New DateTimePicker()
        Private ReadOnly numDuration As New NumericUpDown()
        Private ReadOnly numTicketPrice As New NumericUpDown()
        Private ReadOnly btnAdd As New Button()
        Private ReadOnly btnUpdate As New Button()
        Private ReadOnly btnDelete As New Button()
        Private ReadOnly btnClearForm As New Button()
        Private ReadOnly btnBack As New Button()

        Public Sub New()
            InitializeLayout()
            WireEvents()
            LoadPerformances()
        End Sub

        Private Sub InitializeLayout()
            Text = "frmPerformanceMaster - Quản lý suất diễn"
            StartPosition = FormStartPosition.CenterParent
            Width = 1000
            Height = 720
            MinimumSize = New Size(980, 680)

            Dim grpSearch As New GroupBox() With {
                .Text = "Tìm kiếm suất diễn",
                .Location = New Point(15, 15),
                .Size = New Size(950, 95)
            }

            Dim lblSearchName As New Label() With {.Text = "Tên vở diễn:", .Location = New Point(15, 30), .AutoSize = True}
            txtSearchName.Location = New Point(95, 26)
            txtSearchName.Width = 220

            Dim lblFrom As New Label() With {.Text = "Từ:", .Location = New Point(335, 30), .AutoSize = True}
            dtpSearchFrom.Location = New Point(365, 26)
            dtpSearchFrom.CustomFormat = "dd/MM/yyyy HH:mm"
            dtpSearchFrom.Format = DateTimePickerFormat.Custom
            dtpSearchFrom.ShowCheckBox = True
            dtpSearchFrom.Checked = False
            dtpSearchFrom.Width = 180

            Dim lblTo As New Label() With {.Text = "Đến:", .Location = New Point(565, 30), .AutoSize = True}
            dtpSearchTo.Location = New Point(600, 26)
            dtpSearchTo.CustomFormat = "dd/MM/yyyy HH:mm"
            dtpSearchTo.Format = DateTimePickerFormat.Custom
            dtpSearchTo.ShowCheckBox = True
            dtpSearchTo.Checked = False
            dtpSearchTo.Width = 180

            btnSearch.Text = "Tìm"
            btnSearch.Location = New Point(795, 24)
            btnSearch.Size = New Size(65, 28)

            btnClearSearch.Text = "Xóa lọc"
            btnClearSearch.Location = New Point(865, 24)
            btnClearSearch.Size = New Size(70, 28)

            grpSearch.Controls.Add(lblSearchName)
            grpSearch.Controls.Add(txtSearchName)
            grpSearch.Controls.Add(lblFrom)
            grpSearch.Controls.Add(dtpSearchFrom)
            grpSearch.Controls.Add(lblTo)
            grpSearch.Controls.Add(dtpSearchTo)
            grpSearch.Controls.Add(btnSearch)
            grpSearch.Controls.Add(btnClearSearch)

            dgvPerformances.Location = New Point(15, 125)
            dgvPerformances.Size = New Size(950, 330)
            dgvPerformances.ReadOnly = True
            dgvPerformances.AllowUserToAddRows = False
            dgvPerformances.AllowUserToDeleteRows = False
            dgvPerformances.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            dgvPerformances.MultiSelect = False
            dgvPerformances.AutoGenerateColumns = False
            dgvPerformances.RowHeadersVisible = False
            dgvPerformances.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            dgvPerformances.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(Performance.Id),
                .HeaderText = "ID",
                .FillWeight = 12
            })
            dgvPerformances.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(Performance.PlayName),
                .HeaderText = "Tên vở diễn",
                .FillWeight = 33
            })
            dgvPerformances.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(Performance.StartTime),
                .HeaderText = "Thời gian bắt đầu",
                .FillWeight = 24,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "dd/MM/yyyy HH:mm"}
            })
            dgvPerformances.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(Performance.DurationMinutes),
                .HeaderText = "Thời lượng (phút)",
                .FillWeight = 14
            })
            dgvPerformances.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(Performance.TicketPrice),
                .HeaderText = "Giá vé (VND)",
                .FillWeight = 17,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N0"}
            })
            dgvPerformances.Columns.Add(New DataGridViewTextBoxColumn() With {
                .DataPropertyName = NameOf(Performance.StatusDisplay),
                .HeaderText = "Trạng thái",
                .FillWeight = 14
            })

            Dim grpEditor As New GroupBox() With {
                .Text = "Thông tin suất diễn",
                .Location = New Point(15, 470),
                .Size = New Size(950, 185)
            }

            Dim lblPlayName As New Label() With {.Text = "Tên vở diễn:", .Location = New Point(15, 35), .AutoSize = True}
            txtPlayName.Location = New Point(110, 30)
            txtPlayName.Width = 350

            Dim lblStartTime As New Label() With {.Text = "Bắt đầu:", .Location = New Point(480, 35), .AutoSize = True}
            dtpStartTime.Location = New Point(540, 30)
            dtpStartTime.Width = 210
            dtpStartTime.CustomFormat = "dd/MM/yyyy HH:mm"
            dtpStartTime.Format = DateTimePickerFormat.Custom
            dtpStartTime.ShowUpDown = True

            Dim lblDuration As New Label() With {.Text = "Thời lượng:", .Location = New Point(15, 75), .AutoSize = True}
            numDuration.Location = New Point(110, 70)
            numDuration.Minimum = 30
            numDuration.Maximum = 480
            numDuration.Increment = 15
            numDuration.Width = 120
            numDuration.Value = 120

            Dim lblTicketPrice As New Label() With {.Text = "Giá vé (VND):", .Location = New Point(250, 75), .AutoSize = True}
            numTicketPrice.Location = New Point(340, 70)
            numTicketPrice.Minimum = 1000D
            numTicketPrice.Maximum = 10000000D
            numTicketPrice.Increment = 10000D
            numTicketPrice.DecimalPlaces = 0
            numTicketPrice.ThousandsSeparator = True
            numTicketPrice.Width = 140
            numTicketPrice.Value = 120000D

            btnAdd.Text = "Thêm mới"
            btnAdd.Location = New Point(15, 125)
            btnAdd.Size = New Size(120, 32)

            btnUpdate.Text = "Cập nhật"
            btnUpdate.Location = New Point(145, 125)
            btnUpdate.Size = New Size(120, 32)

            btnDelete.Text = "Xóa"
            btnDelete.Location = New Point(275, 125)
            btnDelete.Size = New Size(120, 32)

            btnClearForm.Text = "Làm mới form"
            btnClearForm.Location = New Point(405, 125)
            btnClearForm.Size = New Size(120, 32)

            btnBack.Text = "Quay lại"
            btnBack.Location = New Point(535, 125)
            btnBack.Size = New Size(120, 32)

            grpEditor.Controls.Add(lblPlayName)
            grpEditor.Controls.Add(txtPlayName)
            grpEditor.Controls.Add(lblStartTime)
            grpEditor.Controls.Add(dtpStartTime)
            grpEditor.Controls.Add(lblDuration)
            grpEditor.Controls.Add(numDuration)
            grpEditor.Controls.Add(lblTicketPrice)
            grpEditor.Controls.Add(numTicketPrice)
            grpEditor.Controls.Add(btnAdd)
            grpEditor.Controls.Add(btnUpdate)
            grpEditor.Controls.Add(btnDelete)
            grpEditor.Controls.Add(btnClearForm)
            grpEditor.Controls.Add(btnBack)

            Controls.Add(grpSearch)
            Controls.Add(dgvPerformances)
            Controls.Add(grpEditor)

            CancelButton = btnBack
        End Sub

        Private Sub WireEvents()
            AddHandler btnSearch.Click, AddressOf btnSearch_Click
            AddHandler btnClearSearch.Click, AddressOf btnClearSearch_Click
            AddHandler btnAdd.Click, AddressOf btnAdd_Click
            AddHandler btnUpdate.Click, AddressOf btnUpdate_Click
            AddHandler btnDelete.Click, AddressOf btnDelete_Click
            AddHandler btnClearForm.Click, AddressOf btnClearForm_Click
            AddHandler btnBack.Click, AddressOf btnBack_Click
            AddHandler dgvPerformances.SelectionChanged, AddressOf dgvPerformances_SelectionChanged
        End Sub

        Private Sub LoadPerformances()
            Try
                _performanceRepository.SyncPerformanceStatuses()

                Dim startFrom As Nullable(Of DateTime) = Nothing
                If dtpSearchFrom.Checked Then
                    startFrom = New DateTime(
                        dtpSearchFrom.Value.Year,
                        dtpSearchFrom.Value.Month,
                        dtpSearchFrom.Value.Day,
                        dtpSearchFrom.Value.Hour,
                        dtpSearchFrom.Value.Minute,
                        0)
                End If

                Dim startTo As Nullable(Of DateTime) = Nothing
                If dtpSearchTo.Checked Then
                    startTo = New DateTime(
                        dtpSearchTo.Value.Year,
                        dtpSearchTo.Value.Month,
                        dtpSearchTo.Value.Day,
                        dtpSearchTo.Value.Hour,
                        dtpSearchTo.Value.Minute,
                        0).AddMinutes(1).AddTicks(-1)
                End If

                If startFrom.HasValue AndAlso startTo.HasValue AndAlso startFrom.Value > startTo.Value Then
                    MessageBox.Show("Khoảng thời gian tìm kiếm không hợp lệ: 'Từ' phải nhỏ hơn hoặc bằng 'Đến'.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim performances As List(Of Performance) = _performanceRepository.GetPerformances(txtSearchName.Text, startFrom, startTo)
                dgvPerformances.DataSource = performances

                If performances.Count = 0 Then
                    _selectedPerformanceId = Nothing
                End If
            Catch ex As Exception
                ShowError(ex)
            End Try
        End Sub

        Private Function ValidateFormInputs() As Boolean
            If String.IsNullOrWhiteSpace(txtPlayName.Text) Then
                MessageBox.Show("Vui lòng nhập tên vở diễn.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtPlayName.Focus()
                Return False
            End If

            If numDuration.Value <= 0 Then
                MessageBox.Show("Thời lượng phải lớn hơn 0.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numDuration.Focus()
                Return False
            End If

            If numTicketPrice.Value <= 0 Then
                MessageBox.Show("Giá vé phải lớn hơn 0.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numTicketPrice.Focus()
                Return False
            End If

            Return True
        End Function

        Private Function ValidateNewPerformanceStartTime(startTime As DateTime) As Boolean
            Dim minAllowedStartTime As DateTime = DateTime.Now.AddHours(MinHoursBeforeStartForNewPerformance)
            If startTime < minAllowedStartTime Then
                MessageBox.Show(
                    $"Thời gian bắt đầu của suất diễn mới phải từ {minAllowedStartTime:dd/MM/yyyy HH:mm} trở đi.",
                    "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                dtpStartTime.Focus()
                Return False
            End If

            Return True
        End Function

        Private Function BuildFormPerformance() As Performance
            Return New Performance With {
                .Id = If(_selectedPerformanceId.HasValue, _selectedPerformanceId.Value, 0),
                .PlayName = txtPlayName.Text.Trim(),
                .StartTime = dtpStartTime.Value,
                .DurationMinutes = Convert.ToInt32(numDuration.Value),
                .TicketPrice = numTicketPrice.Value
            }
        End Function

        Private Function GetSelectedPerformance() As Performance
            If dgvPerformances.CurrentRow Is Nothing Then
                Return Nothing
            End If

            Return TryCast(dgvPerformances.CurrentRow.DataBoundItem, Performance)
        End Function

        Private Sub PopulateForm(performance As Performance)
            If performance Is Nothing Then
                Return
            End If

            _selectedPerformanceId = performance.Id
            txtPlayName.Text = performance.PlayName
            dtpStartTime.Value = performance.StartTime

            Dim durationValue As Decimal = Convert.ToDecimal(performance.DurationMinutes)
            If durationValue < numDuration.Minimum Then
                durationValue = numDuration.Minimum
            End If
            If durationValue > numDuration.Maximum Then
                durationValue = numDuration.Maximum
            End If
            numDuration.Value = durationValue

            Dim ticketPriceValue As Decimal = performance.TicketPrice
            If ticketPriceValue < numTicketPrice.Minimum Then
                ticketPriceValue = numTicketPrice.Minimum
            End If
            If ticketPriceValue > numTicketPrice.Maximum Then
                ticketPriceValue = numTicketPrice.Maximum
            End If
            numTicketPrice.Value = ticketPriceValue
        End Sub

        Private Sub ResetEditForm()
            _selectedPerformanceId = Nothing
            txtPlayName.Clear()
            dtpStartTime.Value = DateTime.Now.AddHours(MinHoursBeforeStartForNewPerformance)
            numDuration.Value = 120
            numTicketPrice.Value = 120000D
            dgvPerformances.ClearSelection()
            txtPlayName.Focus()
        End Sub

        Private Sub ShowError(ex As Exception)
            Dim message As String = ex.Message
            Dim current As Exception = ex
            While current IsNot Nothing
                Dim dbEx As PostgresException = TryCast(current, PostgresException)
                If dbEx IsNot Nothing Then
                    message = dbEx.MessageText
                    Exit While
                End If

                current = current.InnerException
            End While

            If message = ex.Message AndAlso ex.InnerException IsNot Nothing Then
                message = ex.InnerException.Message
            End If

            MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Sub

        Private Sub btnSearch_Click(sender As Object, e As EventArgs)
            LoadPerformances()
        End Sub

        Private Sub btnClearSearch_Click(sender As Object, e As EventArgs)
            txtSearchName.Clear()
            dtpSearchFrom.Checked = False
            dtpSearchTo.Checked = False
            LoadPerformances()
        End Sub

        Private Sub btnAdd_Click(sender As Object, e As EventArgs)
            If Not ValidateFormInputs() Then
                Return
            End If

            Dim confirmResult As DialogResult = MessageBox.Show(
                "Lưu suất diễn mới?",
                "Xác nhận lưu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmResult <> DialogResult.Yes Then
                Return
            End If

            Try
                Dim model As Performance = BuildFormPerformance()
                If Not ValidateNewPerformanceStartTime(model.StartTime) Then
                    Return
                End If

                Dim newId As Integer = _performanceRepository.InsertPerformance(model)
                MessageBox.Show($"Đã thêm suất diễn mới (ID: {newId}).", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
                dtpSearchFrom.Checked = False
                dtpSearchTo.Checked = False
                LoadPerformances()
                ResetEditForm()
            Catch ex As Exception
                ShowError(ex)
            End Try
        End Sub

        Private Sub btnUpdate_Click(sender As Object, e As EventArgs)
            If Not _selectedPerformanceId.HasValue Then
                MessageBox.Show("Vui lòng chọn một suất diễn để cập nhật.", "Chưa chọn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If Not ValidateFormInputs() Then
                Return
            End If

            Dim confirmResult As DialogResult = MessageBox.Show(
                $"Lưu thay đổi cho suất diễn ID {_selectedPerformanceId.Value}?",
                "Xác nhận lưu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmResult <> DialogResult.Yes Then
                Return
            End If

            Try
                Dim model As Performance = BuildFormPerformance()
                _performanceRepository.UpdatePerformance(model)
                MessageBox.Show("Đã cập nhật suất diễn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
                LoadPerformances()
            Catch ex As Exception
                ShowError(ex)
            End Try
        End Sub

        Private Sub btnDelete_Click(sender As Object, e As EventArgs)
            If Not _selectedPerformanceId.HasValue Then
                MessageBox.Show("Vui lòng chọn một suất diễn để xóa.", "Chưa chọn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim confirmResult As DialogResult = MessageBox.Show(
                $"Bạn có chắc muốn xóa suất diễn ID {_selectedPerformanceId.Value}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If confirmResult <> DialogResult.Yes Then
                Return
            End If

            Try
                _performanceRepository.DeletePerformance(_selectedPerformanceId.Value)
                MessageBox.Show("Đã xóa suất diễn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
                LoadPerformances()
                ResetEditForm()
            Catch ex As Exception
                ShowError(ex)
            End Try
        End Sub

        Private Sub btnClearForm_Click(sender As Object, e As EventArgs)
            ResetEditForm()
        End Sub

        Private Sub btnBack_Click(sender As Object, e As EventArgs)
            Close()
        End Sub

        Private Sub dgvPerformances_SelectionChanged(sender As Object, e As EventArgs)
            Dim selected As Performance = GetSelectedPerformance()
            If selected Is Nothing Then
                Return
            End If

            PopulateForm(selected)
        End Sub
    End Class
End Namespace

