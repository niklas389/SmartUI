Imports System.Runtime.InteropServices
Imports System.Windows.Interop

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
        ' ...
        WCA_ACCENT_POLICY = 19
        ' ...
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

    Dim use_wcom As Boolean = IO.File.Exists(".\config\wcom_allowed")
    Private Sub wnd_flyout_weather_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.Top = My.Computer.Screen.WorkingArea.Top
        Me.Left = My.Computer.Screen.WorkingArea.Left

        EnableBlur()
    End Sub

    Dim str_condition As String = Nothing
    Private Sub wwnd_flyout_weather_Loaded(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        Me.Height = 135

        Dim ddiff_wcom As Double = DateTime.Now.Subtract(MainWindow.WData_wcom_lastUpdate).TotalMinutes
        Dim ddiff_oww As Double = DateTime.Now.Subtract(MainWindow.wData_lastUpdate).TotalMinutes

        str_condition = MainWindow.oww_data_condition

        If use_wcom = True Then
            str_condition &= " bei " & MainWindow.wData_temp & "C"
            lbl_weather_humidity.Content = "Luftfeuchtigkeit: " & MainWindow.wData_humidity
            lbl_weather_winddir.Content = "Windgeschwindigkeit: " & MainWindow.wData_windSpeed
            If MainWindow.wData_rain <> "null l/qm" Then lbl_weather_windspeed.Content = "Regenmenge: " & MainWindow.wData_rain Else lbl_weather_windspeed.Content = "Regenmenge: n.A."
            lbl_weather_airPressure.Content = "Luftdruck: " & MainWindow.wData_airPressure

            If CInt(TimeSpan.FromMinutes(ddiff_wcom).TotalMinutes) < 1 Then
                lbl_weather_info.Content = "Jetzt aktualisiert (wetter.com)"
            Else
                lbl_weather_info.Content = "Vor " & TimeSpan.FromMinutes(ddiff_wcom).ToString("mm").Remove(0, 1) & " Minuten aktualisiert (wetter.com)"
            End If

        Else
            str_condition &= " bei " & MainWindow.oww_data_temp.ToString.Replace(",", ".") & "°C"
            lbl_weather_humidity.Content = "Luftfeuchtigkeit: " & MainWindow.oww_data_humidity & "%"
            lbl_weather_airPressure.Content = "Luftdruck: " & MainWindow.oww_data_pressure & " hPa"
            lbl_weather_winddir.Content = "Windrichtung: " & MainWindow.oww_data_winddirection
            lbl_weather_windspeed.Content = "Windstärke: " & MainWindow.oww_data_windspeed & "m/s"

            If CInt(TimeSpan.FromMinutes(ddiff_oww).TotalMinutes) <= 1 Then
                lbl_weather_info.Content = "Jetzt aktualisiert (OpenWeather)"
            Else
                lbl_weather_info.Content = "Vor " & TimeSpan.FromMinutes(ddiff_oww).ToString("mm").Remove(0, 1) & " Minuten aktualisiert (OpenWeather)"
            End If
        End If

        lbl_weather_condition.Content = str_condition
    End Sub

    Private Sub wnd_flyout_volume_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave
        Me.Hide()
    End Sub


End Class
