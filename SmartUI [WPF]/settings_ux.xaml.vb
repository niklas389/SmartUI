Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

Public Class wnd_settings
#Region "Blur"
    'Blur
    <DllImport("user32.dll")>
    Friend Shared Function SetWindowCompositionAttribute(hwnd As IntPtr, ByRef data As WindowCompositionAttributeData) As Integer
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure WindowCompositionAttributeData
        Public Attribute As WindowCompositionAttribute
        Public Data As IntPtr
        Public SizeOfData As Integer
    End Structure

    Friend Enum WindowCompositionAttribute
        ' ...
        WCA_ACCENT_POLICY = 19
        ' ...
    End Enum

    Friend Enum AccentState
        ACCENT_DISABLED = 0
        ACCENT_ENABLE_GRADIENT = 1
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2
        ACCENT_ENABLE_BLURBEHIND = 3
        ACCENT_INVALID_STATE = 4
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure AccentPolicy
        Public AccentState As AccentState
        Public AccentFlags As Integer
        Public GradientColor As Integer
        Public AnimationId As Integer
    End Structure

    Friend Sub EnableBlur()
        Dim windowHelper = New WindowInteropHelper(Me)

        Dim accent = New AccentPolicy()
        Dim accentStructSize = Marshal.SizeOf(accent)
        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND

        Dim accentPtr = Marshal.AllocHGlobal(accentStructSize)
        Marshal.StructureToPtr(accent, accentPtr, False)

        Dim data = New WindowCompositionAttributeData() With {
            .Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            .SizeOfData = accentStructSize,
            .Data = accentPtr
        }

        SetWindowCompositionAttribute(windowHelper.Handle, data)

        Marshal.FreeHGlobal(accentPtr)
    End Sub
#End Region

#Region "Window CMD"
    'Move Window
    Private Sub lbl_header_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_MouseLeftButtonDown(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        Me.Hide()
    End Sub

    Private Sub btn_wnd_minimize_MouseLeftButtonUp(sender As Object, e As RoutedEventArgs) Handles btn_wnd_minimize.Click
        Me.WindowState = WindowState.Minimized
    End Sub

    Private Sub wnd_settings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        EnableBlur() 'enable blurred window bg
        lib_hVOSD.Init() 'bring up lib_HVOSD
        Me.Hide()

        matc_tabctrl.Visibility = Visibility.Visible
        matc_main.Visibility = Visibility.Hidden
        matc_weather.Visibility = Visibility.Hidden
        matc_changelog.Visibility = Visibility.Hidden
        matc_spotify.Visibility = Visibility.Hidden
        flyout_cache_reset.IsOpen = False
        flyout_settings_saved.IsOpen = False
        pring_flyout_settings_saved.Visibility = Visibility.Hidden

        'Spotify Check
        If IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Spotify\Spotify.exe") Then
            lbl_spotifyCheck.Content = "Spotify installiert (v" & MainWindow._sAPI_ClientVersion & ") und " & If(Process.GetProcessesByName("Spotify").Length > 1, "gestartet", "nicht gestartet (!)")
        Else
            lbl_spotifyCheck.Content = "Spotify ist nicht Installiert."
        End If

        If IO.File.Exists("wcom_allowed") Then
            lbl_wcom_check.Content = "( ! ) wetter.com wird zum abrufen der aktuellen Wetterlage genutzt."
        Else
            lbl_wcom_check.Visibility = Visibility.Hidden
        End If

        lbl_cpr.Content = "Version: " & My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor & " - " & IO.File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory & "\SmartUI.exe").ToString("yyMMdd") & " - by Niklas Wagner"

        load_settings()
        changelog_load()
    End Sub

    Public Shared net_NIC_list As String = ";"

    Private Sub wnd_settings_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub

        ComboBox_net_interface.Items.Clear()
        ComboBox_net_interface.Items.Add("Deaktiviert")

        For Each str As String In net_NIC_list.Split(CType(";", Char()))
            If str = "" Then Exit For
            ComboBox_net_interface.Items.Add(str)
        Next

        'Network
        If ini.ReadValue("NET", "ComboBox_net_interface", "") = "null" Then
            ComboBox_net_interface.SelectedIndex = 0
        Else
            ComboBox_net_interface.SelectedItem = ini.ReadValue("NET", "ComboBox_net_interface", "")
        End If

        matc_tabctrl.SelectedIndex = 0
        lbl_header.Content = "EINSTELLUNGEN"
        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing

        cache_getSize()
    End Sub
