#nullable enable
using VYaml.Emitter;
using VYaml.Parser;

namespace VYaml.Serialization
{
    public class BooleanFormatter : IYamlFormatter<bool>
    {
        public static readonly BooleanFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, in bool value, YamlSerializationContext context)
        {
            emitter.WriteBool(value);
        }

        public bool Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            var result = parser.GetScalarAsBool();
            parser.Read();
            return result;
        }
    }

    public class NullableBooleanFormatter : IYamlFormatter<bool?>
    {
        public static readonly NullableBooleanFormatter Instance = new();

        public void Serialize(ref Utf8YamlEmitter emitter, in bool? value, YamlSerializationContext context)
        {
            if (value.HasValue)
            {
                emitter.WriteBool(value.GetValueOrDefault());
            }
            else
            {
                emitter.WriteNull();
            }
        }

        public bool? Deserialize(ref YamlParser parser, YamlDeserializationContext context)
        {
            if (parser.IsNullScalar())
            {
                parser.Read();
                return default;
            }

            var result = parser.GetScalarAsBool();
            parser.Read();
            return result;
        }
    }
}
