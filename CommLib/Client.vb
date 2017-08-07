Imports System.Net.Sockets

''' <summary>
''' Represents a client that can connect to a TCP/IP server.
''' </summary>
<CLSCompliant(True)>
Public Class Client

    Private Client As TcpClient
    Private Kill As Boolean = False

    ''' <summary>
    ''' Returns a value indicating whether there is currently an active TCP connection.
    ''' </summary>
    Public ReadOnly Property Connected As Boolean
        Get
            Return Client.Connected
        End Get
    End Property

    ''' <summary>
    ''' Gets or sets the remote host name the client should connect to.
    ''' </summary>
    Public Property RemoteIP As String

    ''' <summary>
    ''' Gets or sets the TCP port the client should use while connecting.
    ''' </summary>
    Public Property RemotePort As Integer

    ''' <summary>
    ''' Raised when a network operation, such as connecting, sending or receiving, fails.
    ''' </summary>
    ''' <param name="ex">The exception that was thrown.</param>
    Public Event OnError(ex As Exception)
    ''' <summary>
    ''' Raised when a connection to a host has been successfully established.
    ''' </summary>
    Public Event OnConnected()
    ''' <summary>
    ''' Raised when a connection to a remote host has been closed.
    ''' </summary>
    Public Event OnDisconnected()
    ''' <summary>
    ''' Raised when a packet has been received from the network.
    ''' </summary>
    ''' <param name="packet">A byte array containing the packet.</param>
    Public Event OnDataReceived(packet As Byte())

    Private ReadBuffer(4095) As Byte
    Private PacketBuffer() As Byte

    Public Sub New()
        RemotePort = 8080
        Client = New TcpClient
        PacketBuffer = New Byte() {}
        'Client.NoDelay = True
    End Sub

    ''' <summary>
    ''' Initializes the TCP socket and connects to a network as specified by RemoteIP and RemotePort.
    ''' </summary>
    Public Sub Connect()
        Kill = False
        Client = New TcpClient()
        Client.BeginConnect(RemoteIP, RemotePort, New AsyncCallback(AddressOf ConnectCallback), Nothing)
    End Sub

    Private Sub ConnectCallback(ir As IAsyncResult)
        Try
            ' accept connection - if there was an error, it will be thrown by this call
            Client.EndConnect(ir)
            ' start reader
            Client.GetStream.BeginRead(ReadBuffer, 0, 4096, AddressOf ReadCallback, Nothing)
            ' notify
            RaiseEvent OnConnected()
        Catch ex As SocketException
            RaiseEvent OnError(ex)
        End Try
    End Sub

    ''' <summary>
    ''' Closes the network connection and cleans up.
    ''' </summary>
    Public Sub Disconnect()
        If Not Client.Connected Then Throw New InvalidOperationException("No open connection to close.")

        Kill = True
        Client.Close()
    End Sub

    Private Sub ReadCallback(ir As IAsyncResult)
        Try
            Dim Read As Integer = Client.GetStream.EndRead(ir)

            If Read = 0 Then
                ' connection lost
                Client.Close()
                RaiseEvent OnDisconnected()
            Else
                ' successfully read buffer
                Dim receiveQueue As New Queue(Of Byte())
                Dim nextPos As Integer = PacketBuffer.Length
                ReDim Preserve PacketBuffer(0 To PacketBuffer.Length + Read - 1)
                Buffer.BlockCopy(readBuffer, 0, PacketBuffer, nextPos, Read)

                While PacketBuffer.Length > 4
                    ' get length (network order / big-endian) and reverse if necessary for this cpu
                    Dim lengthBuffer(0 To 3) As Byte
                    Buffer.BlockCopy(PacketBuffer, 0, lengthBuffer, 0, 4)
                    If BitConverter.IsLittleEndian Then lengthBuffer.Reverse()

                    Dim nextPacketLength As Integer = BitConverter.ToInt32(lengthBuffer, 0)
                    If PacketBuffer.Length >= nextPacketLength + 4 Then
                        Dim finishedPacket(0 To nextPacketLength - 1) As Byte
                        Buffer.BlockCopy(PacketBuffer, 4, finishedPacket, 0, nextPacketLength)
                        receiveQueue.Enqueue(finishedPacket)

                        ' remove this packet from the buffer by recreating the client buffer
                        Dim newClientBuffer() As Byte = New Byte() {}
                        If PacketBuffer.Length - (nextPacketLength + 4) > 0 Then
                            ReDim newClientBuffer(0 To PacketBuffer.Length - (nextPacketLength + 4) - 1)
                            Buffer.BlockCopy(PacketBuffer, nextPacketLength + 4, newClientBuffer, 0, PacketBuffer.Length - (nextPacketLength + 4))
                        End If
                        PacketBuffer = newClientBuffer
                    Else
                        Exit While
                    End If
                End While
                ' propagate to user
                While receiveQueue.Count > 0
                    RaiseEvent OnDataReceived(receiveQueue.Dequeue)
                End While
                ' start async read again
                Client.GetStream.BeginRead(readBuffer, 0, 4096, AddressOf ReadCallback, Nothing)
            End If
        Catch ex As Exception
            If Not Kill Then RaiseEvent OnError(ex)
            ' connection lost
            Client.Close()
            RaiseEvent OnDisconnected()
        End Try
    End Sub

    ''' <summary>
    ''' Asynchronously writes a byte array to the network stream.
    ''' </summary>
    ''' <param name="Packet">Byte array to send to the server</param>
    ''' <exception cref="InvalidOperationException">The client is not connected.</exception>
    ''' <exception cref="ArgumentException">Packet is null or zero bytes.</exception>
    Public Sub Send(packet() As Byte)
        If Not Client.Connected Then Throw New InvalidOperationException("Client is not connected.")
        If packet Is Nothing OrElse packet.Length < 1 Then Throw New ArgumentException("Invalid packet.")

        Dim sendBf(packet.Length + 3) As Byte
        Dim lenBf() As Byte = BitConverter.GetBytes(packet.Length)
        If BitConverter.IsLittleEndian Then lenBf.Reverse() ' so the length buffer is always sent big-endian
        Buffer.BlockCopy(lenBf, 0, sendBf, 0, 4) ' byte count
        Buffer.BlockCopy(packet, 0, sendBf, 4, packet.Length) ' packet contents

        Try
            Client.GetStream.BeginWrite(sendBf, 0, sendBf.Length, AddressOf SendCallback, Nothing)
        Catch ex As ObjectDisposedException
            ' swallow erroneous access to a dead socket
        End Try
    End Sub

    Private Sub SendCallback(ir As IAsyncResult)
        Try
            Client.GetStream.EndWrite(ir)
        Catch ex As Exception
            RaiseEvent OnError(ex)
        End Try
    End Sub

End Class
