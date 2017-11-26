Imports System
Imports System.Windows
Imports System.Windows.Media.Animation

Public Class wnd_flyout_appmenu

    Dim m_ht As Double = 90
#Region "WND Show/Hide"
    Private Sub wnd_flyout_appmenu_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.Top = My.Computer.Screen.WorkingArea.Top
        Me.Left = My.Computer.Screen.WorkingArea.Width - Me.RenderSize.Width

        If IO.File.Exists(".\config\wcom_allowed") Then
            m_ht = 120
            btn_do_restart.Margin = New Thickness(0, 60, 0, 0)
            btn_do_exit.Margin = New Thickness(0, 90, 0, 0)
        Else
            m_ht = 90
            btn_do_restart.Margin = New Thickness(0, 30, 0, 0)
            btn_do_exit.Margin = New Thickness(0, 60, 0, 0)
            btn_intern_log.Visibility = Visibility.Hidden
        End If

        Me.Height = m_ht
    End Sub

    Private Sub wnd_flyout_appmenu_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        Me.Height = m_ht
    End Sub

    Private Sub wnd_flyout_appmenu_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave
        Me.Hide()
    End Sub

    Private Sub anim_Completed(sender As Object, e As EventArgs)
        Close()
    End Sub

#End Region

    Public Shared ui_settings As New wnd_settings
    Private Sub btn_intern_appsettings_Click(sender As Object, e As RoutedEventArgs) Handles btn_intern_appsettings.Click
        ui_settings.Show()
    End Sub

    Private Sub btn_do_restart_Click(sender As Object, e As RoutedEventArgs) Handles btn_do_restart.Click
        'App restart
        System.Diagnostics.Process.Start(System.AppDomain.CurrentDomain.BaseDirectory & "SmartUI.exe")
        My.Application.Shutdown()
    End Sub

    Dim i_exit As Integer = 0
    Private Sub btn_do_exit_Click(sender As Object, e As RoutedEventArgs) Handles btn_do_exit.Click
        If i_exit = 0 Then
            i_exit = 1
            btn_do_exit.Content = "[erneut klicken zum beenden]"
            tmr_reset.Start()

        ElseIf i_exit = 1 Then
            My.Application.Shutdown()
        End If
    End Sub

    Private WithEvents tmr_reset As New System.Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 3), .IsEnabled = False}
    Private Sub tmr_reset_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmr_reset.Tick
        i_exit = 0
        btn_do_exit.Content = "Beenden"
        tmr_reset.Stop()
    End Sub


    Private Sub btn_intern_log_Click(sender As Object, e As RoutedEventArgs) Handles btn_intern_log.Click
        MainWindow.wnd_log.Show()
    End Sub
End Class
