
Imports System.Windows
Imports System.ComponentModel
Imports EPDM.Interop.epdm
Imports System.Windows.Controls
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Threading
Imports System.Threading
Imports Microsoft.Win32

Public Class MainWindow
    Implements INotifyPropertyChanged
    Dim _SearchBoxText As String
    Dim _ShrinkWindow As Boolean
    Dim Vault8 As IEdmVault8 = New EdmVault5
    Dim Searcher As IEdmSearch7
    Dim VaultCol As EdmViewInfo()

    Dim _hotKeyQuickFocus As New HotKey(Key.Oem3, KeyModifier.Ctrl, AddressOf OnHotKeyQuickFocusHandler, False)

    Private Shared EmptyDelegate As Action
    Public Property OpenFilesFolder As Boolean
    Public Property Version As String = "Version :" & Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString
    Public Property DefaultWatermarkText As String
        Get
            Return My.Settings.DefaultSearchText
        End Get
        Set(value As String)
            My.Settings.DefaultSearchText = value
            My.Settings.Save()
        End Set
    End Property
    Private _WatermarkText As String = DefaultWatermarkText
    Private _ShowCmdPreview As Boolean = True

#Region "Commands"
    Public Shared ReadOnly SearchCommand As New RoutedUICommand("Search", "Search", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.Enter, ModifierKeys.None)})
    Public Shared ReadOnly OpenDwg As New RoutedUICommand("Open Drawing", "OpenDwg", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.Back, ModifierKeys.Shift)})
    Public Shared ReadOnly OpenModel As New RoutedUICommand("Open Model", "OpenModel", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.Enter, ModifierKeys.Shift)})
    Public Shared ReadOnly OpenFolder As New RoutedUICommand("Open Folder", "OpenFolder", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.Enter, ModifierKeys.Control)})
    Public Shared ReadOnly QuickFocus As New RoutedUICommand("Quick Focus", "QuickFocus", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.Oem3, ModifierKeys.Control)})

    Public Shared ReadOnly ClearTxtCommand As New RoutedUICommand("Clear Text", "Cleartext", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.X, ModifierKeys.Control)})
    Public Shared ReadOnly PasteCommand As New RoutedUICommand("Paste", "Paste", GetType(MainWindow), New InputGestureCollection() From {New KeyGesture(Key.V, ModifierKeys.Control)})
    Public Shared ReadOnly CloseCommand As New RoutedUICommand("Close", "Close", GetType(MainWindow))
    Public Shared ReadOnly ShowIconCommand As New RoutedUICommand("ShowIcon", "ShowIcon", GetType(MainWindow))



    Private Sub CommandBinding_Search_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        BeginSearch(False)
    End Sub
    Private Sub CommandBinding_OpenDwg_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        Dim PDFtmp = PDFEnabled
        Dim SldDrwtmp = SldDrwEnabled
        Dim Dwgtmp = DwgEnabled
        Dim Modeltmp = ModelEnabled


        PDFEnabled = False
        SldDrwEnabled = True
        DwgEnabled = True
        ModelEnabled = False

        SP_Open.IsOpen = False
        BeginSearch(False)

        PDFEnabled = PDFtmp
        SldDrwEnabled = SldDrwtmp
        DwgEnabled = Dwgtmp
        ModelEnabled = Modeltmp
    End Sub
    Private Sub CommandBinding_OpenFolder_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        BeginSearch(True)
    End Sub
    Private Sub CommandBinding_OpenModel_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        Dim PDFtmp = PDFEnabled
        Dim SldDrwtmp = SldDrwEnabled
        Dim Dwgtmp = DwgEnabled
        Dim Modeltmp = ModelEnabled

        PDFEnabled = False
        SldDrwEnabled = False
        DwgEnabled = True
        ModelEnabled = True

        SP_Open.IsOpen = False
        BeginSearch(False)

        PDFEnabled = PDFtmp
        SldDrwEnabled = SldDrwtmp
        DwgEnabled = Dwgtmp
        ModelEnabled = Modeltmp
    End Sub
    Private Sub CommandBinding_QuickFocus_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        OnHotKeyQuickFocusHandler(Nothing)
    End Sub
    Private Sub CommandBinding_ClearTxt_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        SearchBoxText = ""
    End Sub
    Private Sub CommandBinding_Paste_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        Try
            TB_SearchTermTextBox.Text = Strings.Left(SearchBoxText, TB_SearchTermTextBox.SelectionStart) & Clipboard.GetText & Strings.Mid(SearchBoxText, TB_SearchTermTextBox.SelectionStart, SearchBoxText.Length)
        Catch
        End Try
    End Sub
    Private Sub CommandBinding_Close_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        LeftPos = Me.Left
        TopPos = Me.Top
        My.Settings.Save()
        Me.Close()
    End Sub
    Private Sub CommandBinding_ShowIcon_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        Me.ShowInTaskBar = Not Me.ShowInTaskBar
    End Sub

    Private Sub CommandBinding_OnFocus_CanExecute(sender As Object, e As CanExecuteRoutedEventArgs)
        e.CanExecute = True
    End Sub
    Private Sub CommandBinding_AlwaysCanExecute(sender As Object, e As CanExecuteRoutedEventArgs)
        e.CanExecute = True
    End Sub
