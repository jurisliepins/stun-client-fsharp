namespace STUN.Client.FSharp

open System
open System.Net
open System.Net.Sockets

[<RequireQualifiedAccess>]
module STUNClient =
    let private createTxId (): byte [] =
        Guid.NewGuid().ToByteArray()
    
    let private compareTxIds
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
        (writeFailure:         STUNQueryError -> STUNStateResult)
        (readFailure:          STUNQueryError -> STUNStateResult): STUNStateResult =
        let queryResult = sendQuery requestMessage
        match queryResult with
        | QuerySuccess responseMessage ->
            match responseMessage with
            | BindingResponse     (responseTxId, attributes) -> bindingResponse      responseTxId attributes
            | BindingErrorResponse(responseTxId, attributes) -> bindingErrorResponse responseTxId attributes
            | _ ->
                StateFailure(BadResponse)
        | QueryWriteFailure error -> writeFailure error
        | QueryReadFailure  error -> readFailure  error

    let bindingResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (_:            STUNAttribute list)
        (nextFn:       unit -> STUNStateResult): STUNStateResult =
        if not (compareTxIds requestTxId responseTxId) then
            StateFailure(BadTransactionId)
        else
            nextFn ()

    let bindingErrorResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (attributes:   STUNAttribute list): STUNStateResult =
        if not (compareTxIds requestTxId responseTxId) then
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
        
    and private mappedAddressStateWriteFailure (error: STUNQueryError): STUNStateResult =
        StateFailure(error)
        
    and private mappedAddressStateReadFailure (error: STUNQueryError): STUNStateResult =
        StateFailure(error)
            
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

    and private sameAddressStateBindingResponse
        (requestTxId:    byte [])
        (responseTxId:   byte [])
        (attributes:     STUNAttribute list)
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint): STUNStateResult =
        let handleResponse () =
            StateSuccess(OpenInternet, localEndpoint, publicEndpoint)
        bindingResponse requestTxId responseTxId attributes handleResponse
    
    and private sameAddressStateBindingErrorResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (attributes:   STUNAttribute list): STUNStateResult =
        bindingErrorResponse requestTxId responseTxId attributes

    and private sameAddressStateWriteFailure (error: STUNQueryError): STUNStateResult =
        StateFailure(error)
        
    and private sameAddressStateReadFailure
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint): STUNQueryError -> STUNStateResult =
        let handleFailure (_: STUNQueryError) =
            StateSuccess(SymmetricUDPFirewall, localEndpoint, publicEndpoint)
        handleFailure
        
    and private sameAddressState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint): STUNStateResult =
        let requestMessage = BindingRequest(requestTxId, [ChangeRequestAttribute(true, true)])
        
        let bindingResponse      responseTxId attributes = sameAddressStateBindingResponse requestTxId responseTxId attributes localEndpoint publicEndpoint
        let bindingErrorResponse responseTxId attributes = sameAddressStateBindingErrorResponse requestTxId responseTxId attributes

        let writeFailure = sameAddressStateWriteFailure
        let readFailure  = sameAddressStateReadFailure localEndpoint publicEndpoint

        handleStateTransition
                sendQuery
                requestMessage
                bindingResponse
                bindingErrorResponse
                writeFailure
                readFailure

    and private changeAddressStateBindingResponse
        (requestTxId:    byte [])
        (responseTxId:   byte [])
        (attributes:     STUNAttribute list)
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint): STUNStateResult =
        let handleResponse () =
            StateSuccess(FullCone, localEndpoint, publicEndpoint)
        bindingResponse requestTxId responseTxId attributes handleResponse
    
    and private changeAddressStateBindingErrorResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (attributes:   STUNAttribute list): STUNStateResult =
        bindingErrorResponse requestTxId responseTxId attributes
    
    and private changeAddressStateWriteFailure (error: STUNQueryError): STUNStateResult =
        StateFailure(error)
    
    and private changeAddressStateReadFailure
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTx:      byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint) =
        let handleResponse (_: STUNQueryError) =
            executeStates sendQuery (MappedAddressState(requestTx, localEndpoint, (Some publicEndpoint)))
        handleResponse
    
    and private changeAddressState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint) =
        let requestMessage = BindingRequest(requestTxId, [ChangeRequestAttribute(true, true)])

        let bindingResponse      responseTxId attributes = changeAddressStateBindingResponse requestTxId responseTxId attributes localEndpoint publicEndpoint
        let bindingErrorResponse responseTxId attributes = changeAddressStateBindingErrorResponse requestTxId responseTxId attributes

        let writeFailure = changeAddressStateWriteFailure
        let readFailure  = changeAddressStateReadFailure sendQuery requestTxId localEndpoint publicEndpoint
        
        handleStateTransition
                sendQuery
                requestMessage
                bindingResponse
                bindingErrorResponse
                writeFailure
                readFailure

    and private changePortStateBindingResponse
        (requestTxId:    byte [])
        (responseTxId:   byte [])
        (attributes:     STUNAttribute list)
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint): STUNStateResult =
        let handleResponse () =
            StateSuccess(RestrictedCone, localEndpoint, publicEndpoint)
        bindingResponse requestTxId responseTxId attributes handleResponse
    
    and private changePortStateBindingErrorResponse
        (requestTxId:  byte [])
        (responseTxId: byte [])
        (attributes:   STUNAttribute list): STUNStateResult =
        bindingErrorResponse requestTxId responseTxId attributes
    
    and private changePortStateWriteFailure (error: STUNQueryError): STUNStateResult = 
        StateFailure(error)
    
    and private changePortStateReadFailure
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint): STUNQueryError -> STUNStateResult =
        let handleResponse (_: STUNQueryError) =
            StateSuccess(PortRestrictedCone, localEndpoint, publicEndpoint)
        handleResponse
    
    and private changePortState
        (sendQuery:      STUNMessage -> STUNQueryResult)
        (requestTxId:    byte [])
        (localEndpoint:  IPEndPoint)
        (publicEndpoint: IPEndPoint) =
        let requestMessage = BindingRequest(requestTxId, [ChangeRequestAttribute(false, true)])

        let bindingResponse      responseTxId attributes = changePortStateBindingResponse requestTxId responseTxId attributes localEndpoint publicEndpoint
        let bindingErrorResponse responseTxId attributes = changePortStateBindingErrorResponse requestTxId responseTxId attributes

        let writeFailure = changePortStateWriteFailure
        let readFailure  = changePortStateReadFailure localEndpoint publicEndpoint
        
        handleStateTransition
                sendQuery
                requestMessage
                bindingResponse
                bindingErrorResponse
                writeFailure
                readFailure
        
    and executeStates (sendQuery: STUNMessage -> STUNQueryResult) (state: STUNState) =
        // Mapped-Address State
        //  * Success -> 
        //      * Public IP same as local IP -> 
        //          Same-Address State
        //              * Success -> OpenInternet
        //              * Failure -> 
        //                  * ResponseFailure -> SymmetricUDPFirewall
        //                  * Otherwise       -> BadResponse
        //      * Public IP not same as local IP -> 
        //          Change-Address State
        //              * Success -> FullCone
        //              * Failure -> 
        //                  * ResponseFailure -> 
        //                      Mapped-Address State
        //                          * Success -> 
        //                              * Public IP same as public IP returned in previous request ->
        //                                  Change-Port State
        //                                      * Success -> RestrictedCone
        //                                      * Failure -> PortRestrictedCone
        //                              * Public IP not same as public IP returned in previous request -> Symmetric
        //                          * Failure -> BadResponse
        //                  * Otherwise -> BadResponse
        //  * Failure -> 
        //      * ResponseFailure -> ResponseFailure (UDP blocked)
        //      * Otherwise       -> BadResponse
        match state with
        | MappedAddressState(requestTxId, localEndpoint, publicEndpoint) -> mappedAddressState sendQuery requestTxId localEndpoint publicEndpoint
        | SameAddressState  (requestTxId, localEndpoint, publicEndpoint) -> sameAddressState   sendQuery requestTxId localEndpoint publicEndpoint
        | ChangeAddressState(requestTxId, localEndpoint, publicEndpoint) -> changeAddressState sendQuery requestTxId localEndpoint publicEndpoint
        | ChangePortState   (requestTxId, localEndpoint, publicEndpoint) -> changePortState    sendQuery requestTxId localEndpoint publicEndpoint

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