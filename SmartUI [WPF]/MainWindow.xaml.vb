Imports System
Imports System.ComponentModel
Imports System.Globalization
Imports System.Net.NetworkInformation
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows
Imports System.Windows.Input
Imports System.Windows.Interop
Imports System.Windows.Media
Imports CoreAudioApi
'Imports for SPOTIFY-API .NET
Imports SpotifyAPI.Local 'Enums
Imports SpotifyAPI.Local.Models 'Models for the JSON-responses

Class MainWindow
    Dim ini As New ini_file
    Public Shared wnd_log As New wnd_log

    Public Shared settings_update_needed As Boolean = False
    Public Shared weather_update_needed As Boolean = False

    Public Shared suiversion As String = My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor & ".4"

#Region "Blur & Dock"
    'Dock
    Const ABM_NEW As Int32 = 0
    Const ABM_REMOVE As Int32 = 1
    Const ABM_SETPOS As Int32 = 3

    Private Structure APPBARDATA
        Public cbSize As Int32
        Public hwnd As IntPtr
        Public uCallbackMessage As [Delegate]
        Public uEdge As Int32
        Public rc As RECT
        Public lParam As Int32
    End Structure
    Private Structure RECT
        Public rLeft As Int32
        Public rTop As Int32
        Public rRight As Int32
        Public rBottom As Int32
    End Structure

    Private Declare Function apiSHAppBarMessage Lib "shell32" Alias "SHAppBarMessage" (ByVal dwMessage As Int32, ByRef pData As APPBARDATA) As Int32
    Private abd As New APPBARDATA

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        apiSHAppBarMessage(ABM_REMOVE, abd)
    End Sub

    'LC
    Private Sub MainWindow_LocationChanged(sender As Object, e As EventArgs) Handles Me.LocationChanged, Me.SizeChanged
        Me.Width = SystemParameters.PrimaryScreenWidth
        Me.Top = 0
        Me.Left = 0
    End Sub

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
        WCA_ACCENT_POLICY = 19
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

    Private Sub sui_dock_blur(ByVal Optional e_blur As Boolean = True)
        'DOCK
        On Error Resume Next
        apiSHAppBarMessage(ABM_REMOVE, abd)
        abd = New APPBARDATA
        abd.cbSize = Marshal.SizeOf(abd)
        abd.uEdge = 1
        Me.Width = SystemParameters.WorkArea.Width
        Me.Height = 25
        abd.rc.rLeft = CInt(SystemParameters.WorkArea.Left)
        abd.rc.rRight = CInt(SystemParameters.WorkArea.Right)
        abd.rc.rTop = CInt(SystemParameters.WorkArea.Top)
        abd.rc.rBottom = 25
        apiSHAppBarMessage(ABM_NEW, abd)
        apiSHAppBarMessage(ABM_SETPOS, abd)
        Me.Topmost = True
        Me.Top = 0
        Me.Left = 0

        If e_blur = True Then
            Dim windowHelper = New WindowInteropHelper(Me)

            Dim accent = New AccentPolicy()
            Dim accentStructSize = Marshal.SizeOf(accent)
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND

            Dim accentPtr = Marshal.AllocHGlobal(accentStructSize)
            Marshal.StructureToPtr(accent, accentPtr, False)

            Dim data = New WindowCompositionAttributeData() With {
            .Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            .SizeOfData = accentStructSize,
            .Data = accentPtr}

            SetWindowCompositionAttribute(windowHelper.Handle, data)

            Marshal.FreeHGlobal(accentPtr)
        End If
    End Sub
#End Region

