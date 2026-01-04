using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DeckMiner.Config
{
    public class CardLevelsYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Dictionary<int, List<int>>);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is Scalar scalar && (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "null"))
            {
                parser.Consume<Scalar>();
                return new Dictionary<int, List<int>>();
            }

            var dict = new Dictionary<int, List<int>>();

            parser.Consume<MappingStart>();
            while (parser.Current is not MappingEnd)
            {
                var keyScalar = parser.Consume<Scalar>();
                int key = int.Parse(keyScalar.Value);

                var list = new List<int>();
                parser.Consume<SequenceStart>();
                while (parser.Current is not SequenceEnd)
                {
                    var valScalar = parser.Consume<Scalar>();
                    list.Add(int.Parse(valScalar.Value));
                }
                parser.Consume<SequenceEnd>();

                dict[key] = list;
            }
            parser.Consume<MappingEnd>();

            return dict;
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            var dict = (Dictionary<int, List<int>>)value!;

            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            foreach (var kvp in dict)
            {
                emitter.Emit(new Scalar(kvp.Key.ToString()));

                // Force Flow style for the list of levels: [120, 1, 14]
                emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
                foreach (var val in kvp.Value)
                {
                    emitter.Emit(new Scalar(val.ToString()));
                }
                emitter.Emit(new SequenceEnd());
            }

            emitter.Emit(new MappingEnd());
        }
    }
}
