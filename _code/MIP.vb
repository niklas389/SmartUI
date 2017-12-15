
'If Not e_title = media_last_title Or wpf_helper.helper_label_gc(lbl_spotify) = "Spotify" Then
'    media_last_title = e_title

'    If Not media_widget_opened = 1 Then

'        If e_title.Contains("(") And Not e_title.StartsWith("(") Then
'            wpf_helper.helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("(") - 1)))
'            'check & add info in SubLabel
'            test_trk_rest = media_trk_adinfo(e_title.Substring(e_title.IndexOf("("), e_title.Length - e_title.IndexOf("("))) & " ٠ "

'        ElseIf e_title.Contains("-") Then
'            If e_title.Substring(0, (e_title.IndexOf("-"))).EndsWith(" ") = True Then
'                wpf_helper.helper_label(lbl_spotify, e_title.Substring(0, (e_title.IndexOf("-") - 1)))
'                'check & add info in SubLabel
'                test_trk_rest = media_trk_adinfo(e_title.Substring(e_title.IndexOf("-"), e_title.Length - e_title.IndexOf("-"))) & " ٠ "

'            Else
'                'Fuck
'                test_trk_rest = ""

'                If e_title.Length > 41 Then
'                    wpf_helper.helper_label(lbl_spotify, e_title.Remove(40, e_title.Length - 40) & "...")
'                Else
'                    wpf_helper.helper_label(lbl_spotify, e_title)
'                End If
'            End If

'        Else
'            test_trk_rest = ""

'            If e_title.Length > 41 Then
'                wpf_helper.helper_label(lbl_spotify, e_title.Remove(40, e_title.Length - 40) & "...")
'            Else
'                wpf_helper.helper_label(lbl_spotify, e_title)
'            End If
'        End If
'    End If
'End If



'If Not e_artist & " -" & e_Tremaining = media_last_artist_time Or wpf_helper.helper_label_gc(lbl_spotify_remaining) = " " Then
'    media_last_artist_time = test_trk_rest & e_artist & " ٠ " & e_Tremaining

'    If media_widget_opened = 0 Then
'        wpf_helper.helper_label(lbl_spotify_remaining, media_last_artist_time)
'    Else
'        wnd_flyout_media.str_media_time = e_pb_val & "%" & e_pb_max & "#" & e_Tremaining
'    End If
'End If


'OTHER THINGS .NET SPOTIFY API
'    'advertLabel.Text = If(track.IsAd(), "ADVERT", "")
'    bigAlbumPicture.Image = Await track.GetAlbumArtAsync(AlbumArtSize.Size640)
'    smallAlbumPicture.Image = Await track.GetAlbumArtAsync(AlbumArtSize.Size160)
'    'volumeLabel.Text = (e.NewVolume * 100).ToString(CultureInfo.InvariantCulture)
