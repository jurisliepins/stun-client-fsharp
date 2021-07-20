namespace STUN.Client.FSharp

open System.Net
open System.Net.Sockets

[<RequireQualifiedAccess>]
module STUNSocket =
    
    [<Literal>]
    let private defaultTimeout = 2000000

    [<Literal>]
    let private defaultBufferLength = 2048

    let private socketResult result =
        match result with Ok(count, endpoint) -> Ok(count, endpoint) | Error exn -> Error exn

    type private Socket with
        member this.Read(buffer: byte [], count: int): Result<int * EndPoint, exn> =
            match (Socket.poll defaultTimeout SelectMode.SelectRead this) with
            | Ok success -> 
                if success then
                    let readResult = Socket.receiveFrom buffer 0 count SocketFlags.None this 
                    readResult |> socketResult
                else 
                    Error(exn "Polling socket timed out")
            | Error exn -> Error exn 
        
        member this.Write(buffer: byte [], count: int, endpoint: IPEndPoint) =
            let writeResult = Socket.sendTo buffer 0 count SocketFlags.None endpoint this
            writeResult |> socketResult 

    let sendQuery (socket: Socket) (serverEndpoint: IPEndPoint) (requestMessage: STUNMessage): STUNQueryResult = 
        let handleResponse () =
            let readBuffer = Array.zeroCreate defaultBufferLength
            match socket.Read(readBuffer, readBuffer.Length) with
            | Ok _ ->
                match readBuffer |> STUNParser.tryReadMessageBytes with
                | Ok responseMessage -> QuerySuccess(responseMessage)
                | Error _            -> QueryReadFailure(BadResponse) 
            | Error exn -> QueryReadFailure(ResponseFailure exn)
        
        let handleRequest () =
            match requestMessage |> STUNParser.tryWriteMessageBytes with
            | Ok writeBuffer -> 
                match socket.Write(writeBuffer, writeBuffer.Length, serverEndpoint) with 
                | Ok      _ -> handleResponse ()
                | Error exn -> QueryReadFailure(RequestFailure exn)
            | Error _ -> QueryReadFailure(BadRequest)
            
        handleRequest ()