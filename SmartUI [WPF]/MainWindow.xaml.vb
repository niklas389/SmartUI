Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Interop
Imports System.Xml
Imports CoreAudioApi
'Imports nSpotify

'Imports for SPOTIFY-API .NET
Imports SpotifyAPI.Local 'Enums
Imports SpotifyAPI.Local.Models 'Models for the JSON-responses

Class MainWindow
    Dim ini As New ini_file
    Dim mysettings As New wnd_settings
    Dim wConf_useWcom As Boolean = False

    Public Shared settings_update_needed As Boolean = False

    Dim log_cat As String = "MW"
    Public Shared wnd_log As New wnd_log

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

    Private Sub DockToTop()
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
    End Sub

    Private Sub MainWindow_LocationChanged(sender As Object, e As EventArgs) Handles Me.LocationChanged
        Me.Top = 0
        Me.Left = 0
    End Sub

    Private Sub MainWindow_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        Me.Width = SystemParameters.PrimaryScreenWidth
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

#Region "MyBase & Startup"
    Public Sub New()
        wnd_log.outputBox.AppendText("<< SmartUI LOG >>")
        wnd_log.outputBox.AppendText(vbNewLine & "<< Made in 2016/17 by Niklas Wagner in Hannover (DE) >>")
        wnd_log.outputBox.AppendText(vbNewLine & "App startup time: " & DateTime.Now.Day & ". " & CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) & " - " & DateTime.Now.ToLongTimeString)
        wnd_log.outputBox.AppendText(vbNewLine & "APP Version: " & My.Application.Info.Version.ToString & " - " & IO.File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory & "\SmartUI.exe").ToString("yyMMdd"))
        wnd_log.outputBox.AppendText(vbNewLine & "OS Version: " & Environment.OSVersion.ToString & vbNewLine)

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.
    End Sub

    '------------------------------------------------------------------------------------------

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        DockToTop()
        EnableBlur()

        'If IO.File.Exists(".\config\wcom_allowed") Then wnd_log.Show()

        lbl_clock.Content = "SmartUI"
        lbl_clock_weekday.Content = My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor

        helper_grid(grd_volume, False)
        helper_grid(grd_spotify, False)
        helper_grid(grd_weather, False)
        helper_grid(grd_network, False)
        helper_grid(grd_menu_right, False)

        init_spotifyAPI() 'Init Spotify-API

        ui_clock_weekday = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(DateTime.Now.DayOfWeek)

        settings_load()

        ''Add log entry if this is this versions first run
        If Not My.Application.Info.Version.ToString = ini.ReadValue("app", "firstrun", "") Then
            wnd_log.AddLine(log_cat & "-INFO", "First start after updating the app")
        End If
    End Sub

    Public Sub settings_load()
        'LOG
        If settings_update_needed Then wnd_log.AddLine(log_cat & "-SETTINGS", "Loading updated settings") Else wnd_log.AddLine(log_cat & "-SETTINGS", "Loading settings")

        'Seconds
        If CType(ini.ReadValue("UI", "cb_wndmain_clock_enabled", "True"), Boolean) = False Then ui_clock_style = 0 Else ui_clock_style = 1

        If Not ui_clock_style = 0 Then
            If CType(ini.ReadValue("UI", "cb_wndmain_clock_seconds", "False"), Boolean) = True Then ui_clock_style = 2
            If CBool(ini.ReadValue("UI", "cb_wndmain_clock_weekday", "True")) = True Then ui_clock_style += 10 'Weekday
        End If

        'weather
        wData_enabled = CBool(ini.ReadValue("Weather", "cb_wndmain_weather_enabled", "False"))
        wConf_API_cityID = CInt(ini.ReadValue("Weather", "txtBx_weather_zipcode", "0"))
        wConf_API_key = (ini.ReadValue("Weather", "txtBx_weather_APIkey", "0"))

        'Network
        net_allSpeeds = CType(ini.ReadValue("UI", "cb_wndmain_net_iconDisableSpeedLimit", "False"), Boolean)
        net_textAllSpeeds = CType(ini.ReadValue("UI", "cb_wndmain_net_textDisableSpeedLimit", "False"), Boolean)

        'media 
        trk_show_progess = CType(ini.ReadValue("Spotify", "cb_wndmain_spotify_progress", "False"), Boolean)
    End Sub

    Private WithEvents tmr_aInit As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5), .IsEnabled = True}
    Private Sub tmr_aInit_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_aInit.Tick
        init_coreaudio()

        helper_grid(grd_menu_right, True)

        lbl_weather.Content = "--°" 'Change text to this to avoid that user see's labels standard text
        helper_grid(grd_weather, wData_enabled)

        'Weather
        oww_update() 'openweather

        If IO.File.Exists(".\config\wcom_allowed") Then
            wnd_log.AddLine(log_cat & "-WEATHER", "wetter.com is allowed")
            wConf_useWcom = True
            wcom_update()
        End If

        tmr_clock.Start()
        tmr_network.Start()
        mpb_indicateLoading.Visibility = Visibility.Hidden
        mpb_indicateLoading.Margin = New Thickness(0, -5, 0, 0)
        mpb_indicateLoading.IsIndeterminate = False
        mpb_indicateLoading.Foreground = Brushes.White
        mpb_indicateLoading.Opacity = 0.5

        wnd_flyout_appmenu.ui_settings.Show()

        If sAPI_allowed = True Then
            helper_grid(grd_spotify, e_playing) 'grid is only visible if Spotify is playing something.
            helper_grid(grd_link, Not e_playing)
        End If

        tmr_aInit.Stop()
    End Sub

