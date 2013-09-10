Imports System.IO
Imports System.Threading
Imports System.Xml
Imports Microsoft.Office.Interop
Imports System.Collections.Specialized

Module NessusConversion

    Public strFolder As String = String.Empty
    Public strFile As String = String.Empty
    Public strEnv As String = String.Empty
    Public OutputFile As String = String.Empty
    Public FileFormat As String = "xls"
    Public keepProcessing As Boolean = True
    Public myForm As New frmMain
    Const xlCellTypeLastCell As Integer = 11
    Const SheetCount As Integer = 3
    Dim RowCount(SheetCount) As Integer
    Dim oExcel As Excel.Application
    Dim oWB As Excel.Workbook
    Dim oSheet As Excel.Worksheet
    Dim oRng As Excel.Range

    Sub Main()
        Dim arrArgs = My.Application.CommandLineArgs
        Dim CarryOn As Boolean = False

        If arrArgs.Count - 1 = -1 Then
            System.Windows.Forms.Application.Run(myForm)

            'ShowHelp("Folder path not correctly identified.")

        Else
            For i = 0 To arrArgs.Count - 1
                Select Case True
                    Case arrArgs(i).ToLower.StartsWith("/d")
                        Try
                            Dim colonPlace As Integer = arrArgs.Item(i).IndexOf(":") + 1
                            strFolder = arrArgs.Item(i).Substring(colonPlace, arrArgs.Item(i).Length - colonPlace)

                        Catch e As Exception
                            ShowHelp("Folder path not correctly identified.")
                            Exit Sub
                        End Try

                        If Not Directory.Exists(strFolder) Then
                            ShowHelp("Folder path (" & strFolder & ") not found.")
                            Exit Sub
                        End If

                    Case arrArgs(i).ToLower.StartsWith("/e")
                        Try
                            Dim colonPlace As Integer = arrArgs.Item(i).IndexOf(":") + 1
                            strEnv = arrArgs.Item(i).Substring(colonPlace, arrArgs.Item(i).Length - colonPlace)
                            strEnv.Replace("\", " ").Replace("/", " ").Replace(":", " ").Replace("?", " ").Replace("*", " ").Replace("[", " ").Replace("]", " ")

                        Catch e As Exception
                            ShowHelp("Environment name not correctly identified.")
                            Exit Sub
                        End Try

                        If strEnv = "" Then
                            ShowHelp("Environment name invalid.")
                            Exit Sub
                        ElseIf strEnv.Length > 13 Then
                            Console.WriteLine("Environment name is longer than 13 characters, it will be truncated.")
                        End If


                    Case arrArgs(i).ToLower.StartsWith("/x")
                        Try
                            Dim colonPlace As Integer = arrArgs.Item(i).IndexOf(":") + 1
                            strFile = arrArgs.Item(i).Substring(colonPlace, arrArgs.Item(i).Length - colonPlace)

                        Catch e As Exception
                            ShowHelp("Exclusion file path not correctly identified.")
                            Exit Sub
                        End Try

                        If Not File.Exists(strFile) Then
                            ShowHelp("Exclusion file (" & strFile & ") not found.")
                            Exit Sub
                        End If

                    Case arrArgs(i).ToLower.StartsWith("/o")
                        Try
                            Dim colonPlace As Integer = arrArgs.Item(i).IndexOf(":") + 1
                            OutputFile = arrArgs.Item(i).Substring(colonPlace, arrArgs.Item(i).Length - colonPlace)

                        Catch e As Exception
                            ShowHelp("Excel output file path not correctly identified.")
                            Exit Sub
                        End Try

                        If File.Exists(OutputFile) Then
                            ShowHelp("Excel output file (" & OutputFile & ") already exists.")
                            Exit Sub
                        End If

                    Case arrArgs(i).ToLower.StartsWith("/f")
                        Try
                            Dim colonPlace As Integer = arrArgs.Item(i).IndexOf(":") + 1
                            FileFormat = arrArgs.Item(i).Substring(colonPlace, arrArgs.Item(i).Length - colonPlace)

                        Catch e As Exception
                            ShowHelp("File Format not correctly identified.")
                            Exit Sub
                        End Try

                        If FileFormat.ToLower <> "xls" And FileFormat.ToLower <> "xlsx" And FileFormat.ToLower <> "csv" And FileFormat.ToLower <> "tsv" Then
                            ShowHelp("File format provided (" & FileFormat.ToLower & ") is not available.  Options are: xls, csv or tsv.")
                            Exit Sub
                        End If

                    Case arrArgs(i).ToLower.StartsWith("/y")
                        CarryOn = True

                    Case Else
                        ShowHelp()
                        Exit Sub

                End Select
            Next

            If Not CarryOn Then
                Dim line As String
                Console.WriteLine()
                Console.WriteLine("Using Directory: " & strFolder)
                Console.WriteLine("Using Exclusion File: " & strFile)
                Console.WriteLine("Using Env: " & strEnv)
                Console.WriteLine("Output File Path: " & OutputFile)

                Do
                    Console.Write("Are these values correct? (y/n)")
                    line = Console.ReadLine()
                    If Not line.ToLower.StartsWith("y") Then
                        Console.WriteLine("Answer is not yes... Exiting.")
                        Exit Sub
                    End If
                Loop While line Is Nothing
            End If

            Select Case FileFormat.ToLower
                Case "csv"
                    ConvertPlainText()
                Case "tsv"
                    ConvertPlainText()
                Case "xls"
                    ConvertExcel()
                Case "xlsx"
                    ConvertExcel()
                Case "excel"
                    ConvertExcel()
                Case Else
                    ShowHelp("File format (" & FileFormat.ToLower & ") not recognized.")
            End Select
        End If

    End Sub

    Public Sub ConvertExcel()

        ' Start Excel and get Application object.
        oExcel = CreateObject("Excel.Application")
        oExcel.Visible = False
        oExcel.UserControl = False

        AddInfoToBox("Running (Excel will be shown after completion) ... ")

        ' Get a new workbook.
        oWB = oExcel.Workbooks.Add
        oSheet = oWB.ActiveSheet

        Dim myDir() As String = Directory.GetFiles(strFolder)

        For i As Integer = 1 To SheetCount
            DoHeader(i)
        Next

        For Each myFile As String In myDir
            Dim thisFile As New FileInfo(myFile)

            If thisFile.Extension.ToLower = ".nessus" Then
                Dim myXml As New XmlDocument
                Try
                    myXml.Load(thisFile.FullName)
                Catch ex As Exception
                    MsgBox("XML not formatted properly or has been truncated.", MsgBoxStyle.Exclamation, "XML Error")
                    AddInfoToBox("XML not formatted properly or has been truncated, XML could not be loaded." & vbCrLf & ex.Message)
                    Exit For
                End Try

                Dim xmlHosts As XmlNodeList = myXml.GetElementsByTagName("ReportHost")

                For i As Integer = 0 To xmlHosts.Count - 1
                    Dim host As String = String.Empty, port As String = String.Empty, pluginName As String = String.Empty
                    Dim pluginID As String = String.Empty, severity As Integer = 0
                    Dim synopsis As String = String.Empty, description As String = String.Empty, solution As String = String.Empty
                    Dim strSummary As String = String.Empty

                    'Future Variables
                    'Dim cve As String = String.Empty, xfref As String = String.Empty
                    'Dim cvss_vector As String = String.Empty, cvss_base_score As String = String.Empty, 
                    'see_also As String = String.Empty
                    'Dim plugin_publication_date As Date, vuln_publication_date As Date, risk_factor As String = String.Empty

                    Try
                        host = xmlHosts.Item(i).Attributes("name").InnerText
                        AddInfoToBox(host)
                    Catch ex As Exception
                        MsgBox("Host Name Could Not be Set", MsgBoxStyle.Exclamation, "XML Error")
                        AddInfoToBox("Host Name could not be set")
                    End Try

                    Dim xmlItems As XmlNodeList = xmlHosts.Item(i).ChildNodes

                    For Each Item As XmlNode In xmlItems
                        If Item.Name = "ReportItem" Then

                            Try
                                severity = CInt(Item.Attributes("severity").Value)
                            Catch ex As Exception
                                AddInfoToBox("Severity Not Found")
                            End Try

                            'If Severity is 0 then we don't want it.
                            If severity > 0 Then
                                Try
                                    pluginName = Item.Attributes("pluginName").Value
                                Catch ex As Exception
                                    AddInfoToBox("pluginName Not Found")
                                End Try

                                Try
                                    pluginID = Item.Attributes("pluginID").Value
                                Catch ex As Exception
                                    AddInfoToBox("pluginID Not Found")
                                End Try

                                Try
                                    port = Item.Attributes("port").Value
                                Catch ex As Exception
                                    AddInfoToBox("port Not Found")
                                End Try

                                'This is the spot where the exception catalog will do its thing.
                                'Check the file for the pluginID and host variables to see if this should be printed.

                                AddInfoToBox("Plugin ID: " & pluginID & "  Plugin Name: " & pluginName)
                                For Each Detail As XmlNode In Item.ChildNodes
                                    Select Case Detail.Name
                                        Case "synopsis"
                                            synopsis = Detail.InnerText
                                        Case "description"
                                            description = Detail.InnerText
                                        Case "solution"
                                            solution = Detail.InnerText
                                    End Select
                                Next

                                'Bring Them Together
                                strSummary = synopsis & vbCrLf & description & vbCrLf & solution

                                AddInfoToBox("Summary: " & vbCrLf & strSummary)

                                'Send to Excel
                                DoXL(pluginID, pluginName, severity, strSummary, port, host)

                                'Bounce out if the user wants to stop the madness.
                                If keepProcessing = False Then
                                    myForm.btnGo.Enabled = True
                                    Exit Sub
                                End If

                                ' This Else is a response to a 0 severity rating
                                'Else

                            End If
                        End If
                    Next
                Next
            End If
        Next

        For i As Integer = 1 To SheetCount
            WrapUp(i)
        Next

        CreatePivotTable()

        If myForm.Visible = False Then  'If the person is not running the GUI, then use the /o attribute from the command line
            If File.Exists(OutputFile) Then
                File.Delete(OutputFile)
            End If
            If Not OutputFile = "" Then
                oWB.SaveAs(OutputFile)
                oWB.Close()
                oWB = Nothing
                oExcel.Quit()

            Else 'If not display the spreadsheet and let them handle it.
                oExcel.Visible = True

            End If
        Else
            oExcel.Visible = True
        End If

    End Sub

    Public Sub AddInfoToBox(ByVal strMessage As String)
        'If myForm.txtStatus.Text = "" Then
        '    myForm.txtStatus.Text = strMessage
        'Else
        '    myForm.txtStatus.Text = myForm.txtStatus.Text & vbCrLf & strMessage
        'End If
        Console.WriteLine(strMessage)

    End Sub

    Sub DoHeader(ByVal intSheet As Integer)

        oExcel.Sheets(intSheet).Select()
        oExcel.Cells.Select()
        oExcel.Selection.Font.Size = 8
        oExcel.Selection.WrapText = True
        oExcel.Rows("1:1").Select()
        oExcel.Selection.Font.FontStyle = "Bold"
        oExcel.Selection.HorizontalAlignment = -4131
        oExcel.Selection.VerticalAlignment = -4160

        RowCount(intSheet) = 1
        Dim Column As Integer = 1

        'Put Headers row on
        oExcel.Cells(RowCount(intSheet), Column).Value = "Nessus ID"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Level"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Title"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Description"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Affected Port"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Affected System"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Lookup"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Exclusion Justification"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Discovery Date"
        Column = Column + 1
        oExcel.Cells(RowCount(intSheet), Column).Value = "Expiration Date"

        oExcel.Columns("A:A").ColumnWidth = 10.0
        oExcel.Columns("B:B").ColumnWidth = 10.0
        oExcel.Columns("C:C").ColumnWidth = 10.0
        oExcel.Columns("D:D").ColumnWidth = 51.86
        oExcel.Columns("E:E").ColumnWidth = 20.0
        oExcel.Columns("F:F").ColumnWidth = 20.0
        oExcel.Columns("G:G").ColumnWidth = 10.0
        oExcel.Columns("H:H").ColumnWidth = 52
        oExcel.Columns("I:I").ColumnWidth = 20.0
        oExcel.Columns("J:J").ColumnWidth = 20.0

        RowCount(intSheet) += 1

        oExcel.Columns("A:J").Select()
        oExcel.Range("A2").Activate()
        oExcel.Range("A2").Select()
        oExcel.ActiveWindow.FreezePanes = True
        Column = 1
    End Sub

    Private Sub DoXL(ByVal pluginID As String, ByVal pluginName As String, ByVal severity As Integer, _
                     ByVal strSummary As String, ByVal port As String, ByVal AffectedSystem As String)

        Dim ThisSheet As Integer = 1
        Dim severityName As String

        'CheckXML will return text if this finding is in the catalog
        Dim Justification As String = String.Empty
        Dim ExpirationDate As Date = "12/31/2099", DiscoveryDate As Date = "01/01/1970"

        If strFile <> "" And CheckExceptionsXML(pluginID, port, AffectedSystem).Contains("||") Then
            Justification = Split(CheckExceptionsXML(pluginID, port, AffectedSystem), "||")(0)
            DiscoveryDate = Split(CheckExceptionsXML(pluginID, port, AffectedSystem), "||")(1)
            ExpirationDate = Split(CheckExceptionsXML(pluginID, port, AffectedSystem), "||")(2)
        End If

        If Justification <> String.Empty And Justification <> "-1" And ExpirationDate > Now Then
            ThisSheet = 3

        Else
            If severity <= 1 Then
                ThisSheet = 2
            Else
                ThisSheet = 1
            End If
        End If

        oExcel.Sheets(ThisSheet).Select()
        oExcel.Range("A2").Select()

        AddInfoToBox("Selecting Sheet " & ThisSheet)

        'Parsing out the Compliance Report

        'If pluginID = Configuration.ConfigurationManager.AppSettings("ComplianceAuditIDWindows") _
        '    Or pluginID = Configuration.ConfigurationManager.AppSettings("ComplianceAuditIDUnix") Then

        '    'Begin custom handling of Audit Report
        '    AddInfoToBox("This result is a compliance check")



        'Else
        oExcel.Cells(RowCount(ThisSheet), 1).Value = pluginID

        Select Case severity
            Case 4
                severityName = "Critical"
            Case 3
                severityName = "High"
            Case 2
                severityName = "Medium"
            Case 1
                severityName = "Low"
            Case 0
                severityName = "Informational"
            Case Else
                severityName = "Other"
        End Select

        oExcel.Cells(RowCount(ThisSheet), 2).Value = severityName
        oExcel.Cells(RowCount(ThisSheet), 3).Value = pluginName
        oExcel.Cells(RowCount(ThisSheet), 4).Value = strSummary
        oExcel.Cells(RowCount(ThisSheet), 5).Value = port
        oExcel.Cells(RowCount(ThisSheet), 6).Value = AffectedSystem
        oExcel.Cells(RowCount(ThisSheet), 7).Value = "Lookup"
        oExcel.Cells(RowCount(ThisSheet), 8).Value = Justification

        If DiscoveryDate <> "01/01/1970" And Not IsNothing(DiscoveryDate) Then
            Debug.Print("Discovery Date " & DiscoveryDate)
            oExcel.Cells(RowCount(ThisSheet), 9).Value = DiscoveryDate
        End If

        If ExpirationDate <> "12/31/2099" And Not IsNothing(ExpirationDate) Then
            Debug.Print("Expiration Date: " & ExpirationDate)
            oExcel.Cells(RowCount(ThisSheet), 10).Value = ExpirationDate
        End If

        oRng = oExcel.Range("G" & RowCount(ThisSheet))

        Try
            oSheet.Hyperlinks.Add _
             (oRng, "http://www.nessus.org/plugins/index.php?view=single&id=" & pluginID)

        Catch ex As Exception
            MsgBox("Hyperlink could not be added.", MsgBoxStyle.Exclamation, "Excel Error")
            AddInfoToBox(ex.Message)
        End Try

        RowCount(ThisSheet) += 1
    End Sub

    Sub WrapUp(ByVal intSheet As Integer)

        oExcel.Sheets(intSheet).Select()
        oExcel.Cells.Select()
        oExcel.Selection.HorizontalAlignment = -4131
        oExcel.Selection.VerticalAlignment = -4160
        oExcel.Range("A1").Select()
        oExcel.Sheets(intSheet).Select()

        Dim SheetName As String

        If strEnv.Length >= 13 Then
            SheetName = strEnv.Substring(0, 13)
        Else
            SheetName = strEnv
        End If

        Select Case intSheet
            Case 1
                SheetName = SheetName & " - High & Medium"
            Case 2
                SheetName = SheetName & " - Low"
            Case 3
                SheetName = SheetName & " - Exceptions"
            Case Else
                AddInfoToBox("Incorrect Sheet Reference")
                Exit Sub
        End Select

        Try
            oExcel.Sheets(intSheet).Name = SheetName
        Catch ex As Exception
            AddInfoToBox("Bad Sheet Name: " & SheetName & ".  You will need to create it manually.")
        End Try

    End Sub

    Sub CreatePivotTable()
        Const xlCount As Integer = -4112

        If strEnv.Length >= 13 Then
            strEnv = strEnv.Substring(0, 13)
        End If

        Dim PivotTableName As String = strEnv.Replace(" ", "_") & "Pivot"

        oExcel.Sheets.Add(Before:=oExcel.Sheets(1))

        Dim TotalRows As Integer = 0

        For i As Integer = 2 To SheetCount
            oExcel.Sheets(i).Select()

            If i = 2 Then
                oExcel.Range("A1:F" & RowCount(i - 1)).Select()
            Else
                oExcel.Range("A2:F" & RowCount(i - 1)).Select()
            End If

            oExcel.Selection.Copy()
            oExcel.Sheets(1).Select()

            If i = 2 Then
                oExcel.Range("A1").Select()
            Else
                oExcel.Range("A" & TotalRows).Select()
            End If

            oExcel.Sheets(1).Paste()

            TotalRows += RowCount(i - 1) - 1

            oExcel.Range("A1").Select()
        Next
        oExcel.Application.CutCopyMode = False


        oExcel.Sheets(1).Name = strEnv & " All"
        oExcel.Sheets(1).Visible = False

        oExcel.Sheets.Add(Before:=oExcel.Sheets(1))
        oExcel.Sheets(1).Select()
        oExcel.Sheets(1).Name = strEnv & " RollUp"

        Dim xlDestWSheet As Excel.Worksheet = CType(oWB.Worksheets(1), Excel.Worksheet)
        Dim SourceData As String = "'" & strEnv & " All'!R1C1:R" & TotalRows & "C6"

        Dim xlDestRange As Excel.Range = xlDestWSheet.Range("A1")

        Dim ptCache As Excel.PivotCache = oWB.PivotCaches.Create(SourceType:=Excel.XlPivotTableSourceType.xlDatabase, _
                                                                 SourceData:=SourceData, _
                                                                 Version:=Excel.XlPivotTableVersionList.xlPivotTableVersion14)

        Dim myPivots As Excel.PivotTables = xlDestWSheet.PivotTables
        Dim ptTable As Excel.PivotTable = myPivots.Add(PivotCache:=ptCache, TableDestination:=xlDestRange)

        oExcel.Sheets(1).Select()

        With ptTable
            .PivotFields("Level").Orientation = Excel.XlPivotFieldOrientation.xlRowField
            .PivotFields("Level").Position = 1
            .PivotFields("Title").Orientation = Excel.XlPivotFieldOrientation.xlRowField
            .PivotFields("Title").Position = 2
            .AddDataField(ptTable.PivotFields("Affected System"), "Count of Affected System", xlCount)
            .PivotFields("Affected System").Orientation = Excel.XlPivotFieldOrientation.xlColumnField
            .PivotFields("Affected System").Position = 1
        End With

        oExcel.Range("A3").Select()
        oExcel.ActiveWindow.FreezePanes = True
        oExcel.ActiveWindow.Zoom = 80

    End Sub

    Public Sub ConvertPlainText()
        Dim myDir() As String = Directory.GetFiles(strFolder)


        For Each myFile As String In myDir
            Dim thisFile As New FileInfo(myFile)

            If thisFile.Extension.ToLower = ".nessus" Then
                Dim myXml As New XmlDocument
                Try
                    myXml.Load(thisFile.FullName)
                Catch ex As Exception
                    MsgBox("XML not formatted properly or has been truncated.", MsgBoxStyle.Exclamation, "XML Error")
                    AddInfoToBox("XML not formatted properly or has been truncated, XML could not be loaded." & vbCrLf & ex.Message)
                    Exit For
                End Try

                Dim xmlHosts As XmlNodeList = myXml.GetElementsByTagName("ReportHost")

                For i As Integer = 0 To xmlHosts.Count - 1
                    Dim host As String = String.Empty, port As String = String.Empty, pluginName As String = String.Empty
                    Dim pluginID As String = String.Empty, severity As Integer = 0
                    Dim synopsis As String = String.Empty, description As String = String.Empty, solution As String = String.Empty
                    Dim strSummary As String = String.Empty

                    'Future Variables
                    'Dim cve As String = String.Empty, xfref As String = String.Empty
                    'Dim cvss_vector As String = String.Empty, cvss_base_score As String = String.Empty, 
                    'see_also As String = String.Empty
                    'Dim plugin_publication_date As Date, vuln_publication_date As Date, risk_factor As String = String.Empty

                    Try
                        host = xmlHosts.Item(i).Attributes("name").InnerText
                        AddInfoToBox(host)
                    Catch ex As Exception
                        MsgBox("Host Name Could Not be Set", MsgBoxStyle.Exclamation, "XML Error")
                        AddInfoToBox("Host Name could not be set")
                    End Try

                    Dim xmlItems As XmlNodeList = xmlHosts.Item(i).ChildNodes

                    For Each Item As XmlNode In xmlItems
                        If Item.Name = "ReportItem" Then

                            Try
                                severity = CInt(Item.Attributes("severity").Value)
                            Catch ex As Exception
                                AddInfoToBox("Severity Not Found")
                            End Try

                            'If Severity is 0 then we don't want it.
                            If severity > 0 Then
                                Try
                                    pluginName = Item.Attributes("pluginName").Value
                                Catch ex As Exception
                                    AddInfoToBox("pluginName Not Found")
                                End Try

                                Try
                                    pluginID = Item.Attributes("pluginID").Value
                                Catch ex As Exception
                                    AddInfoToBox("pluginID Not Found")
                                End Try

                                Try
                                    port = Item.Attributes("port").Value
                                Catch ex As Exception
                                    AddInfoToBox("port Not Found")
                                End Try

                                'This is the spot where the false positive catalog will do its thing.
                                'Check the file for the pluginID and host variables to see if this should be printed.

                                AddInfoToBox("Plugin ID: " & pluginID & "  Plugin Name: " & pluginName)
                                For Each Detail As XmlNode In Item.ChildNodes
                                    Select Case Detail.Name
                                        Case "synopsis"
                                            synopsis = Detail.InnerText
                                        Case "description"
                                            description = Detail.InnerText
                                        Case "solution"
                                            solution = Detail.InnerText
                                    End Select
                                Next

                                'Bring Them Together
                                strSummary = synopsis & vbCrLf & description & vbCrLf & solution

                                AddInfoToBox("Summary: " & vbCrLf & strSummary)
                                DoXL(pluginID, pluginName, severity, strSummary, port, host)

                                'Bounce out if the user wants to stop the madness.
                                If keepProcessing = False Then
                                    myForm.btnGo.Enabled = True
                                    Exit Sub
                                End If

                                ' This Else is a response to a 0 severity rating
                                'Else

                            End If
                        End If
                    Next
                Next
            End If
        Next
    End Sub

    Function CheckExceptionsXML(ByVal thisPluginID As Integer, ByVal thisPort As Integer, ByVal thisIP As String) As String

        Dim catalogXml As New XmlDocument

        If NessusConversion.strFile <> "" Then
            'Attempt to load the Exceptions XML
            Try
                catalogXml.Load(strFile)
            Catch ex As Exception
                MsgBox("Catalog XML Not Formatted Properly", MsgBoxStyle.Exclamation, "Catalog XML Error")
                AddInfoToBox("Catalog XML Could not be loaded." & vbCrLf & ex.Message)
                Return "-1"
            End Try

            'Search for applicable nodes
            Dim xmlExceptions As XmlNodeList = catalogXml.SelectNodes("//exclusion[@pluginid='" & thisPluginID & _
                                                                      "' and @port='" & thisPort & _
                                                                      "' and @ipaddress='" & thisIP & "']")

            Dim ReturnText As String = String.Empty
            Dim ExpirationDate, DiscoveryDate As Date

            'Ensure that there is something to look at
            If xmlExceptions.Count > 0 Then

                'Should be only one, but there maybe more.  Take the last one.
                For Each myNode As XmlNode In xmlExceptions

                    Try
                        DiscoveryDate = myNode.Attributes("disdate").Value
                    Catch ex As Exception
                        DiscoveryDate = "01/01/1970"
                    End Try

                    Try
                        ExpirationDate = myNode.Attributes("expdate").Value
                    Catch ex As Exception
                        ExpirationDate = "12/31/2099"
                    End Try

                    ReturnText = myNode.InnerText & "||" & DiscoveryDate & "||" & ExpirationDate
                Next

            End If

            Return ReturnText
        Else
            Return False
        End If

    End Function

    Sub ShowHelp(Optional ByVal ErrorText As String = "")

        'Display custom error if I want
        If ErrorText <> "" Then
            Console.WriteLine()
            Console.WriteLine(ErrorText)
        End If

        'Otherwise just show this 
        Console.WriteLine()
        Console.WriteLine("Nessus File Conversion Utility with exclusions support.")
        Console.WriteLine("Usage (must include colon): ")
        Console.WriteLine(vbTab & "/d:<path>" & vbTab & "Path to Nessus Files")
        Console.WriteLine(vbTab & "/e:<name>" & vbTab & "Environment Name")
        Console.WriteLine(vbTab & "/x:<path>" & vbTab & "Exclusion XML File Path")
        Console.WriteLine(vbTab & "/o:<path>" & vbTab & "Output File Path")
        Console.WriteLine(vbTab & "/f:<xlsx|csv|tsv>" & vbTab & "File Format")
        Console.WriteLine(vbTab & "/y" & vbTab & vbTab & "No Prompting, answer yes.")
    End Sub
End Module
