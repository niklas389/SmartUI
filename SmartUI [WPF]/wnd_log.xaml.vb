Imports System
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Interop
Imports System.Windows.Threading

Public Class wnd_log

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

    Friend Sub wnd_enbaleBlur()
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
#End Region

    Private Sub wnd_log_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        wnd_enbaleBlur()
    End Sub

    Public Sub AddLine(cat As String, msg As String)
        outputBox.AppendText(NewLine & DateTime.Now.ToLongTimeString & " | " & cat & " > " & msg)
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