#End Region

#Region "Read/Save Settings"
    Dim ini As New ini_file

    Private Sub btn_settings_save_Click(sender As Object, e As RoutedEventArgs) Handles btn_settings_save.Click
        ini.WriteValue("UI", "cb_wndmain_clock_enabled", CType(cb_wndmain_clock_enabled.IsChecked, String))
        ini.WriteValue("UI", "cb_wndmain_clock_seconds", CType(cb_wndmain_clock_seconds.IsChecked, String))
        ini.WriteValue("UI", "cb_wndmain_clock_weekday", CType(cb_wndmain_clock_weekday.IsChecked, String))

        'Spotify
        ini.WriteValue("Spotify", "cb_wndmain_spotify_progress", CType(cb_wndmain_spotify_progress.IsChecked, String))

        'Weather
        ini.WriteValue("Weather", "cb_wndmain_weather_enabled", CType(cb_wndmain_weather_enabled.IsChecked, String))
        If Not txtBx_weather_zipcode.Text = "<City ID>" Then ini.WriteValue("Weather", "txtBx_weather_zipcode", txtBx_weather_zipcode.Text)
        If Not txtBx_weather_APIkey.Text = "<API Key>" Then ini.WriteValue("Weather", "txtBx_weather_APIkey", txtBx_weather_APIkey.Text)

        'Network
        If Not ComboBox_net_interface.SelectedIndex = -1 Then
            If Not ComboBox_net_interface.SelectedItem.ToString = "Deaktiviert" Then
                ini.WriteValue("NET", "ComboBox_net_interface", ComboBox_net_interface.SelectedItem.ToString)
            Else
                ini.WriteValue("NET", "ComboBox_net_interface", "null")
            End If
        End If

        ini.WriteValue("UI", "cb_wndmain_net_iconDisableSpeedLimit", CType(cb_wndmain_net_iconDisableSpeedLimit.IsChecked, String))

        'others
        ini.WriteValue("SYS", "cb_other_disableVolumeOSD", CType(cb_other_disableVolumeOSD.IsChecked, String))
        ini.WriteValue("SYS", "cb_other_startup_play", CType(cb_other_startup_play.IsChecked, String))


        'Show flyout after saving settings
        grd_bottom_strip.Visibility = Visibility.Hidden
        lbl_flyout_settings_text.Content = "Ihre Änderungen wurden gespeichert!"
        flyout_settings_saved.IsAutoCloseEnabled = True
        flyout_settings_saved.IsOpen = True

        MainWindow.settings_update_needed = True
    End Sub

    'Hide settings flyout
    Private Sub flyout_settings_saved_ClosingFinished(sender As Object, e As RoutedEventArgs) Handles flyout_settings_saved.ClosingFinished
        If flyout_settings_saved.IsAutoCloseEnabled = False Then Exit Sub

        grd_bottom_strip.Visibility = Visibility.Visible

        'GoTo Main Page and Make Btn visible again
        If matc_tabctrl.SelectedIndex = 1 Then
            matc_tabctrl.SelectedIndex = 0
            btn_ovlay_setKey.Visibility = Visibility.Visible

            If IO.File.Exists(".\config\wcom_allowed") Then lbl_wcom_check.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub load_settings()
        cb_wndmain_clock_enabled.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_clock_enabled", "True"), Boolean)
        cb_wndmain_clock_seconds.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_clock_seconds", "False"), Boolean)
        cb_wndmain_clock_weekday.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_clock_weekday", "True"), Boolean)

        'Spotify
        cb_wndmain_spotify_progress.IsChecked = CType(ini.ReadValue("Spotify", "cb_wndmain_spotify_progress", "False"), Boolean)

        'Weather
        cb_wndmain_weather_enabled.IsChecked = CType(ini.ReadValue("Weather", "cb_wndmain_weather_enabled", "False"), Boolean)

        If ini.ReadValue("Weather", "txtBx_weather_zipcode", "0") = "0" Then
            txtBx_weather_zipcode.Text = "<City ID>"
        Else
            txtBx_weather_zipcode.Text = ini.ReadValue("Weather", "txtBx_weather_zipcode", "0")
        End If

        If ini.ReadValue("Weather", "txtBx_weather_APIkey", "0") = "0" Then
            txtBx_weather_APIkey.Text = "<API Key>"
        Else
            txtBx_weather_APIkey.Text = ini.ReadValue("Weather", "txtBx_weather_APIkey", "0")
        End If

        'Network
        If ini.ReadValue("NET", "ComboBox_net_interface", "") = "null" Then
            ComboBox_net_interface.SelectedIndex = 0
        Else
            ComboBox_net_interface.SelectedItem = ini.ReadValue("NET", "ComboBox_net_interface", "")
        End If

        cb_wndmain_net_iconDisableSpeedLimit.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_net_iconDisableSpeedLimit", "False"), Boolean)
        cb_wndmain_net_textDisableSpeedLimit.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_net_textDisableSpeedLimit", "False"), Boolean)

        'Others
        cb_other_disableVolumeOSD.IsChecked = CType(ini.ReadValue("SYS", "cb_other_disableVolumeOSD", "False"), Boolean)
        cb_other_startup_play.IsChecked = CType(ini.ReadValue("SYS", "cb_other_startup_play", "False"), Boolean)

        'Others End
    End Sub

    Private Sub cb_wndmain_net_iconDisableSpeedLimit_Unchecked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_net_iconDisableSpeedLimit.Unchecked
        cb_wndmain_net_textDisableSpeedLimit.IsChecked = False
        cb_wndmain_net_textDisableSpeedLimit.IsEnabled = False
    End Sub

    Private Sub cb_wndmain_net_iconDisableSpeedLimit_checked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_net_iconDisableSpeedLimit.Checked
        cb_wndmain_net_textDisableSpeedLimit.IsEnabled = True
    End Sub

    Private Sub wnd_settings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ini.WriteValue("app", "firstrun", CType(My.Application.Info.Version.ToString, String))
    End Sub

    Dim lib_hVOSD As New HideVolumeOSD.HideVolumeOSDLib

    Private Sub cb_other_disableVolumeOSD_checked(sender As Object, e As RoutedEventArgs) Handles cb_other_disableVolumeOSD.Checked, cb_other_disableVolumeOSD.Unchecked
        If cb_other_disableVolumeOSD.IsChecked = True Then lib_hVOSD.HideOSD() Else lib_hVOSD.ShowOSD()
    End Sub

