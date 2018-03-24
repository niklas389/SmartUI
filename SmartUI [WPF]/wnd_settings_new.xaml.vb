Imports System.Windows
Imports System.Windows.Input

Public Class wnd_settings_new
    Private Sub wnd_settings_new_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        cls_blur_behind.blur(Me, True)

        matc_head_main.Visibility = Visibility.Hidden
        matc_head_thirdparty.Visibility = Visibility.Hidden
        sep1.Visibility = Visibility.Hidden
    End Sub

    Private Sub grd_header_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles grd_header.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_MouseLeftButtonDown(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        Me.Visibility = Visibility.Hidden
    End Sub

    Private Sub btn_wnd_minimize_MouseLeftButtonUp(sender As Object, e As RoutedEventArgs) Handles btn_wnd_minimize.Click
        Me.WindowState = WindowState.Minimized
    End Sub

    'OLD SETTINGS
    Private Sub btn_wnd_minimize_Copy_Click(sender As Object, e As RoutedEventArgs) Handles btn_wnd_minimize_Copy.Click
        wnd_flyout_appmenu.ui_settings.Show()
    End Sub

    Private Sub dbg_goto_3rdpty_Click(sender As Object, e As RoutedEventArgs) Handles dbg_goto_3rdpty.Click
        matc_settings.SelectedIndex = 1
    End Sub
End Class
