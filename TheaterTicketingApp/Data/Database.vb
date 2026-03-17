Option Strict On
Option Explicit On

Imports System
Imports Microsoft.EntityFrameworkCore
Imports Npgsql

Namespace TheaterTicketingApp.Data
    Public Module Database
        Private Const DefaultConnectionString As String = "Host=172.16.1.82;Port=5432;Database=theater_ticketing;Username=postgres;Password=postgres"
        Private ReadOnly SchemaLock As New Object()
        Private SchemaEnsured As Boolean = False

        Public Function GetConnectionString() As String
            Dim fromEnv As String = Environment.GetEnvironmentVariable("THEATER_DB_CONNECTION")
            If String.IsNullOrWhiteSpace(fromEnv) Then
                Return DefaultConnectionString
            End If

            Return fromEnv.Trim()
        End Function

        Public Function CreateDbContext() As TheaterDbContext
            EnsureSchema()

            Dim optionsBuilder As New DbContextOptionsBuilder(Of TheaterDbContext)()
            optionsBuilder.UseNpgsql(GetConnectionString())
            Return New TheaterDbContext(optionsBuilder.Options)
        End Function

        Private Sub EnsureSchema()
            SyncLock SchemaLock
                If SchemaEnsured Then
                    Return
                End If

                Using connection As New NpgsqlConnection(GetConnectionString())
                    connection.Open()

                    Const sql As String =
                        "ALTER TABLE performances ADD COLUMN IF NOT EXISTS ticket_price NUMERIC(12,2);" &
                        "UPDATE performances SET ticket_price = 120000 WHERE ticket_price IS NULL OR ticket_price <= 0;" &
                        "ALTER TABLE performances ALTER COLUMN ticket_price SET DEFAULT 120000;" &
                        "ALTER TABLE performances ALTER COLUMN ticket_price SET NOT NULL;" &
                        "ALTER TABLE performances ADD COLUMN IF NOT EXISTS status VARCHAR(20);" &
                        "UPDATE performances " &
                        "SET status = CASE " &
                        "    WHEN NOW() >= start_time + (duration_minutes * INTERVAL '1 minute') THEN 'ENDED' " &
                        "    WHEN NOW() >= start_time THEN 'IN_PROGRESS' " &
                        "    ELSE 'NOT_STARTED' " &
                        "END;" &
                        "ALTER TABLE performances ALTER COLUMN status SET DEFAULT 'NOT_STARTED';" &
                        "ALTER TABLE performances ALTER COLUMN status SET NOT NULL;" &
                        "DO $$ " &
                        "BEGIN " &
                        "    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_performances_ticket_price_positive') THEN " &
                        "        ALTER TABLE performances ADD CONSTRAINT ck_performances_ticket_price_positive CHECK (ticket_price > 0); " &
                        "    END IF; " &
                        "    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_performances_status_valid') THEN " &
                        "        ALTER TABLE performances ADD CONSTRAINT ck_performances_status_valid CHECK (status IN ('NOT_STARTED', 'IN_PROGRESS', 'ENDED')); " &
                        "    END IF; " &
                        "END $$;"

                    Using cmd As New NpgsqlCommand(sql, connection)
                        cmd.ExecuteNonQuery()
                    End Using
                End Using

                SchemaEnsured = True
            End SyncLock
        End Sub
    End Module
End Namespace