#End Region

    Private Sub btn_changelog_back_Click(sender As Object, e As RoutedEventArgs) Handles ico_backToStart.MouseLeftButtonDown
        matc_tabctrl.SelectedIndex = 0
        lbl_header.Content = "EINSTELLUNGEN"
    End Sub

    Private Sub flyout_settings_saved_closed(sender As Object, e As RoutedEventArgs) Handles flyout_settings_saved.ClosingFinished
        pring_flyout_settings_saved.Visibility = Visibility.Hidden
    End Sub

#Region "Weather & API-Key Overlay"
    Private Sub cb_wndmain_weather_enabled_Checked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_weather_enabled.Click
        If txtBx_weather_zipcode.Text = "<City ID>" Or txtBx_weather_APIkey.Text = "<API Key>" Then
            cb_wndmain_weather_enabled.IsChecked = False
            MessageBox.Show("Bitte geben sie die ID ihres Ortes und ihren OpenWeather API-Key ein, damit wir die Wetterdaten abrufen können." & vbCrLf &
                            "Falls sie keinen API-Key haben, können sie diesen KOSTENLOS bei OpenWeather erhalten, klicken sie dafür auf 'Einrichten > API-Key erhalten'", "OpenWeather | Einrichten", MessageBoxButton.OK, MessageBoxImage.Asterisk)
        End If

    End Sub

    Private Async Sub weather_api_test()
        lbl_wcom_check.Visibility = Visibility.Hidden
        btn_ovlay_setKey.Visibility = Visibility.Hidden

        flyout_settings_saved.IsAutoCloseEnabled = False
        flyout_settings_saved.IsOpen = True

        lbl_flyout_settings_text.Content = "Einen moment, bitte..."

        Dim oww_api_request As WebRequest = WebRequest.Create("http://api.openweathermap.org/data/2.5/weather?id=" & txtBx_weather_zipcode.Text & "&APPID=" & txtBx_weather_APIkey.Text & "&mode=xml&units=metric&lang=DE")
        Dim oww_api_respone As WebResponse
        Dim oww_api_txt As String = ""

        Try
            'server response
            oww_api_respone = Await oww_api_request.GetResponseAsync()
            Dim oww_api_dataStream As Stream = oww_api_respone.GetResponseStream()
            ' Open the stream using a StreamReader for easy access.
            Dim oww_api_reader As New StreamReader(oww_api_dataStream)
            oww_api_txt = oww_api_reader.ReadToEnd

            If oww_api_txt.Length > 1 And oww_api_txt.Contains("temp") Then
                lbl_flyout_settings_text.Content = "API-Test Erfolgreich!"
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                lbl_flyout_settings_text.Content = "Vergessen Sie nicht, ihre Änderungen zu Speichern!"
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                flyout_settings_saved.IsOpen = False
                matc_tabctrl.SelectedIndex = 0

            Else
                lbl_flyout_settings_text.Content = "Fehler beim API-Test (!)"
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                lbl_flyout_settings_text.Content = "API-Key & City-ID prüfen"
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                flyout_settings_saved.IsOpen = False
            End If

            lbl_wcom_check.Visibility = Visibility.Visible
            btn_ovlay_setKey.Visibility = Visibility.Visible

        Catch ex As WebException
            api_errmsg("API-Key & City-ID prüfen (PE)")
        End Try

    End Sub

    Private Async Sub api_errmsg(e_str As String)
        lbl_flyout_settings_text.Content = e_str
        Await Task.Run(Sub() Threading.Thread.Sleep(2500))
        flyout_settings_saved.IsOpen = False
        lbl_wcom_check.Visibility = Visibility.Visible
        btn_ovlay_setKey.Visibility = Visibility.Visible
    End Sub

    'API Overlay
    Private Sub btn_ovlay_setKey_Click(sender As Object, e As RoutedEventArgs) Handles btn_ovlay_setKey.Click
        weather_api_test()
    End Sub

    Private Sub btn_overlay_show_Click(sender As Object, e As RoutedEventArgs) Handles btn_overlay_show.Click
        matc_tabctrl.SelectedIndex = 1
        lbl_header.Content = "WETTER"
    End Sub

    Private Sub btn_oww_getKey_Click(sender As Object, e As RoutedEventArgs) Handles btn_oww_getKey.Click
        Process.Start("http://openweathermap.org/appid")
    End Sub