#End Region

#Region "Clock"
    Private WithEvents tmr_clock As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1), .IsEnabled = False}
    Public ui_clock_weekday As String = "Mo"
    Public ui_clock_style As Integer = -1
    Dim clock_date As Boolean = False

    Private Sub tmr_clock_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_clock.Tick
        If DateTime.Now.ToLongTimeString = "00:00:00" Then ui_clock_weekday = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(DateTime.Now.DayOfWeek)

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
                    If wConf_useWcom = True Then wcom_update()
                    If DateTime.Now.Minute = 0 Or DateTime.Now.Minute = 30 Then oww_update()
            End Select
        End If

        'WA Spotify v2 (Aggressive) | Restart sotify evp in case of error
        If dbg_sptfy = dbg_sptfy_2 And e_playing = True Then
            sAPI_error_count += 1

            If sAPI_error_count > 4 Then
                helper_grid(grd_spotify, False) 'grid is only visible if Spotify is playing something.
                helper_grid(grd_link, True)
                helper_image(icn_run_spotify, "pack://application:,,,/Resources/ic_error_outline_white_24dp.png")
            Else

                wnd_log.AddLine(log_cat & "-MEDIA", "-------------------")
                flyout_media.Close() 'close media widget
                media_widget_opened = -1


                init_spotifyAPI()
                wnd_log.AddLine(log_cat & "-MEDIA", " - Spotify API restarted")
                wnd_log.AddLine(log_cat & "-MEDIA", "-------------------")
                sAPI_error = True
            End If

        Else
            sAPI_error_count = 0
        End If

        dbg_sptfy = dbg_sptfy_2

        If settings_update_needed Then
            settings_load()
            settings_update_needed = False
        End If
    End Sub

    'Show Date on MouseOver
    Private Sub lbl_clock_MouseEnter(sender As Object, e As MouseEventArgs) Handles lbl_clock.MouseEnter, lbl_clock_weekday.MouseEnter
        clock_date = True 'Block content change from clock timer
        lbl_clock.Content = DateTime.Now.Day & ". " & CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) & " " & DateTime.Now.Year
        lbl_clock_weekday.Content = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek) & ","
    End Sub

    Private Sub lbl_clock_MouseLeave(sender As Object, e As MouseEventArgs) Handles lbl_clock.MouseLeave, lbl_clock_weekday.MouseLeave
        clock_date = False 'Allow content change from clock timer
    End Sub

    Private Sub lbl_clock_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_clock.SizeChanged, lbl_clock_weekday.SizeChanged
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
        wnd_log.AddLine(log_cat & "-CORE AUDIO", "Initializing...")
        AddHandler audio_device.AudioEndpointVolume.OnVolumeNotification, AddressOf AudioEndpointVolume_OnVolumeNotification

        helper_grid(grd_volume, True)

        ca_update(Math.Round(audio_device.AudioEndpointVolume.MasterVolumeLevelScalar * 100, 0), audio_device.AudioEndpointVolume.Mute)
    End Sub

    Private Sub AudioEndpointVolume_OnVolumeNotification(data As AudioVolumeNotificationData)
        ca_update(Math.Round(data.MasterVolume * 100, 0), data.Muted)
    End Sub

    Private Sub ca_update(ByVal e_volume As Double, ByVal e_muted As Boolean)
        If e_volume < 1 Or e_muted = True Then
            helper_image(icn_volume, "pack://application:,,,/Resources/snd_off.png")
            helper_label(lbl_volume, "Mute")

        Else
            helper_label(lbl_volume, e_volume & "%")

            Select Case e_volume
                Case < 10
                    helper_image(icn_volume, "pack://application:,,,/Resources/snd_vLow.png")
                Case < 30
                    helper_image(icn_volume, "pack://application:,,,/Resources/snd_low.png")
                Case < 60
                    helper_image(icn_volume, "pack://application:,,,/Resources/snd_mid.png")
                Case Else
                    helper_image(icn_volume, "pack://application:,,,/Resources/snd_high.png")
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
        wnd_log.AddLine(log_cat & "-MEDIA", "Init Spotify-API .NET...")
        spotifyapi = New SpotifyLocalAPI

        'Check if Spotify (and WebHelper) are running
        If Not SpotifyLocalAPI.IsSpotifyRunning() Then
            wnd_log.AddLine(log_cat & "-MEDIA", "Spotify isn't running!")
        End If

        If Not SpotifyLocalAPI.IsSpotifyWebHelperRunning() Then
            wnd_log.AddLine(log_cat & "-MEDIA", "SpotifyWebHelper isn't running!")
        End If

        Try
            Dim sAPI_connected As Boolean = spotifyapi.Connect
            sAPI_allowed = sAPI_connected
            If sAPI_connected = True Then
                wnd_log.AddLine(log_cat & "-MEDIA", "Connection established succesfully!")
                sAPI_UpdateInfos()

                spotifyapi.ListenForEvents = True
                sAPI_error = False
            Else
                wnd_log.AddLine(log_cat & "-MEDIA", "Couldn't connect - API disabled until next start!")
                sAPI_error = True
                'helper_grid(grd_spotify, False) 'grid is only visible if Spotify is playing something. | it's working without this
                'helper_grid(grd_link, False)
            End If

        Catch ex As Exception
            MessageBox.Show(ex.InnerException.Message)
        End Try

        AddHandler spotifyapi.OnPlayStateChange, AddressOf spotifyapi_OnPlayStateChange
        AddHandler spotifyapi.OnTrackChange, AddressOf spotifyapi_OnTrackChange
        AddHandler spotifyapi.OnTrackTimeChange, AddressOf spotifyapi_OnTrackTimeChange
        AddHandler spotifyapi.OnVolumeChange, AddressOf spotifyapi_OnVolumeChange

        '_spotify.SynchronizingObject = this;
    End Sub

    Public Sub sAPI_UpdateInfos()
        Dim status As StatusResponse = spotifyapi.GetStatus
        wnd_log.AddLine(log_cat & "-MEDIA", "Spotify-Client Version: " & spotifyapi.GetStatus.ClientVersion.ToString)
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

    Private Sub spotifyapi_OnVolumeChange(sender As Object, e As VolumeChangeEventArgs)
        'volumeLabel.Text = (e.NewVolume * 100).ToString(CultureInfo.InvariantCulture)
    End Sub

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
            helper_image(icn_spotify, "pack://application:,,,/Resources/ic_pause_white_24dp.png")
        Else
            tmr_mediaInfo_delay.Stop()
            helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
            helper_grid(grd_spotify, e.Playing) 'grid is only visible if Spotify is playing something.
            helper_grid(grd_link, Not e.Playing)
        End If
    End Sub

    Private WithEvents tmr_mediaInfo_delay As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 3), .IsEnabled = False}
    Private Sub tmr_mediaInfo_delay_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_mediaInfo_delay.Tick
        helper_grid(grd_spotify, e_playing) 'delayed hiding after playing is paused
        helper_grid(grd_link, Not e_playing)
        tmr_mediaInfo_delay.Stop()
    End Sub


