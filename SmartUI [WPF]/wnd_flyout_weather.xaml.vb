Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Xml

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

#Region "Window"
    Private Sub wnd_flyout_weather_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.Top = My.Computer.Screen.WorkingArea.Top
        Me.Left = My.Computer.Screen.WorkingArea.Left

        EnableBlur()

        If IO.File.Exists(".\config\wcom_allowed") Then
            MainWindow.wnd_log.AddLine("CONFIG" & "-WEATHER", "wetter.com is allowed")
            wConf_useWcom = True
        End If
    End Sub
#End Region

    Private Sub wnd_flyout_volume_LostFocus(sender As Object, e As RoutedEventArgs) Handles Me.MouseLeave
        Me.Hide()
    End Sub

    Private Sub img_data_refresh_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles img_data_refresh.MouseUp
        oww_update()
        wcom_update()
    End Sub

#Region "Weather Data"
    Dim ini As New ini_file
    Public Shared wConf_useWcom As Boolean

    Dim icn_basePath As String = AppDomain.CurrentDomain.BaseDirectory & "\Resources\wIcons\"

    Private wConf_API_cityID As Integer = 0
    Private wConf_API_key As String = ""
    Dim oww_xml As XmlDocument

    Public Shared oww_temp_now As String = "X°"
    Private oww_temp_min As String = "X°"
    Private oww_temp_max As String = "X°"
    Private oww_humidity As String = "X"
    Private oww_pressure As String = "X"
    'Private oww_pressure_unit As String = "X"
    Private oww_windspeed As Double = 404
    Private oww_winddir As String = "X"
    Private oww_winddir_deg As Integer = 0
    Private oww_windcond As String = "X"
    Private oww_condition As String = "X"

    Private oww_data_conditionID As Integer = 0
    Private oww_data_location As String = ""
    Private oww_data_country As String = ""

    'OpenWeather API - DATA PATH -> (path_cache & "weather\oww_data.xml")
    Dim path_cache_weather As String = AppDomain.CurrentDomain.BaseDirectory & "cache\weather\"

    Private Sub oww_update(ByVal Optional e_force_oww As Boolean = False)
        'Process new Data
        If File.Exists(path_cache_weather & "oww_data.xml") Then
            Dim XMLReader As Xml.XmlReader = New Xml.XmlTextReader(path_cache_weather & "oww_data.xml")

            ' Es folgt das Auslesen der XML-Datei 
            Dim xmlid As String = ""
            With XMLReader
                Do While .Read ' Es sind noch Daten vorhanden 
                    ' Welche Art von Daten liegt an? 
                    If .NodeType = Xml.XmlNodeType.Element Then

                        xmlid = .Name
                        If xmlid = "country" Then oww_data_country = .ReadElementContentAsString

                        If .AttributeCount > 0 Then
                            While .MoveToNextAttribute ' nächstes 
                                If xmlid = "city" And .Name = "name" Then oww_data_location = .Value
                                If xmlid = "temperature" And .Name = "value" Then oww_temp_now = .Value.Remove(.Value.Length - 1, 1)
                                If xmlid = "temperature" And .Name = "max" Then oww_temp_max = .Value.Remove(.Value.Length - 1, 1)
                                If xmlid = "temperature" And .Name = "min" Then oww_temp_min = .Value.Remove(.Value.Length - 1, 1)
                                If xmlid = "humidity" And .Name = "value" Then oww_humidity = .Value
                                If xmlid = "pressure" And .Name = "value" Then oww_pressure = .Value
                                'If xmlid = "pressure" And .Name = "unit" Then oww_pressure_unit = .Value

                                If xmlid = "speed" And .Name = "value" Then oww_windspeed = Math.Round(CDbl(.Value) / 3.6, 1) 'windspeed in M/S conv. to km/h with sp
                                If xmlid = "wind" And .Name = "name" Then oww_windcond = .Value
                                If xmlid = "direction" And .Name = "code" Then oww_winddir = .Value
                                If xmlid = "direction" And .Name = "value" Then oww_winddir_deg = CInt(.Value)
                                If xmlid = "weather" And .Name = "value" Then oww_condition = .Value
                                If xmlid = "weather" And .Name = "number" Then oww_data_conditionID = CInt(.Value)

                            End While
                        End If
                    End If
                Loop
                .Close()
            End With
        Else

        End If

        'DEBUG:  Show Wdata after Update
        'MessageBox.Show("City: " & oww_data_location & " // Country: " & oww_data_country & vbCrLf & "Temp: " & oww_data_temp & "// Tmin: " &
        'oww_data_Tmin & " // Tmax: " & oww_data_Tmax & vbCrLf & "Humidity: " & oww_data_humidity & vbCrLf & "Pressure: " & oww_data_pressure &
        '                        "Wind Speed: " & oww_data_windspeed & " // Wind Condition: " & oww_data_windcondition & " // Winddirection: " & oww_data_winddirection & vbCrLf &
        '"Weather txt: " & oww_data_condition & " // Weather Code: " & oww_data_conditionID)

        Select Case oww_data_conditionID
            'Conditions as described at: http://openweathermap.org/weather-conditions

            Case 200 To 232 'Thunderstorm / Gewitter
                wpf_helper.helper_image(img_now_condition, icn_basePath & "7.png")

            Case 300 To 310 'Drizzle / Nieselregen
                wpf_helper.helper_image(img_now_condition, icn_basePath & "2.png")

            Case 311 To 321
                wpf_helper.helper_image(img_now_condition, icn_basePath & "6.png")

            Case 500 To 531 'Rain / Regen
                wpf_helper.helper_image(img_now_condition, icn_basePath & "8.png")

            Case 600 'Light Snow
                wpf_helper.helper_image(img_now_condition, icn_basePath & "9.png")

            Case 601 To 622 'Snow / Schnee
                wpf_helper.helper_image(img_now_condition, icn_basePath & "10.png")

            Case 700 To 781 'Atmosphere 
                wpf_helper.helper_image(img_now_condition, icn_basePath & "1.png")

            Case 800 'Clear / Klar

                If DateTime.Now.Hour > 20 Or DateTime.Now.Hour < 6 Then
                    wpf_helper.helper_image(img_now_condition, icn_basePath & "5n.png")
                Else
                    wpf_helper.helper_image(img_now_condition, icn_basePath & "5.png")
                End If

            Case 801 To 802
                If DateTime.Now.Hour > 20 Or DateTime.Now.Hour < 6 Then
                    wpf_helper.helper_image(img_now_condition, icn_basePath & "3n.png")
                Else
                    wpf_helper.helper_image(img_now_condition, icn_basePath & "3.png")
                End If

            Case 803 To 804 'Cloudy / Wolkig
                wpf_helper.helper_image(img_now_condition, icn_basePath & "4.png")

            Case 900 To 906 'Extreme / Extremes Unwetter
                wpf_helper.helper_image(img_now_condition, icn_basePath & "0.png")

            Case 951 To 962 'Additional / Extra
                wpf_helper.helper_image(img_now_condition, icn_basePath & "0.png")

            Case Else
                wpf_helper.helper_image(img_now_condition, icn_basePath & "0.png")
                MainWindow.wnd_log.AddLine("ATT" & "-WEATHER", "No condition icon (" & oww_data_conditionID & ")")

        End Select

        If wData_lastUpdate = Nothing Then
            DateTime.TryParse(ini.ReadValue("Weather", "last_update", ""), wData_lastUpdate)
        End If

        'Wetter.com Data - Normally not used because we have no permission to use it.

        If wConf_useWcom = False Then
            Me.lbl_now_temp.Content = oww_temp_now & "°"
        End If

        lbl_now_txt.Content = oww_condition
        lbl_location.Content = oww_data_location

        lbl_now_windspeed.Content = oww_windspeed
        lbl_now_humidity.Content = oww_humidity

        lbl_now_pressure.Content = oww_pressure

        lbl_now_winddir.Content = oww_winddir
        lbl_now_winddir_deg.Content = oww_winddir_deg
        img_now_winddir.RenderTransform = New RotateTransform(oww_winddir_deg)
    End Sub

    Public Shared wData_temp As String
    Public Shared wData_condition As String
    Public Shared wData_humidity As String
    Public Shared wData_rain As String
    Public Shared wData_airPressure As String
    Public Shared wData_windDir As String
    Public Shared wData_windSpeed As String

    Public Shared wData_lastUpdate As Date
    Public Shared WData_wcom_lastUpdate As Date
    Private wData_updateWA As Integer

    Private Async Sub wcom_update(ByVal Optional e_station As Integer = 7704, ByVal Optional e_failed As Boolean = False)
        'Quelltext herunterladen
        Dim wStationData_request As WebRequest = WebRequest.Create("http://netzwerk.wetter.com/api/stationdata/" & e_station & "/1/")
        Dim wStationData_respone As WebResponse = Await wStationData_request.GetResponseAsync()

        ' Get the stream containing content returned by the server.
        Dim wStationData_dataStream As Stream = wStationData_respone.GetResponseStream()
        ' Open the stream using a StreamReader for easy access.
        Dim wStationData_reader As New StreamReader(wStationData_dataStream)
        ' Read the content.
        Dim wStationData_data As String = wStationData_reader.ReadToEnd()

        Dim int As Integer = 0
        For Each data_set As String In wStationData_data.Split(CType("}", Char()))
            int += 1
        Next
        'Example string:    "1464900000":{"hu":90,"te":17.7,"dp":15.6,"pr":1011.9,"pa":null,"ws":0.2,"wd":135

        If wStationData_data.Length < 10 Then
            wcom_error(e_station)
            Exit Sub
        End If

        Dim sSplit As String() = wStationData_data.Split(CType("}", Char()))
        Dim ssSplit As String() = sSplit(int - 3).ToString.Split(CType(":", Char()))

        'temperature
        If ssSplit(3).Substring(0, ssSplit(3).Length - 5) = "null" Then
            wData_temp = "--°"
        Else
            wData_temp = ssSplit(3).Substring(0, ssSplit(3).Length - 5) & "°"
        End If

        'Windspeed
        If ssSplit(7).Substring(0, ssSplit(7).Length - 5) = "null" Then
            wData_windSpeed = "0 km/h"
        Else
            wData_windSpeed = CDbl((ssSplit(7).Substring(0, ssSplit(7).Length - 5))) * 3.6 / 10 & " km/h"
        End If

        'humidity
        If ssSplit(2).Substring(0, ssSplit(2).Length - 5) = "null" Then
            wData_humidity = "N/A"
        Else
            wData_humidity = ssSplit(2).Substring(0, ssSplit(2).Length - 5) & "%"
        End If

        'wind direction
        wData_windDir = ssSplit(8) & "°"
        'wind speed
        If (ssSplit(7).Substring(0, ssSplit(7).Length - 5)) = "null" Then
            wData_windSpeed = "N/A"
        Else
            wData_windSpeed = CDbl((ssSplit(7).Substring(0, ssSplit(7).Length - 5))) * 3.6 / 10 & " km/h"
        End If
        'rain
        wData_rain = ssSplit(6).Substring(0, ssSplit(6).Length - 5) & " l/qm"
        'air pressure
        wData_airPressure = ssSplit(5).Substring(0, ssSplit(5).Length - 5) & " hPa"

        lbl_now_temp.Content = wData_temp

        wStationData_reader.Close()
        wStationData_respone.Close()

        wData_updateWA = DateTime.Now.Minute
        WData_wcom_lastUpdate = DateTime.Now
    End Sub

    Private Sub wcom_error(e_station As Integer)
        If e_station = 16549 Then
            MainWindow.wnd_log.AddLine("ERR" & "-WEATHER", "WCOM API-Error with Station 16549 - Fallback to OpenWeather")
            lbl_now_temp.Content = oww_temp_now.ToString & "°"
            Exit Sub
        End If

        MainWindow.wnd_log.AddLine("ATT" & "-WEATHER", "WCOM API-Error with Station 7704, trying 16549")
        wcom_update(16549)
    End Sub
#End Region

    Dim str_condition As String = Nothing
    Private Sub wwnd_flyout_weather_Loaded(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        If Me.Visibility = Visibility.Hidden Then Exit Sub
        Me.Height = 230

        oww_update()
        wcom_update()
    End Sub

    Private Sub lbl_now_temp_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles lbl_now_temp.SizeChanged
        img_now_condition.Margin = New Thickness(lbl_now_temp.RenderSize.Width, 10, 0, 0)
        lbl_now_txt.Margin = New Thickness(lbl_now_temp.RenderSize.Width, 40, 0, 0)
    End Sub



End Class
