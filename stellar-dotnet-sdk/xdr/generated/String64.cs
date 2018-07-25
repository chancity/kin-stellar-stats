// Automatically generated by xdrgen
// DO NOT EDIT or your changes may be overwritten

namespace stellar_dotnet_sdk.xdr
{
// === xdr source ============================================================

//  typedef string string64<64>;

//  ===========================================================================
    public class String64
    {
        public String64()
        {
        }

        public String64(string value)
        {
            InnerValue = value;
        }

        public string InnerValue { get; set; } = default(string);

        public static void Encode(XdrDataOutputStream stream, String64 encodedString64)
        {
            stream.WriteString(encodedString64.InnerValue);
        }

        public static String64 Decode(XdrDataInputStream stream)
        {
            var decodedString64 = new String64();
            decodedString64.InnerValue = stream.ReadString();
            return decodedString64;
        }
    }
}