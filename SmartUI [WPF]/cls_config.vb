Imports System

Public Class cls_config
    Private Declare Ansi Function read_string Lib "kernel32.dll" Alias "GetPrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpDefault As String, ByVal lpReturnedString As String, ByVal nSize As Int32, ByVal lpFileName As String) As Int32
    Private Declare Ansi Function write_string Lib "kernel32.dll" Alias "WritePrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpString As String, ByVal lpFileName As String) As Int32

    Dim conf_basepath As String = AppDomain.CurrentDomain.BaseDirectory & "config\"
    Dim ini_file_settings As String = "settings.ini"
    Dim ini_file_weather As String = "weather.ini"

    Public Function read(ByVal strSection As String, ByVal strKey As String, ByVal strDefault As String, ByVal Optional file As Integer = 0) As String
        'Funktion zum Lesen
        'strSection = Sektion in der INI-Datei
        'strKey = Name des Schlüssels
        'strDefault = Standardwert, wird zurückgegeben, wenn der Wert in der INI-Datei nicht gefunden wurde
        'File = 0 = general config / 1 = weather config
        Dim strTemp As String = Microsoft.VisualBasic.Space(1024), lLength As Integer
        Dim strFile As String

        If file = 0 Then
            strFile = ini_file_settings
        Else
            strFile = ini_file_weather
        End If

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

        If Not IO.Directory.Exists(conf_basepath) Then IO.Directory.CreateDirectory(conf_basepath)

        Return (Not (write_string(strSection, strKey, strValue, conf_basepath & strFile) = 0))
    End Function

    Public Function debugging_enabled() As Boolean
        Return IO.File.Exists(conf_basepath & "debug")
    End Function
End Class
