namespace STUN.Client.FSharp

open System.Net.Sockets
open System.Net
open System.Collections

[<RequireQualifiedAccess>]
module Socket =

    let private tryWith fn =
        try fn () |> Ok with exn -> exn |> Error

    let private tryWithSocket fn socket =
        try fn (); socket |> Ok with exn -> Error exn
    
    let create
        (addressFamily: AddressFamily)
        (socketType:    SocketType)
        (protocolType:  ProtocolType): Result<Socket, exn> = 
        let create () =
            new Socket(addressFamily, socketType, protocolType)
        tryWith create 

    let bind (localEndpoint: EndPoint) (socket: Socket): Result<Socket, exn> = 
        let bind () =
            socket.Bind(localEndpoint)
        tryWithSocket bind socket

    let listen (backlog: int) (socket: Socket): Result<Socket, exn> = 
        let listen () =
            socket.Listen(backlog)
        tryWithSocket listen socket

    let connect (remoteEndpoint: EndPoint) (socket: Socket): Result<Socket, exn> = 
        let connect () =
            socket.Connect(remoteEndpoint)
        tryWithSocket connect socket

    let accept (socket: Socket): Result<Socket, exn> =
        let accept () =
            socket.Accept()
        accept |> tryWith

    let shutdown (how: SocketShutdown) (socket: Socket): Result<Socket, exn> =
        let shutdown () =
            socket.Shutdown(how)
        tryWithSocket shutdown socket

    let close (socket: Socket): Result<Socket, exn> =
        let close () =
            socket.Close()
        tryWithSocket close socket

    let getSocketOption
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (socket:      Socket): Result<obj, exn> =
        let getSocketOption () =
            socket.GetSocketOption(optionLevel, optionName)
        tryWith getSocketOption

    let setSocketOption
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   obj)
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        tryWithSocket setSocketOption socket 

    let setSocketOptionBool
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   bool)
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        tryWithSocket setSocketOption socket 

    let setSocketOptionInteger
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   int)
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        tryWithSocket setSocketOption socket

    let setSocketOptionBytes
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   byte [])
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        tryWithSocket setSocketOption socket

    let setIPProtectionLevel (socket: Socket) (level: IPProtectionLevel): Result<Socket, exn> =
        let setIPProtectionLevel () =
            socket.SetIPProtectionLevel(level)
        tryWithSocket setIPProtectionLevel socket

    let ioControl
        (controlCode: IOControlCode)
        (inValue:     byte [])
        (outValue:    byte [])
        (socket:      Socket): Result<int, exn> =
        let ioControl () =
            socket.IOControl(controlCode, inValue, outValue)
        tryWith ioControl

    let poll (microSeconds: int) (mode: SelectMode) (socket: Socket): Result<bool, exn> =
        let poll () =
            socket.Poll(microSeconds, mode)
        tryWith poll

    let select
        (checkRead:    IList)
        (checkWrite:   IList)
        (checkError:   IList)
        (microSeconds: int): Result<unit, exn> =
        let select () =
            Socket.Select(checkRead, checkWrite, checkError, microSeconds)
        tryWith select

    let receive
        (buffer:      byte [])
        (offset:      int)
        (size:        int)
        (socketFlags: SocketFlags)
        (socket:      Socket): Result<int, exn> =
        let receive () =
            socket.Receive(buffer, offset, size, socketFlags)
        tryWith receive
    
    let receiveFrom
        (buffer:      byte [])
        (offset:      int)
        (size:        int)
        (socketFlags: SocketFlags)
        (socket:      Socket): Result<int * EndPoint, exn> =
        let receiveFrom () =
            let mutable remoteEndpoint = ((IPEndPoint(IPAddress.Any, 0)):> EndPoint)
            socket.ReceiveFrom(buffer, offset, size, socketFlags, &remoteEndpoint), remoteEndpoint
        tryWith receiveFrom

    let send
        (buffer:      byte [])
        (offset:      int)
        (size:        int)
        (socketFlags: SocketFlags)
        (socket:      Socket): Result<int, exn> =
        let send () =
            socket.Send(buffer, offset, size, socketFlags)
        tryWith send
    
    let sendTo
        (buffer:         byte [])
        (offset:         int)
        (size:           int)
        (socketFlags:    SocketFlags)
        (remoteEndpoint: EndPoint)
        (socket:         Socket): Result<int * EndPoint, exn> =
        let sendTo () =
            socket.SendTo(buffer, offset, size, socketFlags, remoteEndpoint), remoteEndpoint
        tryWith sendTo

    let getPeerName (socket: Socket): Result<EndPoint, exn> =
        let getPeerName () =
            socket.RemoteEndPoint
        tryWith getPeerName
    
    let getHostName (): Result<string, exn> =
        let getHostName () =
            Dns.GetHostName()
        tryWith getHostName