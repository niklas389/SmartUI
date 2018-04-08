Imports System
Imports System.IO
Imports System.Net
Imports System.Xml

Public Class cls_weather
    Shared conf_ini As New cls_config
    Shared logcat As String = "WEATHER"
    'general config
    Public Shared conf_enabled As Boolean = False
    Public Shared conf_wcom_enabled As Boolean = False
    Public Shared conf_bzm_enabled As Boolean = False
    'OPENWEATHER config
    Public Shared oww_API_cityID As Integer
    Public Shared oww_API_key As String

#Region "CONFIG"
    'SETTINGS
    Public Shared Sub enabled(ByVal e_enabled As Boolean)
        conf_enabled = e_enabled
        conf_ini.write("GEN", "enabled", conf_enabled.ToString, 1)
    End Sub

    Public Shared Sub oww_set_data(ByVal e_cID As Integer, ByVal e_key As String)
        oww_API_cityID = e_cID
        oww_API_key = e_key

        conf_ini.write("OWW", "cid", oww_API_cityID.ToString, 1)
        conf_ini.write("OWW", "key", oww_API_key, 1)
    End Sub

#End Region

#Region "GENERAL"
    Public Shared Sub init_update(ByVal Optional e_init As Boolean = False)
        If e_init = True Then
            'load settings
            conf_enabled = CBool(conf_ini.read("GEN", "enabled", "False", 1))
            oww_API_cityID = CInt(conf_ini.read("OWW", "cid", "0", 1))
            oww_API_key = conf_ini.read("OWW", "key", "0", 1)

            'wetter.com file
            conf_wcom_enabled = File.Exists(AppDomain.CurrentDomain.BaseDirectory & "\config\wcom_allowed")

            MainWindow.wnd_log.AddLine(logcat & "-INFO", "Init - (Service Enabled: " & conf_enabled.ToString & " / wetter.com allowed: " & conf_wcom_enabled & ")")
        End If

        MainWindow.wnd_log.AddLine(logcat & "-INFO", "Updating...")
        If conf_enabled = True Then
            oww_update()
            tmr_update_oww.Start() 'enable oww updater /30mins
        End If

        If conf_wcom_enabled = True Then
            wcom_update()
            tmr_update_wcom.Start() 'enable wcom updater /5mins
        End If

        'If conf_bzm_enabled = True Then
        'get_bzm_baqi()
        'End If
    End Sub
#End Region

#Region "UPDATE"
    Public Shared WithEvents tmr_update_oww As New Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 30, 0), .IsEnabled = False}
    Public Shared Sub tmr_update_oww_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_update_oww.Tick
        oww_update() 'oww_update
    End Sub

    Public Shared WithEvents tmr_update_wcom As New Windows.Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 5, 0), .IsEnabled = False}
    Public Shared Sub tmr_update_wcom_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmr_update_wcom.Tick
        wcom_update() 'update_wcom
    End Sub
#End Region

