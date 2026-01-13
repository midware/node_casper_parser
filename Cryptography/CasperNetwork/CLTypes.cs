using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using Casper.Network.SDK.Types;
using NodeCasperParser.Cryptography.CasperNetwork;
using Org.BouncyCastle.Crypto.Macs;


namespace NodeCasperParser.Cryptography.CasperNetwork
{
    /// <summary>
    /// This util includes helper methods in binary format for Casper Network CLTypes
    /// </summary>
    public class CLTypes
    {
        public (CLType, byte[]) MatchBytesToCLType(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Bytes array is empty or null.");

            var tag = bytes[0];
            var remainder = bytes.AsSpan().Slice(1).ToArray();

            switch (tag)
            {
                case (byte)CLType.Bool:
                    return (CLType.Bool, remainder);
                case (byte)CLType.I32:
                    return (CLType.I32, remainder);
                case (byte)CLType.I64:
                    return (CLType.I64, remainder);
                case (byte)CLType.U8:
                    return (CLType.U8, remainder);
                case (byte)CLType.U32:
                    return (CLType.U32, remainder);
                case (byte)CLType.U64:
                    return (CLType.U64, remainder);
                case (byte)CLType.U128:
                    return (CLType.U128, remainder);
                case (byte)CLType.U256:
                    return (CLType.U256, remainder);
                case (byte)CLType.U512:
                    return (CLType.U512, remainder);
                case (byte)CLType.Unit:
                    return (CLType.Unit, remainder);
                case (byte)CLType.String:
                    return (CLType.String, remainder);
                case (byte)CLType.Key:
                    return (CLType.Key, remainder);
                case (byte)CLType.URef:
                    return (CLType.URef, remainder);
                case (byte)CLType.Option:
                    var (innerTypeOption, optionRemainder) = MatchBytesToCLType(remainder);
                    return (CLType.Option, optionRemainder);
                case (byte)CLType.List:
                    var (innerTypeList, listRemainder) = MatchBytesToCLType(remainder);
                    return (CLType.List, listRemainder);
                case (byte)CLType.ByteArray:
                    var size = BitConverter.ToInt32(remainder, 0);
                    remainder = remainder.AsSpan().Slice(4).ToArray();
                    return (CLType.ByteArray, remainder);
                case (byte)CLType.Result:
                    var (okType, okTypeRemainder) = MatchBytesToCLType(remainder);
                    var (errType, errTypeRemainder) = MatchBytesToCLType(okTypeRemainder);
                    return (CLType.Result, errTypeRemainder);
                case (byte)CLType.Map:
                    var (keyType, remainderKey) = MatchBytesToCLType(remainder);
                    var (valueType, remainderValue) = MatchBytesToCLType(remainderKey);
                    return (CLType.Map, remainderValue);
                case (byte)CLType.Tuple1:
                    var (innerType1, remainder1) = MatchBytesToCLType(remainder);
                    return (CLType.Tuple1, remainder1);
                case (byte)CLType.Tuple2:
                    var (innerType2_1, remainder2_1) = MatchBytesToCLType(remainder);
                    var (innerType2_2, remainder2_2) = MatchBytesToCLType(remainder2_1);
                    return (CLType.Tuple2, remainder2_2);
                case (byte)CLType.Tuple3:
                    var (innerType3_1, remainder3_1) = MatchBytesToCLType(remainder);
                    var (innerType3_2, remainder3_2) = MatchBytesToCLType(remainder3_1);
                    var (innerType3_3, remainder3_3) = MatchBytesToCLType(remainder3_2);
                    return (CLType.Tuple3, remainder3_3);
                case (byte)CLType.Any:
                    return (CLType.Any, remainder);
                case (byte)CLType.PublicKey:
                    return (CLType.PublicKey, remainder);
                default:
                    throw new Exception("Unsupported CLType tag.");
            }
        }
    }
}
