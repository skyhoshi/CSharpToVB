﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports CS = Microsoft.CodeAnalysis.CSharp
Imports VB = Microsoft.CodeAnalysis.VisualBasic
Imports VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Public Module TriviaExtensions

    <Extension>
    Friend Function AdjustWhitespace(Whitespace As SyntaxTrivia, adjusment As Integer) As SyntaxTrivia
        Return VBFactory.Whitespace(StrDup(Math.Max(Whitespace.ToString.Length - adjusment, 1), " "c))
    End Function

    <Extension>
    Friend Function AdjustWhitespace(Trivia As SyntaxTrivia, nextTrivia As SyntaxTrivia, afterLineContinue As Boolean) As SyntaxTrivia
        If Trivia.Span.Length = nextTrivia.Span.Length Then
            Return Trivia
        End If
        Dim lineContinueOffset As Integer = If(afterLineContinue, 2, 0)
        If Trivia.Span.Length > nextTrivia.Span.Length Then
            Return VBFactory.Whitespace(New String(" "c, Math.Max(Trivia.FullWidth - lineContinueOffset, 1)))
        End If
        Return VBFactory.Whitespace(New String(" "c, Math.Max(nextTrivia.FullWidth - lineContinueOffset, 1)))
    End Function

    <Extension>
    Friend Function DirectiveNotAllowedHere(Trivia As SyntaxTrivia) As List(Of SyntaxTrivia)
        Dim NewTriviaList As New List(Of SyntaxTrivia)
        Dim LeadingTriviaList As New List(Of SyntaxTrivia) From {
            SpaceTrivia,
            LineContinuation,
            SpaceTrivia
        }
        Dim TriviaAsString As String = ""

        If Trivia.IsKind(VB.SyntaxKind.DisabledTextTrivia) Then
            NewTriviaList.AddRange(LeadingTriviaList)
            NewTriviaList.Add(VBFactory.CommentTrivia($" ' TODO VB does not allow Disabled Text here, original text:"))
            NewTriviaList.Add(VBEOLTrivia)
            Dim TextStrings() As String = Trivia.ToFullString.SplitLines
            For Each TriviaAsString In TextStrings
                NewTriviaList.AddRange(LeadingTriviaList)
                NewTriviaList.Add(VBFactory.CommentTrivia($" ' {TriviaAsString}".Replace("  ", " ", StringComparison.Ordinal).TrimEnd))
                NewTriviaList.Add(VBEOLTrivia)
            Next
            If NewTriviaList.Last.IsKind(VB.SyntaxKind.EndOfLineTrivia) Then
                NewTriviaList.RemoveAt(NewTriviaList.Count - 1)
            End If
            Return NewTriviaList
        End If

        Select Case Trivia.RawKind
            Case VB.SyntaxKind.IfDirectiveTrivia
                TriviaAsString = $"#If {Trivia.ToFullString.Substring("#if".Length).Trim.WithoutNewLines(" "c)}"
            Case VB.SyntaxKind.ElseDirectiveTrivia
                TriviaAsString = $"#Else {Trivia.ToFullString.Substring("#Else".Length).Trim.WithoutNewLines(" "c)}"
            Case VB.SyntaxKind.ElseIfDirectiveTrivia
                TriviaAsString = $"#ElseIf {Trivia.ToFullString.Substring("#Else If".Length).Trim.WithoutNewLines(" "c)}"
            Case VB.SyntaxKind.EndIfDirectiveTrivia
                TriviaAsString = $"#EndIf {Trivia.ToFullString.Substring("#End if".Length).Trim.WithoutNewLines(" "c)}"
            Case VB.SyntaxKind.DisableWarningDirectiveTrivia
                TriviaAsString = $"#Disable Warning Directive {Trivia.ToFullString.Substring("#Disable Warning".Length).Trim.WithoutNewLines(" "c)}"
            Case VB.SyntaxKind.EnableWarningDirectiveTrivia
                TriviaAsString = $"#Enable Warning Directive {Trivia.ToFullString.Substring("#Enable Warning".Length).Trim.WithoutNewLines(" "c)}"
            Case Else
                Stop
        End Select
        Const Msg As String = " ' TODO VB does not allow directives here, original directive: "
        NewTriviaList = New List(Of SyntaxTrivia) From {
            SpaceTrivia,
            LineContinuation,
            VBEOLTrivia,
            SpaceTrivia,
            LineContinuation,
            SpaceTrivia,
            VBFactory.CommentTrivia($"{Msg}{TriviaAsString}".Replace("  ", " ", StringComparison.Ordinal).TrimEnd)
            }
        Return NewTriviaList
    End Function

    <Extension>
    Friend Function FullWidth(Trivia As SyntaxTrivia) As Integer
        Return Trivia.FullSpan.Length
    End Function

    <Extension>
    Friend Function IsComment(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsSingleLineComment OrElse Trivia.IsMultiLineComment OrElse Trivia.IsDocComment
    End Function

    <Extension>
    Friend Function IsCommentOrDirectiveTrivia(t As SyntaxTrivia) As Boolean
        Return t.IsComment OrElse t.IsDirective
    End Function

    <Extension>
    Friend Function IsDocComment(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(VB.SyntaxKind.DocumentationCommentExteriorTrivia) OrElse Trivia.IsKind(VB.SyntaxKind.DocumentationCommentTrivia)
    End Function

    <Extension>
    Friend Function IsEndOfLine(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(CS.SyntaxKind.EndOfLineTrivia) OrElse
            Trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia)
    End Function

    <Extension>
    Friend Function IsKind(Trivia As SyntaxTrivia, ParamArray kinds() As VB.SyntaxKind) As Boolean
        For Each kind As VB.SyntaxKind In kinds
            If Trivia.IsKind(kind) Then
                Return True
            End If
        Next
        Return False
    End Function

    <Extension>
    Friend Function IsMultiLineComment(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(CS.SyntaxKind.MultiLineCommentTrivia) OrElse
            Trivia.IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia) OrElse
            Trivia.IsKind(CS.SyntaxKind.MultiLineDocumentationCommentTrivia)
    End Function

    <Extension>
    Friend Function IsMultiLineDocComment(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(CS.SyntaxKind.MultiLineDocumentationCommentTrivia)
    End Function

    <Extension>
    Friend Function IsNone(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.RawKind = 0
    End Function

    <Extension>
    Friend Function IsRegularOrDocComment(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsSingleLineComment() OrElse Trivia.IsMultiLineComment() OrElse Trivia.IsDocComment()
    End Function

    <Extension>
    Friend Function IsSingleLineComment(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia) OrElse
            Trivia.IsKind(CS.SyntaxKind.SingleLineDocumentationCommentTrivia) OrElse
            Trivia.IsKind(VB.SyntaxKind.CommentTrivia)
    End Function

    <Extension>
    Friend Function IsWhitespace(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(CS.SyntaxKind.WhitespaceTrivia) OrElse
            Trivia.IsKind(CS.SyntaxKind.EndOfLineTrivia) OrElse
            Trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) OrElse
            Trivia.IsKind(VB.SyntaxKind.WhitespaceTrivia)
    End Function

    <Extension>
    Friend Function IsWhitespaceOrEndOfLine(Trivia As SyntaxTrivia) As Boolean
        Return Trivia.IsKind(CS.SyntaxKind.WhitespaceTrivia) OrElse
            Trivia.IsKind(CS.SyntaxKind.EndOfLineTrivia) OrElse
            Trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) OrElse
            Trivia.IsKind(VB.SyntaxKind.WhitespaceTrivia)
    End Function

End Module