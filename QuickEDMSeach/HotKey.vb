
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Net.Mime
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows
Imports System.Windows.Input
Imports System.Windows.Interop


Public Class HotKey
    Implements IDisposable

    Private Shared _dictHotKeyToCalBackProc As Dictionary(Of Integer, HotKey)

    <DllImport("user32.dll")>
    Private Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInt32, vlc As UInt32) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function

    Public Const WmHotKey As Integer = &H312

    Private _disposed As Boolean = False

    Public Property Key() As Key
    Public Property KeyModifiers() As KeyModifier
    Public Property Action() As Action(Of HotKey)
    Public Property Id() As Integer
    Public Property HandleHotKey As Boolean


    Public Sub New(k As Key, keyModifiers__1 As KeyModifier, action__2 As Action(Of HotKey), Optional iHandleHotKey As Boolean = True, Optional register__3 As Boolean = True)
        Key = k
        KeyModifiers = keyModifiers__1
        Action = action__2
        If register__3 Then
            Register()
        End If
    End Sub

    Public Function Register() As Boolean
        Dim virtualKeyCode As Integer = KeyInterop.VirtualKeyFromKey(Key)
        Id = virtualKeyCode + (CInt(KeyModifiers) * &H10000)
        Dim result As Boolean = RegisterHotKey(IntPtr.Zero, Id, KeyModifiers, virtualKeyCode)

        If _dictHotKeyToCalBackProc Is Nothing Then
            _dictHotKeyToCalBackProc = New Dictionary(Of Integer, HotKey)()
            AddHandler ComponentDispatcher.ThreadFilterMessage, New ThreadMessageEventHandler(AddressOf ComponentDispatcherThreadFilterMessage)
        End If

        _dictHotKeyToCalBackProc.Add(Id, Me)

        ' Debug.Print(result.ToString() + ", " + Id + ", " + virtualKeyCode)
        Return result
    End Function

    Public Sub Unregister()
        Dim hotKey As HotKey = Nothing
        If _dictHotKeyToCalBackProc.TryGetValue(Id, hotKey) Then
            UnregisterHotKey(IntPtr.Zero, Id)
        End If
    End Sub
    Private Shared Sub ComponentDispatcherThreadFilterMessage(ByRef msg As MSG, ByRef handled As Boolean)
        If Not handled Then
            If msg.message = WmHotKey Then
                Dim hotKey As HotKey = Nothing

                If _dictHotKeyToCalBackProc.TryGetValue(CInt(msg.wParam), hotKey) Then
                    If hotKey.Action IsNot Nothing Then
                        hotKey.Action.Invoke(hotKey)
                    End If
                    If hotKey.HandleHotKey Then
                        handled = True
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        ' This object will be cleaned up by the Dispose method.
        ' Therefore, you should call GC.SupressFinalize to
        ' take this object off the finalization queue
        ' and prevent finalization code for this object
        ' from executing a second time.
        GC.SuppressFinalize(Me)
    End Sub
    Protected Overridable Sub Dispose(disposing As Boolean)
        ' Check to see if Dispose has already been called.
        If Not Me._disposed Then
            ' If disposing equals true, dispose all managed
            ' and unmanaged resources.s
            If disposing Then
                ' Dispose managed resources.
                Unregister()
            End If

            ' Note disposing has been done.
            _disposed = True
        End If
    End Sub
End Class

' ******************************************************************
<Flags> _
Public Enum KeyModifier
    None = &H0
    Alt = &H1
    Ctrl = &H2
    NoRepeat = &H4000
    Shift = &H4
    Win = &H8
End Enum
