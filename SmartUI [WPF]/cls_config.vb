Imports System

Public Class cls_config
    Private Declare Ansi Function read_string Lib "kernel32.dll" Alias "GetPrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpDefault As String, ByVal lpReturnedString As String, ByVal nSize As Int32, ByVal lpFileName As String) As Int32
    Private Declare Ansi Function write_string Lib "kernel32.dll" Alias "WritePrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpString As String, ByVal lpFileName As String) As Int32

    Dim conf_basepath As String = AppDomain.CurrentDomain.BaseDirectory
    Dim ini_file_settings As String = "config\settings.ini"
    Dim ini_file_weather As String = "config\weather.ini"
    Dim ini_file_buildinfos As String = "build_info"

    Public Function read(ByVal strSection As String, ByVal strKey As String, ByVal strDefault As String, ByVal Optional file As Integer = 0) As String
        'Funktion zum Lesen
        'strSection = Sektion in der INI-Datei
        'strKey = Name des Schlüssels
        'strDefault = Standardwert, wird zurückgegeben, wenn der Wert in der INI-Datei nicht gefunden wurde
        'File = 0 = general config / 1 = weather config
        Dim strTemp As String = Microsoft.VisualBasic.Space(1024), lLength As Integer
        Dim strFile As String

        Select Case file
            Case 1
                strFile = ini_file_weather
            Case 2
                strFile = ini_file_buildinfos
            Case Else
                strFile = ini_file_settings
        End Select

        lLength = read_string(strSection, strKey, strDefault, strTemp, strTemp.Length, conf_basepath & strFile)
        Return (strTemp.Substring(0, lLength))
    End Function

    Public Function write(ByVal strSection As String, ByVal strKey As String, ByVal strValue As String, ByVal Optional file As Integer = 0) As Boolean
        'Funktion zum Schreiben
        'strSection = Sektion in der INI-Datei
        'strKey = Name des Schlüssels
        'strValue = Wert, der geschrieben werden soll
        'File = 0 = general config / 1 = weather config

        Dim strFile As String
        If file = 0 Then
            strFile = ini_file_settings
        Else
            strFile = ini_file_weather
        End If

        If Not IO.Directory.Exists(conf_basepath & "config\") Then IO.Directory.CreateDirectory(conf_basepath & "config\")

        Return (Not (write_string(strSection, strKey, strValue, conf_basepath & strFile) = 0))
    End Function

    'VAR
    Public Shared ui_blur_enabled As Boolean
    Public Shared ui_blur_transparency As Byte = 165
    Public Shared debugging_enabled As Boolean

    Public Shared build_version As String
    Public Shared build_date As String

    Public Sub load_variables()
        ui_blur_enabled = CType(read("UI", "cb_wndmain_blur_enabled", "True"), Boolean)
        ui_blur_transparency = CType(read("UI", "slider_bg_transp", "165"), Byte)

        build_version = read("BUILD", "version", MainWindow.suiversion, 2)
        build_date = read("BUILD", "date", "N/A", 2)
        debugging_enabled = IO.File.Exists(conf_basepath & "config\debug")
    End Sub
End Class
