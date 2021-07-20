namespace STUN.Client.FSharp

module Program =
    open System
    open System.Net
    
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

    let private tryParseEndpoint (endpoint: string) = try parseEndpoint endpoint |> Ok with exn -> Error exn
    
    let private parseCommandLineArgs (args: string []) =
        if Array.isEmpty args then
            raise (Exception "Command line args must not be empty")
        else
            args |> Array.toList
                
    let private tryParseCommandLineArgs (args: string []) = try parseCommandLineArgs args |> Ok with exn -> Error exn
    
    let private printUsage () =
        printfn "Usage: stun-client-fsharp [command] [command-option]"
        printfn ""
        printfn "Commands:"
        printfn "   -h | --help                 Display this help menu."
        printfn "   -s | --server-endpoint      IPv4 STUN server endpoint."
        printfn ""

    let private queryServerEndpoint (serverEndpoint: string) =
        match tryParseEndpoint serverEndpoint with
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
        | Error _ ->
            printUsage ()
        
    [<EntryPoint>]
    let main (args: string []): int = 
        match tryParseCommandLineArgs args with
        | Ok (command::commandOptions) ->
            match (command::commandOptions) with
            | "-h"    ::_
            | "--help"::_ ->
                printUsage ()
            | "-s"               ::serverEndpoint::_
            | "--server-endpoint"::serverEndpoint::_ ->
                queryServerEndpoint serverEndpoint                
            | _ -> 
                printUsage ()
        | _ ->
            printUsage ()
        0