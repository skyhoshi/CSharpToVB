﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Extensions
Imports Microsoft.CodeAnalysis
Imports ProgressReportLibrary
Imports SupportClasses
Imports CS = Microsoft.CodeAnalysis.CSharp
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace Utilities
    Public Module DoConversion
' ReSharper disable once InconsistentNaming
        Friend s_thisLock As New Object

        Private Function GetDefaultVersionForLanguage(language As String) As Integer
            If language Is Nothing Then
                Throw New ArgumentNullException(NameOf(language))
            End If
            If language.StartsWith("cs", StringComparison.OrdinalIgnoreCase) Then
                Return CS.LanguageVersion.CSharp8
            End If
            If language.StartsWith("vb", StringComparison.OrdinalIgnoreCase) Then
                Return VB.LanguageVersion.Latest
            End If
            Throw New ArgumentException($"{language} not supported!")
        End Function

        Public Function ConvertInputRequest(requestToConvert As ConvertRequest, defaultVbOptions As DefaultVbOptions, csPreprocessorSymbols As List(Of String), vbPreprocessorSymbols As List(Of KeyValuePair(Of String, Object)), optionalReferences() As MetadataReference, reportException As Action(Of Exception), mProgress As IProgress(Of ProgressReport), cancelToken As CancellationToken) As ConversionResult
            Dim codeWithOptions As CodeWithOptions = New CodeWithOptions(requestToConvert).SetFromLanguageVersion(GetDefaultVersionForLanguage("cs")).SetToLanguageVersion(GetDefaultVersionForLanguage("vb"))
            SyncLock requestToConvert.UsedStacks
                requestToConvert.UsedStacks.Push(New Dictionary(Of String, SymbolTableEntry)(StringComparer.Ordinal))
            End SyncLock
            If requestToConvert.SourceCode Is Nothing Then
                Throw New Exception($"{NameOf(requestToConvert.SourceCode)} should not be Nothing")
            End If
            If optionalReferences Is Nothing Then
                Throw New ArgumentNullException(NameOf(optionalReferences))
            End If
            Dim tree As SyntaxTree = ParseCSharpSource(requestToConvert.SourceCode, csPreprocessorSymbols)
            Dim csOptions As CS.CSharpCompilationOptions =
                New CS.CSharpCompilationOptions(
                                outputKind:=Nothing,
                                reportSuppressedDiagnostics:=False,
                                moduleName:=Nothing,
                                mainTypeName:=Nothing,
                                scriptClassName:=Nothing,
                                usings:=Nothing,
                                OptimizationLevel.Debug,
                                checkOverflow:=False,
                                allowUnsafe:=True,
                                cryptoKeyContainer:=Nothing,
                                cryptoKeyFile:=Nothing,
                                cryptoPublicKey:=Nothing,
                                delaySign:=Nothing,
                                Platform.AnyCpu,
                                ReportDiagnostic.Default,
                                warningLevel:=4,
                                specificDiagnosticOptions:=Nothing,
                                concurrentBuild:=True,
                                deterministic:=False,
                                xmlReferenceResolver:=Nothing,
                                sourceReferenceResolver:=Nothing,
                                metadataReferenceResolver:=Nothing,
                                assemblyIdentityComparer:=Nothing,
                                strongNameProvider:=Nothing,
                                publicSign:=False
                                )
            Dim compilation As Compilation =
                CS.CSharpCompilation.Create(assemblyName:=NameOf(Conversion),
                                            {tree},
                                            optionalReferences,
                                            csOptions
                                           )
            Try
                Dim sourceTree As CS.CSharpSyntaxNode = DirectCast(tree.GetRoot(cancelToken), CS.CSharpSyntaxNode)
                If requestToConvert.SkipAutoGenerated AndAlso
                    sourceTree.SyntaxTree.IsGeneratedCode(Function(t As SyntaxTrivia) As Boolean
                                                              Return t.IsComment OrElse t.IsRegularOrDocComment
                                                          End Function, cancelToken) Then
                    Return New ConversionResult(Array.Empty(Of Exception))
                Else
                    Dim convertedNode As VB.VisualBasicSyntaxNode =
                            sourceTree.DoConversion(compilation.GetSemanticModel(tree, ignoreAccessibility:=True),
                                                    defaultVbOptions,
                                                    codeWithOptions.Request.SkipAutoGenerated,
                                                    reportException,
                                                    mProgress,
                                                    cancelToken
                                                   )
                    requestToConvert.UsedStacks.Clear()
                    Return New ConversionResult(convertedNode, LanguageNames.CSharp, LanguageNames.VisualBasic, vbPreprocessorSymbols)
                End If
            Catch ex As OperationCanceledException
                Return New ConversionResult(Array.Empty(Of Exception))
            Catch ex As Exception
                Return New ConversionResult(ex)
            End Try
        End Function

    End Module
End Namespace
