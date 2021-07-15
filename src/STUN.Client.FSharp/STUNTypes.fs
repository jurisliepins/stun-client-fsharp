namespace STUN.Client.FSharp

open System.Net

type STUNAttribute =
    | MappedAddressAttribute    of IPEndPoint
    | ResponseAddressAttribute  of IPEndPoint
    | ChangeRequestAttribute    of bool * bool
    | SourceAddressAttribute    of IPEndPoint
    | ChangedAddressAttribute   of IPEndPoint
    | UsernameAttribute         of string
    | PasswordAttribute         of string
    | MessageIntegrityAttribute
    | ErrorCodeAttribute        of ErrorCode * string
    | ReflectedFromAttribute
    with 
        member this.ToUInt16() =
            match this with
            | MappedAddressAttribute    _ -> 1us
            | ResponseAddressAttribute  _ -> 2us
            | ChangeRequestAttribute    _ -> 3us
            | SourceAddressAttribute    _ -> 4us
            | ChangedAddressAttribute   _ -> 5us
            | UsernameAttribute         _ -> 6us
            | PasswordAttribute         _ -> 7us
            | MessageIntegrityAttribute   -> 8us
            | ErrorCodeAttribute        _ -> 9us
            | ReflectedFromAttribute      -> 11us
        override this.ToString() =
            match this with
            | MappedAddressAttribute    _ -> "MappedAddressAttribute"
            | ResponseAddressAttribute  _ -> "ResponseAddressAttribute"
            | ChangeRequestAttribute    _ -> "ChangeRequestAttribute"
            | SourceAddressAttribute    _ -> "SourceAddressAttribute"
            | ChangedAddressAttribute   _ -> "ChangedAddressAttribute"
            | UsernameAttribute         _ -> "UsernameAttribute"
            | PasswordAttribute         _ -> "PasswordAttribute"
            | MessageIntegrityAttribute   -> "MessageIntegrityAttribute"
            | ErrorCodeAttribute        _ -> "ErrorCodeAttribute"
            | ReflectedFromAttribute      -> "ReflectedFromAttribute"

type STUNMessage =
    | BindingRequest            of byte [] * STUNAttribute list
    | BindingResponse           of byte [] * STUNAttribute list
    | BindingErrorResponse      of byte [] * STUNAttribute list
    | SharedSecretRequest       of byte [] * STUNAttribute list
    | SharedSecretResponse      of byte [] * STUNAttribute list
    | SharedSecretErrorResponse of byte [] * STUNAttribute list
    with 
        member this.ToUInt16() =
            match this with
            | BindingRequest            _ -> 0x0001us
            | BindingResponse           _ -> 0x0101us
            | BindingErrorResponse      _ -> 0x0111us
            | SharedSecretRequest       _ -> 0x0002us
            | SharedSecretResponse      _ -> 0x0102us
            | SharedSecretErrorResponse _ -> 0x0112us
        override this.ToString() =
            match this with
            | BindingRequest            _ -> "BindingRequest"
            | BindingResponse           _ -> "BindingResponse"
            | BindingErrorResponse      _ -> "BindingErrorResponse"
            | SharedSecretRequest       _ -> "SharedSecretRequest"
            | SharedSecretResponse      _ -> "SharedSecretResponse"
            | SharedSecretErrorResponse _ -> "SharedSecretErrorResponse"

type STUNQueryError = 
    /// Unexpected error.
    | Unknown
    /// Server responded with an error, see ErrorCode and error phrase.
    | ServerError     of errorCode: ErrorCode * errorMessage: string
    /// Indicates that the server responded with bad data.
    | BadResponse
    /// Indicates that the server responded with a message that contains a different transaction ID.
    | BadTransactionId
    /// Indicates that we failed to establish a connection with the server.
    | RequestFailure  of string
    /// Indicates that the server didn't respond to a request within a time interval.
    | ResponseFailure of string
            
type STUNQueryResult = 
    | QuerySuccess      of STUNMessage
    | QueryWriteFailure of STUNQueryError
    | QueryReadFailure  of STUNQueryError
    
type STUNState =
    | MappedAddressState of requestTxId: byte [] * localEndpoint: IPEndPoint * publicEndpoint: IPEndPoint option
    | SameAddressState   of requestTxId: byte [] * localEndpoint: IPEndPoint * publicEndpoint: IPEndPoint
    | ChangeAddressState of requestTxId: byte [] * localEndpoint: IPEndPoint * publicEndpoint: IPEndPoint
    | ChangePortState    of requestTxId: byte [] * localEndpoint: IPEndPoint * publicEndpoint: IPEndPoint
    
type STUNStateResult =
    | StateSuccess of
        natType:        NATType    *
        localEndpoint:  IPEndPoint *
        publicEndpoint: IPEndPoint 
    | StateFailure of
        queryError: STUNQueryError