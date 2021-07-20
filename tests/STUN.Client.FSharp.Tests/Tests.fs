module Tests

open Xunit
open Xunit.Abstractions
open STUN.Client.FSharp
open System.Net

type STUNTests(outputHelper: ITestOutputHelper) =
    
    let rec containsChangeAddressRequestAttribute (attributes: STUNAttribute list) =
        match attributes with
        | head :: tail -> 
            match head with
            | ChangeRequestAttribute(true, true) -> true
            | _ -> containsChangeAddressRequestAttribute tail
        | [] -> false

    let rec containsChangePortRequestAttribute (attributes: STUNAttribute list) =
        match attributes with
        | head :: tail -> 
            match head with
            | ChangeRequestAttribute(false, true) -> true
            | _ -> containsChangePortRequestAttribute tail
        | [] -> false

    let executeStatesShouldFail (): unit =
        Assert.True(false, "executing states should have failed")

    let executeStatesShouldNotHaveFailed (error: STUNQueryError): unit =
        Assert.True(false, (sprintf "executing states should not have failed with %A" error))
    
    let executeStatesShouldReturn (returnType: string): unit =
        Assert.True(false, (sprintf "executing states should have returned a '%s'" returnType))

    [<Fact>]
    let ``Test UdpBlocked result`` () =
        let mockRequestTxId   = STUNClient.createTxId ()
        let mockLocalEndpoint = IPEndPoint(0L, 0)
        
        let mockSendQuery (_: STUNMessage) =
            QueryReadFailure(ResponseFailure(exn "Mock error"))

        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess _ ->
            executeStatesShouldFail ()
        | StateFailure queryError -> 
            match queryError with
            | ResponseFailure _ ->
                ()
            | _ ->
                executeStatesShouldReturn "ResponseFailure"
            
    [<Fact>]
    let ``Test OpenInternet result`` () =
        let mockRequestTxId    = STUNClient.createTxId ()
        let mockLocalEndpoint  = IPEndPoint(0L, 0)
        let mockPublicEndpoint = IPEndPoint(0L, 0)
        
        let mockSendQuery (message: STUNMessage): STUNQueryResult = 
            match message with 
            | BindingRequest(_, attributes) when (List.isEmpty attributes) -> 
                QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
            | BindingRequest _ -> 
                QuerySuccess(BindingResponse(mockRequestTxId, List.empty))
            | _ -> 
                raise (exn "Unexpected message type")

        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess(natType, _, _) -> 
            match natType with
            | OpenInternet ->
                ()
            | _ ->
                executeStatesShouldReturn "OpenInternet"
        | StateFailure error ->
            executeStatesShouldNotHaveFailed error

    [<Fact>]
    let ``Test FullCone result`` () =
        let mockRequestTxId    = STUNClient.createTxId ()
        let mockLocalEndpoint  = IPEndPoint(0L, 0)
        let mockPublicEndpoint = IPEndPoint(1L, 0)
    
        let mockSendQuery (message: STUNMessage): STUNQueryResult = 
            match message with 
            | BindingRequest(_, attributes) when (List.isEmpty attributes) -> 
                QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
            | BindingRequest _ -> 
                QuerySuccess(BindingResponse(mockRequestTxId, List.empty))
            | _ ->
                raise (exn "Unexpected message type")

        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess(natType, _, _) -> 
            match natType with
            | FullCone ->
                ()
            | _ ->
                executeStatesShouldReturn "FullCone"
        | StateFailure error ->
            executeStatesShouldNotHaveFailed error

    [<Fact>]
    let ``Test RestrictedCone result`` () =
        let mockRequestTxId    = STUNClient.createTxId ()
        let mockLocalEndpoint  = IPEndPoint(0L, 0)
        let mockPublicEndpoint = IPEndPoint(1L, 0)
    
        let mutable mappedAddressCallCount: int = 0
        let mockSendQuery (message: STUNMessage): STUNQueryResult = 
            match message with 
            | BindingRequest(_, attrs) when (List.isEmpty attrs) -> 
                mappedAddressCallCount <- mappedAddressCallCount + 1
                if (mappedAddressCallCount < 2) then
                    QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
                else 
                    QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
            | BindingRequest(_, attributes) when (containsChangeAddressRequestAttribute attributes) -> QueryReadFailure(Unknown)
            | BindingRequest(_, attributes) when (containsChangePortRequestAttribute    attributes) -> QuerySuccess(BindingResponse(mockRequestTxId, List.empty))
            | _ ->
                raise (exn "Unexpected message type")

        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess(natType, _, _) -> 
            match natType with
            | RestrictedCone ->
                ()
            | _ ->
                executeStatesShouldReturn "RestrictedCone"
        | StateFailure error ->
            executeStatesShouldNotHaveFailed error

    [<Fact>]
    let ``Test PortRestrictedCone result`` () =
        let mockRequestTxId    = STUNClient.createTxId ()
        let mockLocalEndpoint  = IPEndPoint(0L, 0)
        let mockPublicEndpoint = IPEndPoint(1L, 0)

        let mutable mappedAddressCallCount: int = 0
        let mockSendQuery (message: STUNMessage): STUNQueryResult = 
            match message with 
            | BindingRequest(_, attributes) when (List.isEmpty attributes) -> 
                mappedAddressCallCount <- mappedAddressCallCount + 1
                if (mappedAddressCallCount < 2) then
                    QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
                else 
                    QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
            | BindingRequest(_, attributes) when (containsChangeAddressRequestAttribute attributes) -> QueryReadFailure(Unknown)
            | BindingRequest(_, attributes) when (containsChangePortRequestAttribute    attributes) -> QueryReadFailure(Unknown)
            | _ ->
                raise (exn "Unexpected message type")

        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess(natType, _, _) -> 
            match natType with
            | PortRestrictedCone ->
                ()
            | _ ->
                executeStatesShouldReturn "PortRestrictedCone"
        | StateFailure error ->
            executeStatesShouldNotHaveFailed error

    [<Fact>]
    let ``Test Symmetric result`` () =
        let mockRequestTxId     = STUNClient.createTxId ()
        let mockLocalEndpoint   = IPEndPoint(0L, 0)
        let mockPublicEndpoint1 = IPEndPoint(1L, 0)
        let mockPublicEndpoint2 = IPEndPoint(2L, 0)
    
        let mutable mappedAddressCallCount: int = 0
        let mockSendQuery (message: STUNMessage): STUNQueryResult = 
            match message with 
            | BindingRequest(_, attributes) when (List.isEmpty attributes) -> 
                mappedAddressCallCount <- mappedAddressCallCount + 1
                if (mappedAddressCallCount < 2) then
                    QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint1]))
                else 
                    QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint2]))
            | BindingRequest(_, attributes) when (containsChangeAddressRequestAttribute attributes) -> QueryReadFailure(Unknown)
            | _ ->
                raise (exn "Unexpected message type")
        
        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess(natType, _, _) -> 
            match natType with
            | Symmetric ->
                ()
            | _ ->
                executeStatesShouldReturn "Symmetric"
        | StateFailure error ->
            executeStatesShouldNotHaveFailed error

    [<Fact>]
    let ``Test SymmetricUDPFirewall result`` () =
        let mockRequestTxId    = STUNClient.createTxId ()
        let mockLocalEndpoint  = IPEndPoint(0L, 0)
        let mockPublicEndpoint = IPEndPoint(0L, 0)
        
        let mockSendQuery (message: STUNMessage): STUNQueryResult = 
            match message with 
            | BindingRequest(_, attributes) when (List.isEmpty attributes) -> 
                QuerySuccess(BindingResponse(mockRequestTxId, [MappedAddressAttribute mockPublicEndpoint]))
            | BindingRequest _ ->
                QueryReadFailure(Unknown)
            | _ ->
                raise (exn "Unexpected message type")

        let result = STUNClient.executeStates
                         mockSendQuery
                         (MappedAddressState(mockRequestTxId, mockLocalEndpoint, None))
        match result with
        | StateSuccess(natType, _, _) -> 
            match natType with
            | SymmetricUDPFirewall ->
                ()
            | _ ->
                executeStatesShouldReturn "SymmetricUDPFirewall"
        | StateFailure error ->
            executeStatesShouldNotHaveFailed error