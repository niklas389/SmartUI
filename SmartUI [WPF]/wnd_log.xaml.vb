Imports System
Imports System.Windows
Imports System.Windows.Input

Public Class wnd_log

#Region "Window CMD"
    Private Sub btn_wnd_hide_Click(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        Me.Hide()
    End Sub

    Private Sub btn_wnd_minimize_Click(sender As Object, e As RoutedEventArgs) Handles btn_wnd_minimize.Click
        Me.WindowState = WindowState.Minimized
    End Sub

    Private Sub wnd_log_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub wnd_log_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        cls_blur_behind.blur(Me, cls_config.ui_blur_enabled)
    End Sub
#End Region

    Public Sub AddLine(cat As String, msg As String, ByVal Optional color As String = "inf")

        Dim tr As New Documents.TextRange(outputBox.Document.ContentEnd, outputBox.Document.ContentEnd)
        Dim clr As Media.Brush = Media.Brushes.White
        tr.Text = NewLine & DateTime.Now.ToLongTimeString & " | " & cat & " > " & msg

        Select Case color
            Case "inf"
                clr = Media.Brushes.White
            Case "err"
                clr = Media.Brushes.Red
            Case "att"
                clr = Media.Brushes.Yellow
            Case "add"
                clr = Media.Brushes.Gray

            Case "wea"
                clr = Media.Brushes.Turquoise
        End Select

        tr.ApplyPropertyValue(Documents.TextElement.ForegroundProperty, clr)

        'outputBox.AppendText(NewLine & DateTime.Now.ToLongTimeString & " | " & cat & " > " & msg)
        outputBox.ScrollToEnd()
    End Sub

    Private Sub btn_log_clear_Click(sender As Object, e As RoutedEventArgs) Handles btn_log_clear.Click
        outputBox.Document.Blocks.Clear()
        outputBox.AppendText(DateTime.Now.ToLongTimeString & " | Cleared Log")
    End Sub

    Private Sub btn_app_kill_Click(sender As Object, e As RoutedEventArgs) Handles btn_app_kill.Click
        Diagnostics.Process.Start("taskkill", " /f /im SmartUI.exe")
    End Sub

End Class