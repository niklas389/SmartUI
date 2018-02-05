﻿Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports nUpdate.Updating

Public Class wnd_settings
    Dim updater_enabled As Boolean = False

    Public Shared ui_blur_enabled As Boolean

#Region "Window CMD"
    'Move Window
    Private Sub lbl_header_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_MouseLeftButtonDown(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        Me.Visibility = Visibility.Hidden
        lasttab_lic = False
    End Sub

    Private Sub btn_wnd_minimize_MouseLeftButtonUp(sender As Object, e As RoutedEventArgs) Handles btn_wnd_minimize.Click
        Me.WindowState = WindowState.Minimized
    End Sub

    Private Sub wnd_settings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        lib_hVOSD.Init() 'bring up lib_HVOSD
        Me.Hide()

        matc_tabctrl.Visibility = Visibility.Visible
        matc_main.Visibility = Visibility.Hidden
        matc_weather.Visibility = Visibility.Hidden
        matc_changelog.Visibility = Visibility.Hidden
        matc_spotify.Visibility = Visibility.Hidden
        matc_updates.Visibility = Visibility.Hidden
        matc_3rdpty.Visibility = Visibility.Hidden

        flyout_cache_reset.IsOpen = False
        flyout_message.IsOpen = False
        pring_flyout_message.Visibility = Visibility.Hidden

        lbl_cpr.Content = "Version: " & MainWindow.suiversion & " - " & IO.File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory & "\SmartUI.exe").ToString("yyMMdd")

        load_settings()
        get_3rdptysw_versions()
    End Sub

    Public Shared net_NIC_list As String = ";"

    Private Sub wnd_settings_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Or lasttab_lic = True Then Exit Sub

        matc_tabctrl.SelectedIndex = 0

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
            If Not ComboBox_net_interface.SelectedItem Is ini.ReadValue("NET", "ComboBox_net_interface", "") Then ComboBox_net_interface.SelectedIndex = 1
        End If

        flyout_cache_reset.IsOpen = False
        matc_tabctrl.Effect = Nothing

        lbl_header.Content = "EINSTELLUNGEN"
    End Sub
#End Region

#Region "Read/Save Settings"
    Dim ini As New ini_file

    Private Sub btn_settings_save_Click(sender As Object, e As RoutedEventArgs) Handles btn_settings_save.Click
        ini.WriteValue("UI", "cb_wndmain_clock_enabled", CType(cb_wndmain_clock_enabled.IsChecked, String))
        ini.WriteValue("UI", "cb_wndmain_clock_seconds", CType(cb_wndmain_clock_seconds.IsChecked, String))
        ini.WriteValue("UI", "cb_wndmain_clock_weekday", CType(cb_wndmain_clock_weekday.IsChecked, String))
        'Window Blur
        ini.WriteValue("UI", "cb_wndmain_blur_enabled", CType(cb_wndmain_blur_enabled.IsChecked, String))

        'Spotify
        ini.WriteValue("Spotify", "cb_wndmain_spotify_progress", CType(cb_wndmain_spotify_progress.IsChecked, String))

        'Weather
        cls_weather.enabled(cb_wndmain_weather_enabled.IsChecked.Value)

        If Not txtBx_weather_cid.Text = "<City ID>" And Not txtBx_weather_APIkey.Text = "<API Key>" Then _
            cls_weather.oww_set_data(CInt(txtBx_weather_cid.Text), txtBx_weather_APIkey.Text)

        'Network
        If Not ComboBox_net_interface.SelectedIndex = -1 And Not ComboBox_net_interface.SelectedIndex = 0 Then
            ini.WriteValue("NET", "ComboBox_net_interface", ComboBox_net_interface.SelectedItem.ToString)
        Else
            ini.WriteValue("NET", "ComboBox_net_interface", "null")
        End If

        ini.WriteValue("UI", "cb_wndmain_net_iconDisableSpeedLimit", CType(cb_wndmain_net_iconDisableSpeedLimit.IsChecked, String))

        'others
        ini.WriteValue("SYS", "cb_other_disableVolumeOSD", CType(cb_other_disableVolumeOSD.IsChecked, String))
        ini.WriteValue("SYS", "cb_other_startup_play", CType(cb_other_startup_play.IsChecked, String))


        'Show flyout after saving settings
        grd_bottom_strip.Visibility = Visibility.Hidden

        show_flyout("Änderungen gespeichert!", False)

        MainWindow.settings_update_needed = True
        cls_blur_behind.blur(Me, ui_blur_enabled)
    End Sub

    Private Sub load_settings()
        cb_wndmain_clock_enabled.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_clock_enabled", "True"), Boolean)
        cb_wndmain_clock_seconds.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_clock_seconds", "False"), Boolean)
        cb_wndmain_clock_weekday.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_clock_weekday", "True"), Boolean)

        'Window Blur
        cb_wndmain_blur_enabled.IsChecked = CType(ini.ReadValue("UI", "cb_wndmain_blur_enabled", "False"), Boolean)
        cls_blur_behind.blur(Me, ui_blur_enabled)

        'Spotify
        cb_wndmain_spotify_progress.IsChecked = CType(ini.ReadValue("Spotify", "cb_wndmain_spotify_progress", "False"), Boolean)

        'Weather
        cb_wndmain_weather_enabled.IsChecked = cls_weather.conf_enabled

        txtBx_weather_cid.Text = cls_weather.oww_API_cityID.ToString
        txtBx_weather_APIkey.Text = cls_weather.oww_API_key

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

    Private Sub cb_wndmain_blur_enabled_Checked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_blur_enabled.Checked, cb_wndmain_blur_enabled.Unchecked
        ui_blur_enabled = cb_wndmain_blur_enabled.IsChecked.Value
    End Sub

