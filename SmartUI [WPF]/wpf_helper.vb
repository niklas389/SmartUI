Imports System.Threading
Public Class wpf_helper

    Public Shared Sub helper_image(ByVal ctrl As Controls.Image, ByVal Optional e_content As String = "<nothing>", ByVal Optional e_visible As Boolean = Nothing)
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
End Class
