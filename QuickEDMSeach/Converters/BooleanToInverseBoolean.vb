Imports System.Globalization
<ValueConversion(GetType(Boolean), GetType(Boolean))>
Public Class BooleanToInverseBoolean
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        If Not (TypeOf value Is Boolean) Then
            Return Nothing
        End If
        Return Not CBool(value)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        If Not (TypeOf value Is Boolean) Then
            Return Nothing
        End If
        Return Not CBool(value)
    End Function
End Class
