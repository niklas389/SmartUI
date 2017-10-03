Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

Public Class changelog_ui

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
    'Move Window
    Private Sub lbl_header_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Me.DragMove()
    End Sub

    Private Sub btn_wnd_hide_MouseLeftButtonDown(sender As Object, e As RoutedEventArgs) Handles btn_wnd_hide.Click
        'DialogResult = True
        Me.Close()
    End Sub
#End Region

    Private Sub changelog_ui_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        EnableBlur()

        If IO.File.Exists(".\changelog") Then
            Dim docpath As String = ".\changelog"
            Dim range As System.Windows.Documents.TextRange
            Dim fStream As System.IO.FileStream
            If System.IO.File.Exists(docpath) Then
                Try
                    ' Open the document in the RichTextBox.
                    range = New System.Windows.Documents.TextRange(rtb_chLog.Document.ContentStart, rtb_chLog.Document.ContentEnd)
                    fStream = New System.IO.FileStream(docpath, System.IO.FileMode.Open)
                    range.Load(fStream, DataFormats.Text)
                    fStream.Close()

                Catch generatedExceptionName As System.Exception
                    'MessageBox.Show("Couldn't load Changelog.")
                    rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog nicht verfügbar")
                End Try
            End If
        Else
            rtb_chLog.Document.ContentStart.InsertTextInRun("Changelog nicht verfügbar")
        End If
    End Sub
End Class