#End Region


#Region "Threads"
    Dim Timer As New System.Timers.Timer(2000)
    Dim BKWk As New BackgroundWorker
    Dim Thread As Thread


    Dim VaultName As String
    Dim Extensions As New List(Of String)
#End Region
    Sub New()
        InitializeComponent()
        Me.DataContext = Me
        AddHandler Timer.Elapsed, AddressOf Ticker

        Vault8.GetVaultViews(VaultCol, False)

        For Each item As EdmViewInfo In VaultCol
            Dim MI As New MenuItem With {.Header = item.mbsVaultName, .IsCheckable = True, .IsChecked = item.mbsVaultName = My.Settings.LastLoggedOn}
            AddHandler MI.Checked, AddressOf VaultCheckChange

            SP_Vaults.Children.Add(MI)

            If (item.mbsVaultName = My.Settings.LastLoggedOn) Then
                UpdateWaterMake4Vault(My.Settings.LastLoggedOn)
            End If
        Next

        CheckRunAtStartUp()
        Me.SizeToContent = Windows.SizeToContent.Width
        Me.Left = LeftPos
        Me.Top = TopPos
    End Sub

    Private Sub CheckRunAtStartUp()
        Const RegPath As String = "HKey_Current_User\Software\Microsoft\Windows\CurrentVersion\Run"
        Const Keyname As String = "QuickSearchEDM"
        If RunAtStartUp Then
            Dim Res As String = Registry.GetValue(RegPath, Keyname, "")
            Dim MyLoc As String = """" & Reflection.Assembly.GetExecutingAssembly.Location & """"
            If String.IsNullOrWhiteSpace(Res) Or Res <> MyLoc Then
                Registry.SetValue(RegPath, Keyname, MyLoc, RegistryValueKind.String)
            End If
        Else
            Registry.SetValue(RegPath, Keyname, "", RegistryValueKind.String)
        End If
    End Sub

    Public Sub BeginSearch(ByVal OpenFolder As Boolean)
        Try
            OpenFilesFolder = OpenFolder
            Try
                PG_Searcher.Style = Resources("PB_Searching")
                ControlGrid.Style = Resources("Grid_Transparent")
            Catch ex As Exception
            End Try
            Me.Dispatcher.Invoke(
            New Action(Sub()
                           PG_Searcher.Style = Resources("PB_Searching")
                           ControlGrid.Style = Resources("Grid_Transparent")
                       End Sub)
            )
            For Each item In SP_Vaults.Children
                If TryCast(item, MenuItem) IsNot Nothing Then
                    If TryCast(item, MenuItem).IsChecked Then
                        If Vault8.IsLoggedIn AndAlso Vault8.Name = TryCast(item, MenuItem).Header Then
                            VaultName = Vault8.Name
                        Else
                            VaultName = TryCast(item, MenuItem).Header
                        End If
                    End If
                End If
            Next
            Extensions.Clear()
            If PDFEnabled Then Extensions.Add(".pdf")
            If SldDrwEnabled Then Extensions.Add(".slddrw")
            If DwgEnabled Then Extensions.Add(".dwg")
            If ModelEnabled Then Extensions.Add(".sldasm")
            If ModelEnabled Then Extensions.Add(".sldprt")

            WatermarkText = "" & Trim(TB_Prefix.Text & TB_SearchTermTextBox.Text)
            Dim SearchText As String = TB_Prefix.Text & TB_SearchTermTextBox.Text & "%"

            Thread = New System.Threading.Thread(New ThreadStart(Sub()
                                                                     Search(SearchText, VaultName, Extensions, OpenFilesFolder)
                                                                 End Sub))
            Thread.Start()
            TB_SearchTermTextBox.Text = ""
            ' AddHandler BKWk.DoWork, AddressOf bw_DoWork



            '   BKWk.RunWorkerAsync({SearchText, VaultName, Extensions, OpenFilesFolder})
        Catch
            PG_Searcher.Style = Resources("PB_NotFound")
            ControlGrid.Style = Resources("Grid_Default")
        Finally
        End Try
    End Sub
    Public Sub Search(ByVal SearchText As String, ByVal VaultName As String, ByVal Extension As List(Of String), ByVal OpenFolder As Boolean)
        Try
            If Vault8.IsLoggedIn AndAlso Vault8.Name = VaultName Then
                Searcher.Clear()
            Else
                Vault8.LoginAuto(VaultName, 0)
                Searcher = Vault8.CreateSearch()
            End If
            If Vault8.IsLoggedIn Then
                With Searcher
                    .SetToken(EdmSearchToken.Edmstok_FindFiles, True)
                    .SetToken(EdmSearchToken.Edmstok_Name, SearchText)

                    Dim Res As New List(Of IEdmSearchResult5)

                    Dim Tem As Object = .GetFirstResult()
                    Do While Tem IsNot Nothing
                        Res.Add(Tem)
                        Tem = .GetNextResult
                    Loop
                    If Res.Count > 0 Then
                        Dim FoundItem As IEdmSearchResult5 = Nothing

                        For Each ext As String In Extension
                            For Each Item As IEdmSearchResult5 In Res
                                If Item.Name.ToUpper.Contains(ext.ToUpper) Then
                                    FoundItem = Item
                                    Exit For
                                End If
                            Next
                            If FoundItem IsNot Nothing Then Exit For
                        Next
                        If FoundItem IsNot Nothing Then
                            Try
                                If OpenFolder Then
                                    Process.Start("Explorer.exe", "/select, " & FoundItem.Path)
                                Else

                                    Dim ThisFile As IEdmFile8 = Vault8.GetFileFromPath(FoundItem.Path)
                                    ThisFile.GetFileCopy(0, Nothing, Nothing, EdmGetFlag.EdmGet_RefsVerLatest)
                                    If IO.File.Exists(FoundItem.Path) Then
                                        Me.Dispatcher.Invoke(New Action(Sub() PG_Searcher.Style = Resources("PB_Found")))
                                        Dim proc As Process = New Process()
                                        proc.StartInfo.FileName = FoundItem.Path
                                        proc.StartInfo.UseShellExecute = True
                                        proc.Start()
                                    Else
                                        Me.Dispatcher.Invoke(New Action(Sub() PG_Searcher.Style = Resources("PB_NotFound")))
                                    End If
                                End If
                            Catch ex As System.Exception
                                Me.Dispatcher.Invoke(New Action(Sub() PG_Searcher.Style = Resources("PB_NotFound")))
                            End Try
                        End If
                    Else
                        Me.Dispatcher.Invoke(New Action(Sub() PG_Searcher.Style = Resources("PB_NotFound")))
                    End If
                    Timer.Start()
                End With
            Else
                MsgBox("Can not log into vault")
            End If

        Catch
            Me.Dispatcher.Invoke(New Action(Sub()
                                                PG_Searcher.Style = Resources("PB_NotFound")
                                                ControlGrid.Style = Resources("Grid_Default")
                                            End Sub))

        End Try
    End Sub
    Private Sub bw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Search(e.Argument(0), e.Argument(1), e.Argument(2), e.Argument(3))
    End Sub