#End Region

#Region "MIP (Media Info Processing)"
    Dim trk_show_progess As Boolean = False ' BOL = Toggles visibility of progressbar on top of main overlay

    Dim media_last_title As String = ""
    Dim media_last_artist_time As String = ""

    Dim media_trktype As String = ""

    Dim test_trk_rest As String = ""

    Private Sub sui_media_update(ByVal e_title As String, ByVal e_artist As String, ByVal e_Tremaining As String, ByVal e_pb_val As Double, ByVal e_pb_max As Double, ByVal e_playing As Boolean)
        'Title ------------------
        'If title didn't change - don't update label
        If Not e_title = media_last_title Or helper_label_gc(lbl_spotify) = "Spotify" Then
            media_last_title = e_title

            If Not media_widget_opened = 1 Then

                If e_title.Contains("(") And Not e_title.StartsWith("(") Then
                    helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("(") - 1)))
                    'check & add info in SubLabel
                    test_trk_rest = media_trk_adinfo(e_title.Substring(e_title.IndexOf("("), e_title.Length - e_title.IndexOf("("))) & " ٠ "

                ElseIf e_title.Contains("-") Then
                    If e_title.Substring(0, (e_title.IndexOf("-"))).EndsWith(" ") = True Then
                        helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("-") - 1)))
                        'check & add info in SubLabel
                        test_trk_rest = media_trk_adinfo(e_title.Substring(e_title.IndexOf("-"), e_title.Length - e_title.IndexOf("-"))) & " ٠ "

                    Else
                        'Fuck
                        test_trk_rest = ""

                        If e_title.Length > 41 Then
                            helper_label(lbl_spotify, e_title.Remove(40, e_title.Length - 40) & "...")
                        Else
                            helper_label(lbl_spotify, e_title)
                        End If
                    End If

                Else
                    test_trk_rest = ""

                    If e_title.Length > 41 Then
                        helper_label(lbl_spotify, e_title.Remove(40, e_title.Length - 40) & "...")
                    Else
                        helper_label(lbl_spotify, e_title)
                    End If
                End If
            End If
        End If

        If Not media_trktype & e_artist & " -" & e_Tremaining = media_last_artist_time Or helper_label_gc(lbl_spotify_remaining) = " " Then
            media_last_artist_time = test_trk_rest & e_artist & " ٠ " & e_Tremaining
            wnd_flyout_media.str_media_time = e_pb_val & "%" & e_pb_max & "#" & e_Tremaining

            If Not media_widget_opened = 1 Then helper_label(lbl_spotify_remaining, media_last_artist_time)
        End If

        If trk_show_progess = True Then
            helper_progressBar(mpb_indicateLoading, e_pb_val, e_pb_max, e_playing)
        Else
            If mpb_indicateLoading.Visibility = Visibility.Visible Then helper_progressBar(mpb_indicateLoading, 0, 0, False)
        End If
    End Sub

    'MEDIA GRID UPDATE PART ---------------------------
    Private Function media_trk_adinfo(e As String) As String
        Dim e_fs As String = e

        'remove "(" at start and ")" end
        If e.StartsWith("(") And e.EndsWith(")") Then e_fs = e.Substring(1, e.Length - 2)
        'remove "-" at beginning
        If e.StartsWith("-") Then e_fs = e.Substring(2)

        If e_fs.Length > 30 And e_fs.StartsWith("(") Then
            If Not e_fs.Substring(1, 31).Contains(")") Then
                Return e_fs.Substring(1, 31) & "..."
            Else
                Return e_fs.Substring(0, 30) & "..."
            End If
        ElseIf e_fs.Length > 30 Then
            Return e_fs.Substring(0, 30) & "..."
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
    Private Sub lbl_spotify_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles icn_spotify.MouseWheel, lbl_spotify.MouseWheel, lbl_spotify_remaining.MouseWheel, grd_spotify.MouseWheel
        If spotify_skip_trackChange = True Then
            spotify_skip_trackChange = False
            Exit Sub
        End If

        'Scroll up > Next Track / Skip | Scroll down > Last Track / Return
        If e.Delta > 0 Then spotifyapi.Skip() Else spotifyapi.Previous()

        spotify_skip_trackChange = True
    End Sub

    Private Sub btn_spotify_playpause_Click(sender As Object, e As RoutedEventArgs) Handles icn_spotify.MouseUp, icn_run_spotify.MouseUp
        If sAPI_error = True Then
            sAPI_error_count = 0
            init_spotifyAPI()
            helper_image(icn_run_spotify, "pack://application:,,,/Resources/spotify_notification.png")
            helper_grid(grd_spotify, e_playing) 'grid is only visible if Spotify is playing something.
            helper_grid(grd_link, Not e_playing)
        Else
            ' PlayPause
            If e_playing = False Then spotifyapi.Play() Else spotifyapi.Pause()
        End If
    End Sub

    Private Sub icn_spotify_MouseEnter(sender As Object, e As MouseEventArgs) Handles icn_spotify.MouseEnter
        helper_image(icn_spotify, "pack://application:,,,/Resources/ic_pause_white_24dp.png")
    End Sub

    Private Sub icn_spotify_MouseLeave(sender As Object, e As MouseEventArgs) Handles icn_spotify.MouseLeave
        helper_image(icn_spotify, "pack://application:,,,/Resources/spotify_notification.png")
    End Sub
