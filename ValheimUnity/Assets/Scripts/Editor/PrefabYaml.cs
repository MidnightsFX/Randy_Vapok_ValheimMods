using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

// Unity nulls a serialized reference whose C# field type no longer accepts the referenced component
// (a Text field retyped to TMP_Text), so those links are read from the prefab text before it loads.
public static class PrefabYaml
{
    public const string TextScriptGuid = "5f7201a12d95ffc409449d95f23cf332";
    public const string InputFieldScriptGuid = "d199490a83bb2b844b9695cbf13b01ef";
    public const string DropdownScriptGuid = "0d0b652f32a2cc243917e4028fa0f046";

    public struct Reference
    {
        public string OwnerPath;
        public int OwnerComponentIndex;
        public string PropertyPath;
        public string TargetPath;
    }

    private class Block
    {
        public int ClassId;
        public long FileId;
        public string Body;
    }

    private static readonly Regex BlockHeader = new Regex(@"^--- !u!(\d+) &(-?\d+)", RegexOptions.Multiline);
    private static readonly Regex FileIdRef = new Regex(@"\{fileID: (-?\d+)\}");

    public static List<Reference> TextReferences(string assetPath)
    {
        List<Block> blocks = Parse(File.ReadAllText(assetPath));
        Dictionary<long, string> goNames = new Dictionary<long, string>();
        Dictionary<long, List<long>> goComponents = new Dictionary<long, List<long>>();
        Dictionary<long, long> transformGo = new Dictionary<long, long>();
        Dictionary<long, long> transformFather = new Dictionary<long, long>();
        Dictionary<long, long> componentGo = new Dictionary<long, long>();
        HashSet<long> textIds = new HashSet<long>();

        foreach (Block block in blocks)
        {
            if (block.ClassId == 1)
            {
                Match name = Regex.Match(block.Body, @"^  m_Name: (.*)$", RegexOptions.Multiline);
                if (name.Success)
                {
                    goNames[block.FileId] = name.Groups[1].Value.Trim();
                }

                List<long> components = new List<long>();
                foreach (Match m in Regex.Matches(block.Body, @"- component: \{fileID: (-?\d+)\}"))
                {
                    components.Add(long.Parse(m.Groups[1].Value));
                }
                goComponents[block.FileId] = components;
            }
            else if (block.ClassId == 4 || block.ClassId == 224)
            {
                transformGo[block.FileId] = Field(block.Body, "m_GameObject");
                transformFather[block.FileId] = Field(block.Body, "m_Father");
            }
            else if (block.ClassId == 114)
            {
                componentGo[block.FileId] = Field(block.Body, "m_GameObject");
                if (block.Body.Contains("guid: " + TextScriptGuid) || block.Body.Contains("guid: " + InputFieldScriptGuid) || block.Body.Contains("guid: " + DropdownScriptGuid))
                {
                    textIds.Add(block.FileId);
                }
            }
        }

        Dictionary<long, long> goTransform = new Dictionary<long, long>();
        foreach (KeyValuePair<long, long> pair in transformGo)
        {
            goTransform[pair.Value] = pair.Key;
        }

        string PathOf(long go)
        {
            List<string> parts = new List<string>();
            while (true)
            {
                if (!goTransform.TryGetValue(go, out long transform) || !transformFather.TryGetValue(transform, out long father))
                {
                    return null;
                }

                if (father == 0)
                {
                    parts.Reverse();
                    return string.Join("/", parts);
                }

                if (!goNames.TryGetValue(go, out string name))
                {
                    return null;
                }

                parts.Add(name);
                go = transformGo[father];
            }
        }

        List<Reference> references = new List<Reference>();
        foreach (Block block in blocks)
        {
            if (block.ClassId != 114 || textIds.Contains(block.FileId))
            {
                continue;
            }

            long ownerGo = componentGo[block.FileId];
            string ownerPath = PathOf(ownerGo);
            if (ownerPath == null || !goComponents.TryGetValue(ownerGo, out List<long> components))
            {
                continue;
            }

            string arrayName = null;
            int arrayIndex = 0;
            foreach (string line in block.Body.Split('\n'))
            {
                Match array = Regex.Match(line, @"^  (\w+):\s*$");
                if (array.Success)
                {
                    arrayName = array.Groups[1].Value;
                    arrayIndex = 0;
                    continue;
                }

                Match item = Regex.Match(line, @"^  - \{fileID: (-?\d+)\}");
                Match field = Regex.Match(line, @"^  (\w+): \{fileID: (-?\d+)\}");
                string propertyPath;
                long target;
                if (item.Success && arrayName != null)
                {
                    propertyPath = $"{arrayName}.Array.data[{arrayIndex++}]";
                    target = long.Parse(item.Groups[1].Value);
                }
                else if (field.Success)
                {
                    arrayName = null;
                    propertyPath = field.Groups[1].Value;
                    target = long.Parse(field.Groups[2].Value);
                }
                else
                {
                    if (!line.StartsWith("  - ") && !line.StartsWith("    "))
                    {
                        arrayName = null;
                    }
                    continue;
                }

                if (!textIds.Contains(target))
                {
                    continue;
                }

                string targetPath = PathOf(componentGo[target]);
                if (targetPath == null)
                {
                    continue;
                }

                references.Add(new Reference
                {
                    OwnerPath = ownerPath,
                    OwnerComponentIndex = components.IndexOf(block.FileId),
                    PropertyPath = propertyPath,
                    TargetPath = targetPath,
                });
            }
        }

        return references;
    }