#End Region

#Region "Tab Navigation"
    Private Sub btn_changelog_back_Click(sender As Object, e As RoutedEventArgs) Handles ico_backToStart.MouseLeftButtonDown
        matc_tabctrl.SelectedIndex = 0
        lbl_header.Content = "EINSTELLUNGEN"
        lasttab_lic = False
    End Sub

    Private Sub matc_tabctrl_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles matc_tabctrl.SelectionChanged
        If Not matc_tabctrl.SelectedIndex = 0 Then
            ico_backToStart.Visibility = Visibility.Visible
        Else
            ico_backToStart.Visibility = Visibility.Hidden
        End If
    End Sub
#End Region

#Region "Weather & API-Key Overlay"
    Private Sub cb_wndmain_weather_enabled_Checked(sender As Object, e As RoutedEventArgs) Handles cb_wndmain_weather_enabled.Click
        If txtBx_weather_cid.Text = "<City ID>" Or txtBx_weather_APIkey.Text = "<API Key>" Then
            cb_wndmain_weather_enabled.IsChecked = False
            MessageBox.Show("Bitte geben sie die ID ihres Ortes und ihren OpenWeather API-Key ein, damit wir die Wetterdaten abrufen können." & NewLine &
                            "Falls sie keinen API-Key haben, können sie diesen KOSTENLOS bei OpenWeather erhalten, klicken sie dafür auf 'Einrichten > API-Key erhalten'", "OpenWeather | Einrichten", MessageBoxButton.OK, MessageBoxImage.Asterisk)
        End If

    End Sub

    Private Async Sub weather_api_test()
        lbl_wcom_check.Visibility = Visibility.Hidden
        btn_ovlay_setKey.Visibility = Visibility.Hidden

        show_flyout("Einen moment, bitte...", True, 0)

        Dim oww_api_request As WebRequest = WebRequest.Create("http://api.openweathermap.org/data/2.5/weather?id=" & txtBx_weather_cid.Text & "&APPID=" & txtBx_weather_APIkey.Text & "&mode=xml&units=metric&lang=DE")
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
                show_flyout("API-Test Erfolgreich!", False, 0)
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                show_flyout("Vergessen Sie nicht, ihre Änderungen zu Speichern!", False)
                matc_tabctrl.SelectedIndex = 0
            Else
                show_flyout("Fehler beim API-Test (!)", False, 0)
                Await Task.Run(Sub() Threading.Thread.Sleep(2500))
                show_flyout("API-Key & City-ID prüfen", False)
            End If

            lbl_wcom_check.Visibility = Visibility.Visible
            btn_ovlay_setKey.Visibility = Visibility.Visible

        Catch ex As WebException
            api_errmsg("API-Key & City-ID prüfen (PE)")
        End Try

    End Sub

    Private Sub api_errmsg(e_str As String)
        show_flyout(e_str, False)
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

        If cls_weather.conf_wcom_enabled = True Then
            lbl_wcom_check.Content = "( ! ) wetter.com wird zum abrufen der aktuellen Wetterlage genutzt."
        Else
            lbl_wcom_check.Visibility = Visibility.Hidden
        End If
    End Sub

    Private Sub btn_oww_getKey_Click(sender As Object, e As RoutedEventArgs) Handles btn_oww_getKey.Click
        Process.Start("http://openweathermap.org/appid")
    End Sub

