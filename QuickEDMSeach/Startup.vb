Imports System.Runtime.InteropServices

Module Startup
    Const LogFile As String = "\\syddtcd2\FlowerRobot Programs\EDM Quick Search\Log\Installed.il"
    Public Sub Main()
        Try
            ShowWindow(GetConsoleWindow(), SW_HIDE)
            Try
                If IO.File.Exists(LogFile) Then
                    Using File As IO.StreamWriter = IO.File.AppendText(LogFile)
                        File.WriteLine("User : " & System.Environment.UserName.ToUpper & "     - Version :" & Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString)
                    End Using
                End If
            Catch
            End Try
            Dim Win As New MainWindow
            Win.ShowDialog()
        Catch ex As Exception
            If ex.Message.Contains("Retrieving the COM") Then
                MsgBox("It does not look like EPDM is installed")
            Else
                MsgBox("Can not start program." & vbCrLf & ex.Message & vbCrLf & vbCrLf & ex.StackTrace)
            End If
        End Try
    End Sub
    <DllImport("kernel32.dll")>
    Private Function GetConsoleWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
    End Function

    Const SW_HIDE As Integer = 0
    Const SW_SHOW As Integer = 5
End Module