#Region "Events"
    Private Sub OnHotKeyQuickFocusHandler(hotKey As HotKey)
        ShrinkWindow = False
        Me.Activate()
        Me.Focus()
        TB_SearchTermTextBox.Focus()
    End Sub
    Private Sub OnHotKeyOpenDrawing(hotKey As HotKey)
        If Me.IsFocused Then
            CommandBinding_OpenDwg_Executed(Nothing, Nothing)
        End If
    End Sub

    Private Sub OnHotKeyOpenModel(hotKey As HotKey)
        If Me.IsFocused Then
            CommandBinding_OpenModel_Executed(Nothing, Nothing)
        End If
    End Sub
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub PropertyHasChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
    Private Sub Ticker(ByVal source As Object, ByVal e As System.Timers.ElapsedEventArgs)
        Me.Dispatcher.Invoke(
            New Action(Sub()
                           PG_Searcher.Style = Resources("PB_Default")
                           ControlGrid.Style = Resources("Grid_Default")
                       End Sub))
        Timer.Stop()
    End Sub
#End Region
#Region "From Event"
    Public Property PopupOwnsMouse As Boolean
    Private Sub Window_MouseLeftButtonDown(sender As Object, e As Input.MouseButtonEventArgs)
        Try
            Me.DragMove()
        Catch
        End Try
    End Sub
    Private Sub BTN_Pin_Click(sender As Object, e As RoutedEventArgs)
        ShrinkWindow = Not (ShrinkWindow)
    End Sub

    Private Sub MainForm_MouseEnter(sender As Object, e As MouseEventArgs)
        If ShrinkWindow Then
            ControlGrid.Visibility = Windows.Visibility.Visible
            Me.SizeToContent = Windows.SizeToContent.Width
            Me.Focus()
        End If
    End Sub
    Private Sub MainForm_MouseLeave(sender As Object, e As MouseEventArgs)
        If SearchPopUp.IsMouseOver Or SettingsPopup.IsMouseOver Then
            PopupOwnsMouse = True
            Exit Sub
        End If
        If ShrinkWindow Then
            ControlGrid.Visibility = Windows.Visibility.Collapsed
            Me.SizeToContent = Windows.SizeToContent.Width
        End If
        MainForm.Focus()
        Keyboard.ClearFocus()
    End Sub
    Private Sub MainForm_Deactivated(sender As Object, e As EventArgs)
        Me.Topmost = True
        MainForm.Focus()
    End Sub
    Private Sub MainForm_Closing(sender As Object, e As CancelEventArgs) Handles MainForm.Closing
        LeftPos = Me.Left
        TopPos = Me.Top
        My.Settings.Save()
    End Sub

    Private Sub TB_SearchTermTextBox_MouseLeave(sender As Object, e As MouseEventArgs) Handles TB_SearchTermTextBox.MouseLeave
        'TB_SearchTermTextBox.Focus()
        MainForm.Focus()

    End Sub

    Private Sub SearchPopUp_MouseLeave(sender As Object, e As MouseEventArgs)
        If MainForm.IsMouseOver Then
            PopupOwnsMouse = False
            Exit Sub
        End If
        If ShrinkWindow Then
            ControlGrid.Visibility = Windows.Visibility.Collapsed
            Me.SizeToContent = Windows.SizeToContent.Width
        End If
        MainForm.Focus()
        Keyboard.ClearFocus()
    End Sub
    Private Sub VaultCheckChange(sender As Object, e As RoutedEventArgs)
        Dim CheckedName As String = ""
        For Each Item As Object In SP_Vaults.Children
            Dim MI As MenuItem = TryCast(Item, MenuItem)
            If Item IsNot sender Then
                If MI IsNot Nothing Then
                    Try
                        MI.IsChecked = False
                    Catch
                    End Try
                End If
            End If
            If MI IsNot Nothing Then
                If MI.IsChecked Then
                    CheckedName = MI.Header
                End If
            End If
        Next
        UpdateWaterMake4Vault(CheckedName)
    End Sub
    Sub UpdateWaterMake4Vault(Vault As String)
        Dim NewPrefix As String
        Select Case Vault.ToUpper
            Case "EDM"
                NewPrefix = "OU60"
            Case "EDM_SEAP"
                NewPrefix = "OU65"
            Case Else
                NewPrefix = "OU65"
        End Select
        If Strings.Left(WatermarkText, 4) <> NewPrefix Then
            WatermarkText = NewPrefix & WatermarkText.Substring(4)
        End If
    End Sub
