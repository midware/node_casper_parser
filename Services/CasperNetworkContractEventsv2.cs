using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Security.AccessControl;

using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;
using NodeCasperParser.Cryptography.CasperNetwork;
using static NodeCasperParser.Controllers.CasperNetworkContractsEvents;
using Swashbuckle.Swagger;
using System.ComponentModel;

namespace NodeCasperParser.Controllers
{
    public class Event
    {
        public string Name { get; set; }
        public Dictionary<string, CLValue> Fields { get; set; }

        private const string EVENT_PREFIX = "event_"; // Example prefix

        public Event(string name)
        {
            Name = name;
            Fields = new Dictionary<string, CLValue>();
        }

        public byte[] ToCesBytes()
        {
            List<byte> result = new List<byte>();

            // Prefix the name and convert it to bytes
            string prefixedName = EVENT_PREFIX + Name;
            byte[] eventNameBytes = Encoding.UTF8.GetBytes(prefixedName);
            result.AddRange(eventNameBytes);

            // Iterate over the fields and add their serialized byte values
            foreach (var field in Fields)
            {
                CLValue fieldValue = field.Value;
                byte[] fieldBytes = fieldValue.ToByteArray();//.ToBytes();  // Assuming CLValue has a ToBytes method
                result.AddRange(fieldBytes);
            }

            return result.ToArray();
        }

        public static (string eventName, byte[] eventData) ParseRawEventNameAndData(byte[] bytes)
        {
            if (bytes.Length < 4)
                throw new InvalidOperationException("The byte array is too short to contain the length information.");

            // Read the total length of the event data (first 4 bytes)
            int totalLength = BitConverter.ToInt32(bytes, 0);
            if (totalLength > bytes.Length - 4)
                throw new InvalidOperationException("Specified data length is greater than the available length.");

            // Extract the rest of the bytes which should contain the event name followed by the event data
            byte[] eventDataWithName = new byte[totalLength];
            Array.Copy(bytes, 4, eventDataWithName, 0, totalLength);

            // Find the null terminator for the string (assuming ASCII encoding)
            int nullIndex = Array.IndexOf(eventDataWithName, (byte)0);
            if (nullIndex == -1)
                throw new InvalidOperationException("Event name is not properly terminated.");

            // Convert bytes to string assuming ASCII encoding
            string eventName = Encoding.UTF8.GetString(eventDataWithName, 0, nullIndex);
            if (!eventName.StartsWith(EVENT_PREFIX))
                throw new InvalidOperationException("Event name does not start with the required prefix.");

            // Remove the prefix from the event name
            eventName = eventName.Substring(EVENT_PREFIX.Length);

            // Extract the event data (bytes following the null terminator)
            int eventDataLength = totalLength - nullIndex - 1; // Subtract the null terminator
            byte[] eventData = new byte[eventDataLength];
            if (eventDataLength > 0)
            {
                Array.Copy(eventDataWithName, nullIndex + 1, eventData, 0, eventDataLength);
            }

            return (eventName, eventData);
        }

        private static Dictionary<string, CLValue> ParseDynamicEventDataV2(Schema schema, byte[] eventData)
        {
            // Example parsing logic, adjust according to how your Schema and eventData are structured
            var fields = new Dictionary<string, CLValue>();
           
            foreach (var field in schema.Fields)
            {
                CLType clType = field.Type.Type;
                var (value, newRemainder) = ParseDynamicCLValue2(clType, eventData);
                fields.Add(field.Name, value);
            }

            return fields;
        }

        public static List<(string, CLValue)> ParseDynamicEventData(Schema dynamicEventSchema, byte[] eventData)
        {
            var eventFields = new List<(string, CLValue)>();
            var remainder = eventData;

            //ClValueParser clvp = new ClValueParser();

            foreach (var (fieldName, fieldType) in dynamicEventSchema.ToList())
            {
                var clType = fieldType.Type;//.Downcast();
                var (fieldValue, newRemainder) = ParseDynamicCLValue2(clType, remainder);
                remainder = newRemainder;
                eventFields.Add((fieldName, fieldValue));
            }

            return eventFields;
        }

