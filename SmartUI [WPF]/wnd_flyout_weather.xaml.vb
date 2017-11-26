Imports System
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Interop
Imports System.Windows.Media

Public Class wnd_flyout_weather

#Region "Blur"
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
        WCA_ACCENT_POLICY = 19
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

#Region "Window"
    Private Sub wnd_flyout_weather_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        EnableBlur()
    End Sub

    Private Sub wnd_flyout_volume_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave
        Hide()
    End Sub

    Private Sub lbl_now_temp_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_now_temp.SizeChanged
        img_now_condition.Margin = New Thickness(lbl_now_temp.RenderSize.Width + 5, 10, 0, 0)
        lbl_now_txt.Margin = New Thickness(lbl_now_temp.RenderSize.Width, 40, 0, 0)
    End Sub
#End Region

    Private Sub wwnd_flyout_weather_Loaded(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        Top = 0
        Left = My.Computer.Screen.WorkingArea.Left
        Height = 255

        lbl_location.Content = cls_weather.oww_data_location

        lbl_now_temp.Content = cls_weather.get_temp
        lbl_now_txt.Content = cls_weather.oww_condition
        wpf_helper.helper_image(img_now_condition, cls_weather.get_condition_pic)
        wpf_helper.helper_image(img_weather_bg, cls_weather.get_condition_pic(True))

        lbl_now_windspeed.Content = Math.Round(CDbl(cls_weather.get_wind_speed), 1)
        lbl_now_winddir_deg.Content = cls_weather.get_wind_dir
        img_now_winddir.RenderTransform = New RotateTransform(cls_weather.get_wind_dir - 180)
        lbl_now_winddir.Content = cls_weather.oww_winddir

        lbl_now_humidity.Content = cls_weather.get_humidity
        lbl_now_pressure.Content = cls_weather.get_pressure.ToString

        lbl_now_rain.Content = cls_weather.wcom_rain
    End Sub
End Class
