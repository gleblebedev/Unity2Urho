using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityToCustomEngineExporter.Editor
{
    public class AssetNameCollisionResolver
    {
        private Dictionary<string, Dictionary<Key, string>> _vistedAssets = new Dictionary<string, Dictionary<Key, string>>();
        struct Key
        {
            public string guid;

            public long id;
        }
        public string GetUniqueName(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                return asset.name;
            }

            if (!_vistedAssets.TryGetValue(path, out var values))
            {
                var visitedNames = new HashSet<string>();

                values = new Dictionary<Key, string>();
                _vistedAssets.Add(path, values);
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    Key k;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out k.guid, out k.id))
                    {
                        values[k] = o.name;
                        visitedNames.Add(o.name);
                    }
                }

                foreach (var group in values.GroupBy(_=>_.Value).ToList())
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
                            values[pair.Key] = name;
                        }
                    }
                }
            }



            Key key;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out key.guid, out key.id))
            {
                return asset.name;
            }

            if (!values.TryGetValue(key, out var knownName))
            {
                return asset.name;
            }

            return knownName;
        }
    }
}