#End Region


#Region "nSpotify"
    '    'Dim evp_nSpotify As New EventProvider

    '    Private Sub nSpotify_init()
    '        'If Process.GetProcessesByName("Spotify").Length > 1 Then
    '        '    wnd_log.AddLine(log_cat & "-MEDIA", "Spotify running, loading spotify integration...")
    '        '    'EventHandler
    '        '    AddHandler evp_nSpotify.DataUpdated, AddressOf nSpotify_smthChanged
    '        '    'EventProvider starten
    '        '    evp_nSpotify.Start()
    '        'Else
    '        '    wnd_log.AddLine(log_cat & "-MEDIA", "Spotify not running, disabled integration")
    '        '    evp_nSpotify.Dispose()
    '        '    nSpotify_allowed = False
    '        'End If
    '    End Sub

    '    Dim nSpotify_lblInfo As String

    '    Dim nSpotify_playing As Boolean = False

    '    Dim nSpotify_cnt As Integer = 0
    '    Dim dbg_sptfy As String = ""
    '    Dim dbg_sptfy_2 As String = ""

    '    'Spotify Event Listener
    '    Private Sub nSpotify_smthChanged(sender As Object, e As DataUpdatedEventArgs)
    '        If nSpotify_allowed = False Then Exit Sub
    '        helper_grid(grd_spotify, e.CurrentStatus.Playing) 'grid is only visible if Spotify is playing something.

    '        helper_grid(grd_link, Not e.CurrentStatus.Playing)

    '        'Info is handled in another sub
    '        sui_media_update(e.CurrentStatus.Track.Name, e.CurrentStatus.Track.Artist, DateTime.MinValue.AddSeconds(e.CurrentStatus.Track.Length.TotalSeconds - e.CurrentStatus.PlayingPosition.TotalSeconds).ToString("m:ss"), e.CurrentStatus.PlayingPosition.TotalSeconds, e.CurrentStatus.Track.Length.TotalSeconds, e.CurrentStatus.Playing)

    '        dbg_sptfy = e.CurrentStatus.ToString
    '    End Sub

    'MEDIA GRID UPDATE PART FOLLOWING---------------------------

    '    'workaround > spotify wiederbeleben
    '    Private Sub lbl_spotify_remaining_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles lbl_spotify.MouseRightButtonUp, lbl_spotify_remaining.MouseRightButtonUp, icn_run_spotify.MouseRightButtonUp
    '        'evp_nSpotify.Stop()
    '        'evp_nSpotify.Start()
    '    End Sub
#End Region'_nSpotify is disabled


#Region "Media Widget/Flyout"
    Public Shared media_widget_opened As Integer = 0
    Dim flyout_media As New wnd_flyout_media

    Private Sub lbl_spotify_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles lbl_spotify.MouseLeftButtonUp, lbl_spotify_remaining.MouseLeftButtonUp
        If media_widget_opened = -1 Then flyout_media = New wnd_flyout_media

        If media_widget_opened = 1 Then
            flyout_media.Hide()
            media_widget_opened = 0
        Else
            flyout_media.Show()
            media_widget_opened = 1
            helper_label(lbl_spotify, "Spotify")
            helper_label(lbl_spotify_remaining, " ")
            Me.Topmost = False
            Me.Topmost = True
        End If
    End Sub
