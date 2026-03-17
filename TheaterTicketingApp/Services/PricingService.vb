Option Strict On
Option Explicit On

Namespace TheaterTicketingApp.Services
    Public Module PricingService
        Public Const SingleSeatUnitPrice As Decimal = 120000D

        Public Function GetUnitPrice() As Decimal
            Return SingleSeatUnitPrice
        End Function
    End Module
End Namespace