#Region "MyBase & Startup"
    Public Sub New()
        wnd_log.outputBox.AppendText("SmartUI LOG")
        wnd_log.outputBox.AppendText(NewLine & "Made in 2016/17 by Niklas Wagner in Hannover (DE)")
        wnd_log.outputBox.AppendText(NewLine & "App startup time: " & DateTime.Now.Day & ". " & CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) & " - " & DateTime.Now.ToLongTimeString)
        wnd_log.outputBox.AppendText(NewLine & "APP Version: " & My.Application.Info.Version.ToString & " - " & IO.File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory & "\SmartUI.exe").ToString("yyMMdd"))
        wnd_log.outputBox.AppendText(NewLine & "OS Version: " & Environment.OSVersion.ToString & NewLine)

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        sui_dock_blur()

        'If IO.File.Exists(".\config\wcom_allowed") Then wnd_log.Show()

        lbl_clock.Content = "SmartUI"
        lbl_clock_weekday.Content = "v" & suiversion

        wpf_helper.helper_grid(grd_volume, False)
        wpf_helper.helper_grid(grd_spotify, False)
        wpf_helper.helper_grid(grd_weather, False)
        wpf_helper.helper_grid(grd_network, False)
        wpf_helper.helper_grid(grd_menu_right, False)

        ui_clock_weekday = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(DateTime.Now.DayOfWeek)

        'Add log entry if this is this versions first run
        If Not My.Application.Info.Version.ToString = ini.ReadValue("app", "firstrun", "") Then
            wnd_log.AddLine("INFO", "First start after updating the app" & Environment.NewLine)
        End If

        settings_load()

        AddHandler Microsoft.Win32.SystemEvents.PowerModeChanged, AddressOf SystemEvents_PowerModeChanged
    End Sub

    Public Sub settings_load()
        'LOG
        If settings_update_needed Then
            wnd_log.AddLine("INFO" & "-SETTINGS", "Loading updated settings")
            helper_notification("Einstellungen geladen...")
        Else
            wnd_log.AddLine("INFO" & "-SETTINGS", "Loading settings")
        End If

        'Seconds
        If CType(ini.ReadValue("UI", "cb_wndmain_clock_enabled", "True"), Boolean) = False Then ui_clock_style = 0 Else ui_clock_style = 1

        If Not ui_clock_style = 0 Then
            If CType(ini.ReadValue("UI", "cb_wndmain_clock_seconds", "False"), Boolean) = True Then ui_clock_style = 2
            If CBool(ini.ReadValue("UI", "cb_wndmain_clock_weekday", "True")) = True Then ui_clock_style += 10 'Weekday
        End If

        'Network
        net_allSpeeds = CType(ini.ReadValue("UI", "cb_wndmain_net_iconDisableSpeedLimit", "False"), Boolean)
        net_textAllSpeeds = CType(ini.ReadValue("UI", "cb_wndmain_net_textDisableSpeedLimit", "False"), Boolean)

        'media 
        trk_show_progess = CType(ini.ReadValue("Spotify", "cb_wndmain_spotify_progress", "False"), Boolean)
    End Sub

    Private WithEvents tmr_aInit As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5), .IsEnabled = True}
    Private Sub tmr_aInit_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_aInit.Tick
        cls_weather.init_update(True) 'Init Weather

        wnd_flyout_appmenu.ui_settings.Show()

        wpf_helper.helper_grid(grd_menu_right, True)

        init_coreaudio() 'Init CoreAudio API

        init_spotifyAPI() 'Init Spotify-API

        lbl_weather.Content = "--°" 'Change text to this to avoid that user see's labels standard text
        wpf_helper.helper_grid(grd_weather, cls_weather.conf_enabled)

        tmr_clock.Start()
        tmr_network.Start()

        mpb_indicateLoading.Visibility = Visibility.Hidden
        mpb_indicateLoading.Margin = New Thickness(0, -5, 0, 0)
        mpb_indicateLoading.IsIndeterminate = False
        mpb_indicateLoading.Foreground = Brushes.White
        mpb_indicateLoading.Opacity = 0.5

        If sAPI_allowed = True Then
            wpf_helper.helper_grid(grd_spotify, e_playing) 'grid is only visible if Spotify is playing something.
            wpf_helper.helper_grid(grd_link, Not e_playing)
        End If

        tmr_aInit.Stop()
    End Sub

#End Region

#Region "EVENTS"
    'Update weather on os resume
    Private Sub SystemEvents_PowerModeChanged(ByVal sender As Object, ByVal e As Microsoft.Win32.PowerModeChangedEventArgs)
        'Select Case e.Mode
        '    Case Microsoft.Win32.PowerModes.Resume
        '    Case Microsoft.Win32.PowerModes.StatusChange
        '    Case Microsoft.Win32.PowerModes.Suspend
        'End Select

        If e.Mode = Microsoft.Win32.PowerModes.Resume Then
            wnd_log.AddLine("INFO" & "-SYSEVENT", "OS resumed from Standby/Hibernation")
            lbl_weather.Content = "--°"
            tmr_waitfornetwork.Start()
        End If
    End Sub

    Private WithEvents tmr_waitfornetwork As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5), .IsEnabled = False}
    Private Sub tmr_WaitForNetwork_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_waitfornetwork.Tick
        If My.Computer.Network.IsAvailable = True Then
            cls_weather.init_update(False)
            tmr_waitfornetwork.Stop()
        End If
    End Sub

#End Region

