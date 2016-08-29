Imports System.Globalization
<ValueConversion(GetType(Boolean), GetType(Boolean))>
Public Class AndBooleanMulitConverter
    Implements IMultiValueConverter

    Public Function Convert(values() As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert
        For Each obj As Object In values
            If obj.GetType() = GetType(Boolean) Then
                If Not CBool(obj) Then
                    Return False
                End If
            End If
        Next
        Return True
    End Function

    Public Function ConvertBack(value As Object, targetTypes() As Type, parameter As Object, culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
