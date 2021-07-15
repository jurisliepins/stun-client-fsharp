namespace STUN.Client.FSharp

open System.Net.Sockets
open System.Net
open System.Collections

[<RequireQualifiedAccess>]
module Socket =

    let private tryWith (fn: unit -> 'a): Result<'a,exn> =
        try 
            fn () |> Ok 
        with exn -> exn |> Error

    let private tryWithSocket (fn: unit -> unit, socket: Socket): Result<Socket, exn> =
        try 
            fn ()
            Ok socket 
        with exn -> Error exn
    
    let create
        (addressFamily: AddressFamily)
        (socketType:    SocketType)
        (protocolType:  ProtocolType): Result<Socket, exn> = 
        let create () =
            new Socket(addressFamily, socketType, protocolType)
        create |> tryWith

    let bind (localEndpoint: EndPoint) (socket: Socket): Result<Socket, exn> = 
        let bind () =
            socket.Bind(localEndpoint)
        (bind, socket) |> tryWithSocket

    let listen (backlog: int) (socket: Socket): Result<Socket, exn> = 
        let listen () =
            socket.Listen(backlog)
        (listen, socket) |> tryWithSocket

    let connect (remoteEndpoint: EndPoint) (socket: Socket): Result<Socket, exn> = 
        let connect () =
            socket.Connect(remoteEndpoint)
        (connect, socket) |> tryWithSocket

    let accept (socket: Socket): Result<Socket, exn> =
        let accept () =
            socket.Accept()
        accept |> tryWith

    let shutdown (how: SocketShutdown) (socket: Socket): Result<Socket, exn> =
        let shutdown () =
            socket.Shutdown(how)
        (shutdown, socket) |> tryWithSocket

    let close (socket: Socket): Result<Socket, exn> =
        let close () =
            socket.Close()
        (close, socket) |> tryWithSocket

    let getSocketOption
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (socket:      Socket): Result<obj, exn> =
        let getSocketOption () =
            socket.GetSocketOption(optionLevel, optionName)
        getSocketOption |> tryWith

    let setSocketOption
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   obj)
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        (setSocketOption, socket) |> tryWithSocket

    let setSocketOptionBool
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   bool)
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        (setSocketOption, socket) |> tryWithSocket

    let setSocketOptionInteger
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   int)
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        (setSocketOption, socket) |> tryWithSocket

    let setSocketOptionBytes
        (optionLevel: SocketOptionLevel)
        (optionName:  SocketOptionName)
        (optionArg:   byte [])
        (socket:      Socket): Result<Socket, exn> =
        let setSocketOption () =
            socket.SetSocketOption(optionLevel, optionName, optionArg)
        (setSocketOption, socket) |> tryWithSocket

    let setIPProtectionLevel (socket: Socket) (level: IPProtectionLevel): Result<Socket, exn> =
        let setIPProtectionLevel () =
            socket.SetIPProtectionLevel(level)
        (setIPProtectionLevel, socket) |> tryWithSocket

    let ioControl
        (controlCode: IOControlCode)
        (inValue:     byte [])
        (outValue:    byte [])
        (socket:      Socket): Result<int, exn> =
        let ioControl () =
            socket.IOControl(controlCode, inValue, outValue)
        ioControl |> tryWith

    let poll (microSeconds: int) (mode: SelectMode) (socket: Socket): Result<bool, exn> =
        let poll () =
            socket.Poll(microSeconds, mode)
        poll |> tryWith

    let select
        (checkRead:    IList)
        (checkWrite:   IList)
        (checkError:   IList)
        (microSeconds: int): Result<unit, exn> =
        let select () =
            Socket.Select(checkRead, checkWrite, checkError, microSeconds)
        select |> tryWith

    let receive
        (buffer:      byte [])
        (offset:      int)
        (size:        int)
        (socketFlags: SocketFlags)
        (socket:      Socket): Result<int, exn> =
        let receive () =
            socket.Receive(buffer, offset, size, socketFlags)
        receive |> tryWith
    
    let receiveFrom
        (buffer:      byte [])
        (offset:      int)
        (size:        int)
        (socketFlags: SocketFlags)
        (socket:      Socket): Result<int * EndPoint, exn> =
        let receiveFrom () =
            let mutable remoteEndpoint = ((IPEndPoint(IPAddress.Any, 0)):> EndPoint)
            socket.ReceiveFrom(buffer, offset, size, socketFlags, &remoteEndpoint), remoteEndpoint
        receiveFrom |> tryWith

    let send
        (buffer:      byte [])
        (offset:      int)
        (size:        int)
        (socketFlags: SocketFlags)
        (socket:      Socket): Result<int, exn> =
        let send () =
            socket.Send(buffer, offset, size, socketFlags)
        send |> tryWith
    
    let sendTo
        (buffer:         byte [])
        (offset:         int)
        (size:           int)
        (socketFlags:    SocketFlags)
        (remoteEndpoint: EndPoint)
        (socket:         Socket): Result<int * EndPoint, exn> =
        let sendTo () =
            socket.SendTo(buffer, offset, size, socketFlags, remoteEndpoint), remoteEndpoint
        sendTo |> tryWith

    let getPeerName (socket: Socket): Result<EndPoint, exn> =
        let getPeerName () =
            socket.RemoteEndPoint
        getPeerName |> tryWith
    
    let getHostName (): Result<string, exn> =
        let getHostName () =
            Dns.GetHostName()
        getHostName |> tryWith