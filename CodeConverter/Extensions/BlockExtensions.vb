﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports CSharpToVBConverter.CSharpToVBVisitors.CSharpConverter
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp.Syntax

Imports CS = Microsoft.CodeAnalysis.CSharp
Imports Factory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory
Imports VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Module BlockExtensions

    <Extension>
    Friend Function GetBodyStatements(block As BlockSyntax, nodeVisitor As NodesVisitor, visitor As MethodBodyVisitor, isEvent As Boolean) As SyntaxList(Of VBS.StatementSyntax)
        Dim statements As New List(Of VBS.StatementSyntax)
        For Each localFunction As LocalFunctionStatementSyntax In block.DescendantNodes().OfType(Of LocalFunctionStatementSyntax).ToList()
            Dim emptyStatement As VBS.StatementSyntax = localFunction.Accept(visitor)(0)
            If emptyStatement.GetLeadingTrivia.ContainsCommentOrDirectiveTrivia OrElse
            emptyStatement.GetTrailingTrivia.ContainsCommentOrDirectiveTrivia Then
                statements.Add(emptyStatement)
            End If
        Next
        For Each s As StatementSyntax In block.Statements
            If s.IsKind(CS.SyntaxKind.LocalFunctionStatement) Then
                Continue For
            End If
            If isEvent Then
                Dim cssExpr As ExpressionStatementSyntax = TryCast(s, ExpressionStatementSyntax)
                If cssExpr IsNot Nothing Then
                    Dim assignStmt As AssignmentExpressionSyntax = TryCast(cssExpr.Expression, AssignmentExpressionSyntax)

                    If assignStmt IsNot Nothing Then
                        If assignStmt.Kind = CS.SyntaxKind.SimpleAssignmentExpression Then
                            If assignStmt.Right.Kind() = CS.SyntaxKind.NullLiteralExpression Then
                                statements.Add(Factory.RemoveHandlerStatement(CType(assignStmt.Left.Accept(nodeVisitor), VBS.ExpressionSyntax), Factory.IdentifierName("value")).WithTrailingEol())
                                Continue For
                            End If
                        ElseIf assignStmt.Kind = CS.SyntaxKind.AddAssignmentExpression Then
                            statements.Add(Factory.AddHandlerStatement(CType(assignStmt.Left.Accept(nodeVisitor), VBS.ExpressionSyntax), CType(assignStmt.Right.Accept(nodeVisitor), VBS.ExpressionSyntax)).WithTrailingEol())
                            Continue For
                        ElseIf assignStmt.Kind = CS.SyntaxKind.SubtractAssignmentExpression Then
                            statements.Add(Factory.RemoveHandlerStatement(CType(assignStmt.Left.Accept(nodeVisitor), VBS.ExpressionSyntax), CType(assignStmt.Right.Accept(nodeVisitor), VBS.ExpressionSyntax)).WithTrailingEol())
                            Continue For
                        End If
                    End If
                End If
            End If
            statements.AddRange(s.Accept(visitor))
        Next
        If statements.Any Then
            If block.OpenBraceToken.LeadingTrivia.ContainsCommentOrDirectiveTrivia Then
                statements(0) = statements(0).WithPrependedLeadingTrivia(block.OpenBraceToken.LeadingTrivia.ConvertTriviaList())
            End If
        End If

        Return Factory.List(statements)
    End Function

End Module
