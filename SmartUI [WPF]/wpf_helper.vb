Imports System
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Public Class wpf_helper
    'helper: IMAGE
    Public Shared Sub helper_image(ByVal ctrl As Controls.Image, ByVal Optional e_content As String = "<nothing>", ByVal Optional e_visible As Boolean = Nothing)
        If Not e_content = "<nothing>" Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background,
                                                  New ThreadStart(Sub() ctrl.Source = CType(New ImageSourceConverter().ConvertFromString(e_content), ImageSource)))
        End If

        If Not e_visible = Nothing Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background,
                                                  New ThreadStart(Sub()

                                                                      If e_visible = True Then
                                                                          ctrl.Visibility = Visibility.Visible
                                                                      ElseIf e_visible = False Then
                                                                          ctrl.Visibility = Visibility.Hidden
                                                                      End If
                                                                  End Sub))
        End If
    End Sub

    Public Shared Sub helper_grid(ByVal ctrl As Grid, ByVal Optional e_visible As Boolean = Nothing)
        If e_visible = True Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background, New ThreadStart(Sub() ctrl.Visibility = Visibility.Visible))
        ElseIf e_visible = False Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Background, New ThreadStart(Sub() ctrl.Visibility = Visibility.Hidden))
        End If
    End Sub

    Public Shared Sub helper_label(ByVal ctrl As Label, ByVal Optional e_content As String = Nothing, ByVal Optional e_visible As Visibility = Visibility.Collapsed)
        If Not e_content = Nothing Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() ctrl.Content = e_content))
        End If

        If Not e_visible = Visibility.Collapsed Then
            Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() ctrl.Visibility = e_visible))
        End If
    End Sub

    Public Shared Sub helper_progressBar(ByVal ctrl As MahApps.Metro.Controls.MetroProgressBar, ByVal Optional e_value As Double = -1, ByVal Optional e_max As Double = -1, ByVal Optional e_visible As Boolean = Nothing)
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

    Public Shared Function helper_label_gc(ByVal ctrl As Label) As String
        Dim tmp_str As String = ""
        Application.Current.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, New ThreadStart(Sub() tmp_str = ctrl.Content.ToString))
        Return tmp_str
    End Function
End Class
