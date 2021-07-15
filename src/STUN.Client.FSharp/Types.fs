namespace STUN.Client.FSharp

type ErrorCode =
    | None                  = 0
    | BadRequest            = 400
    | Unauthorized          = 401
    | UnknownAttribute      = 420
    | StaleCredentials      = 430
    | IntegrityCheckFailure = 431
    | MissingUsername       = 432
    | UseTLS                = 433
    | ServerError           = 500
    | GlobalFailure         = 600
    
type NATType =
    /// Unspecified NAT Type.
    | Unspecified
    /// OpenInternet, eg.: Virtual Private Servers.
    | OpenInternet
    /// FullCone NAT.
    | FullCone
    /// RestrictedCone NAT, means that the client can receive data only at the IP addresses that it sent data on before.
    | RestrictedCone
    /// PortRestrictedCone NAT, same as RestrictedCone but port is included too.
    | PortRestrictedCone
    /// Symmetric NAT, means the client picks a different port for every connection it makes.
    | Symmetric
    /// OpenInternet but only received data from addresses that it sent data on before.
    | SymmetricUDPFirewall