#End Region

#Region "GUI Helper"
    Private Sub helper_grid(ByVal ctrl As Grid, ByVal Optional e_visible As Boolean = Nothing)
        If e_visible = True Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background, New ThreadStart(Sub() ctrl.Visibility = Visibility.Visible))
        ElseIf e_visible = False Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background, New ThreadStart(Sub() ctrl.Visibility = Visibility.Hidden))
        End If
    End Sub

    Private Sub helper_label(ByVal ctrl As Label, ByVal Optional e_content As String = Nothing, ByVal Optional e_visible As Boolean = Nothing)
        If Not e_content = Nothing Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() ctrl.Content = e_content))
        End If

        If Not e_visible = Nothing Then
            If e_visible = True Then
                Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() ctrl.Visibility = Visibility.Visible))
            Else
                Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() ctrl.Visibility = Visibility.Hidden))
            End If
        End If
    End Sub

    Private Sub helper_image(ByVal ctrl As Controls.Image, ByVal Optional e_content As String = "<nothing>", ByVal Optional e_visible As Boolean = Nothing)
        If Not e_content = "<nothing>" Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background, New ThreadStart(Sub() ctrl.Source = CType(New ImageSourceConverter().ConvertFromString(e_content), ImageSource)))
        End If

        If Not e_visible = Nothing Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background, New ThreadStart(Sub()

                                                                                                                       If e_visible = True Then
                                                                                                                           ctrl.Visibility = Visibility.Visible
                                                                                                                       ElseIf e_visible = False Then
                                                                                                                           ctrl.Visibility = Visibility.Hidden
                                                                                                                       End If
                                                                                                                   End Sub))
        End If
    End Sub

    Private Sub helper_progressBar(ByVal ctrl As MahApps.Metro.Controls.MetroProgressBar, ByVal Optional e_value As Double = -1, ByVal Optional e_max As Double = -1, ByVal Optional e_visible As Boolean = Nothing)
        Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub()

                                                                                                               If e_visible = True Then
                                                                                                                   ctrl.Visibility = Visibility.Visible
                                                                                                               ElseIf e_visible = False Then
                                                                                                                   ctrl.Visibility = Visibility.Hidden
                                                                                                               End If

                                                                                                               If Not e_value = -1 Then ctrl.Value = e_value
                                                                                                               If Not e_max = -1 Then ctrl.Maximum = e_max

                                                                                                           End Sub))

    End Sub

    Private Function helper_label_gc(ByVal ctrl As Label) As String
        Dim tmp_str As String = ""
        Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() tmp_str = ctrl.Content.ToString))
        Return tmp_str
    End Function
#End Region

#Region "Network Monitoring"
    'Settings
    Dim net_allSpeeds As Boolean = False
    Dim net_textAllSpeeds As Boolean = False

    Dim net_monitoring_allowed As Integer = 0 '0 = disabled / 1 = enabled / -1 = error
    Dim net_monitoredInterface As NetworkInterface
    Dim settings_net_interface As String = ""

    'Update timer
    Private WithEvents tmr_network As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 500), .IsEnabled = False}
    Private Sub tmr_network_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_network.Tick
        If net_monitoring_allowed = -1 Then wnd_log.AddLine(log_cat & "-NET", "Network monitoring state: " & net_monitoring_allowed.ToString)
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

            wnd_log.AddLine(log_cat & "-NET", "Interface: " & net_monitoredInterface.Name)

            helper_grid(grd_network, True)
            net_monitoring_allowed = 1

        Catch
            wnd_log.AddLine(log_cat & "-NET", "Error in 'net_get_interfaces'")
            net_monitoring_allowed = -1
            helper_grid(grd_network, False)
        End Try
    End Sub

    'actual net monitoring:
    Dim net_stat_bSent As Int64
    Dim net_stat_bReceived As Int64

    Dim net_stat_bSent_speed As Integer
    Dim net_stat_bReceived_speed As Integer

    Private Sub net_monitoring()
        If net_monitoring_allowed = 0 Then
            wnd_log.AddLine(log_cat & "-NET", "Network monitoring disabled, GoTo 'net_get_interfaces'")

            net_get_interfaces()
            ' helper_grid(grd_network, False)
            Exit Sub

        ElseIf net_monitoring_allowed = -1 Then 'Exit sub if netmon got an error
            wnd_log.AddLine(log_cat & "-NET", "Network monitoring error, exiting")
            helper_grid(grd_network, False)
            Exit Sub
        End If

        Dim net_NIC_statistic As IPInterfaceStatistics ' = net_monitoredInterface.GetIPStatistics()

        Try
            net_NIC_statistic = net_monitoredInterface.GetIPStatistics()
        Catch ex As Exception
            'Me.net_get_interfaces()
            Exit Sub
        End Try

        If net_monitoredInterface.OperationalStatus = OperationalStatus.Up Then
            helper_image(icn_network_state, "pack://application:,,,/Resources/ic_lan_connected.png")
        Else
            helper_image(icn_network_state, "pack://application:,,,/Resources/ic_ethernet_cable_off_white_21px.png")
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

                Me.lbl_network_traffic_send.Content = Nothing
                Me.icn_network_send.Visibility = Visibility.Hidden

                Me.lbl_network_traffic_receive.Content = Nothing
                Me.icn_network_receive.Visibility = Visibility.Hidden

                wnd_log.AddLine(log_cat & "-NET", "Error in 'get sent/received kbytes'")
                Exit Sub
            End Try
        End If

        net_stat_bSent = net_NIC_statistic.BytesSent
        net_stat_bReceived = net_NIC_statistic.BytesReceived

        'visualize sent v2
        If net_stat_bSent_speed > 0 Then
            If net_allSpeeds = True Or net_stat_bSent_speed > 4096 Then
                Me.icn_network_send.Visibility = Visibility.Visible
                If net_stat_bSent_speed > 51200 Or net_textAllSpeeds = True Then Me.lbl_network_traffic_send.Content = get_formatted_bytes(net_stat_bSent_speed)
            Else
                Me.lbl_network_traffic_send.Content = Nothing
                Me.icn_network_send.Visibility = Visibility.Hidden
            End If

        Else
            Me.lbl_network_traffic_send.Content = Nothing
            Me.icn_network_send.Visibility = Visibility.Hidden
        End If

        'visualize received v2
        If net_stat_bReceived_speed > 0 Then
            If net_allSpeeds = True Or net_stat_bReceived_speed > 4096 Then
                Me.icn_network_receive.Visibility = Visibility.Visible
                If net_stat_bReceived_speed > 51200 Or net_textAllSpeeds = True Then Me.lbl_network_traffic_receive.Content = get_formatted_bytes(net_stat_bReceived_speed)
            Else
                Me.lbl_network_traffic_receive.Content = Nothing
                Me.icn_network_receive.Visibility = Visibility.Hidden
            End If

        Else
            Me.lbl_network_traffic_receive.Content = Nothing
            Me.icn_network_receive.Visibility = Visibility.Hidden
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

    'positioning labels - left
    Private Sub lbl_weather_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_weather.SizeChanged, icn_weather.SizeChanged, grd_weather.SizeChanged, grd_spotify.SizeChanged
        If grd_weather.Visibility = Visibility.Visible Then
            grd_spotify.Margin = New Thickness(grd_weather.RenderSize.Width, 0, 0, 0)
        Else
            grd_spotify.Margin = New Thickness(3, 0, 0, 0)
        End If
    End Sub

