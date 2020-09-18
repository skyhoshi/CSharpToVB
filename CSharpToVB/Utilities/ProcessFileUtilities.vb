﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Threading
Imports CSharpToVBConverter
Imports Microsoft.CodeAnalysis

#If NETCOREAPP3_1 Then

Imports VBMsgBox

#End If

Module ProcessFileUtilities

    ''' <summary>
    ''' Convert one file
    ''' </summary>
    ''' <param name="MainForm"></param>
    ''' <param name="SourceFileNameWithPath">Complete path including file name to file to be converted</param>
    ''' <param name="TargetDirectory">Complete path up to File to be converted</param>
    ''' <param name="SourceLanguageExtension">vb or cs</param>
    ''' <param name="CSPreprocessorSymbols"></param>
    ''' <param name="VBPreprocessorSymbols"></param>
    ''' <param name="OptionalReferences"></param>
    ''' <param name="SkipAutoGenerated"></param>
    ''' <param name="CancelToken"></param>
    ''' <returns>False if error and user wants to stop, True if success or user wants to ignore error.</returns>
    Friend Async Function ProcessFileAsync(MainForm As Form1, SourceFileNameWithPath As String, TargetDirectory As String, SourceLanguageExtension As String, CSPreprocessorSymbols As List(Of String), VBPreprocessorSymbols As List(Of KeyValuePair(Of String, Object)), OptionalReferences() As MetadataReference, SkipAutoGenerated As Boolean, CancelToken As CancellationToken) As Task(Of Boolean)
        If My.Settings.IgnoreFileList.Contains(SourceFileNameWithPath) Then
            Return True
        End If
        With MainForm
            .ButtonStopConversion.Visible = True
            .ConversionOutput.Text = ""
            My.Settings.MRU_Data.mnuAddToMRU(SourceFileNameWithPath)
            .UpdateLastFileMenu()
            .mnuFile.DropDownItems.FileMenuMRUUpdateUI(AddressOf .mnu_MRUList_Click)
            Dim lines As Integer = LoadInputBufferFromStream(MainForm, SourceFileNameWithPath)
            If lines > 0 Then
                Using textProgressBar As TextProgressBar = New TextProgressBar(.ConversionProgressBar)

                    ._requestToConvert = New ConvertRequest(SkipAutoGenerated, New Progress(Of ProgressReport)(AddressOf textProgressBar.Update), ._cancellationTokenSource.Token) With {
                        .SourceCode = MainForm.ConversionInput.Text
                    }
                    If Not Await Convert_Compile_ColorizeAsync(MainForm, ._requestToConvert, CSPreprocessorSymbols, VBPreprocessorSymbols, OptionalReferences, CancelToken).ConfigureAwait(True) Then
                        If ._requestToConvert.CancelToken.IsCancellationRequested Then
                            .ConversionProgressBar.Value = 0
                            Return False
                        End If
                        Dim msgBoxResult As MsgBoxResult
                        If ._doNotFailOnError Then
                            msgBoxResult = MsgBoxResult.Yes
                        Else
                            msgBoxResult = MsgBox($"Conversion failed, do you want to stop processing this file automatically in the future? Yes and No will continue processing files, Cancel will stop conversions!",
                                                 MsgBoxStyle.YesNoCancel Or MsgBoxStyle.Exclamation Or MsgBoxStyle.MsgBoxSetForeground)
                        End If
                        Select Case msgBoxResult
                            Case MsgBoxResult.Cancel
                                ._cancellationTokenSource.Cancel()
                                Return False
                            Case MsgBoxResult.No
                                Return True
                            Case MsgBoxResult.Yes
                                If Not My.Settings.IgnoreFileList.Contains(SourceFileNameWithPath) Then
                                    My.Settings.IgnoreFileList.Add(SourceFileNameWithPath)
                                    My.Settings.Save()
                                End If
                                .ListBoxErrorList.Items.Clear()
                                .LineNumbersForConversionInput.Visible = My.Settings.ShowSourceLineNumbers
                                .LineNumbersForConversionOutput.Visible = My.Settings.ShowDestinationLineNumbers
                                ._doNotFailOnError = True
                                Return True
                        End Select
                    Else
                        If Not String.IsNullOrWhiteSpace(TargetDirectory) Then
                            If ._requestToConvert.CancelToken.IsCancellationRequested Then
                                Return False
                            End If
                            If .LabelErrorCount.Text = "File Skipped" Then
                                Return True
                            End If
                            Dim NewFileName As String = Path.ChangeExtension(New FileInfo(SourceFileNameWithPath).Name, If(SourceLanguageExtension = "vb", "cs", "vb"))
                            WriteTextToStream(TargetDirectory, NewFileName, .ConversionOutput.Text)
                        End If
                        If My.Settings.PauseConvertOnSuccess Then
                            If MsgBox($"{SourceFileNameWithPath} successfully converted, Continue?",
                                      MsgBoxStyle.YesNo Or MsgBoxStyle.Question Or MsgBoxStyle.MsgBoxSetForeground) = MsgBoxResult.No Then
                                Return False
                            End If
                        End If
                    End If
                End Using
                ' 5 second delay
                Const LoopSleep As Integer = 25
                Dim Delay As Integer = (1000 * My.Settings.ConversionDelay) \ LoopSleep
                For index As Integer = 0 To Delay
                    Application.DoEvents()
                    Thread.Sleep(LoopSleep)
                    If CancelToken.IsCancellationRequested Then
                        Return False
                    End If
                Next
                Application.DoEvents()
            Else
                .ConversionOutput.Clear()
            End If
        End With
        Return True
    End Function

    ''' <summary>
    ''' Process all files in the directory passed in, recurse on any directories
    ''' that are found, and process the files they contain.
    ''' </summary>
    ''' <param name="SourceDirectory">Start location of where to process directories</param>
    ''' <param name="TargetDirectory">Start location of where to process directories</param>
    ''' <param name="LastFileNameWithPath">Pass Last File Name to Start Conversion where you left off</param>
    ''' <param name="SourceLanguageExtension">vb or cs</param>
    ''' <param name="FilesProcessed">Count of the number of tiles processed</param>
    ''' <returns>
    ''' False if error and user wants to stop, True if success or user wants to ignore error
    ''' </returns>
    Friend Async Function ProcessFilesAsync(MainForm As Form1, SourceDirectory As String, TargetDirectory As String, SourceLanguageExtension As String, Stats As ProcessingStats, CancelToken As CancellationToken) As Task(Of Boolean)
        With MainForm
            Try
                .ListBoxErrorList.Items.Clear()
                .ListBoxFileList.Items.Clear()
                SetButtonStopAndCursor(MainForm,
                                       .ButtonStopConversion,
                                       StopButtonVisible:=True)
                Stats.TotalFilesToProcess = SourceDirectory.GetFileCount(SourceLanguageExtension,
                                                                         My.Settings.SkipBinAndObjFolders,
                                                                         My.Settings.SkipTestResourceFiles
                                                                        )
                ' Process the list of files found in the directory.
                Return Await ProcessDirectoryAsync(MainForm,
                                                   SourceDirectory,
                                                   TargetDirectory,
                                                   .ButtonStopConversion,
                                                   .ListBoxFileList,
                                                   SourceLanguageExtension,
                                                   Stats,
                                                   AddressOf ProcessFileAsync,
                                                   CancelToken).ConfigureAwait(True)
            Catch ex As OperationCanceledException
                .ConversionProgressBar.Value = 0
            Catch ex As Exception
                ' don't crash on exit
                End
            Finally
                SetButtonStopAndCursor(MainForm,
                                       .ButtonStopConversion,
                                       StopButtonVisible:=False)
            End Try
        End With
        Return False
    End Function

End Module
