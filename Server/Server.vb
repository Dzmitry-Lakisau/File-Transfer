Imports System.Windows.Forms.Application
Imports System.Net
Imports System.IO
Imports System.Net.Sockets
Public Class Form1
    Dim pos As Int32
    Dim buf As Int32 = 1024 * 1024
    Dim serverlistener As TcpListener
    Dim flg As Boolean = True
    Dim filepath As String = Application.StartupPath + "\Temp\"
    Dim server As TcpClient
    Dim fs As FileStream
    Delegate Sub SetTextCallback(ByVal text As String)
    Delegate Sub AddListCallback(ByVal text As String)
    Private Sub AddList(ByVal text As String)
        If Me.ListBox1.InvokeRequired Then
            Dim d As New AddListCallback(AddressOf AddList)
            Me.ListBox1.Invoke(d, New Object() {text})
        Else : Me.ListBox1.Items.Add(text)
        End If
    End Sub
    Private Sub SetText(ByVal text As String)
        If Label3.InvokeRequired Then
            Dim d As New SetTextCallback(AddressOf SetText)
            Me.Label3.Invoke(d, New Object() {text})
        Else
            Me.Label3.Text = text
        End If
    End Sub
    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        Try
            fs.Close()
            flg = False
            My.Computer.FileSystem.DeleteDirectory(filepath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch err As DirectoryNotFoundException
        End Try
    End Sub
    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Button1.Text = "Очистить лог"
        Dim localAddr As IPAddress = IPAddress.Any
        serverlistener = New TcpListener(localAddr, 1001)
        serverlistener.Start()
        ListBox1.Items.Add(DateAndTime.TimeOfDay + " | Прослушка порта.")
        ListBox1.TopIndex = ListBox1.Items.Count - 1
    End Sub

    Private Sub Form1_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        Dim graf As Graphics = e.Graphics()
        Dim pen1 As New Pen(Color.Black, 1)
        graf.DrawLine(pen1, 0, 70, Me.Size.Width, 70)
    End Sub
    Private Sub Form1_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        While flg = True
            DoEvents()
            If serverlistener.Pending = True Then
                If Not (BackgroundWorker1.IsBusy) Then
                    BackgroundWorker1.RunWorkerAsync()
                End If
            End If
        End While
    End Sub
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ListBox1.Items.Clear()
    End Sub
    Private Function Transfer() As Int32
        Try
            '---серверная часть---
            Dim filename As String
            server = serverlistener.AcceptTcpClient
            If server.Connected Then
                Me.AddList(DateAndTime.TimeOfDay + " | Клиент подключился.")
            End If
            Dim stream As NetworkStream = server.GetStream()
            Dim serverreader As BinaryReader = New BinaryReader(stream)
            Dim serverwriter As BinaryWriter = New BinaryWriter(stream)
            Dim filelength As Int64 = serverreader.ReadInt64
            filename = serverreader.ReadString
            My.Computer.FileSystem.CreateDirectory(filepath)
            fs = New FileStream(filepath + filename, FileMode.Create)
            Dim filedata(buf) As Byte
            pos = 0
            While pos <= filelength
                'If server.Available = 0 Then
                If server.Client.Poll(1, SelectMode.SelectRead) And server.Available > 0 Then
                    BackgroundWorker1.ReportProgress((pos / filelength) * 100)
                    Me.SetText("Получено " + (pos / buf).ToString + " из " + Math.Floor(filelength / buf).ToString + " Mb.")
                    filedata = serverreader.ReadBytes(buf)
                    fs.Write(filedata, 0, filedata.Length)
                    pos = pos + buf
                Else
                    Me.SetText("")
                    Me.AddList(DateAndTime.TimeOfDay + " | Не удалось. Клиент недоступен." + Convert.ToString(server.Available))
                    fs.Close()
                    server.Close()
                    My.Computer.FileSystem.DeleteDirectory(filepath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Exit Function
                    'Else
                End If
            End While
            fs.SetLength(filelength)
            fs.Close()
            Me.AddList(DateAndTime.TimeOfDay + " | Принят файл: " + filepath + filename + ".")
            '---клиентская часть---
            pos = 0
            fs = New FileStream(filepath + filename, FileMode.Open)
            serverwriter.Write(fs.Length)
            serverwriter.Write(Mid(fs.Name, InStrRev(fs.Name, "\", , ) + 1))
            While pos <= filelength
                BackgroundWorker1.ReportProgress((pos / filelength) * 100)
                Me.SetText("Отправлено " + (pos / buf).ToString + " из " + Math.Floor(filelength / buf).ToString + " Mb.")
                fs.Read(filedata, 0, filedata.Length)
                serverwriter.Write(filedata) ', 0, buf)
                pos = pos + buf
            End While
            Me.AddList(DateAndTime.TimeOfDay + " | Файл передан обратно.")
            fs.Close()
            Return (pos / filelength) * 50
        Catch e1 As ArgumentNullException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось.")
            MsgBox(e1.ToString)
        Catch e2 As SocketException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось.")
            MsgBox(e2.ToString)
        Catch e3 As OutOfMemoryException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось.")
            MsgBox(e3.ToString)
        Catch e4 As EndOfStreamException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось.")
            MsgBox(e4.ToString)
        Catch e6 As IOException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось.")
            MsgBox(e6.ToString)
        End Try
    End Function

    Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        e.Result = Transfer()
    End Sub
    Private Sub BackgroundWorker1_ProgressChanged(ByVal sender As Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        Me.ProgressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        If e.Error IsNot Nothing Then
            MsgBox(e.Error)
        Else
            Me.ProgressBar1.Value = 0
            ListBox1.Items.Add(DateAndTime.TimeOfDay + " | Прослушка порта.")
            server.Close()
        End If
        'fs.Close()
        ListBox1.TopIndex = ListBox1.Items.Count - 1
        Label3.Text = ""
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Shell("explorer.exe /open," + filepath, vbNormalFocus)
    End Sub
End Class