#End Region

#Region "Changelog & 3rd Party"
    Dim chglog_loaded As Integer = 0
    Dim lasttab_lic As Boolean = False

    Private Sub btn_changelog_Click(sender As Object, e As RoutedEventArgs) Handles btn_changelog.Click
        matc_tabctrl.SelectedIndex = 2
        lbl_header.Content = "CHANGELOG"

        If chglog_loaded <> 1 Then changelog_load()
    End Sub

    Private Sub changelog_load()
        If File.Exists(".\changelog") Then
            Dim docpath As String = ".\changelog"
            Dim range As System.Windows.Documents.TextRange
            Dim fStream As System.IO.FileStream
            If System.IO.File.Exists(docpath) Then
                Try
                    'Open the document in the RichTextBox.
                    range = New System.Windows.Documents.TextRange(rtb_chLog.Document.ContentStart, rtb_chLog.Document.ContentEnd)
                    fStream = New System.IO.FileStream(docpath, IO.FileMode.Open)
                    range.Load(fStream, DataFormats.Text)
                    fStream.Close()
                    chglog_loaded = 1

                Catch generatedExceptionName As System.Exception
                    show_flyout("Couldn't load Changelog", False)
                    If chglog_loaded = -1 Then Exit Sub

                    rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog konnte nicht geladen werden")
                    chglog_loaded = -1
                End Try
            End If
        Else
            show_flyout("Couldn't load Changelog", False)
            If chglog_loaded = -1 Then Exit Sub

            rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog nicht verfügbar")
            chglog_loaded = -1
        End If
    End Sub

    '3rd Party Software
    Private Sub btn_info_3rdpty_Click(sender As Object, e As RoutedEventArgs) Handles btn_info_3rdpty.Click
        matc_tabctrl.SelectedIndex = 5
        lbl_header.Content = "DRITTANBIETER SOFTWARE"
        lasttab_lic = True
    End Sub

    Private Sub scrlvwr_3rdpty_ScrollChanged(sender As Object, e As ScrollChangedEventArgs) Handles scrlvwr_3rdpty.ScrollChanged
        'lbl_test.Content = e.VerticalOffset
        If e.VerticalOffset < 310 Then grd_3rdpty_shadow.Visibility = Visibility.Visible Else grd_3rdpty_shadow.Visibility = Visibility.Hidden
    End Sub

