Imports System
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Animation

Public Class wnd_flyout_media
    Dim hda As Boolean = False

#Region "Window"
    Private Sub wnd_flyout_media_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.Top = My.Computer.Screen.WorkingArea.Top - 25
        Me.Left = My.Computer.Screen.WorkingArea.Left
        pr_loading.IsActive = False
    End Sub

    Private Sub wnd_flyout_media_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Visible Then
            update_widget()
            tmr_update_trackdata.Start()
            anim_slidein()
        End If
    End Sub

    Private Sub wnd_flyout_volume_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave, Me.LostFocus
        If hda = False Then Exit Sub
        MainWindow.media_widget_opened = 0
        MainWindow.media_newtrack = True
        anim_slideout()
    End Sub

    Private Sub anim_slidein()
        Dim dblanim As New DoubleAnimation With {
            .From = -175,
            .To = 0,
            .AutoReverse = False,
            .Duration = TimeSpan.FromSeconds(0.5),
            .EasingFunction = New QuarticEase
        }

        Dim storyboard As New Storyboard()
        Storyboard.SetTarget(dblanim, Me)
        Storyboard.SetTargetProperty(dblanim, New PropertyPath(Window.TopProperty))

        'AddHandler dblanim.Completed, AddressOf dblanim_Completed

        storyboard.Children.Add(dblanim)
        storyboard.Begin(Me)

        hda = True
    End Sub

    Private Sub anim_slideout()
        hda = False

        Dim dblanim As New DoubleAnimation With {
            .From = 0,
            .To = -175,
            .AutoReverse = False,
            .Duration = TimeSpan.FromSeconds(0.5),
            .EasingFunction = New QuarticEase
        }

        Dim storyboard As New Storyboard()
        Storyboard.SetTarget(dblanim, Me)
        Storyboard.SetTargetProperty(dblanim, New PropertyPath(Window.TopProperty))

        AddHandler dblanim.Completed, AddressOf dblanim_Completed

        storyboard.Children.Add(dblanim)
        storyboard.Begin(Me)
    End Sub

    Private Sub dblanim_Completed(sender As Object, e As EventArgs)
        Me.Hide()
        tmr_update_trackdata.Stop()
    End Sub

#End Region

#Region "MEDIA"
    Public Shared str_media_time As String = ""

    'Update timer
    Private WithEvents tmr_update_trackdata As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 250), .IsEnabled = False}
    Private Sub tmr_update_trackdata_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_update_trackdata.Tick
        update_widget()
    End Sub

    Dim uw_err As Boolean = False
    Private Sub update_widget()
        Try
            If Not lbl_trk_title.Content.ToString = short_string(MainWindow._currentTrack.TrackResource.Name, True) Then
                'Update TRACK data
                lbl_trk_album.Content = Nothing
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

            uw_err = False
        Catch ex As Exception
            If uw_err = True Then
                hda = False
                anim_slideout()
                MainWindow.media_widget_opened = 0
                MainWindow.wnd_log.AddLine("ERR" & "-MFLYOUT", "update_widget - " & ex.Message)
            End If

            media_cache_albumArt()
            uw_err = True
        End Try

    End Sub

#End Region

#Region "Cover & String Shortener"
    Dim cache_path As String = AppDomain.CurrentDomain.BaseDirectory & "cache\media\"

    Private Async Sub media_cache_albumArt(ByVal Optional err As Boolean = False)
        albumCover_overlay(True)        'Show loading ani
        Dim trk_uri As String = ""

        Try
            trk_uri = MainWindow._currentTrack.TrackResource.ParseUri.ToString.Remove(0, 14)

            If Not IO.Directory.Exists(cache_path) Then IO.Directory.CreateDirectory(cache_path)

            'get URI before DL image to avoid mismatching info (eg.: track changes while downloading)
            If Not (Await Task.Run(Function() IO.File.Exists(cache_path & trk_uri))) Then '#?
                'Construct a bitmap
                Dim img As New Bitmap(Await Task.Run(Function() MainWindow._currentTrack.GetAlbumArtAsync(SpotifyAPI.Local.Enums.AlbumArtSize.Size320))) 'cover DL
                img.Save(cache_path & trk_uri, Imaging.ImageFormat.Jpeg) 'cache cover as jpeg
                img.Dispose()
            End If

            img_albumCover.Source = (Await Task.Run(Function() CType(New ImageSourceConverter().ConvertFromString(cache_path & trk_uri), ImageSource)))
            img_bg.Source = img_albumCover.Source
            img_cover_error.Visibility = Visibility.Hidden

        Catch ex As Exception
            img_albumCover.Source = CType(New ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/mediaservice_albums.png"), ImageSource)
            img_bg.Source = CType(New ImageSourceConverter().ConvertFromString(AppDomain.CurrentDomain.BaseDirectory & "Resources\no_cover.jpg"), ImageSource)
            img_cover_error.Visibility = Visibility.Visible
            img_cover_error.ToolTip = "Wir hatten bei diesem Titel probleme das Cover abzurufen." & NewLine & "Versuche es später nochmal."
            If IO.File.Exists(cache_path & trk_uri) And err = False Then media_cache_albumArt(True)

            MainWindow.wnd_log.AddLine("ERR" & "-MFLYOUT", "media_cache_albumArt - " & ex.Message)
        End Try

        albumCover_overlay(False, True)        'hide loading ani
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
#End Region

#Region "Buttons"
    ' PlayPause
    Private Sub btn_media_play_Click(sender As Object, e As RoutedEventArgs) Handles btn_media_play.MouseLeftButtonUp
        If MainWindow.e_playing = True Then
            MainWindow.spotifyapi.Pause()
        Else
            MainWindow.spotifyapi.Play()
        End If
    End Sub

    'Next Track / Skip
    Private Sub btn_media_next_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles btn_media_next.MouseLeftButtonUp
        MainWindow.spotifyapi.Skip()
    End Sub

    'Last Track / Return
    Private Sub btn_media_prev_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles btn_media_prev.MouseLeftButtonUp
        MainWindow.spotifyapi.Previous()
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