#End Region

#Region "Changelog & 3rd Party"
    Private Sub btn_changelog_Click(sender As Object, e As RoutedEventArgs) Handles btn_changelog.Click
        matc_tabctrl.SelectedIndex = 2
        lbl_header.Content = "CHANGELOG"
    End Sub

    Private Sub changelog_load()
        If IO.File.Exists(".\changelog") Then
            Dim docpath As String = ".\changelog"
            Dim range As System.Windows.Documents.TextRange
            Dim fStream As System.IO.FileStream
            If System.IO.File.Exists(docpath) Then
                Try
                    ' Open the document in the RichTextBox.
                    range = New System.Windows.Documents.TextRange(rtb_chLog.Document.ContentStart, rtb_chLog.Document.ContentEnd)
                    fStream = New System.IO.FileStream(docpath, System.IO.FileMode.Open)
                    range.Load(fStream, DataFormats.Text)
                    fStream.Close()

                Catch generatedExceptionName As System.Exception
                    'MessageBox.Show("Couldn't load Changelog.")
                    rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog nicht verfügbar")
                End Try
            End If
        Else
            rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog nicht verfügbar")
        End If
    End Sub


    '3rd PTY
    Private Sub btn_info_3rdpty_Click(sender As Object, e As RoutedEventArgs) Handles btn_info_3rdpty.Click
        Dim wnd_3rdparty As New thirdparty_ui
        Me.Visibility = Visibility.Hidden

        wnd_3rdparty.ShowDialog()

        If (wnd_3rdparty.DialogResult.HasValue And wnd_3rdparty.DialogResult.Value) Then
            Me.Visibility = Visibility.Visible
        End If
    End Sub
#End Region

