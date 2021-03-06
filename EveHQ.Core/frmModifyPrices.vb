﻿'==============================================================================
'
' EveHQ - An Eve-Online™ character assistance application
' Copyright © 2005-2014  EveHQ Development Team
'
' This file is part of EveHQ.
'
' The source code for EveHQ is free and you may redistribute 
' it and/or modify it under the terms of the MIT License. 
'
' Refer to the NOTICES file in the root folder of EVEHQ source
' project for details of 3rd party components that are covered
' under their own, separate licenses.
'
' EveHQ is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
' license below for details.
'
' ------------------------------------------------------------------------------
'
' The MIT License (MIT)
'
' Copyright © 2005-2014  EveHQ Development Team
'
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in
' all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
' THE SOFTWARE.
'
' ==============================================================================

Imports DevComponents.AdvTree
Imports DevComponents.DotNetBar
Imports EveHQ.EveData
Imports System.Windows.Forms

Public Class FrmModifyPrices

#Region "Class Variables"

    ReadOnly _itemIDList As New List(Of Integer)
    Dim _pricesChanged As Boolean = False

#End Region

#Region "Form Constructors and related routines"

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Public Sub New(itemIDs As List(Of Integer))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _itemIDList = itemIDs
        UpdatePriceMatrix()

    End Sub

    Private Sub UpdatePriceMatrix()

        ' Set style for the price list
        Dim numberStyle As New ElementStyle
        numberStyle.TextAlignment = eStyleTextAlignment.Far

        adtPrices.BeginUpdate()
        adtPrices.Nodes.Clear()

        For Each itemID As Integer In _itemIDList

            Dim item As EveType = StaticData.Types(itemID)
            Dim itemNode As New Node
            itemNode.Text = item.Name
            itemNode.Name = item.ID.ToString
            itemNode.Image = ImageHandler.GetImage(item.Id, 24)

            ' Add Market Price cell
            Dim marketPrice As Double
            marketPrice = DataFunctions.GetPrice(itemID)
            Dim qCell As New Cell(marketPrice.ToString("N2"))
            qCell.StyleNormal = numberStyle
            itemNode.Cells.Add(qCell)

            ' Add Custom Price cell
            Dim customPrice As Double = 0
            If HQ.CustomPriceList.ContainsKey(itemID) Then
                customPrice = HQ.CustomPriceList(itemID)
            End If
            Dim mlCell As New Cell(customPrice.ToString("N2"))
            mlCell.StyleNormal = numberStyle
            itemNode.Cells.Add(mlCell)

            ' Add Node to the list
            adtPrices.Nodes.Add(itemNode)

        Next
        adtPrices.EndUpdate()

    End Sub

#End Region

#Region "Cell Editing Routines"

    Private Sub adtPrices_AfterCellEdit(sender As Object, e As CellEditEventArgs) Handles adtPrices.AfterCellEdit
        ' Check the new text is valid before comparison
        If e.NewText = "" Or IsNumeric(e.NewText) = False Then
            e.NewText = "0"
        End If
        If CDbl(e.Cell.Text) <> CDbl(e.NewText) Then
            _pricesChanged = True
        End If
    End Sub

#End Region

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ' Check if the prices have changed before closing
        If _pricesChanged = True Then
            Const Msg As String = "At least one price has changed. Are you sure you wish to cancel the changes?"
            Dim reply As DialogResult = MessageBox.Show(Msg, "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If reply = DialogResult.Yes Then
                DialogResult = DialogResult.Cancel
                Close()
            Else
                Exit Sub
            End If
        Else
            DialogResult = DialogResult.Cancel
            Close()
        End If
    End Sub

    Private Sub btnAccept_Click(sender As Object, e As EventArgs) Handles btnAccept.Click
        ' Check if anything has changed first
        If _pricesChanged = False Then
            DialogResult = DialogResult.Cancel
            Close()
        Else
            ' Check we really want to save the prices
            Const Msg As String = "Are you sure you wish to save the changes to the price databases?"
            Dim reply As DialogResult = MessageBox.Show(Msg, "Confirm Price Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If reply = DialogResult.Yes Then
                ' Update the prices
                Call UpdatePrices()
                ' Close the form
                DialogResult = DialogResult.OK
                Close()
            Else
                Exit Sub
            End If
        End If
    End Sub

    Private Sub UpdatePrices()

        ' Check if everything is filled out as it should be
        For Each priceNode As Node In adtPrices.Nodes
            If IsNumeric(priceNode.Cells(1).Text) = False Then
                MessageBox.Show("At least one item contains an invalid market price - you must enter a valid price for this item.", "Error In Price", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            Else
                If CDbl(priceNode.Cells(1).Text) < 0 Then
                    MessageBox.Show("At least one item contains a negative market price - you cannot enter a negative price for this item.", "Error In Price", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                End If
            End If
            If IsNumeric(priceNode.Cells(2).Text) = False Then
                MessageBox.Show("At least one item contains an invalid custom price - you must enter a valid price for this item.", "Error In Price", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            Else
                If CDbl(priceNode.Cells(2).Text) < 0 Then
                    MessageBox.Show("At least one item contains a negative custom price - you cannot enter a negative price for this item.", "Error In Price", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                End If
            End If
        Next

        ' Add the prices to a price list for updating
        Dim customPriceList As New Dictionary(Of Integer, Double)
        For Each priceNode As Node In adtPrices.Nodes
            Dim itemID As Integer = CInt(priceNode.Name)
            Dim customPrice As Double = CDbl(priceNode.Cells(2).Text)

            ' Set the custom price
            If customPrice > 0 Then
                customPriceList.Add(itemID, customPrice)
            ElseIf customPrice = 0 Then
                Call CustomDataFunctions.DeleteCustomPrice(itemID)
            End If

            ' Update the custom prices
            If customPriceList.Count > 0 Then
                Call CustomDataFunctions.SetCustomPrices(customPriceList)
            End If

        Next

    End Sub



End Class