namespace STUN.Client.FSharp

open System
open System.Net
open System.Net.Sockets

[<RequireQualifiedAccess>]
module STUNClient =
    let private createTxId (): byte [] =
        Guid.NewGuid().ToByteArray()
    
    let private validateTxId
        (requestTxId:  byte [])
        (responseTxId: byte []): bool =
        ((Array.compareWith
            (fun (left: byte) (right: byte) -> left.CompareTo(right))
            requestTxId
            responseTxId) <> 0)
    
    let private handleStateTransition
        (sendQuery:            STUNMessage -> STUNQueryResult)
        (requestMessage:       STUNMessage)
        (bindingResponse:      byte [] -> STUNAttribute list -> STUNStateResult)
        (bindingErrorResponse: byte [] -> STUNAttribute list -> STUNStateResult)
        (writeFailure:         unit -> STUNStateResult)
        (readFailure:          unit -> STUNStateResult): STUNStateResult =
        let queryResult = sendQuery requestMessage
        match queryResult with
        | QuerySuccess responseMessage ->
            match responseMessage with
            | BindingResponse     (responseTxId, attributes) -> bindingResponse      responseTxId attributes
            | BindingErrorResponse(responseTxId, attributes) -> bindingErrorResponse responseTxId attributes
            | _ ->
                StateFailure(BadResponse)
        | QueryWriteFailure _ -> writeFailure ()
        | QueryReadFailure  _ -> readFailure  ()

    let bindingResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (_:            STUNAttribute list)
        (nextFn:       unit -> STUNStateResult): STUNStateResult =
        if not (validateTxId requestTxId responseTxId) then
            StateFailure(BadTransactionId)
        else
            nextFn ()

    let bindingErrorResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (attributes:   STUNAttribute list): STUNStateResult =
        if not (validateTxId requestTxId responseTxId) then
            StateFailure(BadTransactionId)
        else
            match (attributes
                  |> Seq.tryFind (fun (attribute: STUNAttribute) ->
                      match attribute with ErrorCodeAttribute _ -> true | _ -> false ))  with
            | Some (ErrorCodeAttribute(errorCode, errorMessage)) -> 
                StateFailure(ServerError(errorCode, errorMessage))
            | _ -> 
                StateFailure(BadResponse)

    let rec private mappedAddressStateBindingResponse
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (responseTxId:   byte [])
        (attributes:     STUNAttribute list)
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint option): STUNStateResult =
        let handleResponse () =
            match (attributes
                   |> Seq.tryFind (fun (attribute: STUNAttribute) ->
                       match attribute with MappedAddressAttribute _ -> true | _ -> false ))  with
            | Some (MappedAddressAttribute nextPublicEndpoint) -> 
                match publicEndpoint with
                | Some publicEndpoint ->
                    if (publicEndpoint.Equals(nextPublicEndpoint)) then
                        executeStates sendQuery (ChangePortState(requestTxId, localEndpoint, publicEndpoint))
                    else
                        StateSuccess(Symmetric, localEndpoint, nextPublicEndpoint)
                | None -> 
                    if localEndpoint.Equals(nextPublicEndpoint) then
                        executeStates sendQuery (SameAddressState(requestTxId, localEndpoint, nextPublicEndpoint))
                    else
                        executeStates sendQuery (ChangeAddressState(requestTxId, localEndpoint, nextPublicEndpoint))
            | _ -> 
                StateFailure(BadResponse)
        bindingResponse requestTxId responseTxId attributes handleResponse
        
    and private mappedAddressStateBindingErrorResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (attributes:   STUNAttribute list): STUNStateResult =
        bindingErrorResponse requestTxId responseTxId attributes
        
    and private mappedAddressStateWriteFailure () =
        StateFailure(RequestFailure "") // TODO: !
        
    and private mappedAddressStateReadFailure () =
        StateFailure(ResponseFailure "") // TODO: !
        
    and private mappedAddressState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint option): STUNStateResult =
        let requestMessage = BindingRequest(requestTxId, List.Empty)
        
        let bindingResponse      responseTxId attributes = mappedAddressStateBindingResponse sendQuery requestTxId responseTxId attributes localEndpoint publicEndpoint
        let bindingErrorResponse responseTxId attributes = mappedAddressStateBindingErrorResponse requestTxId responseTxId attributes
        
        let writeFailure = mappedAddressStateWriteFailure
        let readFailure  = mappedAddressStateReadFailure
        
        handleStateTransition
                sendQuery
                requestMessage
                bindingResponse
                bindingErrorResponse
                writeFailure
                readFailure

    and private sameAddressState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint) =
        StateSuccess

    and private changeAddressState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint) =
        StateSuccess

    and private changePortState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint) =
        StateSuccess
        
    and executeStates (sendQuery: STUNMessage -> STUNQueryResult) (state: STUNState) =
        // Mapped-Address State
        //  * StateSuccess -> 
        //      * Public IP same as local IP -> 
        //          Same-Address State
        //              * StateSuccess -> OpenInternet
        //              * StateFailure -> 
        //                  * ResponseTimeoutError -> SymmetricUDPFirewall
        //                  * Otherwise            -> BadResponse
        //      * Public IP not same as local IP -> 
        //          Change-Address State
        //              * StateSuccess  -> FullCone
        //              * StateFailure -> 
        //                  * ResponseTimeoutError -> 
        //                      Mapped-Address State
        //                          * StateSuccess -> 
        //                              * Public IP same as public IP returned in previous request ->
        //                                  Change-Port State
        //                                      * StateSuccess -> RestrictedCone
        //                                      * StateFailure -> PortRestrictedCone
        //                              * Public IP not same as public IP returned in previous request -> Symmetric
        //                          * StateFailure -> BadResponse
        //                  * Otherwise -> BadResponse
        //  * StateFailure -> 
        //      * ResponseTimeout -> ResponseTimeout (UDP blocked)
        //      * Otherwise       -> BadResponse
        match state with
        | MappedAddressState(requestTxId, localEndpoint, publicEndpoint) -> mappedAddressState sendQuery requestTxId localEndpoint publicEndpoint
//        | SameAddressState  (requestTxId, localEndpoint, publicEndpoint) -> sameAddressState   sendQuery requestTxId localEndpoint publicEndpoint
//        | ChangeAddressState(requestTxId, localEndpoint, publicEndpoint) -> changeAddressState sendQuery requestTxId localEndpoint publicEndpoint
//        | ChangePortState   (requestTxId, localEndpoint, publicEndpoint) -> changePortState    sendQuery requestTxId localEndpoint publicEndpoint

    let execute
        (socket:         Socket)
        (serverEndpoint: IPEndPoint): STUNStateResult =
        let requestTxId = createTxId ()
        let sendQuery   = STUNSocket.sendQuery socket serverEndpoint
        let nextState   = MappedAddressState(requestTxId, (socket.LocalEndPoint :?> IPEndPoint), None)
        executeStates sendQuery nextState

    let asyncExecute
        (socket:         Socket)
        (serverEndpoint: IPEndPoint): Async<STUNStateResult> =
        async {
            return execute socket serverEndpoint
        }