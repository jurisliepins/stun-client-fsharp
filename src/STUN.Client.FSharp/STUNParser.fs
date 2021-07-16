namespace STUN.Client.FSharp

open System.IO
open System.Net
open System.Net.Sockets
open System.Text

[<RequireQualifiedAccess>]
module STUNParser =
    
    let private tryWith args fn =
        try
            fn args |> Ok
        with
            | exn -> Error exn
    
    let private writeAttribute (writer: STUNBinaryWriter) (attribute: STUNAttribute): int64 =
        writer.Write(attribute.ToUInt16())
        let lengthPosition = writer.BaseStream.Position
        writer.Write(0us)
        let bodyBeginPosition = writer.BaseStream.Position
        match attribute with
        | MappedAddressAttribute   endpoint
        | ResponseAddressAttribute endpoint
        | SourceAddressAttribute   endpoint
        | ChangedAddressAttribute  endpoint ->
            writer.Write(0uy)
            match endpoint.AddressFamily with
            | AddressFamily.InterNetwork   -> writer.Write(1uy)
            | AddressFamily.InterNetworkV6 -> writer.Write(2uy)
            | _ ->
                raise (STUNParsingException(sprintf "Address family '%A' is not supported" endpoint.AddressFamily))
            writer.Write(uint16 endpoint.Port)
            writer.Write(endpoint.Address.GetAddressBytes())
        | UsernameAttribute text
        | PasswordAttribute text -> writer.Write(Encoding.ASCII.GetBytes(text))
        | ChangeRequestAttribute (changeIp, changePort) ->
            writer.Write(Array.create 3 (new byte ()))
            let mutable i = 0
            if changeIp   then i <- i ||| 4
            if changePort then i <- i ||| 2
            writer.Write(byte i)
        | ErrorCodeAttribute _
        | MessageIntegrityAttribute
        | ReflectedFromAttribute ->
            raise (
                STUNParsingException(
                    sprintf
                        "Write for '%A', '%A' and '%A' is not supported"
                        MessageIntegrityAttribute
                        ErrorCodeAttribute
                        ReflectedFromAttribute
                )
            )
        let length          = writer.BaseStream.Position - bodyBeginPosition
        let bodyEndPosition = writer.BaseStream.Position
        writer.BaseStream.Position <- lengthPosition
        writer.Write(uint16 length)
        writer.BaseStream.Position <- bodyEndPosition
        writer.BaseStream.Position
    
    let private readAttribute (reader: STUNBinaryReader): STUNAttribute option =
        let attributeType   = reader.ReadUInt16()
        let attributeLength = reader.ReadUInt16()
        match attributeType with
        | 1us | 2us | 4us | 5us -> 
            reader.BaseStream.Position <- reader.BaseStream.Position + 1L
            let ipFamily = reader.ReadByte()
            let port     = reader.ReadUInt16()
            let address  = 
                match ipFamily with
                | 1uy -> IPAddress(reader.ReadBytes(4))
                | 2uy -> IPAddress(reader.ReadBytes(16))
                | _ -> 
                    raise (STUNParsingException (sprintf "Unsupported IP family '%d'" ipFamily))
            match attributeType with
            | 1us -> MappedAddressAttribute   (IPEndPoint(address, (int port))) |> Some
            | 2us -> ResponseAddressAttribute (IPEndPoint(address, (int port))) |> Some
            | 4us -> SourceAddressAttribute   (IPEndPoint(address, (int port))) |> Some
            | 5us -> ChangedAddressAttribute  (IPEndPoint(address, (int port))) |> Some
            | _ -> 
                raise (STUNParsingException (sprintf "Unknown attribute type '%d'" attributeType))
        | 3us ->
            reader.BaseStream.Position <- reader.BaseStream.Position + 3L
            let packedByte = reader.ReadByte()
            let changeIp   = (packedByte &&& 4uy) <> 0uy
            let changePort = (packedByte &&& 2uy) <> 0uy
            ChangeRequestAttribute(changeIp, changePort) |> Some
        | 6us | 7us ->
            match attributeType with 
            | 6us -> UsernameAttribute(Encoding.ASCII.GetString(reader.ReadBytes(int attributeLength))) |> Some
            | 7us -> PasswordAttribute(Encoding.ASCII.GetString(reader.ReadBytes(int attributeLength))) |> Some
            | _ -> 
                raise (STUNParsingException (sprintf "Unknown attribute type '%d'" attributeType))
        | 8us | 9us | 11us ->
            raise (
                STUNParsingException(
                    sprintf
                        "Read for '%A', '%A' and '%A' is not supported"
                        MessageIntegrityAttribute
                        ErrorCodeAttribute
                        ReflectedFromAttribute
                )
            )
        | _ ->  
            reader.BaseStream.Position <- reader.BaseStream.Position + (int64 attributeLength)
            None
            
    let writeMessage (writer: STUNBinaryWriter) (message: STUNMessage): int64 =
        let txId, attributes =
            match message with
            | BindingRequest            (tx, attrs) -> tx, attrs
            | BindingResponse           (tx, attrs) -> tx, attrs
            | BindingErrorResponse      (tx, attrs) -> tx, attrs
            | SharedSecretRequest       (tx, attrs) -> tx, attrs
            | SharedSecretResponse      (tx, attrs) -> tx, attrs
            | SharedSecretErrorResponse (tx, attrs) -> tx, attrs
        writer.Write(message.ToUInt16())
        writer.Write(0us)
        writer.Write(txId)

        let rec writeAttributes (attributes: STUNAttribute list) (length: int64) =
            match attributes with
            | headAttribute::tailAttributes -> 
                let beginPosition = writer.BaseStream.Position
                let endPosition   = writeAttribute writer headAttribute
                writeAttributes tailAttributes (length + endPosition - beginPosition)
            | _ ->
                length
                    
        let length = writeAttributes attributes 0L
        writer.BaseStream.Position <- 2L
        writer.Write(uint16 length)
        writer.BaseStream.Position

    let readMessage (reader: STUNBinaryReader): STUNMessage =
        let parseMessage () =
            let messageLength = reader.ReadUInt16()
            let txId          = reader.ReadBytes(16)
            
            let rec readAttributes (attributes: STUNAttribute list) =
                if (reader.BaseStream.Position - 20L) < (int64 messageLength) then
                    match readAttribute reader with
                    | Some attribute -> readAttributes (attribute::attributes)
                    | None           -> readAttributes attributes
                else
                    List.rev attributes
                    
            txId, (readAttributes List.empty)

        match (reader.ReadUInt16()) with
        | 0x0001us -> BindingRequest           (parseMessage ())
        | 0x0101us -> BindingResponse          (parseMessage ())
        | 0x0111us -> BindingErrorResponse     (parseMessage ())
        | 0x0002us -> SharedSecretRequest      (parseMessage ())
        | 0x0102us -> SharedSecretResponse     (parseMessage ())
        | 0x0112us -> SharedSecretErrorResponse(parseMessage ())
        | messageType -> 
            raise (STUNParsingException (sprintf "Unknown message type '%d'" messageType))
            
    let writeMessageBytes (message: STUNMessage): byte [] =
        use memoryStream = new MemoryStream()
        use binaryWriter = new STUNBinaryWriter(memoryStream)
        writeMessage binaryWriter message |> ignore
        memoryStream.ToArray()

    let readMessageBytes (message: byte []): STUNMessage =
        use memoryStream = new MemoryStream(message, 0, message.Length)
        use binaryReader = new STUNBinaryReader(memoryStream)
        readMessage binaryReader