        private static (CLValue, byte[]) ParseSimpleType<T>(byte[] bytes, CLType clType)
        {
            // CLTypes clt = new CLTypes();
            // var (value, remainder) = clt.MatchBytesToCLType(bytes);
            ClValueParser clvp = new ClValueParser();
            var (value, remainder) = clvp.parse_dynamic_clvalue(bytes);

            var clValue = new CLValue(remainder, value);
            return (clValue, remainder);
        }

        private static (CLValue, byte[]) ParseComplexType<T>(byte[] bytes, CLType clType) where T : ICLType, new()
        {
            //  CLTypes clt = new CLTypes();
            //  var (value, remainder) = clt.MatchBytesToCLType(bytes);//FromBytes<T>(bytes);

            ClValueParser clvp = new ClValueParser();
            var (value, remainder) = clvp.parse_dynamic_clvalue(bytes);
            var clValue = new CLValue(remainder, value);
            return (clValue, remainder);
        }

        private static (CLValue, byte[]) ParseDynamicCLValue2(CLType clType, byte[] bytes)
        {
            // Assuming we have a method to parse CLValue from bytes based on CLType
            // This is a simplified placeholder logic
            //     CLTypes clt = new CLTypes();
            //     var value = clt.MatchBytesToCLType(bytes);//ConvertFromBytes(clType, bytes);

            ClValueParser clvp = new ClValueParser();
            var value = clvp.parse_dynamic_clvalue(bytes);

            var remainingBytes = bytes.Skip(Convert.ToInt32(value.Item2)).ToArray();
            return (new CLValue(value.Item2, clType), remainingBytes);
        }

        public class U128 : ICLType { }
        public class U256 : ICLType { }
        public class U512 : ICLType { }
        public class Unit : ICLType { }
        public class Key : ICLType { }
        public class URef : ICLType { }
        public class PublicKey : ICLType { }

        public interface ICLType { }

        public static (CLValue clValue, byte[] remainder) ParseDynamicCLValue(CLType clType, byte[] bytes)
        {
            switch (clType)
            {
                case CLType.Bool:
                    return ParseSimpleType<bool>(bytes, CLType.Bool);
                case CLType.I32:
                    return ParseSimpleType<int>(bytes, CLType.I32);
                case CLType.I64:
                    return ParseSimpleType<long>(bytes, CLType.I64);
                case CLType.U8:
                    return ParseSimpleType<byte>(bytes, CLType.U8);
                case CLType.U32:
                    return ParseSimpleType<uint>(bytes, CLType.U32);
                case CLType.U64:
                    return ParseSimpleType<ulong>(bytes, CLType.U64);
                case CLType.U128:
                    return ParseComplexType<U128>(bytes, CLType.U128);
                case CLType.U256:
                    return ParseComplexType<U256>(bytes, CLType.U256);
                case CLType.U512:
                    return ParseComplexType<U512>(bytes, CLType.U512);
                case CLType.Unit:
                    return ParseSimpleType<Unit>(bytes, CLType.Unit);
                case CLType.String:
                    return ParseSimpleType<string>(bytes, CLType.String);
                case CLType.Key:
                    return ParseComplexType<Key>(bytes, CLType.Key);
                case CLType.URef:
                    return ParseComplexType<URef>(bytes, CLType.URef);
                case CLType.PublicKey:
                    return ParseComplexType<PublicKey>(bytes, CLType.PublicKey);
                default:
                    throw new NotSupportedException($"Unsupported CLType: {clType}");
            }
        }        
    }

    public class CesMetadataRef
    {
        public URef EventsSchema { get; set; }
        public URef EventsLength { get; set; }
        public URef EventsData { get; set; }

        public CesMetadataRef(URef eventsSchema, URef eventsLength, URef eventsData)
        {
            EventsSchema = eventsSchema;
            EventsLength = eventsLength;
            EventsData = eventsData;
        }
    }

    public static class CesMetadataFetcher
    {
        private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");

        private const string EventsSchemaKey = "__events_schema";
        private const string EventsLengthKey = "__events_length";
        private const string EventsDataKey = "__events";

