#nullable enable
using VYaml.Emitter;
using VYaml.Parser;

namespace VYaml.Serialization
{
    public class CharFormatter : IYamlFormatter<char>
    {
        public static readonly CharFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, in char value, YamlSerializationContext context)
        {
            emitter.WriteInt32(value);
        }

        public char Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            var result = parser.GetScalarAsUInt32();
            parser.Read();
            return checked((char)result);
        }
    }

    public class NullableCharFormatter : IYamlFormatter<char?>
    {
        public static readonly NullableCharFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, in char? value, YamlSerializationContext context)
        {
            if (value.HasValue)
            {
                emitter.WriteInt32(value.GetValueOrDefault());
            }
            else
            {
                emitter.WriteNull();
            }
        }

        public char? Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            if (parser.IsNullScalar())
            {
                parser.Read();
                return default;
            }

            var result = parser.GetScalarAsUInt32();
            parser.Read();
            return checked((char)result);
        }
    }
}
