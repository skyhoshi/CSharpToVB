﻿Option Explicit On
Option Infer Off
Option Strict On

Imports System.Runtime.InteropServices

Public Class OpenFolderDialog
    Implements IDisposable

    ''' <summary>
    ''' Gets of Sets the descriptive text displayed above the tree view control in the dialog box
    ''' </summary>
    Public Description As String

    ''' <summary>
    ''' Gets/sets directory in which dialog will be open if there is no recent directory available.
    ''' </summary>
    Public Property DefaultFolder As String

    Public Property FolderMustExist As Boolean

    ''' <summary>
    ''' Gets/sets folder in which dialog will be open.
    ''' </summary>
    Public Property InitialFolder As String

    ''' <summary>
    ''' Gets selected folder.
    ''' </summary>
    Public Property SelectedPath As String

    ''' <summary>
    ''' Gets of sets a value indicating whether the New Folder button appears in the folder browser dialog box.
    ''' </summary>
    ''' <returns></returns>
    Public Property ShowNewFolderButton As Boolean

    'just to have possibility of Using statement.
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Function ShowDialog(ByVal owner As IWin32Window) As DialogResult
        Dim frm As IFileDialog = DirectCast(New FileOpenDialogRCW(), IFileDialog)
        Dim Options As FILEOPENDIALOGOPTIONS
        frm.GetOptions(Options)
        Options = Options Or FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS Or FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM Or FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE Or FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE Or FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT Or FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST
        If Not Me.FolderMustExist Then
            Options = Options Xor FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST Xor FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST
        End If
        frm.SetOptions(Options)
        frm.SetTitle(Me.Description)
        If Me.InitialFolder IsNot Nothing Then
            Dim directoryShellItem As IShellItem = Nothing
            Dim riid As New Guid(ComGuids.IShellItem)
            'IShellItem
            If SHCreateItemFromParsingName(Me.InitialFolder, IntPtr.Zero, riid, directoryShellItem) = HRESULT.S_OK Then
                frm.SetFolder(directoryShellItem)
            End If
        End If
        If Me.DefaultFolder IsNot Nothing Then
            Dim directoryShellItem As IShellItem = Nothing
            Dim riid As New Guid(ComGuids.IShellItem)
            'IShellItem
            If SHCreateItemFromParsingName(Me.DefaultFolder, IntPtr.Zero, riid, directoryShellItem) = HRESULT.S_OK Then
                frm.SetDefaultFolder(directoryShellItem)
            End If
        End If

        If frm.Show(owner.Handle) = HRESULT.S_OK Then
            Dim shellItem As IShellItem = Nothing
            If frm.GetResult(shellItem) = HRESULT.S_OK Then
                Dim pszString As IntPtr
                If shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, pszString) = HRESULT.S_OK Then
                    If pszString <> IntPtr.Zero Then
                        Try
                            Me.SelectedPath = Marshal.PtrToStringAuto(pszString)
                            Return DialogResult.OK
                        Finally
                            Marshal.FreeCoTaskMem(pszString)
                        End Try
                    End If
                End If
            End If
        End If
        Return DialogResult.Cancel
    End Function

End Class