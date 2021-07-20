namespace STUN.Client.FSharp

module Program =
    open System
    open System.Net
    
    let private tryWith args fn =
        try
            fn args |> Ok
        with
            | ex -> Error ex
    
    let private parseEndpoint (endpoint: string) =
        if String.IsNullOrWhiteSpace(endpoint) then
            raise (Exception "Endpoint must not be empty")
        else 
            let (|IPAddress|_|) (value: string) = match IPAddress.TryParse(value) with true, address -> Some(address) | _ -> None
            let (|Port|_|)      (value: string) = match Int32.TryParse(value)     with true, port    -> Some(port)    | _ -> None
            match (endpoint.Split(':') |> Array.toList) with
            | address::port::_ -> 
                match (address, port) with
                | IPAddress address, Port port -> IPEndPoint(address, port)
                | _                , Port port -> IPEndPoint(Dns.GetHostEntry(address).AddressList |> Array.head, port)
                | _ -> 
                    raise (Exception "Invalid address or port")
            | _ -> 
                raise (Exception "Invalid endpoint format")

    let private tryParseEndpoint (endpoint: string) = parseEndpoint |> tryWith endpoint
    
    [<EntryPoint>]
    let main _ =
        match tryParseEndpoint "stun.sovtest.ru:3478" with
        | Ok serverEndpoint ->
            let result = STUNClient.queryWithDefaultSocket serverEndpoint
            match result with
            | Ok stateResult -> 
                match stateResult with
                | StateSuccess(natType, publicEndpoint, localEndpoint) -> 
                    printfn "NatType:        %A" natType
                    printfn "PublicEndpoint: %A" publicEndpoint
                    printfn "LocalEndpoint:  %A" localEndpoint
                | StateFailure queryError -> 
                    printfn "Failed while querying the server - %A" queryError
            | Error exn ->
                printfn "Failed to connect to the server - %A" exn.Message
        | Error exn ->
            printfn "Failed to parse server endpoint - %A" exn.Message 
        0