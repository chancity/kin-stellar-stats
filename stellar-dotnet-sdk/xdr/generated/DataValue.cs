// Automatically generated by xdrgen
// DO NOT EDIT or your changes may be overwritten

namespace stellar_dotnet_sdk.xdr
{
// === xdr source ============================================================

//  typedef opaque DataValue<64>;

//  ===========================================================================
    public class DataValue
    {
        public DataValue()
        {
        }

        public DataValue(byte[] value)
        {
            InnerValue = value;
        }

        public byte[] InnerValue { get; set; } = default(byte[]);

        public static void Encode(XdrDataOutputStream stream, DataValue encodedDataValue)
        {
            var DataValuesize = encodedDataValue.InnerValue.Length;
            stream.WriteInt(DataValuesize);
            stream.Write(encodedDataValue.InnerValue, 0, DataValuesize);
        }

        public static DataValue Decode(XdrDataInputStream stream)
        {
            var decodedDataValue = new DataValue();
            var DataValuesize = stream.ReadInt();
            decodedDataValue.InnerValue = new byte[DataValuesize];
            stream.Read(decodedDataValue.InnerValue, 0, DataValuesize);
            return decodedDataValue;
        }
    }
}