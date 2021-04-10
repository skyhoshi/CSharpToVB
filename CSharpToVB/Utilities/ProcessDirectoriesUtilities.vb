﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis

Public Module ProcessDirectoriesUtilities

    ''' <summary>
    ''' Converts new directory from TargetDirectory/SubdirectoryName
    ''' </summary>
    ''' <param name="targetDirectory"></param>
    ''' <param name="subDirectoryName"></param>
    ''' <returns></returns>
    Private Function ConvertSourceToTargetDirectory(targetDirectory As String, subDirectoryName As String) As String
        If String.IsNullOrWhiteSpace(targetDirectory) Then
            Return ""
        End If
        Return Path.Combine(targetDirectory, New DirectoryInfo(subDirectoryName).Name)
    End Function

    Public Sub LocalUseWaitCursor(mainForm As Form1, waitCursorEnable As Boolean)
        If mainForm Is Nothing Then
            Exit Sub
        End If
        If mainForm.UseWaitCursor <> waitCursorEnable Then
            mainForm.UseWaitCursor = waitCursorEnable
            Application.DoEvents()
        End If
    End Sub

    ''' <summary>
    ''' Needed for Unit Tests
    ''' </summary>
    ''' <param name="mainForm"></param>
    ''' <param name="sourceDirectory">Start location of where to process directories</param>
    ''' <param name="targetDirectory">Start location of where to process directories</param>
    ''' <param name="stopButton">Pass Nothing for Unit Tests</param>
    ''' <param name="listBoxFileList"></param>
    ''' <param name="sourceLanguageExtension">vb or cs</param>
    ''' <param name="stats"></param>
    ''' <param name="processFileAsync"></param>
    ''' <param name="cancelToken"></param>
    ''' <returns>
    ''' False if error and user wants to stop, True if success or user wants to ignore error
    ''' </returns>
    Public Async Function ProcessDirectoryAsync(mainForm As Form1, sourceDirectory As String, targetDirectory As String, stopButton As Button, listBoxFileList As ListBox, sourceLanguageExtension As String, stats As ProcessingStats, processFileAsync As Func(Of Form1, String, String, String, List(Of String), List(Of KeyValuePair(Of String, Object)), MetadataReference(), Boolean, CancellationToken, Task(Of Boolean)), cancelToken As CancellationToken) As Task(Of Boolean)
        If processFileAsync Is Nothing Then
            Throw New ArgumentNullException(NameOf(processFileAsync))
        End If

        If stats Is Nothing Then
            Throw New ArgumentNullException(NameOf(stats))
        End If

        If String.IsNullOrWhiteSpace(sourceDirectory) OrElse Not Directory.Exists(sourceDirectory) Then
            Return True
        End If
        ' Process the list of files found in the directory.
        Try

            For Each sourcePathWithFileName As String In Directory.EnumerateFiles(path:=sourceDirectory, searchPattern:=$"*.{sourceLanguageExtension}")
                stats.FilesProcessed += 1
                If stats.LastFileNameWithPath.Length = 0 OrElse stats.LastFileNameWithPath = sourcePathWithFileName Then
                    stats.LastFileNameWithPath = ""
                    If listBoxFileList IsNot Nothing Then
                        listBoxFileList.Items.Add(New NumberedListItem($"{stats.FilesProcessed.ToString(Globalization.CultureInfo.InvariantCulture),-5} {sourcePathWithFileName}", $"{Path.Combine(targetDirectory, Path.GetFileNameWithoutExtension(sourcePathWithFileName))}.vb"))
                        listBoxFileList.SelectedIndex = listBoxFileList.Items.Count - 1
                        Application.DoEvents()
                    End If

                    If Not Await processFileAsync(mainForm,
                                                  sourcePathWithFileName,
                                                  targetDirectory,
                                                  sourceLanguageExtension,
                                                  New List(Of String) From {My.Settings.Framework},
                                                  New List(Of KeyValuePair(Of String, Object)) From {KeyValuePair.Create(Of String, Object)(My.Settings.Framework, True)},
                                                  CSharpReferences(Assembly.Load("System.Windows.Forms").Location,
                                                  optionalReference:=Nothing).ToArray,
                                                  My.Settings.SkipAutoGenerated,
                                                  cancelToken
                                                 ).ConfigureAwait(True) Then
                        SetButtonStopCursorAndCancelToken(mainForm, stopButtonVisible:=False)
                        Return False
                    End If
                    If mainForm IsNot Nothing Then
                        Dim elapsed As TimeSpan = stats._elapsedTimer.Elapsed
                        mainForm.StatusStripElapasedTimeLabel.Text = $"Elapsed Time - {elapsed.Hours}: {elapsed.Minutes}:{elapsed.Seconds}"
                        mainForm.StatusStripConversionFileProgressLabel.Text = $"Processed {stats.FilesProcessed:N0} of {stats.TotalFilesToProcess:N0} Files"
                        Application.DoEvents()
                    End If
                End If
            Next sourcePathWithFileName
        Catch ex As OperationCanceledException
            Return False
        Catch ex As Exception
            Throw
        End Try
        Dim subDirectoryEntries As String() = Directory.GetDirectories(path:=sourceDirectory)
        Try
            ' Recurse into subdirectories of this directory.
            For Each subDirectory As String In subDirectoryEntries
                Dim dirName As String = New DirectoryInfo(subDirectory).Name.ToUpperInvariant
                If (dirName = "BIN" OrElse dirName = "OBJ" OrElse dirName = "G") AndAlso
                    (mainForm Is Nothing OrElse My.Settings.SkipBinAndObjFolders) Then
                    Continue For
                End If

                If (subDirectory.EndsWith("Test\Resources", StringComparison.OrdinalIgnoreCase) OrElse subDirectory.EndsWith("Setup\Templates", StringComparison.OrdinalIgnoreCase)) AndAlso (mainForm Is Nothing OrElse My.Settings.SkipTestResourceFiles) Then
                    Continue For
                End If
                If Not Await ProcessDirectoryAsync(mainForm, subDirectory, ConvertSourceToTargetDirectory(targetDirectory, subDirectory), stopButton, listBoxFileList, sourceLanguageExtension, stats, processFileAsync, cancelToken).ConfigureAwait(True) Then
                    SetButtonStopCursorAndCancelToken(mainForm, stopButtonVisible:=False)
                    Return False
                End If
            Next subDirectory
        Catch ex As OperationCanceledException
            Return False
        Catch ex As Exception
            Throw
        End Try
        Return True
    End Function

    Friend Sub SetButtonStopCursorAndCancelToken(mainForm As Form1, stopButtonVisible As Boolean)
        If mainForm Is Nothing Then
            Exit Sub
        End If
        If stopButtonVisible Then
            mainForm.LabelErrorCount.Text = $"Number of Errors:"

        End If
        If mainForm.ButtonStopConversion IsNot Nothing Then
            mainForm.ButtonStopConversion.Visible = stopButtonVisible
        End If
        Dim enableControl As Boolean = Not stopButtonVisible
        mainForm.ConversionInput.ReadOnly = Not enableControl
        mainForm.mnuFile.Enabled = enableControl
        mainForm.mnuConvert.Enabled = enableControl
        LocalUseWaitCursor(mainForm:=mainForm, waitCursorEnable:=stopButtonVisible)
    End Sub

    Public Sub WriteTextToStream(directoryName As String, fileName As String, sourceText As String)
        If Not Directory.Exists(directoryName) Then
            Directory.CreateDirectory(directoryName)
        End If
        Using sw As New StreamWriter(Path.Combine(directoryName, fileName), append:=False)
            sw.Write(sourceText)
            sw.Close()
        End Using
    End Sub

End Module