        public static async Task<CesMetadataRef> FetchMetadata(NetCasperClient client, string contractHash)
        {
            var key = $"hash-{contractHash}";

            // Ensure the proper method to query global state is used and handle potential errors or null values.
            var globalStateResult = await client.QueryGlobalState(key);
           /* if (globalStateResult.Parse()?.StoredValue?.Contract == null)
            {
                throw new InvalidOperationException("No contract found at the specified hash or invalid state queried.");
            }*/
            if (globalStateResult == null || globalStateResult.Parse().StoredValue == null)
            {
                throw new InvalidOperationException("Expected CLValue in stored value but found none.");
            }

            var namedKeys = globalStateResult.Parse().StoredValue.Contract.NamedKeys;

            // Extract URefs from named keys
            var eventsSchemaURef = ExtractURefFromNamedKeys(namedKeys, EventsSchemaKey);
            var eventsLengthURef = ExtractURefFromNamedKeys(namedKeys, EventsLengthKey);
            var eventsDataURef = ExtractURefFromNamedKeys(namedKeys, EventsDataKey);

            return new CesMetadataRef(eventsSchemaURef, eventsLengthURef, eventsDataURef);
        }

        private static URef ExtractURefFromNamedKeys(List<NamedKey> namedKeys, string keyName)
        {
            foreach (var key in namedKeys)
            {
                if (key.Name == keyName)
                {
                    if (key.Key is Casper.Network.SDK.Types.URef uref)
                        return uref;

                    throw new InvalidOperationException($"Named key '{keyName}' found but is not a URef as expected.");
                }
            }

            throw new KeyNotFoundException($"URef not found for key: {keyName}");
        }
    }

    public class ClValueParser
    {
        public (CLType, byte[]) /*MatchBytesToCLType*/parse_dynamic_clvalue(byte[] bytes)
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
                    var (innerTypeOption, optionRemainder) = parse_dynamic_clvalue(remainder);
                    return (CLType.Option, optionRemainder);
                case (byte)CLType.List:
                    var (innerTypeList, listRemainder) = parse_dynamic_clvalue(remainder);
                    return (CLType.List, listRemainder);
                case (byte)CLType.ByteArray:
                    var size = BitConverter.ToInt32(remainder, 0);
                    remainder = remainder.AsSpan().Slice(4).ToArray();
                    return (CLType.ByteArray, remainder);
                case (byte)CLType.Result:
                    var (okType, okTypeRemainder) = parse_dynamic_clvalue(remainder);
                    var (errType, errTypeRemainder) = parse_dynamic_clvalue(okTypeRemainder);
                    return (CLType.Result, errTypeRemainder);
                case (byte)CLType.Map:
                    var (keyType, remainderKey) = parse_dynamic_clvalue(remainder);
                    var (valueType, remainderValue) = parse_dynamic_clvalue(remainderKey);
                    return (CLType.Map, remainderValue);
                case (byte)CLType.Tuple1:
                    var (innerType1, remainder1) = parse_dynamic_clvalue(remainder);
                    return (CLType.Tuple1, remainder1);
                case (byte)CLType.Tuple2:
                    var (innerType2_1, remainder2_1) = parse_dynamic_clvalue(remainder);
                    var (innerType2_2, remainder2_2) = parse_dynamic_clvalue(remainder2_1);
                    return (CLType.Tuple2, remainder2_2);
                case (byte)CLType.Tuple3:
                    var (innerType3_1, remainder3_1) = parse_dynamic_clvalue(remainder);
                    var (innerType3_2, remainder3_2) = parse_dynamic_clvalue(remainder3_1);
                    var (innerType3_3, remainder3_3) = parse_dynamic_clvalue(remainder3_2);
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

    public class Schema
    {
        public List<(string Name, CLTypeInfo Type)> Fields { get; set; } = new List<(string Name, CLTypeInfo Type)>();

        public List<(string, CLTypeInfo)> ToList()
        {
            return Fields;
        }
    }
        

    public class CasperNetworkContractsEventsv2
    {        
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer"); 
        private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");
        
        bool debugMode = false;
        

    }
}