#End Region
#Region "Properties"
    Public Shadows Property ShowInTaskBar As Boolean
        Get
            Return My.Settings.ShowInTaskBar
        End Get
        Set(value As Boolean)
            My.Settings.ShowInTaskBar = value
            PropertyHasChanged("ShowInTaskBar")
            My.Settings.Save()
        End Set
    End Property
    Public Property SearchBoxText As String
        Get
            Return _SearchBoxText
        End Get
        Set(value As String)
            _SearchBoxText = value
            PropertyHasChanged("SearchBoxText")
            PropertyHasChanged("WatermarkText")
        End Set
    End Property
    Public Property ShrinkWindow As Boolean
        Get
            Return _ShrinkWindow
        End Get
        Set(value As Boolean)
            _ShrinkWindow = value
            If value Then
                ControlGrid.Visibility = Windows.Visibility.Collapsed
            Else
                ControlGrid.Visibility = Windows.Visibility.Visible
            End If
            Me.SizeToContent = Windows.SizeToContent.Width
            PropertyHasChanged("ShrinkWindow")
        End Set
    End Property
    Public Property WatermarkText As String
        Get
            Dim Value As String = _WatermarkText
            Dim TrimVal As Integer = Value.Length
            If Not String.IsNullOrWhiteSpace(SearchBoxText) Then
                TrimVal = TrimVal - (SearchBoxText.Length)
                Return Strings.Left(Value, TrimVal)
            End If
            Return Value
        End Get
        Private Set(value As String)
            _WatermarkText = value
            PropertyHasChanged("WatermarkText")
        End Set
    End Property
    Public Property PDFEnabled As Boolean
        Get
            Return My.Settings.PDFEnabled
        End Get
        Set(value As Boolean)
            My.Settings.PDFEnabled = value
            PropertyHasChanged("PDFEnabled")
        End Set
    End Property
    Public Property SldDrwEnabled As Boolean
        Get
            Return My.Settings.SldDrwEnabled
        End Get
        Set(value As Boolean)
            My.Settings.SldDrwEnabled = value
            PropertyHasChanged("SldDrwEnabled")
        End Set
    End Property
    Public Property DwgEnabled As Boolean
        Get
            Return My.Settings.DwgEnabled
        End Get
        Set(value As Boolean)
            My.Settings.DwgEnabled = value
            PropertyHasChanged("DwgEnabled")
        End Set
    End Property
    Public Property ModelEnabled As Boolean
        Get
            Return My.Settings.ModelEnabled
        End Get
        Set(value As Boolean)
            My.Settings.ModelEnabled = value
            PropertyHasChanged("ModelEnabled")
        End Set
    End Property
    Public Property TopPos As Double
        Get
            Return My.Settings.TopPos
        End Get
        Set(value As Double)
            My.Settings.TopPos = value
            PropertyHasChanged("TopPos")
        End Set
    End Property
    Public Property LeftPos As Double
        Get
            Return My.Settings.LefPos
        End Get
        Set(value As Double)
            My.Settings.LefPos = value
            PropertyHasChanged("LeftPos")
        End Set
    End Property
    Public Property FormTransparent As Boolean
        Get
            Return My.Settings.FormTransparent
        End Get
        Set(value As Boolean)
            My.Settings.FormTransparent = value
            PropertyHasChanged("FormTransparent")
        End Set
    End Property
    Public Property RunAtStartUp As Boolean
        Get
            Return My.Settings.RunAtStartUp
        End Get
        Set(value As Boolean)
            My.Settings.RunAtStartUp = value
            My.Settings.Save()
            CheckRunAtStartUp()
        End Set
    End Property
    Public Property ShowCmdPreview As Boolean
        Get
            Return My.Settings.ShowCmdPreview
        End Get
        Set(value As Boolean)
            My.Settings.ShowCmdPreview = value
            My.Settings.Save()
            PropertyHasChanged("ShowCmdPreview")
        End Set
    End Property
    Dim _showSettings As Boolean
    Public Property ShowSettings As Boolean
        Get
            Return _showSettings
        End Get
        Set(value As Boolean)
            _showSettings = value
            PropertyHasChanged("ShowSettings")
        End Set
    End Property



#End Region
End Class


