using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using NodeCasperParser.Cryptography.CasperNetwork;
using System.Security.AccessControl;

namespace NodeCasperParser.Controllers
{ 

    public class CasperNetworkContractsEvents
    {        
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer"); 
        private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");
        
        bool debugMode = false;

        private static readonly string EVENTS_SCHEMA_NAMED_KEY = "__events_schema";
        private static readonly string EVENTS_NAMED_KEY = "__events";
       
        public class ContractMetadata
        {
            public Dictionary<string, List<PropertyDefinition>> Schemas { get; set; }
            public byte[] ContractHash { get; set; }
            public byte[] ContractPackageHash { get; set; }
            public string EventsSchemaUref { get; set; }
            public string EventsUref { get; set; }
        }

        public class PropertyDefinition
        {
            public string ParamName { get; set; }
            public CLType ParamType { get; set; }

            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }
        }

        /*
        public Dictionary<string, List<PropertyDefinition>> ParseSchemasFromBytes(byte[] rawSchemas)
        {
            if (rawSchemas.Length < 4)
                throw new Exception("Invalid raw schema length.");

            int schemasNumber = BitConverter.ToInt32(rawSchemas, 0);
            if (schemasNumber <= 0 || schemasNumber > rawSchemas.Length)
                throw new Exception("Invalid schemasNumber value");

            int offset = 4;
            var schemas = new Dictionary<string, List<PropertyDefinition>>();

            for (int i = 0; i < schemasNumber; i++)
            {
                if (offset >= rawSchemas.Length)
                    throw new Exception("Offset exceeds schema data length.");

                (string schemaName, int newOffset) = ParseStringFromBytes(rawSchemas, offset);
                if (newOffset >= rawSchemas.Length || newOffset <= offset)
                    throw new Exception("Invalid string parsing offset.");

                offset = newOffset;

                if (offset >= rawSchemas.Length)
                    throw new Exception("Offset exceeds schema data length before parsing schema.");
               
                (List<PropertyDefinition> schema, int finalOffset) = ParseSchemaFromBytesWithRemainder(rawSchemas, offset);
                if (finalOffset > rawSchemas.Length || finalOffset <= offset)
                    throw new Exception("Invalid schema parsing offset.");
                
                offset = finalOffset;

                schemas[schemaName] = schema;
            }

            return schemas;
        }*/

        public class SchemaField
        {
            public string PropertyName { get; set; }
            public CLType PropertyType { get; set; }
        }

        public class ParsedSchemaWithRemainder
        {
            public List<SchemaField> Schema { get; set; }
            public byte[] Remainder { get; set; }
        }

        public static (byte[] data, byte[] remainder) ParseBytesWithRemainder(byte[] rawBytes)
        {
            if (rawBytes.Length < 4)
                throw new ArgumentException("Invalid input; not enough bytes to read length.");

            int length = BitConverter.ToInt32(rawBytes, 0);

            if (length == 0 || length > rawBytes.Length - 4)
                throw new ArgumentException("Invalid length value.");

            byte[] data = new byte[length];
            byte[] remainder = new byte[rawBytes.Length - 4 - length];

            // Copy data starting from index 4 to the length determined
            Array.Copy(rawBytes, 4, data, 0, length);

            // Copy the remainder if there is any
            if (remainder.Length > 0)
            {
                Array.Copy(rawBytes, 4 + length, remainder, 0, remainder.Length);
            }

            return (data, remainder);
        }