#Region "Credits"
    Dim licvwr As New wnd_licensevwr

    Private Sub lbl_coreaudio_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_coreaudio_url.MouseLeftButtonDown
        Process.Start("http://whenimbored.xfx.net/download-links/?did=5")
    End Sub

    Private Sub lbl_mahapps_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_mahapps_url.MouseLeftButtonDown
        Process.Start("http://mahapps.com")
    End Sub

    Private Sub lbl_newtonsoft_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_newtonsoft_url.MouseLeftButtonDown
        Process.Start("http://www.newtonsoft.com/json")
    End Sub

    Private Sub lbl_nSpotify_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_sAPI_url.MouseLeftButtonDown
        Process.Start("https://github.com/JohnnyCrazy/SpotifyAPI-NET/releases")
    End Sub

    Private Sub lbl_hvOSD_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_hvOSD_url.MouseLeftButtonDown
        Process.Start("http://wordpress.venturi.de/?p=1")
    End Sub

    Private Sub lbl_nUpdate_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_nUpdate_url.MouseLeftButtonDown
        Process.Start("https://www.nupdate.net/")
    End Sub

    Private Sub btn_hvOSD_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_hvOSD_license.Click
        licvwr.show_license(".\licenses\hvOSD_license.txt", "HideVolumeOSD Lizenz")
    End Sub

    Private Sub btn_sAPI_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_sAPI_license.Click
        licvwr.show_license(".\licenses\sAPI_license.txt", "SpotifyAPI-NET Lizenz")
    End Sub

    Private Sub btn_nUpdate_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_nUpdate_license.Click
        licvwr.show_license(".\licenses\nUpdate_license.txt", "nUpdate Lizenz")
    End Sub

    Private Sub btn_newtonsoft_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_newtonsoft_license.Click
        licvwr.show_license(".\licenses\Json.NET_license.txt", "Json.NET Lizenz")
    End Sub

    Private Sub btn_mahapps_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_mahapps_license.Click
        licvwr.show_license(".\licenses\MahApps.Metro_license.txt", "mahapps.metro Lizenz")
    End Sub

    Private Sub btn_coreaudio_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_coreaudio_license.Click
        licvwr.show_license(".\licenses\coreaudio-dotnet_license.txt", "Core Audio .NET Wrapper Lizenz")
    End Sub

#End Region

    Private Sub get_3rdptysw_versions()
        lbl_coreaudio_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\CoreAudioApi.dll").FileVersion
        lbl_mahapps_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\MahApps.Metro.dll").FileVersion
        lbl_newtonsoft_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\Newtonsoft.Json.dll").FileVersion
        lbl_sAPI_version.Content = "Version: 2.17.0"
        lbl_nUpdate_version.Content = "Version: " & FileVersionInfo.GetVersionInfo(".\nUpdate.Internal.dll").FileVersion
    End Sub
#End Region

#Region "Spotify"
    Dim bol_spotify_installed As Boolean

    Private Sub btn_spotify_Click(sender As Object, e As RoutedEventArgs) Handles btn_spotify.Click
        matc_tabctrl.SelectedIndex = 3
        lbl_header.Content = "SPOTIFY"

        lbl_cacheSize.Content = get_cache_size()

        'Spotify Check
        If IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Spotify\Spotify.exe") Then
            lbl_spotifyCheck.Content = "Spotify installiert (v" & MainWindow._sAPI_ClientVersion & ") und " & If(Process.GetProcessesByName("Spotify").Length > 1, "gestartet", "nicht gestartet (!)")
            bol_spotify_installed = True
        Else
            lbl_spotifyCheck.Content = "Spotify ist nicht Installiert."
            bol_spotify_installed = False
        End If
    End Sub

    Private Sub btn_restart_spotify_Click(sender As Object, e As RoutedEventArgs) Handles btn_restart_spotify.Click
        If bol_spotify_installed = True Then flyout_spotify("force_restart") Else show_flyout("Spotify nicht installiert!", False)
    End Sub

    Private Sub flyout_spotify(ctnt As String, ByVal Optional e_csize As String = "")
        Select Case ctnt
            Case "del_cache"
                lbl_reset_cache_header.Content = "Daten im Cache löschen?"
                btn_flyout_spotify_confirm.Tag = "del_cache"
                lbl_flyout_spotify_msg.Content = "Alle Albumcover werden entfernt, es werden " & e_csize & " Speicherplatz freigegeben." & NewLine & "Die Cover werden bei bedarf erneut heruntergeladen."
                btn_flyout_spotify_confirm.Content = "Löschen"

            Case "force_restart"
                lbl_reset_cache_header.Content = "Spotify neu-starten?"
                btn_flyout_spotify_confirm.Tag = "force_restart"
                lbl_flyout_spotify_msg.Content = "Der Spotify Client wird beendet und neu-gestartet"
                btn_flyout_spotify_confirm.Content = "Neu starten"

        End Select

        flyout_cache_reset.IsOpen = True
        Dim c_blur As New Effects.BlurEffect With {.Radius = 10}
        matc_tabctrl.Effect = c_blur
    End Sub