#Region "OPENWEATHER"
    'OpenWeather API - DATA PATH -> (path_cache & "weather\oww_data.xml")
    Private Shared path_cache_weather As String = AppDomain.CurrentDomain.BaseDirectory & "cache\weather\"

    Shared oww_temp_now As String = "--"
    Public Shared oww_temp_min As String = "X°"
    Public Shared oww_temp_max As String = "X°"
    Shared oww_humidity As Integer = -1
    Shared oww_pressure As Integer = -1
    'Private oww_pressure_unit As String = "X"
    Shared oww_windspeed As Double = 0
    Public Shared oww_winddir As String = "X"
    Shared oww_winddir_deg As Double = 0
    Public Shared oww_windcond As String = "X"
    Public Shared oww_condition As String = "X"

    Public Shared oww_data_conditionID As Integer = 0
    Public Shared oww_data_conditionIMG As String = System.AppDomain.CurrentDomain.BaseDirectory & "\Resources\wIcons\0.png"
    Public Shared oww_data_location As String = ""
    Public Shared oww_data_country As String = ""


    Public Shared Async Sub oww_update(ByVal Optional e_force_oww As Boolean = False)
        If conf_enabled = False Or My.Computer.Network.IsAvailable = False Then
            MainWindow.wnd_log.AddLine(logcat & "-ATT", "OWW - Weather service disabled or Network not available!", "att")
            Exit Sub
        End If


        Dim oww_xml As XmlDocument
        Dim oww_API_error As Boolean = False

        'API Update - request weather data
        Dim oww_api_request As WebRequest = WebRequest.Create("http://api.openweathermap.org/data/2.5/weather?id=" & oww_API_cityID & "&APPID=" & oww_API_key & "&mode=xml&units=metric&lang=DE")
        Dim oww_api_respone As WebResponse
        oww_xml = New XmlDocument()

        Try
            oww_api_respone = Await oww_api_request.GetResponseAsync() 'server response
            MainWindow.wnd_log.AddLine(logcat & "-DEBUG", "OWW data updated successfully")

        Catch ex As WebException
            oww_api_respone = Nothing
            oww_API_error = True

            If ex.Status = WebExceptionStatus.ProtocolError Then
                MainWindow.wnd_log.AddLine(logcat & "-ERR", "OWW (401 Zugriff verweigert)", "err")
            Else
                MainWindow.wnd_log.AddLine(logcat & "-ERR", "OWW unknown Error", "err")
            End If
        End Try

        If oww_API_error = False Then
            If Not IO.Directory.Exists(path_cache_weather) Then IO.Directory.CreateDirectory(path_cache_weather)
            Dim oww_api_dataStream As Stream = oww_api_respone.GetResponseStream()
            ' Open the stream using a StreamReader for easy access.
            Dim oww_api_reader As New StreamReader(oww_api_dataStream)
            'Load XML from reader
            oww_xml.LoadXml(oww_api_reader.ReadToEnd())
            'save xml, because we can update only after 10mins
            oww_xml.PreserveWhitespace = True
            oww_xml.Save(path_cache_weather & "oww_data.xml")

            conf_ini.write("STAT", "oww", Date.Now.ToShortTimeString, 1)

        Else
            MainWindow.wnd_log.AddLine(logcat & "-ERR", "OWW API-Error", "err")
        End If
        'End API Update

        'Process new Data
        If File.Exists(path_cache_weather & "oww_data.xml") Then
            Dim XMLReader As Xml.XmlReader = New Xml.XmlTextReader(path_cache_weather & "oww_data.xml")

            Dim xmlid As String = ""
            With XMLReader 'read XML
                Do While .Read
                    If .NodeType = Xml.XmlNodeType.Element Then ' Welche Art von Daten liegt an? 

                        xmlid = .Name
                        'If xmlid = "country" Then oww_data_country = .ReadElementContentAsString

                        If .AttributeCount > 0 Then
                            While .MoveToNextAttribute ' nächstes 
                                If xmlid = "city" And .Name = "name" Then oww_data_location = .Value
                                'REWORK THIS SHIT:
                                If xmlid = "temperature" And .Name = "value" Then oww_temp_now = Math.Round(CDbl(.Value.Replace(".", ",")), 1).ToString.Replace(",", ".")
                                '-------------------
                                If xmlid = "temperature" And .Name = "max" Then oww_temp_max = .Value
                                If xmlid = "temperature" And .Name = "min" Then oww_temp_min = .Value '.Remove(.Value.Length - 1, 1)
                                If xmlid = "humidity" And .Name = "value" Then oww_humidity = CInt(.Value)
                                If xmlid = "pressure" And .Name = "value" Then oww_pressure = CInt(.Value)
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

        Dim icn_basePath As String = System.AppDomain.CurrentDomain.BaseDirectory & "Resources\wIcons\"
        Select Case oww_data_conditionID
            'Conditions as described at: http://openweathermap.org/weather-conditions

            Case 200 To 232 'Thunderstorm / Gewitter
                oww_data_conditionIMG = icn_basePath & "7.png"

            Case 300 To 310 'Drizzle / Nieselregen
                oww_data_conditionIMG = icn_basePath & "2.png"

            Case 311 To 321
                oww_data_conditionIMG = icn_basePath & "6.png"

            Case 500 To 531 'Rain / Regen
                oww_data_conditionIMG = icn_basePath & "8.png"

            Case 600 'Light Snow
                oww_data_conditionIMG = icn_basePath & "9.png"

            Case 601 To 622 'Snow / Schnee
                oww_data_conditionIMG = icn_basePath & "10.png"

            Case 700 To 781 'Atmosphere 
                oww_data_conditionIMG = icn_basePath & "1.png"

            Case 800 'Clear / Klar

                If DateTime.Now.Hour > 20 Or DateTime.Now.Hour < 6 Then
                    oww_data_conditionIMG = icn_basePath & "5n.png"
                Else
                    oww_data_conditionIMG = icn_basePath & "5.png"
                End If

            Case 801 To 802
                If DateTime.Now.Hour > 20 Or DateTime.Now.Hour < 6 Then
                    oww_data_conditionIMG = icn_basePath & "3n.png"
                Else
                    oww_data_conditionIMG = icn_basePath & "3.png"
                End If

            Case 803 To 804 'Cloudy / Wolkig
                oww_data_conditionIMG = icn_basePath & "4.png"

            Case 900 To 906 'Extreme / Extremes Unwetter
                oww_data_conditionIMG = icn_basePath & "0.png"

            Case 951 To 962 'Additional / Extra
                oww_data_conditionIMG = icn_basePath & "0.png"

            Case Else
                oww_data_conditionIMG = icn_basePath & "0.png"
                MainWindow.wnd_log.AddLine(logcat & "-ATT", "No icon available for this condition (" & oww_data_conditionID & ")", "att")
        End Select

        MainWindow.weather_need_update = True
    End Sub