#Region "Clock"
    Private WithEvents tmr_clock As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1), .IsEnabled = False}
    Private ui_clock_weekday As String = "Mo"
    Private ui_clock_style As Integer = -1
    Dim clock_date As Boolean = False

    Private Sub tmr_clock_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_clock.Tick

        If clock_date = False Then
            Select Case ui_clock_style
                Case 0 'no clock
                    lbl_clock.Content = Nothing
                    lbl_clock_weekday.Content = Nothing

                Case 1 'hh:mm
                    lbl_clock.Content = DateTime.Now.ToShortTimeString
                    lbl_clock_weekday.Content = Nothing

                Case 2 'hh:mm:ss
                    lbl_clock.Content = DateTime.Now.ToLongTimeString
                    lbl_clock_weekday.Content = Nothing

                Case 11 'DDD, hh:mm
                    lbl_clock_weekday.Content = ui_clock_weekday & "."
                    lbl_clock.Content = DateTime.Now.ToShortTimeString()

                Case 12 'DDD, hh:mm:ss
                    lbl_clock_weekday.Content = ui_clock_weekday & "."
                    lbl_clock.Content = DateTime.Now.ToLongTimeString()

            End Select
        End If

        'Weather Update
        If DateTime.Now.Second = 55 Then
            Select Case DateTime.Now.Minute
                Case 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55
                    cls_weather.init_update(False, 2) 'wettercom update
                    If DateTime.Now.Minute = 0 Or DateTime.Now.Minute = 30 Then cls_weather.init_update(False, 1) 'oww_update

                    ui_clock_weekday = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(DateTime.Now.DayOfWeek)
            End Select
        End If

        If weather_update_needed = True Then
            lbl_weather.Content = cls_weather.get_temp
            wpf_helper.helper_image(icn_weather, cls_weather.oww_data_conditionIMG)
            weather_update_needed = False
        End If

        'WA Spotify v2 (Aggressive) | Restart sotify evp in case of error
        If dbg_sptfy = dbg_sptfy_2 And e_playing = True Then
            sAPI_error_count += 1

            If sAPI_error_count > 4 Then
                wpf_helper.helper_grid(grd_spotify, False) 'grid is only visible if Spotify is playing something.
                wpf_helper.helper_grid(grd_link, True)
                wpf_helper.helper_image(icn_run_spotify, "pack://application:,,,/Resources/ic_error_outline_white_24dp.png")
            Else

                flyout_media.Close() 'close media widget
                media_widget_opened = -1

                init_spotifyAPI()
                wnd_log.AddLine("ATT" & "-MEDIA", " - Spotify API restarted")
                sAPI_error = True
            End If

        Else
            sAPI_error_count = 0
        End If

        dbg_sptfy = dbg_sptfy_2

        If settings_update_needed = True Then
            settings_load()
            settings_update_needed = False
        End If
    End Sub

    'Show Date on MouseOver
    Private Sub lbl_clock_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles grd_clock.MouseEnter
        clock_date = True 'Block content change from clock timer
        lbl_clock.Content = DateTime.Now.Day & ". " & CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) & " " & DateTime.Now.Year
        lbl_clock_weekday.Content = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek) & ","
    End Sub

    Private Sub lbl_clock_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles grd_clock.MouseLeave
        clock_date = False 'Allow content change from clock timer
    End Sub

    Private Sub lbl_clock_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_clock_weekday.SizeChanged 'lbl_clock.SizeChanged,
        If lbl_clock_weekday.Content Is Nothing Then
            lbl_clock.Margin = New Thickness(0, -2, 0, 0)
        Else
            lbl_clock.Margin = New Thickness(lbl_clock_weekday.RenderSize.Width - 5, -2, 0, 0)
        End If
    End Sub
#End Region

#Region "CoreAudio"
    Private device_enum As New MMDeviceEnumerator()
    Private audio_device As MMDevice = device_enum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)

    Private Sub init_coreaudio()
        wnd_log.AddLine("INFO" & "-CORE AUDIO", "Initializing...")
        AddHandler audio_device.AudioEndpointVolume.OnVolumeNotification, AddressOf AudioEndpointVolume_OnVolumeNotification

        wpf_helper.helper_grid(grd_volume, True)

        ca_update(Math.Round(audio_device.AudioEndpointVolume.MasterVolumeLevelScalar * 100, 0), audio_device.AudioEndpointVolume.Mute)
    End Sub

    Private Sub AudioEndpointVolume_OnVolumeNotification(data As AudioVolumeNotificationData)
        ca_update(Math.Round(data.MasterVolume * 100, 0), data.Muted)
    End Sub

    Private Sub ca_update(ByVal e_volume As Double, ByVal e_muted As Boolean)
        If e_volume < 1 Or e_muted = True Then
            wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_off.png")
            wpf_helper.helper_label(lbl_volume, "Mute")

        Else
            wpf_helper.helper_label(lbl_volume, e_volume & "%")

            Select Case e_volume
                Case < 10
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_vLow.png")
                Case < 30
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_low.png")
                Case < 60
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_mid.png")
                Case Else
                    wpf_helper.helper_image(icn_volume, "pack://application:,,,/Resources/snd_high.png")
            End Select
        End If
    End Sub

    Dim ca_skip_volChange As Boolean
    Private Sub icn_volume_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles icn_volume.MouseWheel, lbl_volume.MouseWheel, grd_volume.MouseWheel
        If ca_skip_volChange = True Then
            ca_skip_volChange = False
            Exit Sub
        End If

        ca_skip_volChange = True

        If e.Delta > 0 Then
            audio_device.AudioEndpointVolume.VolumeStepUp()
        Else
            audio_device.AudioEndpointVolume.VolumeStepDown()
        End If
    End Sub

    Private Sub lbl_volume_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_volume.SizeChanged
        icn_volume.Margin = New Thickness(3, 3, 0, 0)
        grd_network.Margin = New Thickness(0, 0, grd_volume.RenderSize.Width + grd_link.RenderSize.Width + 6, 0)
    End Sub

    Private Sub lbl_clock_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles lbl_volume.MouseUp, icn_volume.MouseUp
        audio_device.AudioEndpointVolume.Mute = Not audio_device.AudioEndpointVolume.Mute
    End Sub