#Region "Weather"
    Dim weather_flyout As New wnd_flyout_weather
    Private Sub icn_weather_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles lbl_weather.MouseUp, icn_weather.MouseUp
        weather_flyout.Show()
    End Sub

    Dim wData_enabled As Boolean = True
    Dim icn_basePath As String = System.AppDomain.CurrentDomain.BaseDirectory & "\Resources\wIcons\"

    Private wConf_API_cityID As Integer = 0
    Private wConf_API_key As String = ""
    Dim oww_xml As XmlDocument

    'Dim oww_data_location As String = ""
    'Dim oww_data_country As String = ""

    Dim oww_data_Tmin As String = "0"
    Dim oww_data_Tmax As String = "0"
    Public Shared oww_data_temp As String = "0"
    Public Shared oww_data_humidity As Integer = 0
    Public Shared oww_data_pressure As Integer = 0
    Public Shared oww_data_windspeed As Integer = 0
    'Dim oww_data_windcondition As String = ""
    Public Shared oww_data_winddirection As String = ""
    Dim oww_data_conditionID As Integer = 0
    Public Shared oww_data_condition As String = ""

    'OpenWeather API
    'DATA PATH -> (path_cache & "weather\oww_data.xml")
    Dim path_cache_weather As String = AppDomain.CurrentDomain.BaseDirectory & "cache\weather\"

    Private Async Sub oww_update(ByVal Optional e_force_oww As Boolean = False)
        If wData_enabled = False Then Exit Sub
        Dim oww_API_error As Boolean = False

        'API Update
        'request weather data
        Dim oww_api_request As WebRequest = WebRequest.Create("http://api.openweathermap.org/data/2.5/weather?id=" & wConf_API_cityID & "&APPID=" & wConf_API_key & "&mode=xml&units=metric&lang=DE")
        Dim oww_api_respone As WebResponse
        oww_xml = New XmlDocument()

        Try
            'server response
            oww_api_respone = Await oww_api_request.GetResponseAsync()

        Catch ex As WebException
            oww_api_respone = Nothing
            oww_API_error = True

            If ex.Status = WebExceptionStatus.ProtocolError Then
                wnd_log.AddLine(log_cat & "-WEATHER", "OWW (401 Zugriff verweigert)")
            Else
                wnd_log.AddLine(log_cat & "-WEATHER", "OWW unknown Error")
            End If
        End Try

        If oww_API_error = False Then
            If Not IO.Directory.Exists(path_cache_weather) Then IO.Directory.CreateDirectory(path_cache_weather)
            Dim oww_api_dataStream As Stream = oww_api_respone.GetResponseStream()
            ' Open the stream using a StreamReader for easy access.
            Dim oww_api_reader As New StreamReader(oww_api_dataStream)
            'Load XML from reader
            oww_xml.LoadXml(oww_api_reader.ReadToEnd())
            'save xml, because we can update only after 10mins
            oww_xml.PreserveWhitespace = True
            oww_xml.Save(path_cache_weather & "oww_data.xml")

            wData_lastUpdate = DateTime.Now
            ini.WriteValue("Weather", "last_update", Date.Now.ToShortTimeString)
            wnd_log.AddLine(log_cat & "-WEATHER", "OWW data updated")
        Else
            wnd_log.AddLine(log_cat & "-WEATHER", "OWW API-Error")
        End If
        'End API Update

        'Process new Data
        If File.Exists(path_cache_weather & "oww_data.xml") Then
            Dim XMLReader As Xml.XmlReader = New Xml.XmlTextReader(path_cache_weather & "oww_data.xml")

            ' Es folgt das Auslesen der XML-Datei 
            Dim xmlid As String = ""
            With XMLReader
                Do While .Read ' Es sind noch Daten vorhanden 
                    ' Welche Art von Daten liegt an? 
                    If .NodeType = Xml.XmlNodeType.Element Then

                        xmlid = .Name
                        'If xmlid = "country" Then oww_data_country = .ReadElementContentAsString

                        ' Ein Element 
                        If .AttributeCount > 0 Then
                            ' Es sind noch weitere Attribute vorhanden 
                            While .MoveToNextAttribute ' nächstes 
                                'If xmlid = "city" And .Name = "name" Then oww_data_location = .Value
                                If xmlid = "temperature" And .Name = "value" Then oww_data_temp = .Value.Remove(.Value.Length - 1, 1)
                                If xmlid = "temperature" And .Name = "max" Then oww_data_Tmax = .Value.Remove(.Value.Length - 1, 1)
                                If xmlid = "temperature" And .Name = "min" Then oww_data_Tmin = .Value.Remove(.Value.Length - 1, 1)
                                If xmlid = "humidity" And .Name = "value" Then oww_data_humidity = CInt(.Value)
                                If xmlid = "pressure" And .Name = "value" Then oww_data_pressure = CInt(.Value)
                                If xmlid = "wind" And .Name = "value" Then oww_data_windspeed = CInt(.Value)
                                'If xmlid = "wind" And .Name = "name" Then oww_data_windcondition = .Value
                                If xmlid = "direction" And .Name = "code" Then oww_data_winddirection = .Value
                                If xmlid = "weather" And .Name = "value" Then oww_data_condition = .Value
                                If xmlid = "weather" And .Name = "number" Then oww_data_conditionID = CInt(.Value)

                            End While
                        End If
                    End If
                Loop  ' Weiter nach Daten schauen 
                .Close()  ' XMLTextReader schließen 
            End With
        Else

        End If

        'DEBUG:  Show Wdata after Update
        'MessageBox.Show("City: " & oww_data_location & " // Country: " & oww_data_country & vbCrLf & "Temp: " & oww_data_temp & "// Tmin: " &
        'oww_data_Tmin & " // Tmax: " & oww_data_Tmax & vbCrLf & "Humidity: " & oww_data_humidity & vbCrLf & "Pressure: " & oww_data_pressure &
        '                        "Wind Speed: " & oww_data_windspeed & " // Wind Condition: " & oww_data_windcondition & " // Winddirection: " & oww_data_winddirection & vbCrLf &
        '"Weather txt: " & oww_data_condition & " // Weather Code: " & oww_data_conditionID)

        Select Case oww_data_conditionID
            'Conditions as described at: http://openweathermap.org/weather-conditions

            Case 200 To 232 'Thunderstorm / Gewitter
                helper_image(icn_weather, icn_basePath & "7.png")

            Case 300 To 310 'Drizzle / Nieselregen
                helper_image(icn_weather, icn_basePath & "2.png")

            Case 311 To 321
                helper_image(icn_weather, icn_basePath & "6.png")

            Case 500 To 531 'Rain / Regen
                helper_image(icn_weather, icn_basePath & "8.png")

            Case 600 'Light Snow
                helper_image(icn_weather, icn_basePath & "9.png")

            Case 601 To 622 'Snow / Schnee
                helper_image(icn_weather, icn_basePath & "10.png")

            Case 700 To 781 'Atmosphere 
                helper_image(icn_weather, icn_basePath & "1.png")

            Case 800 'Clear / Klar

                If DateTime.Now.Hour > 20 Or DateTime.Now.Hour < 6 Then
                    helper_image(icn_weather, icn_basePath & "5n.png")
                Else
                    helper_image(icn_weather, icn_basePath & "5.png")
                End If

            Case 801 To 802
                If DateTime.Now.Hour > 20 Or DateTime.Now.Hour < 6 Then
                    helper_image(icn_weather, icn_basePath & "3n.png")
                Else
                    helper_image(icn_weather, icn_basePath & "3.png")
                End If

            Case 803 To 804 'Cloudy / Wolkig
                helper_image(icn_weather, icn_basePath & "4.png")

            Case 900 To 906 'Extreme / Extremes Unwetter
                helper_image(icn_weather, icn_basePath & "0.png")

            Case 951 To 962 'Additional / Extra
                helper_image(icn_weather, icn_basePath & "0.png")

            Case Else
                helper_image(icn_weather, icn_basePath & "0.png")
                wnd_log.AddLine(log_cat & "-WEATHER", "No condition icon (" & oww_data_conditionID & ")")
        End Select

        If wData_lastUpdate = Nothing Then
            DateTime.TryParse(ini.ReadValue("Weather", "last_update", ""), wData_lastUpdate)
        End If

        'Wetter.com Data - Normally not used because we have no permission to use it.

        If wConf_useWcom = False Then
            Me.lbl_weather.Content = oww_data_temp & "°"
        End If

    End Sub

    Public Shared wData_temp As String
    Public Shared wData_condition As String
    Public Shared wData_humidity As String
    Public Shared wData_rain As String
    Public Shared wData_airPressure As String
    Public Shared wData_windDir As String
    Public Shared wData_windSpeed As String

    Public Shared wData_lastUpdate As Date
    Public Shared WData_wcom_lastUpdate As Date
    Private wData_updateWA As Integer

    Private Async Sub wcom_update(ByVal Optional e_station As Integer = 7704, ByVal Optional e_failed As Boolean = False)
        'Quelltext herunterladen
        Dim wStationData_request As WebRequest = WebRequest.Create("http://netzwerk.wetter.com/api/stationdata/" & e_station & "/1/")
        Dim wStationData_respone As WebResponse = Await wStationData_request.GetResponseAsync()

        ' Get the stream containing content returned by the server.
        Dim wStationData_dataStream As Stream = wStationData_respone.GetResponseStream()
        ' Open the stream using a StreamReader for easy access.
        Dim wStationData_reader As New StreamReader(wStationData_dataStream)
        ' Read the content.
        Dim wStationData_data As String = wStationData_reader.ReadToEnd()

        Dim int As Integer = 0
        For Each data_set As String In wStationData_data.Split(CType("}", Char()))
            int += 1
        Next
        'Example string:    "1464900000":{"hu":90,"te":17.7,"dp":15.6,"pr":1011.9,"pa":null,"ws":0.2,"wd":135

        If wStationData_data.Length < 10 Then
            wcom_error(e_station)
            Exit Sub
        Else
            wnd_log.AddLine(log_cat & "-WEATHER", "WCOM data updated")
        End If

        Dim sSplit As String() = wStationData_data.Split(CType("}", Char()))
        Dim ssSplit As String() = sSplit(int - 3).ToString.Split(CType(":", Char()))

        'temperature
        If ssSplit(3).Substring(0, ssSplit(3).Length - 5) = "null" Then
            wData_temp = "--°"
        Else
            wData_temp = ssSplit(3).Substring(0, ssSplit(3).Length - 5) & "°"
        End If

        'Windspeed
        If ssSplit(7).Substring(0, ssSplit(7).Length - 5) = "null" Then
            wData_windSpeed = "0 km/h"
        Else
            wData_windSpeed = CDbl((ssSplit(7).Substring(0, ssSplit(7).Length - 5))) * 3.6 / 10 & " km/h"
        End If

        'humidity
        If ssSplit(2).Substring(0, ssSplit(2).Length - 5) = "null" Then
            wData_humidity = "N/A"
        Else
            wData_humidity = ssSplit(2).Substring(0, ssSplit(2).Length - 5) & "%"
        End If

        'wind direction
        wData_windDir = ssSplit(8) & "°"
        'wind speed
        If (ssSplit(7).Substring(0, ssSplit(7).Length - 5)) = "null" Then
            wData_windSpeed = "N/A"
        Else
            wData_windSpeed = CDbl((ssSplit(7).Substring(0, ssSplit(7).Length - 5))) * 3.6 / 10 & " km/h"
        End If
        'rain
        wData_rain = ssSplit(6).Substring(0, ssSplit(6).Length - 5) & " l/qm"
        'air pressure
        wData_airPressure = ssSplit(5).Substring(0, ssSplit(5).Length - 5) & " hPa"

        Me.lbl_weather.Content = wData_temp

        wStationData_reader.Close()
        wStationData_respone.Close()

        wData_updateWA = DateTime.Now.Minute
        WData_wcom_lastUpdate = DateTime.Now
    End Sub

    Private Sub wcom_error(e_station As Integer)
        If e_station = 16549 Then
            wnd_log.AddLine(log_cat & "-WEATHER", "WCOM API-Error with Station 16549 - Fallback to OpenWeather")
            Me.lbl_weather.Content = oww_data_temp.ToString & "°"
            Exit Sub
        End If

        wnd_log.AddLine(log_cat & "-WEATHER", "WCOM API-Error with Station 7704, trying 16549")
        wcom_update(16549)
    End Sub
#End Region

#Region "LINK GRID"
    Private Sub grd_link_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles grd_link.IsVisibleChanged
        If CBool(e.NewValue) = True Then
            grd_weather.Margin = New Thickness(25, 0, 0, 0)
        Else
            grd_weather.Margin = New Thickness(1.5, 0, 0, 0)
        End If
    End Sub

    'Icon : SpotifyLink
    Private Sub icn_run_spotify_MouseEnter(sender As Object, e As MouseEventArgs) Handles icn_run_spotify.MouseEnter
        helper_image(icn_run_spotify, "pack://application:,,,/Resources/ic_play_arrow_white_24dp.png")
    End Sub

    Private Sub icn_run_spotify_MouseLeave(sender As Object, e As MouseEventArgs) Handles icn_run_spotify.MouseLeave
        helper_image(icn_run_spotify, "pack://application:,,,/Resources/spotify_notification.png")
    End Sub

    Dim ui_appmenu As New wnd_flyout_appmenu
    Private Sub btn_exit_Click(sender As Object, e As RoutedEventArgs) Handles icn_menu.MouseLeftButtonUp, grd_menu_right.MouseLeftButtonUp
        ui_appmenu.Show()
    End Sub
#End Region

End Class