#Region "Spotify"
    Private Sub btn_spotify_Click(sender As Object, e As RoutedEventArgs) Handles btn_spotify.Click
        matc_tabctrl.SelectedIndex = 3
        lbl_header.Content = "SPOTIFY"
    End Sub

    Private Sub matc_tabctrl_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles matc_tabctrl.SelectionChanged
        If Not matc_tabctrl.SelectedIndex = 0 Then
            ico_backToStart.Visibility = Visibility.Visible
        Else
            ico_backToStart.Visibility = Visibility.Hidden
        End If
    End Sub

    Private Sub btn_restart_spotify_Click(sender As Object, e As RoutedEventArgs) Handles btn_restart_spotify.Click
        flyout_spotify("force_restart")
    End Sub

    Private Sub flyout_spotify(ctnt As String)
        Select Case ctnt
            Case "del_cache"
                btn_flyout_spotify_confirm.Tag = "del_cache"
                lbl_flyout_spotify_msg.Content = "Alle Zwischengespeicherten Album-Cover werden entfernt, sodass" & vbCrLf & "diese erneut heruntergeladen werden müssen"
                btn_flyout_spotify_confirm.Content = "Löschen"

            Case "force_restart"
                btn_flyout_spotify_confirm.Tag = "force_restart"
                lbl_flyout_spotify_msg.Content = "Dies wird die Spotify App beenden und wieder starten"
                btn_flyout_spotify_confirm.Content = "Neu starten"

        End Select

        flyout_cache_reset.IsOpen = True
        Dim c_blur As New Effects.BlurEffect
        c_blur.Radius = 10
        matc_tabctrl.Effect = c_blur
    End Sub

#Region "AlbumArt Cache"
    Private Sub cache_getSize()
        Dim cache_size As Int64
        Dim files_count As Integer
        If IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory & "cache\media\") Then
            For Each file As String In System.IO.Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory & "cache\media\", "*.*", System.IO.SearchOption.AllDirectories)
                cache_size += My.Computer.FileSystem.GetFileInfo(file).Length
                files_count += 1
            Next

            If cache_size = 0 Then
                lbl_cacheSize.Content = "Keine Album-Cover zwischengespeichert"
            ElseIf cache_size < 1048576 Then
                lbl_cacheSize.Content = "Album-Cover zwischengespeichert: " & files_count & " (" & (cache_size / 1024).ToString("0") & "KB)"
            Else
                lbl_cacheSize.Content = "Album-Cover zwischengespeichert: " & files_count & " (" & (cache_size / 1048576).ToString("0.0") & "MB)"
            End If
        Else
            lbl_cacheSize.Content = "Keine Album-Cover zwischengespeichert"
        End If

    End Sub

    Private Sub btn_cache_refresh_Click(sender As Object, e As RoutedEventArgs) Handles btn_cache_refresh.Click
        cache_getSize()
    End Sub

    Private Sub btn_cache_reset_Click(sender As Object, e As RoutedEventArgs) Handles btn_cache_reset.Click
        flyout_spotify("del_cache")
    End Sub

    Private Sub btn_reset_cache_confirm_Click(sender As Object, e As RoutedEventArgs) Handles btn_flyout_spotify_confirm.Click
        If btn_flyout_spotify_confirm.Tag Is "del_cache" Then

            If IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory & "cache\media\") Then
                For Each file As String In System.IO.Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory & "cache\media\", "*.*", System.IO.SearchOption.AllDirectories)
                    Try
                        IO.File.Delete(file)
                    Catch ex As Exception
                    End Try
                Next
            End If
            cache_getSize()

        ElseIf btn_flyout_spotify_confirm.Tag Is "force_restart" Then
            flyout_settings_saved.IsOpen = True
            lbl_flyout_settings_text.Content = "Spotify wird neu-gestartet..."
            pring_flyout_settings_saved.Visibility = Visibility.Visible

            For Each prog As Process In Process.GetProcesses
                If prog.ProcessName = "Spotify" Or prog.ProcessName = "SpotifyWebHelper" Then
                    prog.Kill()
                End If
            Next

            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Spotify\Spotify.exe")
        End If

        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing
    End Sub

    Private Sub btn_reset_cache_cancel_Click(sender As Object, e As RoutedEventArgs) Handles btn_reset_cache_cancel.Click
        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing
    End Sub

    Private Sub cb_other_startup_play_Checked(sender As Object, e As RoutedEventArgs) Handles cb_other_startup_play.Checked
        My.Computer.Audio.Play(System.AppDomain.CurrentDomain.BaseDirectory & "\Resources\win_vis_beta_startup.wav")
    End Sub
#End Region

#End Region

End Class