#End Region

#Region "SpotifyAPI-NET"
    Dim sAPI_allowed As Boolean = False
    Dim sAPI_error As Boolean = False
    Dim sAPI_error_count As Integer = 0
    'needed for recovery (...)
    Dim dbg_sptfy As String = ""
    Dim dbg_sptfy_2 As String = ""

    Public spotifyapi As SpotifyLocalAPI
    Public Shared _currentTrack As Track
    Public Shared _sAPI_ClientVersion As String

    Sub init_spotifyAPI() 'Init and connect Spotify-API
        wnd_log.AddLine("INFO" & "-MEDIA", "Init Spotify-API .NET...")
        spotifyapi = New SpotifyLocalAPI

        'Check if Spotify (and WebHelper) are running
        If Not SpotifyLocalAPI.IsSpotifyRunning() Then
            wnd_log.AddLine("INFO" & "-MEDIA", "Spotify isn't running!")
        End If

        If Not SpotifyLocalAPI.IsSpotifyWebHelperRunning() Then
            wnd_log.AddLine("INFO" & "-MEDIA", "SpotifyWebHelper isn't running!")
        End If

        Try
            Dim sAPI_connected As Boolean = spotifyapi.Connect
            sAPI_allowed = sAPI_connected
            If sAPI_connected = True Then
                wnd_log.AddLine("INFO" & "-MEDIA", "Connection established succesfully!")
                sAPI_UpdateInfos()

                spotifyapi.ListenForEvents = True
                sAPI_error = False
            Else
                wnd_log.AddLine("INFO" & "-MEDIA", "Couldn't connect - API disabled until next start!")
                sAPI_error = True
            End If

        Catch ex As Exception
            wnd_log.AddLine("INFO" & "-MEDIA", "Well, here's something really fucked up...")
        End Try

        AddHandler spotifyapi.OnPlayStateChange, AddressOf spotifyapi_OnPlayStateChange
        AddHandler spotifyapi.OnTrackChange, AddressOf spotifyapi_OnTrackChange
        AddHandler spotifyapi.OnTrackTimeChange, AddressOf spotifyapi_OnTrackTimeChange
        'AddHandler spotifyapi.OnVolumeChange, AddressOf spotifyapi_OnVolumeChange
    End Sub

    Public Sub sAPI_UpdateInfos()
        Dim status As StatusResponse = spotifyapi.GetStatus
        wnd_log.AddLine("INFO" & "-MEDIA", "Spotify-Client Version: " & spotifyapi.GetStatus.ClientVersion.ToString)
        _sAPI_ClientVersion = spotifyapi.GetStatus.ClientVersion.ToString

        'Basic Spotify Infos
        'repeatShuffleLabel.Text = status.Repeat + " and " + status.Shuffle

        If status.Track IsNot Nothing Then
            'Update track infos
            _currentTrack = status.Track
            e_playing = status.Playing
        End If
    End Sub

    '    'advertLabel.Text = If(track.IsAd(), "ADVERT", "")

    '    track.IsAd()
    '    'titleLinkLabel.Text = track.TrackResource.Name
    '    'titleLinkLabel.Tag = track.TrackResource.Uri

    '    'Dim uri As SpotifyUri = track.TrackResource.ParseUri()

    '    bigAlbumPicture.Image = Await track.GetAlbumArtAsync(AlbumArtSize.Size640)
    '    smallAlbumPicture.Image = Await track.GetAlbumArtAsync(AlbumArtSize.Size160)

    'Private Sub spotifyapi_OnVolumeChange(sender As Object, e As VolumeChangeEventArgs)
    '    'volumeLabel.Text = (e.NewVolume * 100).ToString(CultureInfo.InvariantCulture)
    'End Sub

    Private Sub spotifyapi_OnTrackTimeChange(sender As Object, e As TrackTimeChangeEventArgs)
        sui_media_update(_currentTrack.TrackResource.Name, _currentTrack.ArtistResource.Name, DateTime.MinValue.AddSeconds(_currentTrack.Length - CInt(e.TrackTime)).ToString("m:ss"), e.TrackTime, CDbl(_currentTrack.Length), e_playing)
        dbg_sptfy = (_currentTrack.TrackResource.Name & _currentTrack.ArtistResource.Name & DateTime.MinValue.AddSeconds(_currentTrack.Length - CInt(e.TrackTime)).ToString("m:ss") & e.TrackTime.ToString & _currentTrack.Length.ToString & e_playing)
    End Sub


    Private Sub spotifyapi_OnTrackChange(sender As Object, e As TrackChangeEventArgs)
        _currentTrack = e.NewTrack
    End Sub

    Public Shared e_playing As Boolean
    Private Sub spotifyapi_OnPlayStateChange(sender As Object, e As PlayStateEventArgs)
        e_playing = e.Playing

        If e.Playing = False Then
            tmr_mediaInfo_delay.Start()
            wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/ic_pause_white_24dp.png")
        Else
            tmr_mediaInfo_delay.Stop()
            wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
            wpf_helper.helper_grid(grd_spotify, e.Playing) 'grid is only visible if Spotify is playing something.
            wpf_helper.helper_grid(grd_link, Not e.Playing)
        End If
    End Sub

    Private WithEvents tmr_mediaInfo_delay As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 3), .IsEnabled = False}
    Private Sub tmr_mediaInfo_delay_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_mediaInfo_delay.Tick
        wpf_helper.helper_grid(grd_spotify, e_playing) 'delayed hiding after playing is paused
        wpf_helper.helper_grid(grd_link, Not e_playing)
        tmr_mediaInfo_delay.Stop()
    End Sub


