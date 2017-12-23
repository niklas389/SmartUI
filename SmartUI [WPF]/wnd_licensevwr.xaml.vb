Imports System
Imports System.IO
Imports System.Windows
Imports System.Windows.Media.Animation

Public Class wnd_licensevwr

#Region "Window CMD"
    'Move Window
    Private Sub lbl_header_MouseDown(sender As Object, e As Input.MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_Click(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        anim_slideout()
    End Sub
#End Region

#Region "ANIMATION"
    Private Sub anim_slidein()

        Dim dblanim As New DoubleAnimation()
        dblanim.From = -0
        dblanim.To = 1
        dblanim.AutoReverse = False
        dblanim.Duration = TimeSpan.FromSeconds(0.5)
        dblanim.EasingFunction = New QuarticEase

        Dim storyboard As New Storyboard()
        Storyboard.SetTarget(dblanim, Me)
        Storyboard.SetTargetProperty(dblanim, New PropertyPath(Window.OpacityProperty))

        AddHandler dblanim.Completed, AddressOf dblanim_Completed_2

        storyboard.Children.Add(dblanim)
        storyboard.Begin(Me)
    End Sub

    Private Sub dblanim_Completed_2(sender As Object, e As EventArgs)
        If File.Exists(lic_path) Then
            Dim docpath As String = lic_path
            Dim range As Documents.TextRange
            Dim fStream As FileStream

            If File.Exists(docpath) Then
                Try
                    'Open the document in the RichTextBox.
                    range = New Documents.TextRange(rtb_license.Document.ContentStart, rtb_license.Document.ContentEnd)
                    fStream = New FileStream(docpath, FileMode.Open)
                    range.Load(fStream, DataFormats.Text)
                    fStream.Close()

                Catch generatedExceptionName As System.Exception
                    rtb_license.Document.ContentStart.InsertTextInRun("Lizenz konnte nicht geladen werden")
                End Try
            End If
        Else
            rtb_license.Document.ContentStart.InsertTextInRun("Lizenz nicht verfügbar")
        End If

    End Sub

    Private Sub anim_slideout()

        Dim dblanim As New DoubleAnimation()
        dblanim.From = 1
        dblanim.To = 0
        dblanim.AutoReverse = False
        dblanim.Duration = TimeSpan.FromSeconds(0.5)
        dblanim.EasingFunction = New QuarticEase

        Dim storyboard As New Storyboard()
        Storyboard.SetTarget(dblanim, Me)
        Storyboard.SetTargetProperty(dblanim, New PropertyPath(Window.OpacityProperty))

        AddHandler dblanim.Completed, AddressOf dblanim_Completed

        storyboard.Children.Add(dblanim)
        storyboard.Begin(Me)
    End Sub

    Private Sub dblanim_Completed(sender As Object, e As EventArgs)
        Me.Hide()
        wnd_flyout_appmenu.ui_settings.Visibility = Visibility.Visible
    End Sub

#End Region

    Private Sub wnd_licensevwr_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        cls_blur_behind.blur(Me, wnd_settings.ui_blur_enabled)
    End Sub

    Dim lic_path As String
    Public Sub show_license(ByVal lic_file As String, ByVal head_txt As String)
        lic_path = lic_file
        rtb_license.Document.Blocks.Clear()
        rtb_license.Document.ContentStart.InsertTextInRun("Lizenz wird geladen...")

        wnd_flyout_appmenu.ui_settings.Visibility = Visibility.Hidden

        Me.Visibility = Visibility.Visible
        anim_slidein()
        lbl_header.Content = head_txt
    End Sub

End Class
