Imports System.IO
Imports System.Threading
Imports System.Xml

Public Class frmMain

    Dim t As Thread

    Private Sub OpenToolStripButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OpenToolStripButton.Click, OpenToolStripMenuItem.Click
        dlgFolderSelect.ShowDialog()
        NessusConversion.strFolder = dlgFolderSelect.SelectedPath()
        AddInfoToBox(strFolder)
        dlgFolderSelect.Dispose()
    End Sub

    Private Sub SaveToolStripButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveToolStripButton.Click, SaveToolStripMenuItem.Click
        dlgFileSave.ShowDialog()
        File.WriteAllText(dlgFileSave.FileName, txtStatus.Text)
        dlgFileSave.Dispose()
    End Sub

    Private Sub btnClearLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClearLog.Click
        txtStatus.Text = ""
    End Sub

    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        If txtSystem.Text = "" Then
            MsgBox("Please enter a system or environment name", MsgBoxStyle.Exclamation, "Input Validation")
            AddInfoToBox("System Name Not Specified")
            Exit Sub
        Else
            NessusConversion.strEnv = txtSystem.Text()
        End If

        If Not Directory.Exists(dlgFolderSelect.SelectedPath) Then
            MsgBox("Directory Doesn't Exist", MsgBoxStyle.Exclamation, "Directory Error")
            AddInfoToBox("Invalid Directory")
            Exit Sub
        Else
            NessusConversion.strFolder = dlgFolderSelect.SelectedPath
        End If

        If Not File.Exists(dlgCatalogSelect.FileName) Then
            MsgBox("RAFON/FP File Doesn't Exist", MsgBoxStyle.Exclamation, "File Error")
            AddInfoToBox("RAFON/FP File Doesn't Exist")
            'Exit Sub
        Else
            NessusConversion.strFile = dlgCatalogSelect.FileName
        End If

        Dim myFlag As Boolean = False
        Dim myDir() As String = Directory.GetFiles(NessusConversion.strFolder)

        For Each myFile As String In myDir
            Dim thisFile As New FileInfo(myFile)
            If thisFile.Extension.ToLower = ".nessus" Then
                myFlag = True
                Exit For
            End If
        Next

        If myFlag = False Then
            MsgBox("Directory Does Not Contain .nessus Files", MsgBoxStyle.Exclamation, "Directory Error")
            AddInfoToBox("No Files")
            Exit Sub
        End If

        Select Case FileFormat.ToLower
            Case "csv" Or "tsv"
                t = New Thread(AddressOf NessusConversion.ConvertPlainText)
            Case "xls" Or "xlsx"
                t = New Thread(AddressOf NessusConversion.ConvertExcel)
            Case Else
                AddInfoToBox("File format not recognized")
        End Select

        t.Start()

    End Sub

    Private Sub AboutToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AboutToolStripMenuItem1.Click
        Dim AboutBox1 As New NessusToExcel.AboutBox1
        AboutBox1.ShowDialog()
    End Sub

    Private Sub ToolStripButton2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton2.Click
        dlgCatalogSelect.ShowDialog()
        strFile = dlgCatalogSelect.FileName
        AddInfoToBox("RAFON/FP Catalog is: " & strFile)
        dlgCatalogSelect.Dispose()
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        t.Abort()
        AddInfoToBox("Parsing Aborted")
        
    End Sub

    Private Sub btnJustify_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnJustify.Click
        'Check to see if a box is clicked.

        'Ensure that an XML file has been selected.

        'Display the Dialog to add an entry to the XML file.

    End Sub

    Private Sub Text1_Change() Handles txtStatus.TextChanged
        txtStatus.SelectionStart = Len(txtStatus.Text)
        txtStatus.SelectionLength = 0
    End Sub
End Class