#End Region

#Region "MIP (Media Info Processing)"
    Dim trk_show_progess As Boolean = False ' BOL = Toggles visibility of progressbar on top of main overlay

    Dim media_last_title As String = ""
    Dim media_last_artist_time As String = ""
    Dim test_trk_rest As String = ""

    Private Sub sui_media_update(ByVal e_title As String, ByVal e_artist As String, ByVal e_Tremaining As String, ByVal e_pb_val As Double, ByVal e_pb_max As Double, ByVal e_playing As Boolean)
        'Title ------------------ don't update label if title didn't change

        If wpf_helper.helper_label_gc(lbl_spotify) <> media_last_title And media_widget_opened <> 1 Then
            Select Case True
                Case e_title.Contains("(") And Not e_title.StartsWith("(")
                    wpf_helper.helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("(") - 1)))
                    'check & add info in SubLabel
                    test_trk_rest = media_trk_adinfo(e_title.Substring(e_title.IndexOf("("), e_title.Length - e_title.IndexOf("("))) & " ٠ "

                Case e_title.Contains("- ")
                    wpf_helper.helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("-") - 1)))
                    'check & add info in SubLabel
                    test_trk_rest = media_trk_adinfo(e_title.Substring(e_title.IndexOf("-"), e_title.Length - e_title.IndexOf("-"))) & " ٠ "

                Case Else
                    test_trk_rest = ""

                    If e_title.Length > 41 Then
                        wpf_helper.helper_label(lbl_spotify, e_title.Remove(40, e_title.Length - 40) & "...")
                    Else
                        wpf_helper.helper_label(lbl_spotify, e_title)
                    End If
            End Select

            media_last_title = wpf_helper.helper_label_gc(lbl_spotify)
        End If

        If Not e_artist & " -" & e_Tremaining = media_last_artist_time Or wpf_helper.helper_label_gc(lbl_spotify_remaining) = " " Then
            media_last_artist_time = test_trk_rest & e_artist & " ٠ " & e_Tremaining

            If media_widget_opened = 0 Then
                wpf_helper.helper_label(lbl_spotify_remaining, media_last_artist_time)
            Else
                wnd_flyout_media.str_media_time = e_pb_val & "%" & e_pb_max & "#" & e_Tremaining
            End If
        End If

        If trk_show_progess = True Then
            wpf_helper.helper_progressBar(mpb_indicateLoading, e_pb_val, e_pb_max, e_playing)
        Else
            If mpb_indicateLoading.Visibility = Visibility.Visible Then wpf_helper.helper_progressBar(mpb_indicateLoading, , , False)
        End If
    End Sub

    'MEDIA GRID UPDATE PART ---------------------------
    Private Function media_trk_adinfo(e As String) As String
        Dim e_fs As String = e

        'remove "(" at start and ")" end
        If e.StartsWith("(") And e.EndsWith(")") Then e_fs = e.Substring(1, e.Length - 2)
        'remove "-" at beginning
        If e.StartsWith("-") Then e_fs = e.Substring(2)
        'limit to length of 30
        If e_fs.Length > 30 Then
            If e_fs.Substring(0, 30).StartsWith("(") And Not e_fs.Substring(0, 30).Contains(")") Then Return e_fs.Substring(1, 30) & "..." Else Return e_fs.Substring(0, 30) & "..."
        Else
            Return e_fs
        End If
    End Function

    Private Sub lbl_spotify_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_spotify.SizeChanged, lbl_spotify_remaining.SizeChanged
        lbl_spotify.Margin = New Thickness(19, -3, 0, 0)
        lbl_spotify_remaining.Margin = New Thickness(icn_spotify.RenderSize.Width + lbl_spotify.RenderSize.Width - 4, -1, 0, 0)
    End Sub

    'SPOTIFY TRACK CHANGE
    Dim spotify_skip_trackChange As Boolean = False

    'Change track with scrolling up/down
    Private Sub lbl_spotify_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles grd_spotify.MouseWheel
        If spotify_skip_trackChange = True Then
            spotify_skip_trackChange = False
        Else
            'Scroll up > Next Track / Skip | Scroll down > Last Track / Return
            If e.Delta > 0 Then spotifyapi.Skip() Else spotifyapi.Previous()

            spotify_skip_trackChange = True
        End If
    End Sub

    Private Sub btn_spotify_playpause_Click(sender As Object, e As RoutedEventArgs) Handles icn_spotify.MouseUp, icn_run_spotify.MouseUp
        If sAPI_error = True Then
            sAPI_error_count = 0
            init_spotifyAPI()
            wpf_helper.helper_image(icn_run_spotify, "pack://application:,,,/Resources/spotify_notification.png")
            wpf_helper.helper_grid(grd_spotify, e_playing) 'grid is only visible if Spotify is playing something.
            wpf_helper.helper_grid(grd_link, Not e_playing)
        Else
            ' PlayPause
            If e_playing = False Then spotifyapi.Play() Else spotifyapi.Pause()
        End If
    End Sub

    Private Sub icn_spotify_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles icn_spotify.MouseEnter
        wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/ic_pause_white_24dp.png")
    End Sub

    Private Sub icn_spotify_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles icn_spotify.MouseLeave
        wpf_helper.helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
    End Sub

    'positioning labels - left
    Private Sub lbl_weather_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_weather.SizeChanged, icn_weather.SizeChanged, grd_weather.SizeChanged, grd_spotify.SizeChanged
        If grd_weather.Visibility = Visibility.Visible Then
            grd_spotify.Margin = New Thickness(grd_weather.RenderSize.Width, 0, 0, 0)
        Else
            grd_spotify.Margin = New Thickness(3, 0, 0, 0)
        End If
    End Sub
