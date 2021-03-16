﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ReassignedVariable
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ReassignedVariable
    <ExportLanguageService(GetType(IReassignedVariableService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReassignedVariableService
        Inherits AbstractReassignedVariableService(Of
            ParameterSyntax,
            VariableDeclaratorSyntax,
            ModifiedIdentifierSyntax,
            ModifiedIdentifierSyntax,
            IdentifierNameSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub AddVariables(declarator As VariableDeclaratorSyntax, ByRef temporaryArray As TemporaryArray(Of ModifiedIdentifierSyntax))
            For Each id In declarator.Names
                temporaryArray.Add(id)
            Next
        End Sub

        Protected Overrides Function GetIdentifierOfVariable(variable As ModifiedIdentifierSyntax) As SyntaxToken
            Return variable.Identifier
        End Function

        Protected Overrides Function GetIdentifierOfSingleVariableDesignation(variable As ModifiedIdentifierSyntax) As SyntaxToken
            Return variable.Identifier
        End Function

        Protected Overrides Function GetParentScope(localDeclaration As SyntaxNode) As SyntaxNode
            Dim current = localDeclaration
            While current IsNot Nothing
                If TypeOf current Is StatementSyntax Then
                    Exit While
                End If

                current = current.Parent
            End While

            If TypeOf current Is LocalDeclarationStatementSyntax Then
                Return current.Parent
            End If

            Return current
        End Function
    End Class
End Namespace

