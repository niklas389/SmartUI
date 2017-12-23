Imports System
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media

Public Class cls_blur_behind
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

    Public Shared Sub blur(ByVal wnd As Windows.Window, ByVal e_blur_enabled As Boolean)
        Dim windowHelper = New WindowInteropHelper(wnd)
        Dim accent = New AccentPolicy()
        Dim accentStructSize = Marshal.SizeOf(accent)

        If e_blur_enabled = True Then
            wnd.Background = New SolidColorBrush(Color.FromArgb(Convert.ToByte(152), Convert.ToByte(0), Convert.ToByte(0), Convert.ToByte(0))) 'BG 60
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
        Else
            accent.AccentState = AccentState.ACCENT_DISABLED
            wnd.Background = New SolidColorBrush(Color.FromArgb(Convert.ToByte(180), Convert.ToByte(0), Convert.ToByte(0), Convert.ToByte(0))) 'BG 71
        End If

        Dim accentPtr = Marshal.AllocHGlobal(accentStructSize)
        Marshal.StructureToPtr(accent, accentPtr, False)

        Dim data = New WindowCompositionAttributeData() With {
        .Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
        .SizeOfData = accentStructSize,
        .Data = accentPtr}

        SetWindowCompositionAttribute(windowHelper.Handle, data)

        Marshal.FreeHGlobal(accentPtr)
    End Sub
End Class