#End Region

#Region "Media Widget/Flyout"
    Public Shared media_widget_opened As Integer = 0
    Dim flyout_media As New wnd_flyout_media

    Private Sub lbl_spotify_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles grd_spotify.MouseLeftButtonUp
        If media_widget_opened = -1 Then flyout_media = New wnd_flyout_media

        If media_widget_opened = 1 Then
            flyout_media.Hide()
            media_widget_opened = 0
        Else
            flyout_media.Show()
            media_widget_opened = 1
            wpf_helper.helper_label(lbl_spotify, "Spotify")
            wpf_helper.helper_label(lbl_spotify_remaining, " ")
            Me.Activate()
        End If
    End Sub
#End Region

#Region "NOTIFICATION"
    Private Sub helper_notification(e_msg As String, Optional e_icn As String = Nothing)
        Application.Current.Dispatcher.Invoke(Threading.DispatcherPriority.Normal,
                                              New ThreadStart(Sub()
                                                                  fout_notification.IsOpen = True
                                                                  fout_notification.IsAutoCloseEnabled = False
                                                                  fout_notification.IsAutoCloseEnabled = True

                                                                  lbl_notification.Content = e_msg
                                                                  If Not e_icn = Nothing Then
                                                                      icn_notification.Source = CType(New ImageSourceConverter().ConvertFromString(e_icn),
                                                                      ImageSource)
                                                                  Else
                                                                      icn_notification.Source = CType(New ImageSourceConverter().
                                                                      ConvertFromString("pack://application:,,,/Resources/ic_error_outline_white_24dp.png"), ImageSource)
                                                                  End If
                                                              End Sub))
    End Sub
