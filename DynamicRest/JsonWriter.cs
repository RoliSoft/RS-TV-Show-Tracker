// JsonWriter.cs
// Source: https://github.com/NikhilK/dynamicrest/tree/9fae9d32c6ffb13081744afda019cce625311f1e/DynamicRest
// Developer: http://www.nikhilk.net/CSharp-Dynamic-Programming-JSON.aspx
//
// Comments for public methods were added by me, but there are no other modifications to the code.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace RoliSoft.TVShowTracker.DynamicJson {

    // TODO: Add date serialization options
    //       ScriptDate, Ticks, Formatted, Object

    /// <summary>
    /// Provides support for writing JSON.
    /// </summary>
    public sealed class JsonWriter {

        private StringWriter _internalWriter;
        private IndentedTextWriter _writer;
        private Stack<Scope> _scopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> class.
        /// </summary>
        public JsonWriter()
            : this(/* minimizeWhitespace */ false) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> class.
        /// </summary>
        /// <param name="minimizeWhitespace">if set to <c>true</c> minimizes whitespace.</param>
        public JsonWriter(bool minimizeWhitespace)
            : this(new StringWriter(), minimizeWhitespace) {
            _internalWriter = (StringWriter)_writer.Target;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public JsonWriter(TextWriter writer)
            : this(writer, /* minimizeWhitespace */ false) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="minimizeWhitespace">if set to <c>true</c> minimizes whitespace.</param>
        public JsonWriter(TextWriter writer, bool minimizeWhitespace) {
            _writer = new IndentedTextWriter(writer, minimizeWhitespace);
            _scopes = new Stack<Scope>();
        }

        /// <summary>
        /// Gets the json.
        /// </summary>
        /// <value>The json.</value>
        public string Json {
            get {
                if (_internalWriter != null) {
                    return _internalWriter.ToString();
                }
                throw new InvalidOperationException("Only available when you create JsonWriter without passing in your own TextWriter.");
            }
        }

        /// <summary>
        /// Ends the scope.
        /// </summary>
        public void EndScope() {
            if (_scopes.Count == 0) {
                throw new InvalidOperationException("No active scope to end.");
            }

            _writer.WriteLine();
            _writer.Indent--;

            Scope scope = _scopes.Pop();
            if (scope.Type == ScopeType.Array) {
                _writer.Write("]");
            }
            else {
                _writer.Write("}");
            }
        }

        internal static string QuoteJScriptString(string s) {
            if (String.IsNullOrEmpty(s)) {
                return String.Empty;
            }

            StringBuilder b = null;
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < s.Length; i++) {
                char c = s[i];

                // Append the unhandled characters (that do not require special treament)
                // to the string builder when special characters are detected.
                if (c == '\r' || c == '\t' || c == '\"' || c == '\'' ||
                    c == '\\' || c == '\r' || c < ' ' || c > 0x7F) {
                    if (b == null) {
                        b = new StringBuilder(s.Length + 6);
                    }

                    if (count > 0) {
                        b.Append(s, startIndex, count);
                    }

                    startIndex = i + 1;
                    count = 0;
                }

                switch (c) {
                    case '\r':
                        b.Append("\\r");
                        break;
                    case '\t':
                        b.Append("\\t");
                        break;
                    case '\"':
                        b.Append("\\\"");
                        break;
                    case '\'':
                        b.Append("\\\'");
                        break;
                    case '\\':
                        b.Append("\\\\");
                        break;
                    case '\n':
                        b.Append("\\n");
                        break;
                    default:
                        if ((c < ' ') || (c > 0x7F)) {
                            b.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                        }
                        else {
                            count++;
                        }
                        break;
                }
            }

            string processedString = s;
            if (b != null) {
                if (count > 0) {
                    b.Append(s, startIndex, count);
                }
                processedString = b.ToString();
            }

            return processedString;
        }

        /// <summary>
        /// Starts the array scope.
        /// </summary>
        public void StartArrayScope() {
            StartScope(ScopeType.Array);
        }

        /// <summary>
        /// Starts the object scope.
        /// </summary>
        public void StartObjectScope() {
            StartScope(ScopeType.Object);
        }

        private void StartScope(ScopeType type) {
            if (_scopes.Count != 0) {
                Scope currentScope = _scopes.Peek();
                if ((currentScope.Type == ScopeType.Array) &&
                    (currentScope.ObjectCount != 0)) {
                    _writer.WriteTrimmed(", ");
                }

                currentScope.ObjectCount++;
            }

            Scope scope = new Scope(type);
            _scopes.Push(scope);

            if (type == ScopeType.Array) {
                _writer.Write("[");
            }
            else {
                _writer.Write("{");
            }
            _writer.Indent++;
            _writer.WriteLine();
        }

        /// <summary>
        /// Writes the name.
        /// </summary>
        /// <param name="name">The name.</param>
        public void WriteName(string name) {
            if (String.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name");
            }
            if (_scopes.Count == 0) {
                throw new InvalidOperationException("No active scope to write into.");
            }
            if (_scopes.Peek().Type != ScopeType.Object) {
                throw new InvalidOperationException("Names can only be written into Object scopes.");
            }

            Scope currentScope = _scopes.Peek();
            if (currentScope.Type == ScopeType.Object) {
                if (currentScope.ObjectCount != 0) {
                    _writer.WriteTrimmed(", ");
                }

                currentScope.ObjectCount++;
            }

            _writer.Write("\"");
            _writer.Write(name);
            _writer.WriteTrimmed("\": ");
        }

        private void WriteCore(string text, bool quotes) {
            if (_scopes.Count != 0) {
                Scope currentScope = _scopes.Peek();
                if (currentScope.Type == ScopeType.Array) {
                    if (currentScope.ObjectCount != 0) {
                        _writer.WriteTrimmed(", ");
                    }

                    currentScope.ObjectCount++;
                }
            }

            if (quotes) {
                _writer.Write('"');
            }
            _writer.Write(text);
            if (quotes) {
                _writer.Write('"');
            }
        }

        /// <summary>
        /// Writes the boolean value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteValue(bool value) {
            WriteCore(value ? "true" : "false", /* quotes */ false);
        }

        /// <summary>
        /// Writes the int32 value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteValue(int value) {
            WriteCore(value.ToString(CultureInfo.InvariantCulture), /* quotes */ false);
        }

        /// <summary>
        /// Writes the float value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteValue(float value) {
            WriteCore(value.ToString(CultureInfo.InvariantCulture), /* quotes */ false);
        }

        /// <summary>
        /// Writes the double value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteValue(double value) {
            WriteCore(value.ToString(CultureInfo.InvariantCulture), /* quotes */ false);
        }

        /// <summary>
        /// Writes the date time value.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        public void WriteValue(DateTime dateTime) {
            if (dateTime < JsonReader.MinDate) {
                throw new ArgumentOutOfRangeException("dateTime");
            }

            long value = ((dateTime.Ticks - JsonReader.MinDateTimeTicks) / 10000);
            WriteCore("\\@" + value.ToString(CultureInfo.InvariantCulture) + "@", /* quotes */ true);
        }

        /// <summary>
        /// Writes the string value.
        /// </summary>
        /// <param name="s">The string.</param>
        public void WriteValue(string s) {
            if (s == null) {
                WriteCore("null", /* quotes */ false);
            }
            else {
                WriteCore(QuoteJScriptString(s), /* quotes */ true);
            }
        }

        /// <summary>
        /// Writes the list.
        /// </summary>
        /// <param name="items">The items.</param>
        public void WriteValue(ICollection items) {
            if ((items == null) || (items.Count == 0)) {
                WriteCore("[]", /* quotes */ false);
            }
            else {
                StartArrayScope();

                foreach (object o in items) {
                    WriteValue(o);
                }

                EndScope();
            }
        }

        /// <summary>
        /// Writes the dictionary.
        /// </summary>
        /// <param name="record">The dictionary.</param>
        public void WriteValue(IDictionary record) {
            if ((record == null) || (record.Count == 0)) {
                WriteCore("{}", /* quotes */ false);
            }
            else {
                StartObjectScope();

                foreach (DictionaryEntry entry in record) {
                    string name = entry.Key as string;
                    if (String.IsNullOrEmpty(name)) {
                        throw new ArgumentException("Key of unsupported type contained in Hashtable.");
                    }

                    WriteName(name);
                    WriteValue(entry.Value);
                }

                EndScope();
            }
        }

        /// <summary>
        /// Writes the object value.
        /// </summary>
        /// <param name="o">The object.</param>
        public void WriteValue(object o) {
            if (o == null) {
                WriteCore("null", /* quotes */ false);
            }
            else if (o is bool) {
                WriteValue((bool)o);
            }
            else if (o is int) {
                WriteValue((int)o);
            }
            else if (o is float) {
                WriteValue((float)o);
            }
            else if (o is double) {
                WriteValue((double)o);
            }
            else if (o is DateTime) {
                WriteValue((DateTime)o);
            }
            else if (o is string) {
                WriteValue((string)o);
            }
            else if (o is IDictionary) {
                WriteValue((IDictionary)o);
            }
            else if (o is ICollection) {
                WriteValue((ICollection)o);
            }
            else {
                StartObjectScope();

                PropertyDescriptorCollection propDescs = TypeDescriptor.GetProperties(o);
                foreach (PropertyDescriptor propDesc in propDescs) {
                    WriteName(propDesc.Name);
                    WriteValue(propDesc.GetValue(o));
                }

                EndScope();
            }
        }


        private enum ScopeType {

            Array = 0,

            Object = 1
        }

        /// <summary>
        /// Represents a scope.
        /// </summary>
        private sealed class Scope {

            private int _objectCount;
            private ScopeType _type;

            /// <summary>
            /// Initializes a new instance of the <see cref="Scope"/> class.
            /// </summary>
            /// <param name="type">The type.</param>
            public Scope(ScopeType type) {
                _type = type;
            }

            /// <summary>
            /// Gets or sets the object count.
            /// </summary>
            /// <value>The object count.</value>
            public int ObjectCount {
                get {
                    return _objectCount;
                }
                set {
                    _objectCount = value;
                }
            }

            /// <summary>
            /// Gets the type.
            /// </summary>
            /// <value>The type.</value>
            public ScopeType Type {
                get {
                    return _type;
                }
            }
        }


        /// <summary>
        /// Extends TextWriter with support for writing indented code.
        /// </summary>
        private sealed class IndentedTextWriter : TextWriter {

            private TextWriter _writer;
            private bool _minimize;

            private int _indentLevel;
            private bool _tabsPending;
            private string _tabString;

            /// <summary>
            /// Initializes a new instance of the <see cref="IndentedTextWriter"/> class.
            /// </summary>
            /// <param name="writer">The writer.</param>
            /// <param name="minimize">if set to <c>true</c> minimizes.</param>
            public IndentedTextWriter(TextWriter writer, bool minimize)
                : base(CultureInfo.InvariantCulture) {
                _writer = writer;
                _minimize = minimize;

                if (_minimize) {
                    NewLine = "\r";
                }

                _tabString = "  ";
                _indentLevel = 0;
                _tabsPending = false;
            }

            /// <summary>
            /// When overridden in a derived class, returns the <see cref="T:System.Text.Encoding"/> in which the output is written.
            /// </summary>
            /// <value></value>
            /// <returns>The Encoding in which the output is written.</returns>
            public override Encoding Encoding {
                get {
                    return _writer.Encoding;
                }
            }

            /// <summary>
            /// Gets or sets the line terminator string used by the current TextWriter.
            /// </summary>
            /// <value></value>
            /// <returns>The line terminator string for the current TextWriter.</returns>
            public override string NewLine {
                get {
                    return _writer.NewLine;
                }
                set {
                    _writer.NewLine = value;
                }
            }

            /// <summary>
            /// Gets or sets the indent level.
            /// </summary>
            /// <value>The indent level.</value>
            public int Indent {
                get {
                    return _indentLevel;
                }
                set {
                    Debug.Assert(value >= 0);
                    if (value < 0) {
                        value = 0;
                    }
                    _indentLevel = value;
                }
            }

            /// <summary>
            /// Gets the target text writer.
            /// </summary>
            /// <value>The target text writer.</value>
            public TextWriter Target {
                get {
                    return _writer;
                }
            }

            /// <summary>
            /// Closes the current writer and releases any system resources associated with the writer.
            /// </summary>
            public override void Close() {
                _writer.Close();
            }

            /// <summary>
            /// Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
            /// </summary>
            public override void Flush() {
                _writer.Flush();
            }

            private void OutputTabs() {
                if (_tabsPending) {
                    if (_minimize == false) {
                        for (int i = 0; i < _indentLevel; i++) {
                            _writer.Write(_tabString);
                        }
                    }
                    _tabsPending = false;
                }
            }

            /// <summary>
            /// Writes the specified string.
            /// </summary>
            /// <param name="s">The string.</param>
            public override void Write(string s) {
                OutputTabs();
                _writer.Write(s);
            }

            /// <summary>
            /// Writes the text representation of a Boolean value to the text stream.
            /// </summary>
            /// <param name="value">The Boolean to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(bool value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes a character to the text stream.
            /// </summary>
            /// <param name="value">The character to write to the text stream.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(char value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes a character array to the text stream.
            /// </summary>
            /// <param name="buffer">The character array to write to the text stream.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(char[] buffer) {
                OutputTabs();
                _writer.Write(buffer);
            }

            /// <summary>
            /// Writes a subarray of characters to the text stream.
            /// </summary>
            /// <param name="buffer">The character array to write data from.</param>
            /// <param name="index">Starting index in the buffer.</param>
            /// <param name="count">The number of characters to write.</param>
            /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>. </exception>
            /// <exception cref="T:System.ArgumentNullException">The <paramref name="buffer"/> parameter is null. </exception>
            /// <exception cref="T:System.ArgumentOutOfRangeException">
            /// 	<paramref name="index"/> or <paramref name="count"/> is negative. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(char[] buffer, int index, int count) {
                OutputTabs();
                _writer.Write(buffer, index, count);
            }

            /// <summary>
            /// Writes the text representation of an 8-byte floating-point value to the text stream.
            /// </summary>
            /// <param name="value">The 8-byte floating-point value to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(double value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes the text representation of a 4-byte floating-point value to the text stream.
            /// </summary>
            /// <param name="value">The 4-byte floating-point value to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(float value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes the text representation of a 4-byte signed integer to the text stream.
            /// </summary>
            /// <param name="value">The 4-byte signed integer to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(int value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes the text representation of an 8-byte signed integer to the text stream.
            /// </summary>
            /// <param name="value">The 8-byte signed integer to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(long value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes the text representation of an object to the text stream by calling ToString on that object.
            /// </summary>
            /// <param name="value">The object to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void Write(object value) {
                OutputTabs();
                _writer.Write(value);
            }

            /// <summary>
            /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)"/>.
            /// </summary>
            /// <param name="format">The formatting string.</param>
            /// <param name="arg0">An object to write into the formatted string.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="format"/> is null. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception>
            public override void Write(string format, object arg0) {
                OutputTabs();
                _writer.Write(format, arg0);
            }

            /// <summary>
            /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)"/>.
            /// </summary>
            /// <param name="format">The formatting string.</param>
            /// <param name="arg0">An object to write into the formatted string.</param>
            /// <param name="arg1">An object to write into the formatted string.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="format"/> is null. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception>
            public override void Write(string format, object arg0, object arg1) {
                OutputTabs();
                _writer.Write(format, arg0, arg1);
            }

            /// <summary>
            /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)"/>.
            /// </summary>
            /// <param name="format">The formatting string.</param>
            /// <param name="arg">The object array to write into the formatted string.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="format"/> or <paramref name="arg"/> is null. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to <paramref name="arg"/>. Length. </exception>
            public override void Write(string format, params object[] arg) {
                OutputTabs();
                _writer.Write(format, arg);
            }

            /// <summary>
            /// Writes the line without tabs.
            /// </summary>
            /// <param name="s">The string.</param>
            public void WriteLineNoTabs(string s) {
                _writer.WriteLine(s);
            }

            /// <summary>
            /// Writes the line.
            /// </summary>
            /// <param name="s">The string.</param>
            public override void WriteLine(string s) {
                OutputTabs();
                _writer.WriteLine(s);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes a line terminator to the text stream.
            /// </summary>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine() {
                OutputTabs();
                _writer.WriteLine();
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of a Boolean followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The Boolean to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(bool value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes a character followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The character to write to the text stream.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(char value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes an array of characters followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="buffer">The character array from which data is read.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(char[] buffer) {
                OutputTabs();
                _writer.WriteLine(buffer);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes a subarray of characters followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="buffer">The character array from which data is read.</param>
            /// <param name="index">The index into <paramref name="buffer"/> at which to begin reading.</param>
            /// <param name="count">The maximum number of characters to write.</param>
            /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>. </exception>
            /// <exception cref="T:System.ArgumentNullException">The <paramref name="buffer"/> parameter is null. </exception>
            /// <exception cref="T:System.ArgumentOutOfRangeException">
            /// 	<paramref name="index"/> or <paramref name="count"/> is negative. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(char[] buffer, int index, int count) {
                OutputTabs();
                _writer.WriteLine(buffer, index, count);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of a 8-byte floating-point value followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The 8-byte floating-point value to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(double value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of a 4-byte floating-point value followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The 4-byte floating-point value to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(float value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of a 4-byte signed integer followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The 4-byte signed integer to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(int value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of an 8-byte signed integer followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The 8-byte signed integer to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(long value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of an object by calling ToString on this object, followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The object to write. If <paramref name="value"/> is null, only the line termination characters are written.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(object value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)"/>.
            /// </summary>
            /// <param name="format">The formatted string.</param>
            /// <param name="arg0">The object to write into the formatted string.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="format"/> is null. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception>
            public override void WriteLine(string format, object arg0) {
                OutputTabs();
                _writer.WriteLine(format, arg0);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)"/>.
            /// </summary>
            /// <param name="format">The formatting string.</param>
            /// <param name="arg0">The object to write into the format string.</param>
            /// <param name="arg1">The object to write into the format string.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="format"/> is null. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception>
            public override void WriteLine(string format, object arg0, object arg1) {
                OutputTabs();
                _writer.WriteLine(format, arg0, arg1);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)"/>.
            /// </summary>
            /// <param name="format">The formatting string.</param>
            /// <param name="arg">The object array to write into format string.</param>
            /// <exception cref="T:System.ArgumentNullException">A string or object is passed in as null. </exception>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to arg.Length. </exception>
            public override void WriteLine(string format, params object[] arg) {
                OutputTabs();
                _writer.WriteLine(format, arg);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the text representation of a 4-byte unsigned integer followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The 4-byte unsigned integer to write.</param>
            /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
            /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
            public override void WriteLine(UInt32 value) {
                OutputTabs();
                _writer.WriteLine(value);
                _tabsPending = true;
            }

            /// <summary>
            /// Writes the significant new line.
            /// </summary>
            public void WriteSignificantNewLine() {
                WriteLine();
            }

            /// <summary>
            /// Writes the new line.
            /// </summary>
            public void WriteNewLine() {
                if (_minimize == false) {
                    WriteLine();
                }
            }

            /// <summary>
            /// Writes the string trimmed.
            /// </summary>
            /// <param name="text">The text.</param>
            public void WriteTrimmed(string text) {
                if (_minimize == false) {
                    Write(text);
                }
                else {
                    Write(text.Trim());
                }
            }
        }
    }
}
