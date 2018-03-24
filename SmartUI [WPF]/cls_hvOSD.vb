Imports System
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows

Namespace HideVolumeOSD
    Public Class HideVolumeOSDLib
        <DllImport("user32.dll")>
        Private Shared Sub keybd_event(bVk As Byte, bScan As Byte, dwFlags As UInteger, dwExtraInfo As Integer)
        End Sub

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function FindWindowEx(hwndParent As IntPtr, hwndChildAfter As IntPtr, lpszClass As String, lpszWindow As String) As IntPtr
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
        End Function

        Private hWndInject As IntPtr = IntPtr.Zero

        Private hide_osd_option As Integer = -1
        Private log_cat As String = "LIB-HV_OSD"

        Public Async Sub Init()
            'MainWindow.wnd_log.AddLine(log_cat, "Init...")

            Dim count As Integer = 1

            While hWndInject = IntPtr.Zero AndAlso count < 5
                keybd_event(CByte(Forms.Keys.VolumeUp), 0, 0, 0)
                keybd_event(CByte(Forms.Keys.VolumeDown), 0, 0, 0)

                hWndInject = FindOSDWindow(count)

                Await Task.Run(Sub() System.Threading.Thread.Sleep(2500))
                count += 1
            End While

            If hWndInject = IntPtr.Zero Then
                'Program.InitFailed = True
                MainWindow.wnd_log.AddLine(log_cat, "Init failed (!)", "err")
                Return

            ElseIf Not hide_osd_option = -1 Then
                'make sure the the window is hidden/Visble
                If hide_osd_option = 1 Then HideOSD() Else ShowOSD()
            End If

            AddHandler Application.Current.Exit, AddressOf Application_ApplicationExit
        End Sub

        Private Function FindOSDWindow(i_try As Integer) As IntPtr

            Dim hwndRet As IntPtr = IntPtr.Zero
            Dim hwndHost As IntPtr = IntPtr.Zero
            Dim pairCount As Integer = 0

            ' search for all windows with class 'NativeHWNDHost'
            While (InlineAssignHelper(hwndHost, FindWindowEx(IntPtr.Zero, hwndHost, "NativeHWNDHost", ""))) <> IntPtr.Zero
                ' if this window has a child with class 'DirectUIHWND' it might be the volume OSD

                If FindWindowEx(hwndHost, IntPtr.Zero, "DirectUIHWND", "") <> IntPtr.Zero Then
                    ' if this is the only pair we are sure
                    If pairCount = 0 Then
                        MainWindow.wnd_log.AddLine(log_cat, "Found OSD window with try: " & i_try, "add")
                        hwndRet = hwndHost
                    End If

                    pairCount += 1

                    ' if there are more pairs the criteria has failed...
                    If pairCount > 1 Then
                        MainWindow.wnd_log.AddLine(log_cat, "error: Multiple pairs found!", "err")
                        Return IntPtr.Zero
                    End If
                End If
            End While

            ' if no window found yet, there is no OSD window at all

            If hwndRet = IntPtr.Zero Then
                MainWindow.wnd_log.AddLine(log_cat, "error: OSD window not found!", "err")
            End If

            Return hwndRet
        End Function

        Private Sub Application_ApplicationExit(sender As Object, e As EventArgs)
            ShowOSD()
        End Sub

        Public Sub HideOSD()
            hide_osd_option = 1
            If hWndInject = IntPtr.Zero Then Exit Sub

            ' SW_MINIMIZE
            ShowWindow(hWndInject, 6)
            MainWindow.wnd_log.AddLine(log_cat, "Volume OSD hidden", "add")
        End Sub

        Public Sub ShowOSD()
            hide_osd_option = 0
            If hWndInject = IntPtr.Zero Then Exit Sub

            ' SW_RESTORE
            ShowWindow(hWndInject, 9)
            MainWindow.wnd_log.AddLine(log_cat, "Volume OSD visible", "add")
        End Sub

        Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class
End Namespace