#End Region

#Region "WETTER.COM"
    Shared wcom_temp_now As String = "--"
    'Public Shared wcom_condition As String
    Shared wcom_wind_speed As String
    Shared wcom_wind_direction As Double
    Shared wcom_humidity As Integer
    Public Shared wcom_rain As String
    Shared wcom_pressure As Integer

    Public Shared Async Sub wcom_update(ByVal Optional e_station As Integer = 16549, ByVal Optional e_failed As Boolean = False)
        If conf_wcom_enabled = False Or My.Computer.Network.IsAvailable = False Then
            MainWindow.wnd_log.AddLine(logcat & "-ATT", "WCOM - wetter.com not allowed or Network not available!", "att")
            Exit Sub
        End If

        'Quelltext herunterladen
        Dim wStationData_request As WebRequest = WebRequest.Create("http://netzwerk.wetter.com/api/stationdata/" & e_station & "/1/")
        Dim wStationData_respone As WebResponse '= Await wStationData_request.GetResponseAsync()

        Try
            wStationData_respone = Await wStationData_request.GetResponseAsync()
            MainWindow.wnd_log.AddLine(logcat & "-DEBUG", "WCOM data updated successfully")
        Catch ex As Exception
            wcom_error(e_station)
            MainWindow.wnd_log.AddLine(logcat & "-DEBUG", "WCOM update failed: " & ex.Message)
            Exit Sub
        End Try

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

        'MessageBox.Show(wStationData_data)
        'Example string:    "1464900000":{"hu":90,"te":17.7,"dp":15.6,"pr":1011.9,"pa":null,"ws":0.2,"wd":135

        If wStationData_data.Length < 10 Then
            wcom_error(e_station)
            Exit Sub
        End If

        Dim sSplit As String() = wStationData_data.Split(CType("}", Char()))
        Dim ssSplit As String() = sSplit(int - 3).ToString.Split(CType(":", Char()))

        wStationData_reader.Close()
        wStationData_respone.Close()

        'temperature
        If ssSplit(3).Substring(0, ssSplit(3).Length - 5) = "null" Then
            wcom_temp_now = "--"
        Else
            wcom_temp_now = ssSplit(3).Substring(0, ssSplit(3).Length - 5) '& "°"
        End If

        'wind speed
        If (ssSplit(7).Substring(0, ssSplit(7).Length - 5)) = "null" Then
            wcom_wind_speed = "N/A"
        Else
            wcom_wind_speed = (CDbl(ssSplit(7).Substring(0, ssSplit(7).Length - 5)) * 3.6 / 10).ToString '& " km/h"
        End If

        'humidity
        If ssSplit(2).Substring(0, ssSplit(2).Length - 5) = "null" Then
            wcom_humidity = -2
        Else
            wcom_humidity = CInt(ssSplit(2).Substring(0, ssSplit(2).Length - 5)) '& "%"
        End If

        'wind direction
        If ssSplit(8) = "null" Then wcom_wind_direction = -1 Else wcom_wind_direction = CDbl(ssSplit(8))

        'rain
        If ssSplit(6).Substring(0, ssSplit(6).Length - 5) = "null" Then
            wcom_rain = "-"
        Else
            wcom_rain = ssSplit(6).Substring(0, ssSplit(6).Length - 5) ' & " l/qm"
        End If

        'air pressure
        wcom_pressure = CInt(Math.Round(CDbl(ssSplit(5).Substring(0, ssSplit(5).Length - 5).Replace(".", ",")), 0)) 'hPa
        'MessageBox.Show(CDbl(ssSplit(5).Substring(0, ssSplit(5).Length - 5).Replace(".", ",")).ToString())

        'last update
        conf_ini.write("STAT", "wcom", DateTime.Now.ToString, 1)

        MainWindow.weather_need_update = True
    End Sub

    Private Shared Sub wcom_error(e_station As Integer)
        If e_station = 7704 Then
            MainWindow.wnd_log.AddLine(logcat & "-ERR", "WCOM - API-Error with backup-station - wetter.com disabled", "err")
            conf_wcom_enabled = False
            Exit Sub
        End If

        MainWindow.wnd_log.AddLine(logcat & "-ATT", "WCOM - API-Error with Station 16549, trying 7704", "att")
        wcom_update(7704)
    End Sub
