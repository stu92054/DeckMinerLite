using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DeckMiner.Config
{
    public class FlowIntListYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(List<int>);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            // Handle null or empty value (e.g. "key: " or "key: null")
            if (parser.Current is Scalar scalar && (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "null"))
            {
                parser.Consume<Scalar>();
                return new List<int>();
            }

            var list = new List<int>();
            parser.Consume<SequenceStart>();
            while (parser.Current is not SequenceEnd)
            {
                var valScalar = parser.Consume<Scalar>();
                if (!int.TryParse(valScalar.Value, out int val))
                {
                    throw new YamlException(valScalar.Start, valScalar.End,
                        $"無效的整數值：'{valScalar.Value}' 不是有效的整數");
                }
                list.Add(val);
            }
            parser.Consume<SequenceEnd>();
            return list;
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            var list = (List<int>)value!;
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
            foreach (var item in list)
            {
                emitter.Emit(new Scalar(item.ToString()));
            }
            emitter.Emit(new SequenceEnd());
        }
    }
}
