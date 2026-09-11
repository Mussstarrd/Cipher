#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cipher.Game.Scenarios
{
    /// <summary>What a parsed JSON node holds.</summary>
    public enum JsonKind { Null, Bool, Number, String, Array, Object }

    /// <summary>
    /// A parsed JSON value, plus the accessors the scenario reader needs.
    ///
    /// Why a hand-rolled reader rather than a library: <c>JsonUtility</c> cannot express the
    /// scenario schema at all (no dictionaries, no polymorphic objective list, no optional
    /// sections), and pulling in Newtonsoft costs a package dependency for perhaps four hundred
    /// lines of work. This also runs unchanged in the pure-C# test host, so scenario files can be
    /// validated without an editor.
    ///
    /// Every accessor is PATH-AWARE. Reading a field carries the dotted path with it, so a typo in
    /// a hand-authored mission reports "waves[2].spawnPerSecond: expected a number, found string"
    /// rather than a null reference three layers away. Mission files are written by hand and
    /// unhelpful parse errors are how a data-driven pipeline stops being used.
    /// </summary>
    public sealed class JsonValue
    {
        public JsonKind Kind { get; }
        /// <summary>Dotted path to this value from the document root, for error messages.</summary>
        public string Path { get; private set; } = "$";

        private readonly bool _bool;
        private readonly double _number;
        private readonly string? _string;
        private readonly List<JsonValue>? _array;
        private readonly Dictionary<string, JsonValue>? _object;

        private JsonValue(JsonKind kind, bool b = false, double n = 0, string? s = null,
                          List<JsonValue>? array = null, Dictionary<string, JsonValue>? obj = null)
        {
            Kind = kind; _bool = b; _number = n; _string = s; _array = array; _object = obj;
        }

        internal static JsonValue Null() => new JsonValue(JsonKind.Null);
        internal static JsonValue Bool(bool b) => new JsonValue(JsonKind.Bool, b: b);
        internal static JsonValue Number(double n) => new JsonValue(JsonKind.Number, n: n);
        internal static JsonValue Str(string s) => new JsonValue(JsonKind.String, s: s);
        internal static JsonValue Array(List<JsonValue> items) => new JsonValue(JsonKind.Array, array: items);
        internal static JsonValue Object(Dictionary<string, JsonValue> members) =>
            new JsonValue(JsonKind.Object, obj: members);

        /// <summary>Stamps paths through the whole tree, once, after parsing.</summary>
        internal void AssignPaths(string path)
        {
            Path = path;
            if (_array != null)
                for (int i = 0; i < _array.Count; i++) _array[i].AssignPaths($"{path}[{i}]");
            if (_object != null)
                foreach (var pair in _object) pair.Value.AssignPaths($"{path}.{pair.Key}");
        }

        public int Count => _array?.Count ?? _object?.Count ?? 0;

        public IReadOnlyList<JsonValue> Items =>
            _array ?? throw Wrong("an array");

        /// <summary>Member names, in file order is not guaranteed; used for unknown-key checks.</summary>
        public IEnumerable<string> Keys =>
            _object?.Keys ?? throw Wrong("an object");

        public bool Has(string key) => _object != null && _object.ContainsKey(key);

        /// <summary>A required member. Throws with the path if absent.</summary>
        public JsonValue Get(string key)
        {
            if (_object == null) throw Wrong("an object");
            if (!_object.TryGetValue(key, out var v))
                throw new ScenarioException($"{Path}: missing required field '{key}'");
            return v;
        }

        /// <summary>An optional member, or null.</summary>
        public JsonValue? Opt(string key) =>
            _object != null && _object.TryGetValue(key, out var v) && v.Kind != JsonKind.Null ? v : null;

        public string AsString() => Kind == JsonKind.String ? _string! : throw Wrong("a string");
        public bool AsBool() => Kind == JsonKind.Bool ? _bool : throw Wrong("a boolean");

        public double AsDouble() => Kind == JsonKind.Number ? _number : throw Wrong("a number");
        public float AsFloat() => (float)AsDouble();

        public int AsInt()
        {
            double d = AsDouble();
            // A fractional count is a content bug worth shouting about, not something to truncate.
            if (Math.Abs(d - Math.Round(d)) > 1e-9)
                throw new ScenarioException($"{Path}: expected a whole number, found {d.ToString(CultureInfo.InvariantCulture)}");
            if (d < int.MinValue || d > int.MaxValue)
                throw new ScenarioException($"{Path}: {d.ToString(CultureInfo.InvariantCulture)} is out of range for an integer");
            return (int)Math.Round(d);
        }

        /// <summary>
        /// Rejects any member this reader does not know. A silently ignored key is how a typo in a
        /// mission file turns into an hour of wondering why the wave table did nothing.
        /// </summary>
        public void RejectUnknownKeys(params string[] known)
        {
            if (_object == null) throw Wrong("an object");
            foreach (var key in _object.Keys)
            {
                if (System.Array.IndexOf(known, key) >= 0) continue;
                throw new ScenarioException(
                    $"{Path}: unknown field '{key}'. Known fields here: {string.Join(", ", known)}");
            }
        }

        private ScenarioException Wrong(string expected) =>
            new ScenarioException($"{Path}: expected {expected}, found {Kind.ToString().ToLowerInvariant()}");

        public static JsonValue Parse(string text)
        {
            var parser = new JsonParser(text ?? throw new ArgumentNullException(nameof(text)));
            var value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw parser.Error("unexpected trailing content");
            value.AssignPaths("$");
            return value;
        }
    }

    /// <summary>Anything wrong with a scenario file: malformed JSON, or a schema violation.</summary>
    public sealed class ScenarioException : Exception
    {
        public ScenarioException(string message) : base(message) { }
    }

    /// <summary>A small recursive-descent JSON reader. Strict: no comments, no trailing commas.</summary>
    internal sealed class JsonParser
    {
        private readonly string _s;
        private int _i;

        internal JsonParser(string s) { _s = s; }

        internal bool AtEnd => _i >= _s.Length;

        internal ScenarioException Error(string message)
        {
            int line = 1, column = 1;
            for (int k = 0; k < Math.Min(_i, _s.Length); k++)
            {
                if (_s[k] == '\n') { line++; column = 1; } else column++;
            }
            return new ScenarioException($"line {line}, column {column}: {message}");
        }

        internal void SkipWhitespace()
        {
            while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\r' || _s[_i] == '\n')) _i++;
        }

        internal JsonValue ParseValue()
        {
            SkipWhitespace();
            if (AtEnd) throw Error("unexpected end of input");

            char c = _s[_i];
            switch (c)
            {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': return JsonValue.Str(ParseString());
                case 't': Expect("true"); return JsonValue.Bool(true);
                case 'f': Expect("false"); return JsonValue.Bool(false);
                case 'n': Expect("null"); return JsonValue.Null();
                default: return JsonValue.Number(ParseNumber());
            }
        }

        private void Expect(string literal)
        {
            if (_i + literal.Length > _s.Length || string.CompareOrdinal(_s, _i, literal, 0, literal.Length) != 0)
                throw Error($"expected '{literal}'");
            _i += literal.Length;
        }

        private JsonValue ParseObject()
        {
            var members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            _i++;   // '{'
            SkipWhitespace();
            if (!AtEnd && _s[_i] == '}') { _i++; return JsonValue.Object(members); }

            while (true)
            {
                SkipWhitespace();
                if (AtEnd || _s[_i] != '"') throw Error("expected a field name in double quotes");
                string key = ParseString();
                if (members.ContainsKey(key)) throw Error($"duplicate field '{key}'");

                SkipWhitespace();
                if (AtEnd || _s[_i] != ':') throw Error($"expected ':' after '{key}'");
                _i++;

                members[key] = ParseValue();

                SkipWhitespace();
                if (AtEnd) throw Error("unexpected end of input inside an object");
                if (_s[_i] == ',') { _i++; continue; }
                if (_s[_i] == '}') { _i++; return JsonValue.Object(members); }
                throw Error("expected ',' or '}'");
            }
        }

        private JsonValue ParseArray()
        {
            var items = new List<JsonValue>();
            _i++;   // '['
            SkipWhitespace();
            if (!AtEnd && _s[_i] == ']') { _i++; return JsonValue.Array(items); }

            while (true)
            {
                items.Add(ParseValue());
                SkipWhitespace();
                if (AtEnd) throw Error("unexpected end of input inside an array");
                if (_s[_i] == ',') { _i++; continue; }
                if (_s[_i] == ']') { _i++; return JsonValue.Array(items); }
                throw Error("expected ',' or ']'");
            }
        }

        private string ParseString()
        {
            _i++;   // opening quote
            var sb = new StringBuilder();
            while (true)
            {
                if (AtEnd) throw Error("unterminated string");
                char c = _s[_i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }

                if (AtEnd) throw Error("unterminated escape sequence");
                char e = _s[_i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_i + 4 > _s.Length) throw Error("truncated \\u escape");
                        if (!ushort.TryParse(_s.Substring(_i, 4), NumberStyles.HexNumber,
                                             CultureInfo.InvariantCulture, out ushort code))
                            throw Error("malformed \\u escape");
                        sb.Append((char)code);
                        _i += 4;
                        break;
                    default: throw Error($"unknown escape '\\{e}'");
                }
            }
        }

        private double ParseNumber()
        {
            int start = _i;
            if (!AtEnd && (_s[_i] == '-' || _s[_i] == '+')) _i++;
            while (!AtEnd && (char.IsDigit(_s[_i]) || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E'
                              || ((_s[_i] == '-' || _s[_i] == '+') && (_s[_i - 1] == 'e' || _s[_i - 1] == 'E'))))
                _i++;

            string token = _s.Substring(start, _i - start);
            if (token.Length == 0)
                throw Error($"unexpected character '{(AtEnd ? ' ' : _s[start])}'");
            // InvariantCulture: a machine reading a mission file must not depend on the player's locale.
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw Error($"'{token}' is not a number");
            return value;
        }
    }
}