#End Region

#Region "Breezometer API"
    Public Shared bzm_location_long As Double
    Public Shared bzm_location_lat As Double
    Public Shared bzm_location_key As String

    Public Shared bzm_data As String

    Public Shared Async Sub get_bzm_baqi()
        If conf_bzm_enabled = False Or My.Computer.Network.IsAvailable = False Then
            MainWindow.wnd_log.AddLine(logcat & "-ATT", "BZM - Weather service disabled or Network not available!", "att")
            Exit Sub
        End If

        Dim bzm_api_request As WebRequest = WebRequest.Create("https://api.breezometer.com/baqi/?" & "lat=" & bzm_location_lat & "&lon=" & bzm_location_lat & "&key=" & bzm_location_key)
        Dim bzm_api_respone As WebResponse
        Dim bzm_data_json As Newtonsoft.Json.JsonTextReader

        Try
            bzm_api_respone = Await bzm_api_request.GetResponseAsync() 'server response
        Catch ex As WebException

        End Try

    End Sub
#End Region

    Public Shared Function get_temp() As String
        If conf_wcom_enabled = True And Not wcom_temp_now = "--" Then Return wcom_temp_now & "°" Else Return oww_temp_now.ToString & "°"
    End Function

    Public Shared Function get_humidity() As Integer
        If conf_wcom_enabled = True Then Return wcom_humidity Else Return oww_humidity
    End Function

    Public Shared Function get_wind_speed() As String
        If conf_wcom_enabled = True And Not wcom_wind_speed = "N/A" Then
            Return wcom_wind_speed
        Else
            Return oww_windspeed.ToString
        End If
    End Function

    Public Shared Function get_wind_dir() As Double
        'If conf_wcom_enabled = True Then Return wcom_wind_direction Else Return oww_winddir_deg
        Return oww_winddir_deg
    End Function

    Public Shared Function get_pressure() As Integer
        If conf_wcom_enabled = True Then Return wcom_pressure Else Return oww_pressure
    End Function

    Public Shared Function get_condition_pic(ByVal Optional e_img As Boolean = False) As String
        If e_img = False Then
            Return oww_data_conditionIMG
        Else
            Return oww_data_conditionIMG.Replace("wIcons", "wImg").Replace(".png", ".jpg")
        End If
    End Function

    'Public Shared Function get_bzm() As Integer
    '    Return oww_pressure
    'End Function
End Class
