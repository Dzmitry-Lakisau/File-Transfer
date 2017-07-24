Imports System.Net.Sockets
Imports System.IO
Public Class Form1
    Dim pos As Int32
    Dim buf As Int32 = 1024 * 1024
    Dim client As TcpClient
    Dim filename As String
    Dim filepath As String = Application.StartupPath + "\Temp\"
    Dim fs As FileStream
    Dim stream As NetworkStream
    Dim x As System.ComponentModel.DoWorkEventArgs
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        OpenFileDialog1.ShowDialog()
        TextBox1.Text = OpenFileDialog1.FileName
    End Sub
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
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        If TextBox1.Text = "" Then
            ListBox1.Items.Add(DateAndTime.TimeOfDay + " | Нечего передавать. Сначала выберите файл.")
            ListBox1.TopIndex = ListBox1.Items.Count - 1
        Else
            filename = TextBox1.Text
            Me.Button2.Enabled = 0
            BackgroundWorker1.RunWorkerAsync()
        End If
    End Sub
    Private Function Transfer() As Int32
        Try
            '---клиентская часть---
            fs = File.Open(filename, FileMode.Open)
            Dim filelength As Int64 = fs.Length
            pos = 0
            Me.AddList(DateAndTime.TimeOfDay + " | Отправляется " + filename + ".")
            Dim clientwriter As BinaryWriter
            Dim clientreader As BinaryReader
            client = New TcpClient(TextBox2.Text, 1001)
            stream = client.GetStream()
            clientwriter = New BinaryWriter(stream)
            clientreader = New BinaryReader(stream)
            clientwriter.Write(filelength)
            clientwriter.Write(Mid(filename, InStrRev(filename, "\", , ) + 1))
            Dim filedata(buf) As Byte
            While pos <= filelength
                BackgroundWorker1.ReportProgress((pos / filelength) * 100)
                Me.SetText("Отправлено " + (pos / buf).ToString + " из " + Math.Floor(filelength / buf).ToString + " Mb.")
                fs.Read(filedata, 0, filedata.Length)
                clientwriter.Write(filedata)
                pos = pos + buf
            End While
            Me.AddList(DateAndTime.TimeOfDay + " | Файл передан. Получение обратно.")
            fs.Close()
            '----серверная часть---
            pos = 0
            filelength = clientreader.ReadInt64
            filename = clientreader.ReadString
            fs = New FileStream(filepath + filename, FileMode.Create)
            While pos <= filelength ' - pos
                BackgroundWorker1.ReportProgress((pos / filelength) * 100)
                Me.SetText("Получено " + (pos / buf).ToString + " из " + Math.Floor(filelength / buf).ToString + " Mb.")
                filedata = clientreader.ReadBytes(buf)
                fs.Write(filedata, 0, filedata.Length)
                pos = pos + buf
            End While
            fs.SetLength(filelength)
            fs.Close()
            Me.AddList(DateAndTime.TimeOfDay + " | Файл получен обратно.")
            clientwriter.Close()
            stream.Close()
            client.Close()
            Return pos / filelength * 50
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
        Catch e5 As FileNotFoundException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось.")
            MsgBox("Файл " + filename + " не найден.")
        Catch e6 As IOException
            Me.SetText("")
            Me.AddList(DateAndTime.TimeOfDay + " | Не удалось. Возможно сервер отменил передачу.")
            'MsgBox(e6.ToString)
        End Try
    End Function
    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        stream.Close()
        client.Close()
        My.Computer.FileSystem.DeleteDirectory(filepath, FileIO.DeleteDirectoryOption.DeleteAllContents)
    End Sub
    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        TextBox2.Text = "192.168.1.2"
        My.Computer.FileSystem.CreateDirectory(filepath)
    End Sub
    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        ListBox1.Items.Clear()
    End Sub
    Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        e.Result = Transfer()
    End Sub
    Private Sub BackgroundWorker1_ProgressChanged(ByVal sender As Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        Me.ProgressBar1.Value = e.ProgressPercentage
    End Sub
    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        If e.Error IsNot Nothing Then
            Me.ListBox1.Items.Add(DateAndTime.TimeOfDay + " | Не удалось передать")
        ElseIf e.Cancelled Then
            stream.Close()
            client.Close()
            Me.ProgressBar1.Value = 0
            Me.Button2.Enabled = 1
        Else
            stream.Close()
            client.Close()
            fs.Close()
            Me.ProgressBar1.Value = 0
            ListBox1.TopIndex = ListBox1.Items.Count - 1
            Label3.Text = ""
        End If
        Me.Button2.Enabled = 1
    End Sub
    Private Sub Form1_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        Dim graf As Graphics = e.Graphics()
        Dim pen1 As New Pen(Color.Black, 1)
        graf.DrawLine(pen1, 0, 170, Me.Size.Width, 170)
        graf.DrawLine(pen1, 0, 230, Me.Size.Width, 230)
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Shell("explorer.exe /open," + filepath, vbNormalFocus)
    End Sub
End Class