#End Region

#Region "Network Monitoring"
    'Settings
    Dim net_allSpeeds As Boolean = False
    Dim net_textAllSpeeds As Boolean = False

    Dim net_monitoring_allowed As Integer = 0 '0 = disabled / 1 = enabled / -1 = error
    Dim net_monitoredInterface As NetworkInterface
    Dim settings_net_interface As String = ""

    'Update timer
    Private WithEvents tmr_network As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 500), .IsEnabled = False}
    Private Sub tmr_network_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_network.Tick
        If net_monitoring_allowed = -1 Then wnd_log.AddLine("INFO" & "-NET", "Network monitoring state: " & net_monitoring_allowed.ToString)
        If net_monitoring_allowed = -1 Then tmr_network.Stop()
        net_monitoring()
    End Sub

    'NetMon Step 1
    Private Sub net_get_interfaces()
        If net_monitoring_allowed = -1 Then Exit Sub

        Try
            Dim net_allNICs As NetworkInterface() = NetworkInterface.GetAllNetworkInterfaces()
            wnd_settings.net_NIC_list = Nothing

            For i As Integer = 0 To net_allNICs.Length - 1
                Dim net_interface As NetworkInterface = net_allNICs(i)

                If net_interface.NetworkInterfaceType <> NetworkInterfaceType.Tunnel AndAlso net_interface.NetworkInterfaceType <> NetworkInterfaceType.Loopback Then
                    If Not ini.ReadValue("NET", "ComboBox_net_interface", "NOT_SET") = "null" And Not ini.ReadValue("NET", "ComboBox_net_interface", "NOT_SET") = "NOT_SET" Then
                        If net_interface.Name = ini.ReadValue("NET", "ComboBox_net_interface", "NOT_SET") Then net_monitoredInterface = net_interface
                    End If


                    wnd_settings.net_NIC_list &= net_interface.Name & ";"
                End If
            Next

            wnd_log.AddLine("INFO" & "-NET", "Seleceted Interface: " & net_monitoredInterface.Name)

            wpf_helper.helper_grid(grd_network, True)
            net_monitoring_allowed = 1

        Catch
            wnd_log.AddLine("ERR" & "-NET", "Error in 'net_get_interfaces'")
            net_monitoring_allowed = -1
            wpf_helper.helper_grid(grd_network, False)
        End Try
    End Sub

    'actual net monitoring:
    Dim net_stat_bSent As Int64
    Dim net_stat_bReceived As Int64

    Dim net_stat_bSent_speed As Integer
    Dim net_stat_bReceived_speed As Integer

    Private Sub net_monitoring()
        If net_monitoring_allowed = 0 Then
            wnd_log.AddLine("INFO" & "-NET", "Network monitoring disabled or no interface selected, GoTo 'net_get_interfaces'")

            net_get_interfaces()
            ' helper_grid(grd_network, False)
            Exit Sub

        ElseIf net_monitoring_allowed = -1 Then 'Exit sub if netmon got an error
            wnd_log.AddLine("ERR" & "-NET", "Network monitoring error, exiting")
            wpf_helper.helper_grid(grd_network, False)
            Exit Sub
        End If

        Dim net_NIC_statistic As IPInterfaceStatistics

        Try
            net_NIC_statistic = net_monitoredInterface.GetIPStatistics()
        Catch ex As Exception
            Exit Sub
        End Try

        If net_monitoredInterface.OperationalStatus = OperationalStatus.Up Then
            wpf_helper.helper_image(icn_network_state, "pack://application:,,,/Resources/ic_lan_connected.png")
        Else
            wpf_helper.helper_image(icn_network_state, "pack://application:,,,/Resources/ic_ethernet_cable_off_white_21px.png")
        End If

        If net_stat_bSent = 0 And net_stat_bReceived = 0 Then
            net_stat_bSent = net_NIC_statistic.BytesSent
            net_stat_bReceived = net_NIC_statistic.BytesReceived

        Else
            Try
                'get sent/received kbytes
                net_stat_bSent_speed = CInt(net_NIC_statistic.BytesSent - net_stat_bSent) * 2
                net_stat_bReceived_speed = CInt(net_NIC_statistic.BytesReceived - net_stat_bReceived) * 2
                'wnd_log.AddLine(log_cat & "-NET-MON", "bSent: " & net_stat_bSent_speed & " / bReceived: " & net_stat_bReceived_speed)
            Catch ex As Exception

                lbl_network_traffic_send.Content = Nothing
                icn_network_send.Visibility = Visibility.Hidden

                lbl_network_traffic_receive.Content = Nothing
                icn_network_receive.Visibility = Visibility.Hidden

                wnd_log.AddLine("ERR" & "-NET", "Error in 'get sent/received kbytes'")
                Exit Sub
            End Try
        End If

        net_stat_bSent = net_NIC_statistic.BytesSent
        net_stat_bReceived = net_NIC_statistic.BytesReceived

        'visualize sent v2
        If net_stat_bSent_speed > 0 Then
            If net_allSpeeds = True Or net_stat_bSent_speed > 4096 Then
                icn_network_send.Visibility = Visibility.Visible
                If net_stat_bSent_speed > 51200 Or net_textAllSpeeds = True Then Me.lbl_network_traffic_send.Content = get_formatted_bytes(net_stat_bSent_speed)
            Else
                lbl_network_traffic_send.Content = Nothing
                icn_network_send.Visibility = Visibility.Hidden
            End If

        Else
            lbl_network_traffic_send.Content = Nothing
            icn_network_send.Visibility = Visibility.Hidden
        End If

        'visualize received v2
        If net_stat_bReceived_speed > 0 Then
            If net_allSpeeds = True Or net_stat_bReceived_speed > 4096 Then
                icn_network_receive.Visibility = Visibility.Visible
                If net_stat_bReceived_speed > 51200 Or net_textAllSpeeds = True Then Me.lbl_network_traffic_receive.Content = get_formatted_bytes(net_stat_bReceived_speed)
            Else
                lbl_network_traffic_receive.Content = Nothing
                icn_network_receive.Visibility = Visibility.Hidden
            End If

        Else
            lbl_network_traffic_receive.Content = Nothing
            icn_network_receive.Visibility = Visibility.Hidden
        End If
    End Sub

    'convert bytes
    Private Function get_formatted_bytes(bytes As Integer) As String
        If bytes < 1024 Then
            Return Math.Round(CDbl(bytes), 0) & "B/s"
        ElseIf bytes < 1048576 Then
            Return Math.Round(CDbl(bytes / 1024), 0) & "KB/s"
        ElseIf bytes < 1073741824 Then
            Return Math.Round(CDbl(bytes / 1048576), 1) & "MB/s"
        Else
            Return Math.Round(CDbl(bytes / 1073741824), 1) & "GB/s"
        End If
    End Function

    'Net Grid Size
    Private Sub lbl_network_traffic_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_network_traffic_receive.SizeChanged, lbl_network_traffic_send.SizeChanged
        icn_network_send.Margin = New Thickness(0, 0, 0, 0)
        lbl_network_traffic_send.Margin = New Thickness(icn_network_send.RenderSize.Width, -1, 0, 0)

        icn_network_receive.Margin = New Thickness(icn_network_send.RenderSize.Width + lbl_network_traffic_send.RenderSize.Width, 0, 0, 0)
        lbl_network_traffic_receive.Margin = New Thickness(icn_network_send.RenderSize.Width + lbl_network_traffic_send.RenderSize.Width + icn_network_receive.RenderSize.Width, -1, 0, 0)

        icn_network_state.Margin = New Thickness((icn_network_send.RenderSize.Width + lbl_network_traffic_send.RenderSize.Width + icn_network_receive.RenderSize.Width + lbl_network_traffic_receive.RenderSize.Width + 3), 3, 0, 0)
    End Sub


