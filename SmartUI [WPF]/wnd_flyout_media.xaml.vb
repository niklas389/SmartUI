Imports System
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Input
Imports System.Windows.Media

Public Class wnd_flyout_media

#Region "Window"
    Private Sub wnd_flyout_media_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.Top = My.Computer.Screen.WorkingArea.Top - 25
        Me.Left = My.Computer.Screen.WorkingArea.Left
    End Sub

    Private Sub wnd_flyout_media_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Visible Then
            update_widget()
            tmr_update_trackdata.Start()
        Else
            tmr_update_trackdata.Stop()
        End If
    End Sub

    Private Sub wnd_flyout_volume_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave, Me.LostFocus
        Me.Hide()
        MainWindow.media_widget_opened = 0
    End Sub
#End Region

    Public Shared str_media_time As String = ""

    'Update timer
    Private WithEvents tmr_update_trackdata As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 250), .IsEnabled = False}
    Private Sub tmr_update_trackdata_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_update_trackdata.Tick
        update_widget()
    End Sub

    Private Sub update_widget()
        Try
            If Not lbl_trk_title.Content.ToString = short_string(MainWindow._currentTrack.TrackResource.Name, True) Then
                'Update TRACK data
                lbl_trk_title.Content = short_string(MainWindow._currentTrack.TrackResource.Name, True)
                lbl_trk_artist.Content = short_string(MainWindow._currentTrack.ArtistResource.Name)
                lbl_trk_album.Content = short_string(MainWindow._currentTrack.AlbumResource.Name)

                'update album art
                media_cache_albumArt()
            End If

            lbl_trk_time_elapsed.Content = DateTime.MinValue.AddSeconds(CDbl(str_media_time.Substring(0, str_media_time.IndexOf("%")))).ToString("m:ss")
            lbl_trk_time_remaining.Content = str_media_time.Substring(str_media_time.IndexOf("#") + 1, (str_media_time.Length - str_media_time.IndexOf("#") - 1))

            pb_trk_progress.Value = CDbl(str_media_time.Substring(0, str_media_time.IndexOf("%")))
            pb_trk_progress.Maximum = CDbl(str_media_time.Substring(str_media_time.IndexOf("%") + 1, (str_media_time.IndexOf("#") - str_media_time.IndexOf("%") - 1)))

            If MainWindow.e_playing = True Then
                btn_media_play.Source = CType(New ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/ic_pause_white_24dp.png"), ImageSource)
            Else
                btn_media_play.Source = CType(New ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/ic_play_arrow_white_24dp.png"), ImageSource)
            End If

        Catch ex As Exception
            Me.Hide()
            MainWindow.media_widget_opened = 0

        End Try
    End Sub


#Region "Cover & String Shortener"
    Dim cache_path As String = AppDomain.CurrentDomain.BaseDirectory & "cache\media\"

    Private Async Sub media_cache_albumArt(ByVal Optional err As Boolean = False)
        'Show loading ani
        grd_loading.Visibility = Visibility.Visible
        pr_loading.IsActive = True
        albumCover_overlay(True)

        'MessageBox.Show(MainWindow._currentTrack.TrackResource.ParseUri.ToString)
        Dim trk_uri As String = MainWindow._currentTrack.TrackResource.ParseUri.ToString.Remove(0, 14)

        Try
            If Not IO.Directory.Exists(cache_path) Then IO.Directory.CreateDirectory(cache_path)

            'get URI before DL image to avoid mismatching info (eg.: user changes track while downloading)

            If Not IO.File.Exists(cache_path & trk_uri) Then
                'Construct a bitmap
                Dim img As New Bitmap(Await Task.Run(Function() MainWindow._currentTrack.GetAlbumArtAsync(SpotifyAPI.Local.Enums.AlbumArtSize.Size320))) 'cover DL
                img.Save(cache_path & trk_uri, Imaging.ImageFormat.Jpeg) 'save cover

                img.Dispose()
            End If

            img_albumCover.Source = (Await Task.Run(Function() CType(New ImageSourceConverter().ConvertFromString(cache_path & trk_uri), ImageSource)))
            img_bg.Source = img_albumCover.Source

            img_cover_error.Visibility = Visibility.Hidden

        Catch ex As Exception
            img_albumCover.Source = CType(New ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/mediaservice_albums.png"), ImageSource)
            img_bg.Source = CType(New ImageSourceConverter().ConvertFromString(AppDomain.CurrentDomain.BaseDirectory & "Resources\no_cover.jpg"), ImageSource)
            img_cover_error.Visibility = Visibility.Visible
            img_cover_error.ToolTip = "Wir hatten bei diesem Titel probleme das Cover abzurufen." & NewLine & "Prüfe deine Internetverbindung und probiere es noch einmal."
            If IO.File.Exists(cache_path & trk_uri) And err = False Then media_cache_albumArt(True)
        End Try

        'hide loading ani
        grd_loading.Visibility = Visibility.Hidden
        pr_loading.IsActive = False
        albumCover_overlay(False, True)
    End Sub

    'String shortener
    Private Function short_string(e_str As String, Optional ByVal e_vShort As Boolean = False) As String
        Try
            If e_vShort = False Then
                If MainWindow._currentTrack.TrackResource.Name.Length > 51 Then
                    Return e_str.Remove(50, e_str.Length - 50) & "..."
                Else
                    Return e_str
                End If
            Else
                If MainWindow._currentTrack.TrackResource.Name.Length > 36 Then

                    Return e_str.Remove(35, e_str.Length - 35) & "..."
                Else
                    Return e_str
                End If
            End If
        Catch ex As Exception
            Return e_str

        End Try
    End Function
#End Region 'ReWork of Local Track needed!

#Region "Buttons"
    'Spotify Change Track 
    <DllImport("user32")>
    Private Shared Sub keybd_event(bVk As Byte, bScan As Byte, dwFlags As UInteger, dwExtraInfo As Integer)
    End Sub
    Private Const KEYEVENTF_EXTENDEDKEY As Integer = &H1
    Private Const KEYEVENTF_KEYUP As Integer = &H2
    Private Const VK_MEDIA_NEXT_TRACK As Byte = &HB0
    Private Const VK_MEDIA_PREV_TRACK As Byte = &HB1
    Private Const VK_MEDIA_PLAY_PAUSE As Byte = &HB3

    Private Sub btn_media_play_Click(sender As Object, e As RoutedEventArgs) Handles btn_media_play.MouseLeftButtonUp
        ' PlayPause
        keybd_event(VK_MEDIA_PLAY_PAUSE, &H45, KEYEVENTF_EXTENDEDKEY, 0)
        keybd_event(VK_MEDIA_PLAY_PAUSE, &H45, KEYEVENTF_EXTENDEDKEY Or KEYEVENTF_KEYUP, 0)
    End Sub

    Private Sub btn_media_next_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles btn_media_next.MouseLeftButtonUp
        'Next Track / Skip
        keybd_event(VK_MEDIA_NEXT_TRACK, &H45, KEYEVENTF_EXTENDEDKEY, 0)
        keybd_event(VK_MEDIA_NEXT_TRACK, &H45, KEYEVENTF_EXTENDEDKEY Or KEYEVENTF_KEYUP, 0)

        update_widget()
    End Sub

    Private Sub btn_media_prev_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles btn_media_prev.MouseLeftButtonUp
        'Last Track / Return
        keybd_event(VK_MEDIA_PREV_TRACK, &H45, KEYEVENTF_EXTENDEDKEY, 0)
        keybd_event(VK_MEDIA_PREV_TRACK, &H45, KEYEVENTF_EXTENDEDKEY Or KEYEVENTF_KEYUP, 0)

        update_widget()
    End Sub
#End Region

    Private Sub img_albumCover_MouseEnter(sender As Object, e As MouseEventArgs) Handles img_albumCover.MouseEnter, img_cover_error.MouseEnter
        albumCover_overlay(False)
    End Sub

    Private Sub img_albumCover_MouseLeave(sender As Object, e As MouseEventArgs) Handles grd_loading.MouseLeave
        albumCover_overlay(False, True)
    End Sub

    Private Sub img_albumCover_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles grd_loading.MouseUp
        Diagnostics.Process.Start(MainWindow._currentTrack.TrackResource.ParseUri.ToString)
    End Sub

    Private Sub albumCover_overlay(e_loading As Boolean, Optional hidden As Boolean = False)
        If hidden = True Then
            grd_loading.Visibility = Visibility.Hidden
            pr_loading.IsActive = False
            Exit Sub
        Else
            grd_loading.Visibility = Visibility.Visible
        End If

        If e_loading = True Then
            pr_loading.IsActive = True
            pr_loading.Visibility = Visibility.Visible
            ic_overlay_openExt.Visibility = Visibility.Hidden
            lbl_overlay_openExt.Visibility = Visibility.Hidden
        Else
            pr_loading.IsActive = False
            pr_loading.Visibility = Visibility.Hidden
            ic_overlay_openExt.Visibility = Visibility.Visible
            lbl_overlay_openExt.Visibility = Visibility.Visible
        End If
    End Sub
End Class