#Region "AlbumArt Cache"
    Private Sub btn_cache_refresh_Click(sender As Object, e As RoutedEventArgs) Handles btn_cache_refresh.Click
        lbl_cacheSize.Content = get_cache_size()
    End Sub

    Private Sub btn_cache_reset_Click(sender As Object, e As RoutedEventArgs) Handles btn_cache_reset.Click
        flyout_spotify("del_cache", get_cache_size(True))
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

            lbl_cacheSize.Content = get_cache_size()

        ElseIf btn_flyout_spotify_confirm.Tag Is "force_restart" Then
            show_flyout("Spotify wird neu-gestartet...", True)

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
        My.Computer.Audio.Play(AppDomain.CurrentDomain.BaseDirectory & "\Resources\win_vis_beta_startup.wav")
    End Sub

    Private Function get_cache_size(ByVal Optional e_notext As Boolean = False) As String
        Dim files_count As Integer
        Dim cache_size As Integer

        If IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory & "cache\media\") Then
            For Each file As String In System.IO.Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory & "cache\media\", "*.*", System.IO.SearchOption.AllDirectories)
                cache_size += CInt(My.Computer.FileSystem.GetFileInfo(file).Length)
                files_count += 1
            Next
        Else
            If e_notext = False Then
                Return "Keine Album-Cover zwischengespeichert"
            Else
                Return "0MB"
            End If
        End If

        If e_notext = False Then
            If cache_size = 0 Then
                Return "Keine Album-Cover zwischengespeichert"
            ElseIf cache_size < 1048576 Then
                Return "Album-Cover zwischengespeichert: " & files_count & " (" & (cache_size / 1024).ToString("0") & "KB)"
            Else
                Return "Album-Cover zwischengespeichert: " & files_count & " (" & (cache_size / 1048576).ToString("0.0") & "MB)"
            End If
        Else
            Return (cache_size / 1048576).ToString("0.0") & "MB"
        End If
    End Function
#End Region

#End Region

#Region "Flyout Message"
    Private Sub show_flyout(ByVal e_msg As String, ByVal e_show_pring As Boolean, ByVal Optional e_timeout As Integer = -1)
        lbl_flyout_message.Content = e_msg
        If e_show_pring = True Then pring_flyout_message.Visibility = Visibility.Visible Else pring_flyout_message.Visibility = Visibility.Hidden

        If e_timeout = -1 Then
            flyout_message.IsAutoCloseEnabled = True
        ElseIf e_timeout = 0 Then
            flyout_message.IsAutoCloseEnabled = False
        Else
            flyout_message.IsAutoCloseEnabled = True
            flyout_message.AutoCloseInterval = e_timeout
        End If

        grd_bottom_strip.Visibility = Visibility.Hidden
        grd_bottom_strip_weather.Visibility = Visibility.Hidden

        flyout_message.IsOpen = True
    End Sub

    Private Sub close_flyout()
        flyout_message.IsOpen = False
    End Sub

    Private Sub flyout_message_closed(sender As Object, e As RoutedEventArgs) Handles flyout_message.ClosingFinished
        pring_flyout_message.Visibility = Visibility.Hidden
        grd_bottom_strip.Visibility = Visibility.Visible
        grd_bottom_strip_weather.Visibility = Visibility.Visible
        If matc_tabctrl.SelectedIndex = 1 Then matc_tabctrl.SelectedIndex = 0
    End Sub

#End Region

