Imports System.Threading
Imports System.Net
Imports System.Net.Sockets

''' <summary>
''' Represents a TCP/IP listen server.
''' </summary>
<CLSCompliant(True)>
Public Class Host

    Private Const CLIENT_ARRAY_SIZE As Integer = 128
    Private Const CLIENT_ARRAY_UPPERBOUND As Integer = CLIENT_ARRAY_SIZE - 1

    ''' <summary>
    ''' Holds information about a connected client.
    ''' </summary>
    Private Class ConnectedClient
        Public Property Socket As TcpClient

        Public Property ReadBuffer As Byte()
        Public Property PacketBuffer As Byte()

        Public Sub New()
            Socket = Nothing
            PacketBuffer = New Byte() {}
            ReadBuffer = New Byte(4095) {}
        End Sub
    End Class

    Private Server As TcpListener
    Private ServerCore As Thread ', Listener As Thread
    Private Kill As Boolean = False, Started As Boolean = False
    Private ListenerStopped As Boolean = True
    Private _MaxClients As Integer

    Private ReadOnly Clients(CLIENT_ARRAY_UPPERBOUND) As ConnectedClient

    ''' <summary>
    ''' Gets or sets the maximum number of client connections.
    ''' </summary>
    ''' <remarks>
    ''' If a user tries to connect while the connection list is full, the incoming connection will be ignored.
    ''' </remarks>
    Public Property MaxClients As Integer
        Get
            Return _MaxClients
        End Get
        Set
            _MaxClients = Math.Min(Value, CLIENT_ARRAY_SIZE)
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the IP address the listener will bind to.
    ''' </summary>
    Public Property LocalIP As IPAddress

    ''' <summary>
    ''' Gets or sets the TCP port the listener will bind to.
    ''' </summary>
    Public Property LocalPort As Integer

    Public Event OnServerStart()
    Public Event OnServerStop()
    Public Event OnClientConnect(ByVal clientID As Integer)
    Public Event OnClientDisconnect(ByVal clientID As Integer)
    Public Event OnDataReceived(ByVal clientID As Integer, ByVal packet As Byte())
    Public Event OnError(ByVal ex As Exception)

    Public Sub New()
        LocalIP = IPAddress.Loopback
        LocalPort = 8080
        MaxClients = CLIENT_ARRAY_SIZE
    End Sub

    ''' <summary>
    ''' Returns a value indicating whether the server is currently listening for clients.
    ''' </summary>
    Public ReadOnly Property Listening As Boolean
        Get
            Return Server IsNot Nothing AndAlso Started
        End Get
    End Property

    Private Sub ListenThread()
        Try
            While Not Kill
                ' find a free slot
                Dim nextSlot As Integer = 0
                While Clients(nextSlot) IsNot Nothing
                    nextSlot += 1
                    If nextSlot >= MaxClients Then
                        ListenerStopped = True
                        Exit Sub
                    End If
                End While
                ' accept tcp client (blocking call)
                Dim Client As New ConnectedClient
                Dim ClientSock As TcpClient = Server.AcceptTcpClient
                Client.Socket = ClientSock
                Clients(nextSlot) = Client
                ' start reading from the net stream
                ClientSock.GetStream.BeginRead(Client.ReadBuffer, 0, 4096, AddressOf ReadCallback, nextSlot)
                ' create a new thread for the client
                'ThreadPool.QueueUserWorkItem(AddressOf DataReadThread, nextSlot)
                ' notify library invoker
                RaiseEvent OnClientConnect(nextSlot)

                Thread.Sleep(10)
            End While
        Catch ex As Exception
            ListenerStopped = True
            If Not Kill Then RaiseEvent OnError(ex)
        End Try
    End Sub

    Private Sub ReadCallback(ir As IAsyncResult)
        Dim clID As Integer = ir.AsyncState
        Dim cl As ConnectedClient = Clients(clID)
        Try
            Dim Read As Integer = cl.Socket.GetStream.EndRead(ir)

            If Read = 0 Then
                ' client disconnected
                If cl IsNot Nothing Then
                    cl.Socket.Close()
                    cl = Nothing
                End If
                RaiseEvent OnClientDisconnect(clID)
            Else
                ' successfully read buffer
                Dim receiveQueue As New Queue(Of Byte())
                ' copy receive buffer to the end of the client's packet buffer
                Dim nextPos As Integer = cl.PacketBuffer.Length
                ReDim Preserve cl.PacketBuffer(0 To cl.PacketBuffer.Length + Read - 1)
                Buffer.BlockCopy(cl.ReadBuffer, 0, cl.PacketBuffer, nextPos, Read)

                While cl.PacketBuffer.Length > 4
                    ' get length (network order / big-endian) and reverse if necessary for this cpu
                    Dim lengthBuffer(0 To 3) As Byte
                    Buffer.BlockCopy(cl.PacketBuffer, 0, lengthBuffer, 0, 4)
                    If BitConverter.IsLittleEndian Then lengthBuffer.Reverse()

                    Dim nextPacketLength As Integer = BitConverter.ToInt32(lengthBuffer, 0)
                    If cl.PacketBuffer.Length >= nextPacketLength + 4 Then
                        Dim finishedPacket(0 To nextPacketLength - 1) As Byte
                        Buffer.BlockCopy(cl.PacketBuffer, 4, finishedPacket, 0, nextPacketLength)
                        receiveQueue.Enqueue(finishedPacket)

                        ' remove this packet from the buffer by recreating the client buffer
                        Dim newClientBuffer() As Byte = New Byte() {}
                        If cl.PacketBuffer.Length - (nextPacketLength + 4) > 0 Then
                            ReDim newClientBuffer(0 To cl.PacketBuffer.Length - (nextPacketLength + 4) - 1)
                            Buffer.BlockCopy(cl.PacketBuffer, nextPacketLength + 4, newClientBuffer, 0, cl.PacketBuffer.Length - (nextPacketLength + 4))
                        End If
                        cl.PacketBuffer = newClientBuffer
                    Else
                        Exit While
                    End If
                End While
                ' propagate to user
                While receiveQueue.Count > 0
                    RaiseEvent OnDataReceived(clID, receiveQueue.Dequeue)
                End While
                ' restart read
                cl.Socket.GetStream.BeginRead(cl.ReadBuffer, 0, 4096, AddressOf ReadCallback, clID)
            End If
        Catch ex As Exception
            If cl Is Nothing OrElse Not cl.Socket.Connected Then
                ' if there was a disconnect, don't bother the client application with the error,
                ' just close the socket and report a disconnect.
                cl.Socket.Close()
                RaiseEvent OnClientDisconnect(clID)
            Else
                RaiseEvent OnError(ex)
            End If
        End Try
    End Sub

    Private Sub ServerLoop()
        While Not Kill
            ' reboot the listener thread
            If ListenerStopped Then
                Dim nextSlot As Integer = 0
                While nextSlot < MaxClients - 1 AndAlso Clients(nextSlot) IsNot Nothing
                    nextSlot += 1
                End While
                If nextSlot < MaxClients Then
                    ' Create and start the listening thread
                    'Listener = New Thread(AddressOf ListenThread)
                    'Listener.IsBackground = True
                    'Listener.Start()
                    ListenerStopped = Not ThreadPool.QueueUserWorkItem(AddressOf ListenThread)
                End If
            End If

            Thread.Sleep(1)
        End While
    End Sub

    ''' <summary>
    ''' Starts the listen server.
    ''' </summary>
    Public Sub Start()
        If Server IsNot Nothing Or Started Then Throw New InvalidOperationException("Socket is already open.")

        Kill = False
        ListenerStopped = True

        ' Open the socket
        Server = New TcpListener(LocalIP, LocalPort)
        'Server.Server.NoDelay = True
        Server.Start()

        ' Create and start the listening thread
        ServerCore = New Thread(AddressOf ServerLoop)
        ServerCore.IsBackground = True
        ServerCore.Start()

        Started = True
        RaiseEvent OnServerStart()
    End Sub

    ''' <summary>
    ''' Closes the listen server and disconnects all clients.
    ''' </summary>
    Public Sub [Stop]()
        If Not Started Then Throw New InvalidOperationException("Server is not running.")

        ' to break the loops and prevent restarting
        Kill = True
        Started = False
        ' drop the clients
        For I As Integer = 0 To CLIENT_ARRAY_UPPERBOUND
            If Clients(I) IsNot Nothing Then
                If Clients(I).Socket.Connected Then Clients(I).Socket.Close()
            End If
        Next
        ' stop the listen server
        Server.Stop()
        Server = Nothing
        ' close the threads
        ServerCore.Abort()
        'If Listener IsNot Nothing AndAlso Listener.IsAlive Then Listener.Abort()
        ' notify invoker
        RaiseEvent OnServerStop()
    End Sub

    ''' <summary>
    ''' Returns the local end point of a client as a String representation of an IPv4 address.
    ''' </summary>
    ''' <param name="clientID">Client index</param>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Function GetClientIP(clientID As Integer) As String
        If Not Started Then Throw New InvalidOperationException("Server is not running.")

        If Clients(clientID) Is Nothing Then Return Nothing
        Return Clients(clientID).Socket.Client.RemoteEndPoint.ToString
    End Function

    ''' <summary>
    ''' Returns a value indicating whether the client socket is still in use.
    ''' </summary>
    ''' <param name="clientID">Client index</param>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Function GetClientAlive(clientID As Integer) As Boolean
        If Not Started Then Throw New InvalidOperationException("Server is not running.")

        If Clients(clientID) Is Nothing Then Return False
        Return Clients(clientID).Socket.Connected
    End Function

    ''' <summary>
    ''' Asynchronously writes a byte array to the network stream of the specified client.
    ''' </summary>
    ''' <param name="clientID">Client index</param>
    ''' <param name="Packet">Byte array to send to a client</param>
    ''' <exception cref="ArgumentException">the specified client index is not in use, *or*
    ''' the specified client does not have a usable connection. *or*
    ''' packet has zero length, *or*
    ''' packet length exceeds 4096 bytes.</exception>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Overloads Sub Send(clientID As Integer, packet() As Byte)
        Send(clientID, packet, AddressOf SendCallback)
    End Sub

    Private Overloads Sub Send(clientID As Integer, packet() As Byte, callback As AsyncCallback)
        If Not Started Then Throw New InvalidOperationException("Server is not running.")
        ' make sure the connection exists
        If Clients(clientID) Is Nothing Then Throw New ArgumentException("No such client exists.")
        If Not Clients(clientID).Socket.Connected Then Throw New ArgumentException("Specified client connection is closed.")
        If packet Is Nothing OrElse packet.Length < 1 Then Throw New ArgumentException("Invalid packet.")

        Dim sendBf(packet.Length + 3) As Byte
        Dim lenBf() As Byte = BitConverter.GetBytes(packet.Length)
        If BitConverter.IsLittleEndian Then lenBf.Reverse()
        Buffer.BlockCopy(lenBf, 0, sendBf, 0, 4) ' byte count
        Buffer.BlockCopy(packet, 0, sendBf, 4, packet.Length) ' packet contents

        Try
            Clients(clientID).Socket.GetStream.BeginWrite(sendBf, 0, sendBf.Length, callback, clientID)
        Catch ex As ObjectDisposedException
            ' swallow erroneous access to a dead socket
        End Try
    End Sub

    Private Sub SendCallback(ir As IAsyncResult)
        Dim clID = CInt(ir.AsyncState)
        Try
            Clients(clID).Socket.GetStream.EndWrite(ir)
        Catch ex As Exception
            If Clients(clID) Is Nothing OrElse Not Clients(clID).Socket.Connected Then
                ' if there was a disconnect, don't bother the client application with the error,
                ' just close the socket and report a disconnect.
                Clients(clID).Socket.Close()
                RaiseEvent OnClientDisconnect(clID)
            Else
                RaiseEvent OnError(ex)
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Asynchronously writes a byte array to all connected clients' network stream.
    ''' </summary>
    ''' <param name="Packet">Byte array to send.</param>
    ''' <remarks>This method is equivalent to iterating through all connected clients and calling Send() to transmit data to them.</remarks>
    ''' <exception cref="ArgumentException">packet has zero length, *or*
    ''' packet length exceeds 4096 bytes.</exception>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Sub Broadcast(packet() As Byte)
        If Not Started Then Throw New InvalidOperationException("Server is not running.")
        ' verify proper packet
        If packet Is Nothing OrElse packet.Length < 1 Then Throw New ArgumentException("Invalid packet.")
        ' iterate through all client slots
        For I As Integer = 0 To MaxClients - 1
            ' make sure the connection exists
            If Clients(I) Is Nothing Then Continue For
            If Not Clients(I).Socket.Connected Then Continue For
            ' send the data
            Send(I, packet)
        Next
    End Sub

    ''' <summary>
    ''' Disconnects the specified client and disposes of the socket. If the client is already disconnected, no exception is thrown.
    ''' </summary>
    ''' <param name="clientID">Client index</param>
    ''' <exception cref="ArgumentException">No client with specified ID is known.</exception>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Overloads Sub Kick(clientID As Integer)
        If Not Started Then Throw New InvalidOperationException("Server is not running.")
        ' make sure the connection exists
        If Clients(clientID) IsNot Nothing AndAlso Clients(clientID).Socket.Connected Then
            ' disconnect the client
            Clients(clientID).Socket.Close()
        End If
        Clients(clientID) = Nothing ' free up the slot
    End Sub

    ''' <summary>
    ''' Transmits a byte array asynchronously. Afterwards, the client is disconnected and the socket is disposed.
    ''' If the client is already disconnected, no exception is thrown.
    ''' </summary>
    ''' <param name="clientID">Client index to send the data to and disconnect.</param>
    ''' <param name="Packet">Byte array to send.</param>
    ''' <exception cref="ArgumentException">Packet is null or zero bytes.</exception>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Overloads Sub Kick(clientID As Integer, packet() As Byte)
        If Not Started Then Throw New InvalidOperationException("Server is not running.")
        If packet Is Nothing OrElse packet.Length < 1 Then Throw New ArgumentException("Invalid packet.")
        ' make sure the connection exists
        If Clients(clientID) IsNot Nothing AndAlso Clients(clientID).Socket.Connected Then
            ' disconnect the client
            Send(clientID, packet, AddressOf DelayKickCallback)
        Else
            Clients(clientID) = Nothing
        End If
    End Sub

    Private Sub DelayKickCallback(ir As IAsyncResult)
        ' called from Kick(Integer, Byte()) - callback, kill the client now
        Try
            Dim clientID As Integer = ir.AsyncState
            Clients(clientID).Socket.GetStream.EndWrite(ir)
            Kick(clientID)
        Catch ex As Exception
            ' swallow exceptions, we shouldn't error out when kicking a bad client
        End Try
    End Sub

End Class
