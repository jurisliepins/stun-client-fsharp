namespace STUN.Client.FSharp

open System.IO
open System

type STUNBinaryReader(stream: Stream) =
    inherit BinaryReader(stream)

    member private this.ReadNetworkBytes(count: int) = 
        let bytes = base.ReadBytes(count)
        if BitConverter.IsLittleEndian then
            Array.Reverse(bytes)
        bytes

    override this.ReadInt16() = 
        BitConverter.ToInt16(this.ReadNetworkBytes(sizeof<int16>), 0);

    override this.ReadUInt16() = 
        BitConverter.ToUInt16(this.ReadNetworkBytes(sizeof<uint16>), 0);

    override this.ReadInt32() = 
        BitConverter.ToInt32(this.ReadNetworkBytes(sizeof<int32>), 0);

    override this.ReadUInt32() = 
        BitConverter.ToUInt32(this.ReadNetworkBytes(sizeof<uint32>), 0);

    override this.ReadInt64() = 
        BitConverter.ToInt64(this.ReadNetworkBytes(sizeof<int64>), 0);

    override this.ReadUInt64() = 
        BitConverter.ToUInt64(this.ReadNetworkBytes(sizeof<uint64>), 0);

type STUNBinaryWriter(stream: Stream) =
    inherit BinaryWriter(stream)

    member private this.WriteNetworkBytes(buffer: byte []) =
        if BitConverter.IsLittleEndian then
            Array.Reverse(buffer);
        base.Write(buffer);

    override this.Write(value: int16) =
        this.WriteNetworkBytes(BitConverter.GetBytes(value));

    override this.Write(value: uint16) =
        this.WriteNetworkBytes(BitConverter.GetBytes(value));

    override this.Write(value: int32) =
        this.WriteNetworkBytes(BitConverter.GetBytes(value));

    override this.Write(value: uint32) =
        this.WriteNetworkBytes(BitConverter.GetBytes(value));

    override this.Write(value: int64) =
        this.WriteNetworkBytes(BitConverter.GetBytes(value));
        
    override this.Write(value: uint64) =
        this.WriteNetworkBytes(BitConverter.GetBytes(value));