    public struct TextOverride
    {
        public string InstanceName;
        public string TargetObjectName;
        public string Value;
    }

    // Per-instance m_Text overrides on nested prefabs die with the Text component they target.
    public static List<TextOverride> NestedTextOverrides(string assetPath)
    {
        List<TextOverride> overrides = new List<TextOverride>();
        Dictionary<string, Dictionary<long, string>> sourceNames = new Dictionary<string, Dictionary<long, string>>();
        foreach (Block block in Parse(File.ReadAllText(assetPath)))
        {
            if (block.ClassId != 1001)
            {
                continue;
            }

            Match source = Regex.Match(block.Body, @"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]+)");
            if (!source.Success)
            {
                continue;
            }

            string guid = source.Groups[1].Value;
            string instanceName = null;
            List<(long target, string value)> texts = new List<(long, string)>();
            foreach (Match m in Regex.Matches(block.Body, @"- target: \{fileID: (-?\d+), guid: [0-9a-f]+,\s*type: 3\}\s*propertyPath: ([^\r\n]+)\r?\n\s*value: ([^\r\n]*)"))
            {
                string property = m.Groups[2].Value.Trim();
                string value = m.Groups[3].Value.Trim();
                if (property == "m_Name")
                {
                    instanceName = value;
                }
                else if (property == "m_Text")
                {
                    texts.Add((long.Parse(m.Groups[1].Value), value));
                }
            }

            if (texts.Count == 0)
            {
                continue;
            }

            if (instanceName == null)
            {
                instanceName = System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            }

            if (!sourceNames.TryGetValue(guid, out Dictionary<long, string> names))
            {
                names = ComponentObjectNames(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                sourceNames[guid] = names;
            }

            foreach ((long target, string value) in texts)
            {
                if (names.TryGetValue(target, out string objectName))
                {
                    overrides.Add(new TextOverride { InstanceName = instanceName, TargetObjectName = objectName, Value = value });
                }
            }
        }

        return overrides;
    }

    private static Dictionary<long, string> ComponentObjectNames(string assetPath)
    {
        Dictionary<long, string> goNames = new Dictionary<long, string>();
        Dictionary<long, long> componentGo = new Dictionary<long, long>();
        foreach (Block block in Parse(File.ReadAllText(assetPath)))
        {
            if (block.ClassId == 1)
            {
                Match name = Regex.Match(block.Body, @"^  m_Name: (.*)$", RegexOptions.Multiline);
                if (name.Success)
                {
                    goNames[block.FileId] = name.Groups[1].Value.Trim();
                }
            }
            else if (block.ClassId == 114)
            {
                componentGo[block.FileId] = Field(block.Body, "m_GameObject");
            }
        }

        Dictionary<long, string> result = new Dictionary<long, string>();
        foreach (KeyValuePair<long, long> pair in componentGo)
        {
            if (goNames.TryGetValue(pair.Value, out string name))
            {
                result[pair.Key] = name;
            }
        }

        return result;
    }

    private static List<Block> Parse(string yaml)
    {
        List<Block> blocks = new List<Block>();
        MatchCollection headers = BlockHeader.Matches(yaml);
        for (int i = 0; i < headers.Count; i++)
        {
            int start = headers[i].Index + headers[i].Length;
            int end = i + 1 < headers.Count ? headers[i + 1].Index : yaml.Length;
            blocks.Add(new Block
            {
                ClassId = int.Parse(headers[i].Groups[1].Value),
                FileId = long.Parse(headers[i].Groups[2].Value),
                Body = yaml.Substring(start, end - start),
            });
        }

        return blocks;
    }

    private static long Field(string body, string name)
    {
        Match m = Regex.Match(body, name + @": \{fileID: (-?\d+)\}");
        return m.Success ? long.Parse(m.Groups[1].Value) : 0;
    }
}
