Imports System.Threading
Imports System.Net
Imports System.Net.Sockets
Imports System.Text

''' <summary>Represents a TCP/IP listen server.</summary>
''' <remarks></remarks>
<CLSCompliant(True)>
Public Class Host

    Private Server As TcpListener
    Private ServerCore As Thread ', Listener As Thread
    Private Kill As Boolean = False, Started As Boolean = False
    Private ListenerStopped As Boolean = True

    Private Clients(0 To 4095) As CommClient

    ''' <summary>Gets or sets the maximum number of client connections.</summary>
    ''' <returns>Integer - max connections</returns>
    ''' <remarks>If a user tries to connect while the connection list is maxed out,
    ''' the incoming connection will be rejected.</remarks>
    Public Property MaxClients As Integer

    ''' <summary>Gets or sets the IP address the listener will bind to.</summary>
    ''' <returns>IPAddress</returns>
    ''' <remarks>Default is IPAddress.Loopback.</remarks>
    Public Property LocalIP As IPAddress

    ''' <summary>Gets or sets the TCP port the listener will bind to.</summary>
    ''' <returns>Integer - port</returns>
    ''' <remarks>Default is 8080.</remarks>
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
        MaxClients = 1000
    End Sub

    ''' <summary>Gets a value indicating whether the server is currently listening for clients.</summary>
    ''' <returns>Boolean - True: Listen server running. False: Not listening.</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property Listening As Boolean
        Get
            Return Server IsNot Nothing AndAlso Started
        End Get
    End Property

    Private Sub ListenThread()
        Debug.WriteLine("Listener started")
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
                Dim Client As New CommClient
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
        Dim clID As Integer = CInt(ir.AsyncState)
        Dim cl As CommClient = Clients(clID)
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
        Debug.WriteLine("Server loop started")

        Dim Iterations As Integer = 0
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

            ' 10 loops per 1 ms
            Iterations += 1
            If Iterations >= 10 Then
                Iterations = 0
                Thread.Sleep(1)
            Else
                Thread.Sleep(0)
            End If
        End While
    End Sub

    ''' <summary>Starts the listen server and fires all associated threads.</summary>
    ''' <remarks></remarks>
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

    ''' <summary>Aborts the processing threads, closes the listen server and disconnects all clients.</summary>
    ''' <remarks></remarks>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Sub [Stop]()
        If Not Started Then Throw New InvalidOperationException("Server is not running.")

        ' to break the loops and prevent restarting
        Kill = True
        Started = False
        ' drop the clients
        For I As Integer = 0 To 4095
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

    ''' <summary>Returns the local end point of a client as a String representation of an IPv4 address.</summary>
    ''' <param name="clientID">Client index</param>
    ''' <returns>String - IPv4 address</returns>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Function GetClientIP(clientID As Integer) As String
        If Not Started Then Throw New InvalidOperationException("Server is not running.")

        If Clients(clientID) Is Nothing Then Return Nothing
        Return Clients(clientID).Socket.Client.RemoteEndPoint.ToString
    End Function

    ''' <summary>Returns a value indicating whether the client socket is still in use.</summary>
    ''' <param name="clientID">Client index</param>
    ''' <returns>Boolean - True: client connected. False: no such client, or connection is closed.</returns>
    ''' <remarks></remarks>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Function GetClientAlive(clientID As Integer) As Boolean
        If Not Started Then Throw New InvalidOperationException("Server is not running.")

        If Clients(clientID) Is Nothing Then Return False
        Return Clients(clientID).Socket.Connected
    End Function

    ''' <summary>Creates a new ghost socket with the Silenced flag set.</summary>
    ''' <param name="clientID">Client index</param>
    ''' <remarks>Silencing clients will cause Send class to them to be ignored.</remarks>
    ''' <exception cref="InvalidOperationException">The server is not running.</exception>
    ''' <exception cref="ArgumentException">The specified client already exists.</exception>
    Public Sub CreateSilenced(clientID As Integer)
        If Not Started Then Throw New InvalidOperationException("Server is not running.")
        If Clients(clientID) IsNot Nothing Then Throw New ArgumentException("Such a client already exists.")

        Dim Client As New CommClient
        Client.Silenced = True
        Clients(clientID) = Client
    End Sub

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
        If Clients(clientID).Silenced Then Exit Sub
        If Not Clients(clientID).Socket.Connected Then Throw New ArgumentException("Specified client connection is closed.")
        If packet Is Nothing OrElse packet.Length < 1 Then Throw New ArgumentException("Invalid packet.")

        Dim sendBf(packet.Length + 3) As Byte
        Dim lenBf() As Byte = BitConverter.GetBytes(packet.Length)
        If BitConverter.IsLittleEndian Then lenBf.Reverse()
        Buffer.BlockCopy(lenBf, 0, sendBf, 0, 4) ' byte count
        Buffer.BlockCopy(packet, 0, sendBf, 4, packet.Length) ' packet contents

        Clients(clientID).Socket.GetStream.BeginWrite(sendBf, 0, sendBf.Length, callback, clientID)
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
            If Clients(I).Silenced Then Continue For
            If Not Clients(I).Socket.Connected Then Continue For
            ' send the data
            Send(I, packet)
        Next
    End Sub

    ''' <summary>
    ''' Disconnects the specified client and disposes of the socket. If the client is already disconnected, no exception is thrown.
    ''' </summary>
    ''' <param name="clientID">Client index</param>
    ''' <remarks></remarks>
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
    ''' <remarks></remarks>
    ''' <exception cref="ArgumentException">Packet is null or zero bytes.</exception>
    ''' <exception cref="InvalidOperationException">Server is not running.</exception>
    Public Overloads Sub Kick(clientID As Integer, packet() As Byte)
        If Not Started Then Throw New InvalidOperationException("Server is not running.")
        If packet Is Nothing OrElse packet.Length < 1 Then Throw New ArgumentException("Invalid packet.")
        ' make sure the connection exists
        If Clients(clientID) IsNot Nothing AndAlso Clients(clientID).Socket.Connected Then
            ' disconnect the client*
            Send(clientID, packet, AddressOf DelayKickCallback)
            'Dim sendBf(Packet.Length) As Byte ' last byte will be 0x0
            'Buffer.BlockCopy(Packet, 0, sendBf, 0, Packet.Length)
            'Clients(clientID).Socket.GetStream.BeginWrite(sendBf, 0, sendBf.Length, New AsyncCallback(AddressOf DelayKickCallback), clientID)
        Else
            Clients(clientID) = Nothing
        End If
    End Sub

    Private Sub DelayKickCallback(ir As IAsyncResult)
        ' called from Kick(Integer, Byte()) - callback, kill the client now
        Try
            Dim clientID As Integer = CInt(ir.AsyncState)
            Clients(clientID).Socket.GetStream.EndWrite(ir)
            Kick(clientID)
        Catch ex As SocketException
            RaiseEvent OnError(ex)
        End Try
    End Sub

End Class

Friend Class CommClient

    Public Socket As TcpClient
    Public Silenced As Boolean

    Public ReadBuffer(0 To 4095) As Byte
    Public PacketBuffer() As Byte

    Public Sub New()
        Socket = Nothing
        Silenced = False
        PacketBuffer = New Byte() {}
    End Sub

End Class