#Region "Updates"
    Dim manager As New UpdateManager(New Uri("https://niklas389.lima-city.de/SmartUI/updates.json"), "<RSAKeyValue><Modulus>1l9BYgpTEN2ID5l489eiFDhCUdHPGvJ21PJ2m1FdVlAzKl7zalkjvzYd8th7KOFaepYMWXryAIJ+v+FimyQ/H6/EcZH26sr+0mqo44UuBkiEvNFprwplmzt++TnuKIY6+exc0+S3NyuB9cxf4z9vKZGWwWrV/n+CXEU3GDAsWsRoAobU5/P2o56GlCfqO+ybyr5yO11O5eLoAYcMNjyY16VHa4aJvx9iPtRI0aM7T6BVuYuR1NTZjkdnQIdAJAY0T/zO47Fb2M34Oo7uDyRxZ4CDTpNya6ejR35PwIkTWfqRXjj6IJsF8rrjB/Xj/MCgLLXjbfy8F4pSmfmyJ56mUzNlA6rSmymvjg0hzSNwQqNKJEeK4W8WDd9Y5w/Lz9KMeNYz8Wy/oUHvJgx5DK2PmIAUuYhLTGO6wqG/IDt0xy5PyLa0NocbMcXneRPqXx7b3rT7uxvruuIyTMHovIln3F+3b7WYrsS52V8wWoOCDFElX+Y7L8gK1kQ4pXJvqFUqWYNGYG1yBorJ+eWc0E1PQ7Gv+ZL8CBigN4idfhRg87O87psXekK4RE76Pfx7RliTUH1P9LfwRrUQjamt60ZFGrmSKgqxMBGvWuYDFGEtai2EouqBsnVkyzfWPPqG4z++XvPsm0ah1+xJOCyRoa7NHkeGA/PZEVGHYmi9+GbqaV0hfmNx89I1EGn7cXC4Y67AnxvMy7N0jxrn3iQG3tI8AsX1ZZeUqYWwXpc8e1/cpXQ1POKu+gVj/sviyilZP4lFkGwR3ZIz2FZPLd547P2raz6xF1zgo722KZJCH2wU9uBDWiCwawv6N8cNHEuD+1b26ouot0O0Dg/9IA7ic3uWUA2o2zs5UxEoNQSc8z6HR+WeJmW5tjnr8ZZoFZECl8cnJmCTBfkwYbtv1xTf+VhxUa4aROMxbBaxTEMpbYVFZ7WYhJ4ncmh2JjNGYmKNphNUxrTcL6mzx8Mc9bdmr+sxymdKHnybjT39oGI7bkh8jeQj/qDMcIARu3nI0ORibThwYffiWXWKPE2o3yE3a/+RUqpEleK1+orzfO5cANZpTcTJ6ouSkNTnlQoNOsXHRhnC5wL2jeKjfUqEdtLHxMWhnhNnzWrWMnlgdfsjyUsUTOx0nWVPFSOACB943QmYxt1a+tNfkJZCkrIQtLlMqFYyUOEcSsDyaKft9FYE+ABKWKwRvO0X8hujTm2XWoNjyox7gCXCzD0DlawsegUgmT62gKxhhkOwbGyBbrGnZutcYJPL4KX5dxC6k25IGVSRsymzXqhvhu3f93Vn+/ynoZctjnYEw9SlRSqzIYR8xi1WdpKkLfe89EHD20wEf3xKL6cIhsd4npV/at6n6HOsxrQnjQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>", New CultureInfo("en"))
    Dim updaterUI As New UpdaterUI(manager, SynchronizationContext.Current)

    Private Sub btn_updates_Click(sender As Object, e As RoutedEventArgs) Handles btn_updates.Click
        If updater_enabled = False Then
            show_flyout("Die suche nach Updates ist in diesem Build deaktiviert", False)
            Exit Sub
        End If

        matc_tabctrl.SelectedIndex = 4
        lbl_header.Content = "UPDATES"
    End Sub

    Private Sub updater_hiddensearch()
        manager.IncludeBeta = True
        manager.CloseHostApplication = True
        manager.SearchForUpdatesAsync()
    End Sub

    Private Sub btn_updates_search_Click(sender As Object, e As RoutedEventArgs) Handles btn_updates_search.Click
        updaterUI.ShowUserInterface()
    End Sub


#End Region

End Class