#End Region

#Region "LINK GRID & FLYOUTS"
    Private Sub grd_link_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles grd_link.IsVisibleChanged
        If CBool(e.NewValue) = True Then
            grd_weather.Margin = New Thickness(25, 0, 0, 0)
        Else
            grd_weather.Margin = New Thickness(1.5, 0, 0, 0)
        End If
    End Sub

    'Icon : SpotifyLink
    Private Sub icn_run_spotify_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles icn_run_spotify.MouseEnter
        wpf_helper.helper_image(icn_run_spotify, "pack://application:,,,/Resources/ic_play_arrow_white_24dp.png")
    End Sub

    Private Sub icn_run_spotify_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles icn_run_spotify.MouseLeave
        wpf_helper.helper_image(icn_run_spotify, "pack://application:,,,/Resources/spotify_notification.png")
    End Sub

    Dim ui_appmenu As New wnd_flyout_appmenu
    Private Sub btn_exit_Click(sender As Object, e As RoutedEventArgs) Handles icn_menu.MouseLeftButtonUp, grd_menu_right.MouseLeftButtonUp
        ui_appmenu.Show()
    End Sub

    Dim weather_flyout As New wnd_flyout_weather
    Private Sub icn_weather_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles grd_weather.MouseUp
        weather_flyout.Show()
        Me.Topmost = False
        Me.Topmost = True
    End Sub
#End Region

End Class
