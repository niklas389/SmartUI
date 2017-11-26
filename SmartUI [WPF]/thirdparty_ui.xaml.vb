Imports System
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Input
Imports System.Windows.Interop

Public Class thirdparty_ui

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

        Dim data = New WindowCompositionAttributeData()
        data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY
        data.SizeOfData = accentStructSize
        data.Data = accentPtr

        SetWindowCompositionAttribute(windowHelper.Handle, data)

        Marshal.FreeHGlobal(accentPtr)
    End Sub
#End Region

#Region "Window CMD"
    'Load
    Private Sub changelog_ui_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        EnableBlur()
    End Sub

    'Move Window
    Private Sub lbl_header_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_MouseLeftButtonDown(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        DialogResult = True
        Me.Close()
    End Sub
#End Region

#Region "Credits"
    Private Sub lbl_mahapps_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_mahapps_url.MouseLeftButtonDown
        System.Diagnostics.Process.Start("http://mahapps.com")
    End Sub

    Private Sub lbl_newtonsoft_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_newtonsoft_url.MouseLeftButtonDown
        System.Diagnostics.Process.Start("http://www.newtonsoft.com/json")
    End Sub

    Private Sub lbl_nSpotify_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_sAPI_url.MouseLeftButtonDown
        System.Diagnostics.Process.Start("https://github.com/JohnnyCrazy/SpotifyAPI-NET/releases")
    End Sub

    Private Sub lbl_hvOSD_url_Click(sender As Object, e As RoutedEventArgs) Handles lbl_hvOSD_url.MouseLeftButtonDown
        System.Diagnostics.Process.Start("http://wordpress.venturi.de/?p=1")
    End Sub

    Private Sub btn_hvOSD_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_hvOSD_license.Click
        System.Diagnostics.Process.Start("hvOSD_license.txt")
    End Sub

    Private Sub btn_sAPI_license_Click(sender As Object, e As RoutedEventArgs) Handles btn_sAPI_license.Click
        System.Diagnostics.Process.Start("sAPI_license.txt")
    End Sub
#End Region

End Class