        public ParsedSchemaWithRemainder ParseSchemaFromBytesWithRemainder(byte[] rawBytes)
        {
            if (rawBytes.Length < 4)
                throw new ArgumentException("Insufficient bytes for parsing. Byte array is too short to contain a 32-bit integer.");

            // Read a 32-bit unsigned integer from the byte array starting at index 0
            int fieldsNumber = BitConverter.ToInt32(rawBytes, 0);

            // Check if the fieldsNumber value is valid
            if (fieldsNumber  > rawBytes.Length) // Simplified validation
                throw new ArgumentException("Invalid fieldsNumber value or insufficient bytes for all fields.");

            // Create the remainder of the array starting from index 4
            byte[] remainder = new byte[rawBytes.Length - 4];
            Array.Copy(rawBytes, 4, remainder, 0, remainder.Length-4);

            var schema = new List<SchemaField>();

            for (int i = 0; i < fieldsNumber; i++)
            {
                (byte[] data, byte[] newRemainder) = ParseBytesWithRemainder(remainder);
                string fieldName = Encoding.UTF8.GetString(data);
              //  (string fieldName, byte[] newRemainder) = ParseStringFromBytes(remainder);
                remainder = newRemainder;

                NodeCasperParser.Cryptography.CasperNetwork.CLTypes casperTypes = new CLTypes();
                (CLType clType, byte[] clTypeRemainder) = casperTypes.MatchBytesToCLType(remainder);
                if (clTypeRemainder.Length == 0)
                    throw new ArgumentException("Remainder is empty after parsing CLType.");

                schema.Add(new SchemaField { PropertyName = fieldName, PropertyType = clType });
                remainder = clTypeRemainder;
            }

            return new ParsedSchemaWithRemainder
            {
                Schema = schema,
                Remainder = remainder
            };
        }

        private (string, byte[]) ParseStringFromBytes(byte[] data)
        {
            if (data.Length < 4)
                throw new ArgumentException("Insufficient bytes to read length.");

            int length = BitConverter.ToInt32(data, 0);
            if (length > data.Length - 4)
                throw new ArgumentException("String length exceeds data length.");

            string result = Encoding.UTF8.GetString(data, 4, length);
            byte[] remainder = new byte[data.Length - 4 - length];
            Array.Copy(data, 4 + length, remainder, 0, remainder.Length);

            return (result, remainder);
        }
        
        private (string, int) ParseStringFromBytes(byte[] data, int startOffset)
        {
            if (startOffset + 4 > data.Length)
                throw new Exception("Not enough data to read length.");

            int length = BitConverter.ToInt32(data, startOffset);
            if (startOffset + 4 + length > data.Length)
                throw new Exception("Not enough data to read the full string.");

            string result = Encoding.UTF8.GetString(data, startOffset + 4, length);
            return (result, startOffset + 4 + length);
        }
               
        private (List<PropertyDefinition>, int) ParseSchemaFromBytesWithRemainder(byte[] data, int startOffset)
        {
            var schema = new List<PropertyDefinition>();
            int count = BitConverter.ToInt32(data, startOffset);
            int offset = startOffset + 4;

            for (int i = 0; i < count; i++)
            {
                if (offset + 4 > data.Length)
                    throw new Exception("Not enough data to read property length.");

                string propertyName = Encoding.UTF8.GetString(data, offset, 4); // Assuming fixed length for demo
                offset += 4;

                string propertyValue = Encoding.UTF8.GetString(data, offset, 4); // Assuming fixed length for demo
                offset += 4;

                schema.Add(new PropertyDefinition { PropertyName = propertyName, PropertyValue = propertyValue });
            }

            return (schema, offset);
        }

        /*
        public async Task<byte[]> FetchContractSchemasBytes(NetCasperClient rpcClient, string contractHash, string stateRootHash)
        {
            // Fetch the contract data
            var key = $"hash-{contractHash}";
            var blockState = await rpcClient.QueryGlobalStateWithBlockHash(key, stateRootHash);

            if (blockState.Parse().StoredValue.Contract == null)
            {
                throw new InvalidOperationException("Contract data not found.");
            }

            // Find the EVENTS_SCHEMA_NAMED_KEY in the NamedKeys
            var eventsSchema = blockState.Parse().StoredValue.Contract.NamedKeys.Find(key => key.Name == EVENTS_SCHEMA_NAMED_KEY);
            if (eventsSchema == null)
            {
                throw new InvalidOperationException($"'{EVENTS_SCHEMA_NAMED_KEY}' uref not found for contract '{contractHash}'.");
            }

            // Query the global state to get the item by URef
            var storedValue = await rpcClient.QueryGlobalState(eventsSchema.Key, stateRootHash);
            if (storedValue?.Parse().StoredValue.CLValue == null)
            {
                throw new InvalidOperationException("Failed to retrieve schema bytes from global state.");
            }

            return storedValue.Parse().StoredValue.CLValue.Bytes;
        }
        */

