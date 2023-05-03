using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public class AssetNameCollisionResolver
    {
        class TimeStampAndAssets
        {
            public DateTime TimeStamp { get; set; }
            public Dictionary<Key, string> Map { get; } = new Dictionary<Key, string>();
        }
        private readonly Dictionary<string, TimeStampAndAssets> _vistedAssets = new Dictionary<string, TimeStampAndAssets>();

        struct Key
        {
            public string guid;

            public long id;

            public Type type;
        }
        public string GetUniqueName(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                return asset.name;
            }

            var timeStamp = File.GetLastWriteTimeUtc(path);

            if (!_vistedAssets.TryGetValue(path, out var values) || values.TimeStamp != timeStamp)
            {
                var visitedNames = new HashSet<string>();

                values = new TimeStampAndAssets{TimeStamp = timeStamp};
                _vistedAssets[path] = values;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path).Where(_ => _ != null))
                {
                    Key k = new Key(){ type = o.GetType() };
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out k.guid, out k.id))
                    {
                        values.Map[k] = o.name;
                        visitedNames.Add(o.name);
                    }
                }

                foreach (var group in values.Map.GroupBy(_=>Tuple.Create(_.Key.type, _.Value) ).ToList())
                {
                    if (group.Skip(1).Any())
                    {
                        foreach (var pair in group.OrderBy(_ => _.Key.id).Skip(1))
                        {
                            int index = 1;
                            string name = pair.Value;
                            while (visitedNames.Contains(name))
                            {
                                ++index;
                                name = $"{pair.Value}({index})";
                            }

                            visitedNames.Add(name);
                            values.Map[pair.Key] = name;
                        }
                    }
                }
            }

            Key key = new Key() { type = asset.GetType() };
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out key.guid, out key.id))
            {
                return asset.name;
            }

            if (!values.Map.TryGetValue(key, out var knownName))
            {
                return asset.name;
            }

            return knownName;
        }
    }
}