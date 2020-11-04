﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports CSharpToVBApp
Imports CSharpToVBConverter
Imports CSharpToVBConverter.ConversionResult

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit

Imports Xunit

Namespace ConvertDirectory.Tests

    ''' <summary>
    ''' Return False to skip test
    ''' </summary>
    <TestClass()> Public Class TestCompile
        Private _lastFileProcessed As String

        Public Shared ReadOnly Property EnableRoslynTests() As Boolean
            Get
                Return Directory.Exists(GetRoslynRootDirectory)
            End Get
        End Property

        Private Async Function TestProcessDirectoryAsync(SourceDirectory As String) As Task(Of Boolean)
            Return Await ProcessDirectoryAsync(MainForm:=Nothing, SourceDirectory, TargetDirectory:="", StopButton:=Nothing, ListBoxFileList:=Nothing, SourceLanguageExtension:="cs", New ProcessingStats(""), AddressOf Me.TestProcessFileAsync, CancellationToken.None).ConfigureAwait(continueOnCapturedContext:=True)
        End Function

        Friend Function TestProcessFileAsync(MainForm As Form1, PathWithFileName As String, TargetDirectory As String, _0 As String, CSPreprocessorSymbols As List(Of String), VBPreprocessorSymbols As List(Of KeyValuePair(Of String, Object)), OptionalReferences() As MetadataReference, SkipAutoGenerated As Boolean, CancelToken As CancellationToken) As Task(Of Boolean)
            ' Save to TargetDirectory not supported
            Assert.True(String.IsNullOrWhiteSpace(TargetDirectory))
            ' Do not delete the next line or the parameter it is needed by other versions of this routine
            _lastFileProcessed = PathWithFileName
            Using fs As FileStream = File.OpenRead(PathWithFileName)
                Dim RequestToConvert As ConvertRequest = New ConvertRequest(mSkipAutoGenerated:=True, mProgress:=Nothing, mCancelToken:=CancelToken) With {
                    .SourceCode = fs.GetFileTextFromStream()
                }

                Dim ResultOfConversion As ConversionResult = ConvertInputRequest(RequestToConvert, New DefaultVBOptions, CSPreprocessorSymbols, VBPreprocessorSymbols, CSharpReferences(Assembly.Load("System.Windows.Forms").Location, OptionalReferences).ToArray, ReportException:=Nothing, mProgress:=Nothing, CancelToken:=CancellationToken.None)
                If ResultOfConversion.ResultStatus = ResultTriState.Failure Then
                    Return Task.FromResult(False)
                End If
                Dim CompileResult As (CompileSuccess As Boolean, EmitResult As EmitResult) = CompileVisualBasicString(StringToBeCompiled:=ResultOfConversion.ConvertedCode, VBPreprocessorSymbols, DiagnosticSeverity.Error, ResultOfConversion)
                If Not CompileResult.CompileSuccess OrElse ResultOfConversion.GetFilteredListOfFailures().Any Then
                    Dim Msg As String = If(CompileResult.CompileSuccess, ResultOfConversion.GetFilteredListOfFailures()(0).GetMessage, "Fatal Compile error")
                    Throw New ApplicationException($"{PathWithFileName} failed to compile with error :{vbCrLf}{Msg}")
                    Return Task.FromResult(False)
                End If
            End Using
            Return Task.FromResult(True)
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCodeStyleAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "CodeStyle")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCoreAnalyzerDriverAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Core", "AnalyzerDriver")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCoreCodeAnalysisTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Core", "CodeAnalysisTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCoreCommandLineAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Core", "CommandLine")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCoreMSBuildTaskAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Core", "MSBuildTask")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCoreMSBuildTaskTestsAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Core", "MSBuildTaskTests")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCorePortableAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Core", "Portable")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpCSCAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "CSC")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpCSharpAnalyzerDriverAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "CSharpAnalyzerDriver")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <Timeout(100000)>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpPortableAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "Portable")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpTestCommandLineAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "Test", "CommandLine")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpTestEmitAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "Test", "Emit")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpTestSemanticAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "Test", "Semantic")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpTestSyntaxAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "Test", "Syntax")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersCSharpTestWinRTAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "CSharp", "Test", "WinRT")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersExtensionAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Extension")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersRealParserTestsAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "RealParserTests")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersServerVBCSCompilerAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Server", "VBCSCompiler")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersServerVBCSCompilerTests() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Server", "VBCSCompilerTests")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersShared() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Shared")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "Test")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryCompilersVisualStudioAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Compilers", "VisualBasic")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryDependenciesAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Dependencies")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryDeploymentAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Deployment")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesCoreAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "Core")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesCoreWpfAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "Core.Wpf")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesCSharpAsync() As Task

            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "CSharp")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesCSharpTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "CSharp.Test")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesCSharpTest2Async() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "CSharp.Test2")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesCSharpWpfAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "CSharp.Wpf")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "Test")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesTest2Async() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "Test2")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesTestUtilitiesAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "TestUtilities")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesTestUtilities2Async() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "TestUtilities2")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesTextAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "Text")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesVisualBasicAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "VisualBasic")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryEditorFeaturesVisualBasicTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "EditorFeatures", "VisualBasicTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryExpressionEvaluatorAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "ExpressionEvaluator")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryFeaturesAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Features")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryInteractiveAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Interactive")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryNuGetAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "NuGet")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryScriptingAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Scripting")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectorySetupAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Setup")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Test")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryToolsAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Tools")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryVisualStudioAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "VisualStudio")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCoreDesktopAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "Core", "Desktop")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCoreMSBuildAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "Core", "MSBuild")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCorePortableAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "Core", "Portable")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCoreTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "CoreTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCoreTestUtilitiesAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "CoreTestUtilities")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCSharpAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "CSharp")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesCSharpTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "CSharpTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesDesktopTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "DesktopTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesMSBuildTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "MSBuildTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesRemoteAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "Remote")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesSharedUtilitiesAndExtensionsAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "SharedUtilitiesAndExtensions")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesVisualBasicAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "VisualBasic")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

        <Trait("Category", "SkipWhenLiveUnitTesting")>
        <ConditionalFact(NameOf(EnableRoslynTests))>
        Public Async Function ConvertDirectoryWorkspacesVisualBasicTestAsync() As Task
            Assert.True(Await Me.TestProcessDirectoryAsync(Path.Combine(GetRoslynRootDirectory(), "src", "Workspaces", "VisualBasicTest")).ConfigureAwait(True), $"Failing file {_lastFileProcessed}")
        End Function

    End Class

End Namespace