        public async Task<byte[]> FetchContractSchemasBytes(Casper.Network.SDK.NetCasperClient _rpcClient, string contractHash, string blockHash)
        {
            try
            {
                var key = $"hash-{contractHash}";

                var globalState = await _rpcClient.QueryGlobalStateWithBlockHash(key, blockHash, EVENTS_SCHEMA_NAMED_KEY);
                if (globalState == null || globalState.Parse().StoredValue == null)
                {
                    throw new InvalidOperationException("Expected CLValue in stored value but found none.");
                }

                var clValue = globalState.Parse().StoredValue.CLValue as CLValue;
                if (clValue == null)
                {
                    throw new InvalidOperationException("Expected CLValue in stored value but got a different type.");
                }

                return clValue.Bytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching contract schemas bytes: {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, ContractMetadata>> GetContractsMetadata(string[] contractHashes, string blockHash)
        {
            Casper.Network.SDK.NetCasperClient _rpcClient = new NetCasperClient(rpcServer);

            var contractsSchemas = new Dictionary<string, ContractMetadata>();
            //var stateRootHash = await _rpcClient.GetStateRootHash();

            foreach (var contractHash in contractHashes)
            {
                var contractPointer = $"hash-{contractHash}";
                var contractData = await _rpcClient.QueryGlobalStateWithBlockHash(contractPointer, blockHash/*stateRootHash*/);
                var contract = contractData.Parse().StoredValue.Contract;

                if (contract == null)
                    throw new Exception("contract data not found");

                string eventsSchemaUref = string.Empty;
                string eventsUref = string.Empty;

                foreach (var namedKey in contract.NamedKeys)
                {
                    if (namedKey.Name == EVENTS_SCHEMA_NAMED_KEY)
                        eventsSchemaUref = namedKey.Key.ToString();
                    else if (namedKey.Name == EVENTS_NAMED_KEY)
                        eventsUref = namedKey.Key.ToString();

                    if (!string.IsNullOrEmpty(eventsSchemaUref) && !string.IsNullOrEmpty(eventsUref))
                        break;
                }

                if (string.IsNullOrEmpty(eventsSchemaUref))
                    throw new Exception($"No '{EVENTS_SCHEMA_NAMED_KEY}' uref found");

                if (string.IsNullOrEmpty(eventsUref))
                    throw new Exception($"No '{EVENTS_NAMED_KEY}' uref found");

               // var schemaResponse = await _rpcClient.QueryGlobalState(eventsSchemaUref, stateRootHash);//.GetStoredValue(stateRootHash, eventsSchemaUref);

                var fetchBytes = await FetchContractSchemasBytes(_rpcClient, contractHash, blockHash);
                var schemas = ParseSchemaFromBytesWithRemainder(fetchBytes, 0);//ParseSchemasFromBytes(fetchBytes);

                var contractPackageHash = contract.ContractPackageHash.Replace("contract-package-wasm", "").Replace("contract-package-","");

                contractsSchemas.Add(eventsUref, new ContractMetadata
                {
                    //Schemas = schemas,
                    ContractHash = ByteUtil.HexToByteArray(contractHash),
                    ContractPackageHash = ByteUtil.HexToByteArray(contractPackageHash),
                    EventsSchemaUref = eventsSchemaUref,
                    EventsUref = eventsUref
                });
            }

            return contractsSchemas;
        }      
    }
}
