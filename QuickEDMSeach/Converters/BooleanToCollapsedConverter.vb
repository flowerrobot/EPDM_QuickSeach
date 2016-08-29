Imports System.Globalization
<ValueConversion(GetType(Boolean), GetType(Visibility))>
Public Class BooleanToCollapsedConverter
    Implements IValueConverter
    Public Property TrueValue() As Visibility
        Get
            Return m_TrueValue
        End Get
        Set
            m_TrueValue = Value
        End Set
    End Property
    Private m_TrueValue As Visibility
    Public Property FalseValue() As Visibility
        Get
            Return m_FalseValue
        End Get
        Set
            m_FalseValue = Value
        End Set
    End Property
    Private m_FalseValue As Visibility

    Public Sub New()
        ' set defaults
        TrueValue = Visibility.Collapsed
        FalseValue = Visibility.Visible
    End Sub

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        If Not (TypeOf value Is Boolean) Then
            Return Nothing
        End If
        Return If(CBool(value), TrueValue, FalseValue)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        If Equals(value, TrueValue) Then
            Return True
        End If
        If Equals(value, FalseValue) Then
            Return False
        End If
        Return Nothing
    End Function
End Class