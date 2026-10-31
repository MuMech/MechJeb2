using System;
using System.IO;
using System.Text;

namespace MuMech
{
    /// <summary>
    ///     Writes a ConfigNode in ConfigNode.Save's format through one retained buffer. KSP's Save builds a string for each line, and with
    ///     KSPCommunityFixes it allocates a 64K StreamWriter for each file, which was most of the garbage of a settings save. Main thread only.
    /// </summary>
    public static class ConfigNodeWriter
    {
        private static readonly UTF8Encoding _utf8    = new UTF8Encoding(false);
        private static readonly string       _newLine = Environment.NewLine;

        private static byte[] _buffer = new byte[64 * 1024];
        private static int    _length;

        /// <summary>
        ///     Writes to a temp file first, so a crash during the write leaves the previous file whole.
        /// </summary>
        public static void Save(ConfigNode root, string path)
        {
            _length = 0;
            WriteValues(root, 0);
            for (int i = 0; i < root.nodes.Count; i++)
                WriteNode(root.nodes[i], 0);

            string temp = path + ".tmp";
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 8))
                file.Write(_buffer, 0, _length);

            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        private static void WriteNode(ConfigNode node, int depth)
        {
            Indent(depth);
            Write(node.name);
            WriteComment(node.comment);
            Write(_newLine);
            Indent(depth);
            Write("{");
            Write(_newLine);
            WriteValues(node, depth + 1);
            for (int i = 0; i < node.nodes.Count; i++)
                WriteNode(node.nodes[i], depth + 1);
            Indent(depth);
            Write("}");
            Write(_newLine);
        }

        private static void WriteValues(ConfigNode node, int depth)
        {
            for (int i = 0; i < node.values.Count; i++)
            {
                ConfigNode.Value value = node.values[i];
                Indent(depth);
                Write(value.name);
                Write(" = ");
                Write(value.value);
                WriteComment(value.comment);
                Write(_newLine);
            }
        }

        private static void WriteComment(string comment)
        {
            if (string.IsNullOrEmpty(comment)) return;
            Write(" // ");
            Write(comment);
        }

        private static void Indent(int depth)
        {
            for (int i = 0; i < depth; i++)
                Write("\t");
        }

        private static void Write(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            int needed = _length + _utf8.GetMaxByteCount(s.Length);
            if (needed > _buffer.Length)
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, needed));
            _length += _utf8.GetBytes(s, 0, s.Length, _buffer, _length);
        }
    }
}
