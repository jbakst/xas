using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace com.redwine.xas
{
    class Parser2
    {
        private enum State {
            START,
            ARRAY_INITALIZER,
            FUNCTION_ARGLIST,
            ASSIGNMENT,
            VARDEF,
            ALLOCATING
        };

        private bool _inComment = false;
        private bool _inFunction = false;
        private bool _inClass = false;
        private string _className;
        private string _fname;
        private char[] WS = new char[] { ' ', '\t' };
        private char[] ALPHANUMERIC = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private bool _setter = false;
        private bool _getter = false;
        private bool _addDTOR = false;
        private int _brace = 0;
        private State _state = State.START;
        private int _fence = 0;
        private string _prev;
        private StringBuilder _sb;

        private Metadata _meta = new Metadata();

        private SymbolTable _symtab = new SymbolTable();
        
        private int _symbolStorage = 0;
        private int _symbolModifier = 0;
        private int _symbolScope = -1;

        private const int STATIC = 0;
        private const int CONST = 1;
        private const int PUBLIC = 2;
        private const int PROTECTED = 3;
        private const int PRIVATE = 4;

        private string _outputpath;

        public string OutputPath
        {
            set
            {
                _outputpath = value;
            }
            get
            {
                return _outputpath;
            }
        }

        public void Parse(string filename)
        {
            StreamReader sr = new StreamReader(filename);

            _prev = "";
            _sb = new StringBuilder();
            string line = "";

            while (!sr.EndOfStream)
            {
                _prev = line;
                line = sr.ReadLine();
                _setter = false;
                _getter = false;
                line = line.Trim();

                switch (_state)
                {
                    case State.START:
                        break;
                    case State.ARRAY_INITALIZER:
                        if (line.Contains("];"))
                        {
                            _state = State.START;
                        }
                        line = line.Replace("[", "{");
                        line = line.Replace("]", "}");
                        break;
                    case State.FUNCTION_ARGLIST:
                        OnFunction(line);
                        break;
                    default:
                        break;
                }

                if (line.StartsWith("//") || line.StartsWith("*"))
                {
                    _inComment = true;
                    _meta.Source = line;
                    continue;
                }
                else
                {
                    if (_inComment)
                    {
                        _inComment = false;
                    }
                }

                if (line.StartsWith("/*"))
                {
                    _inComment = true;
                    _meta.Source = line;
                    continue;
                }
                else if (line.StartsWith("*/"))
                {
                    _inComment = false;
                    _meta.Source = line;
                    continue;
                }

                else if (line.Contains(" function "))
                {
                    line = OnFunction(line);
                }
                else if (line.StartsWith("}"))
                {
                    line = OnBrace(line);

                    // need a better way to convert keywords
                    if (line.Contains(" is "))
                    {
                        // typeid(x) == typeid(y)
                        line = line.Replace("is", " typeid(");
                    }

                    _meta.Source = line;
                    if (_addDTOR)
                    {
                        _meta.Source = "\n~" + _className + "::" + _fname + "\n{\n}\n";
                        _addDTOR = false;
                    }
                }
                else if (line.Contains(" var "))
                {
                    line = ParseVar(line);
                }
                else if (line.Contains(" new "))
                {
                    OnNew(line);
                }
                //if (line.Contains(" as "))
                //{
                //    OnAsKeyword(line);
                //}
                else if (!_inComment && (_inFunction || _inClass))
                {
                    if (line.Contains("}"))
                    {
                        _brace -= 1;
                    }
                    if (line.Contains("{"))
                    {
                        _brace += 1;
                    }
                    if (line.StartsWith("/*"))
                    {
                        _inComment = true;
                    }
                    if (line.StartsWith("*/"))
                    {
                        _inComment = false;
                    }

                    foreach (Symbol s in _symtab)
                    {
                        if (s.Allocator == Symbol.ALLOCATED)
                        {
                            string key = s.Name + ".";
                            if (line.Contains(key))
                            {
                                line = line.Replace(key, s.Name + "->");
                                break;
                            }
                        }
                    }

                    // need a better way to convert keywords
                    if (line.Contains(" is "))
                    {
                        line = line.Replace("is", "instanceof");
                    }

                    _meta.Source = line;
                }
                else if (line.StartsWith("import"))
                {
                    line = OnImport(line);
                }
                else if (line.StartsWith("[Event"))
                {
                    _meta.Source = "// (Not supported) " + line;
                }
                else if (line.StartsWith("package"))
                {
                    line = OnPackage(line);
                }
                else if (line.Contains("class"))
                {
                    line = OnClass(line);
                }
                else if (line.Contains(" static "))
                {
                    line = OnStatic(line);
                }
                else
                {
                    if (line.StartsWith("/*"))
                    {
                        _inComment = true;
                    }
                    if (line.StartsWith("*/"))
                    {
                        _inComment = false;
                    }

                    foreach (Symbol s in _symtab)
                    {
                        if (s.Allocator == Symbol.ALLOCATED)
                        {
                            string key = s.Name + ".";
                            if (line.Contains(key))
                            {
                                if (s.EnclosingType == _fname) 
                                {
                                    line = line.Replace(key, s.Name + "->");
                                    break;
                                }
                            }
                        }
                    }

                    _meta.Source = line;
                }

            }

            sr.Close();

            GenerateCode();
        }

        private Dictionary<string, string> doArguments(string args)
        {
            Dictionary<string, string> argList = new Dictionary<string,string>();

            if (args != "")
            {
                string[] tokens = args.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string s in tokens)
                {
                    string[] arguments = s.Split(new Char[] { ':' });
                    string[] initializer = arguments[1].Split(new char[] { '=' });
                    if (initializer.Length > 1)
                    {
                        argList.Add(arguments[0] + "=" + initializer[1], initializer[0]);
                    }
                    else 
                    {
                        argList.Add(arguments[0], arguments[1]);
                    }
                }
            }

            return argList;
        }

        private void OnNew(string line)
        {
            if (!_inComment)
            {
                int ws = line.IndexOfAny(WS, 0);
                string variable = line.Substring(0, ws);
                string v;

                // find in symbol table
                foreach (Symbol sym in _symtab)
                {
                    if (sym.EnclosingType == _fname)
                    {
                        if (sym.Name == variable)
                        {
                            sym.Allocator = Symbol.ALLOCATED;
                            break;
                        }
                    }
                }

                // find in list
                foreach (String s in _meta.PublicFields)
                {
                    if (s.Contains(variable))
                    {
                        if (!s.Contains("*"))
                        {
                            _meta.PublicFields.Remove(s);
                            v = s;
                            v = v.Replace(variable, "*" + variable);
                            _meta.PublicFields.Add(v);
                        }
                        break;
                    }
                }
                foreach (String s in _meta.ProtectedFields)
                {
                    if (s.Contains(variable))
                    {
                        if (!s.Contains("*"))
                        {
                            _meta.ProtectedFields.Remove(s);
                            v = s;
                            v = v.Replace(variable, "*" + variable);
                            _meta.ProtectedFields.Add(v);
                        }
                        break;
                    }
                }
                foreach (String s in _meta.PrivateFields)
                {
                    if (s.Contains(variable))
                    {
                        if (!s.Contains("*"))
                        {
                            _meta.PrivateFields.Remove(s);
                            v = s;
                            v = v.Replace(variable, "*" + variable);
                            _meta.PrivateFields.Add(v);
                        }
                        break;
                    }
                }

                _meta.Source = line;
            }
        }

        private void GenerateCode()
        {
            CodeGenerator g = new CppCodeGenerator();

            g.Basename = _className;
            g.generate(_outputpath, _meta, _symtab);

        }

        private string RewriteArgs(string line, string asargs, Dictionary<string,string> args)
        {
            StringBuilder sb = new StringBuilder();
            bool hasMore = false;
            foreach (KeyValuePair<string, string> kvp in args)
            {
                if (hasMore) sb.Append(",");
                sb.Append(kvp.Value).Append(" ").Append(kvp.Key);
                hasMore = true;
            }
            line = line.Replace(asargs, sb.ToString());

            return line;
        }

        private string OnFunction(string line)
        {
            if (!_inComment)
            {
                _inFunction = true;

                switch (_state)
                {
                    case State.FUNCTION_ARGLIST:
                        _sb.Append(line);
                        if (line.IndexOf(')') != -1)
                        {
                            line = _sb.ToString();
                            _state = State.START;
                        }
                        else
                        {
                            return line;
                        }
                        break;
                }

                int kOpenParen = line.IndexOf('(');
                int kCloseParen = line.IndexOf(')');
                int kOpenCurly = line.IndexOf('{');
                if (kOpenCurly > 0)
                {
                    _brace += 1;
                }
                int colon = 0;
                if (kCloseParen != -1)
                {
                    colon = line.IndexOf(":", kCloseParen);
                    _state = State.START;
                }
                else
                {
                    _state = State.FUNCTION_ARGLIST;
                    return _sb.Append(line).ToString();
                }

                Symbol s = new Symbol();

                s.Kind = Symbol.FUNCTION;

                Dictionary<string, string> argList;
                if (line[kOpenParen + 1] == ')')
                {
                    argList = doArguments("");
                }
                else
                {
                    string asargs;
                    if (kCloseParen != -1)
                    {
                        asargs = line.Substring(kOpenParen + 1, (kCloseParen - kOpenParen) - 1);
                    }
                    else
                    {
                        asargs = line.Substring(kOpenParen + 1);
                    }
                    argList = doArguments(asargs);
                    line = RewriteArgs(line, asargs, argList);
                }

                s.Arguments = argList;

                int kFunc = line.IndexOf("function") + 8;
                string delim = " \t;";
                char[] anyOf = delim.ToCharArray();

                int index = (colon == -1) ? kCloseParen : colon;
                kCloseParen = line.IndexOf(')');
                int ws = -1;
                if (kCloseParen != -1)
                {
                    colon = line.IndexOf(":", kCloseParen);
                    ws = line.IndexOfAny(anyOf, kCloseParen);
                }

                _fname = line.Substring(kFunc, kOpenParen - kFunc).Trim();
                if (line.Contains("set"))
                {
                    line = line.Replace("set ", "");
                    _fname = _fname.Replace("set ", "");
                    _setter = true;
                    s.Accessor = Symbol.SETTER;
                }
                else if (line.Contains("get"))
                {
                    line = line.Replace("get ", "");
                    _fname = _fname.Replace("get ", "");
                    _getter = true;
                    s.Accessor = Symbol.GETTER;
                }

                s.Name = _fname;

                string rettype = "";

                kCloseParen = line.IndexOf(')');
                colon = -1;
                if (kCloseParen != -1)
                {
                    colon = line.IndexOf(":", kCloseParen);
                    ws = line.IndexOfAny(anyOf, kCloseParen);
                }

                if (colon != -1)
                {
                    int length = ws - colon;
                    if (ws > 0)
                    {
                        rettype = line.Substring(colon + 1, length);
                    }
                    else
                    {
                        rettype = line.Substring(colon + 1);
                    }
                    line = line.Replace(rettype, "");
                    line = line.Replace(":", " ");
                }

                s.ReturnType = rettype;

                if (_setter)
                {
                    // TODO: Use a command line switch to control prefix
                    line = line.Replace("function ", rettype + _className + "::set_");
                }
                else if (_getter)
                {
                    // TODO: Use a command line switch to control prefix
                    line = line.Replace("function ", rettype + _className + "::get_");
                }
                else
                {
                    line = line.Replace("function ", rettype + _className + "::");
                }


                string[] tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (token == "public")
                    {
                        line = line.Replace("public ", "");
                        line = line.Trim();
                        if (_fname == _className)
                        {
                            _meta.Constructor = _fname;
                            _addDTOR = true;
                        }
                        else
                        {
                            _meta.PublicMember = _fname;
                        }

                        s.Scope = Symbol.PUBLIC;
                    }
                    else if (token == "protected") 
                    {
                        line = line.Replace("protected ", "");
                        line = line.Trim();
                        _meta.ProtectedMember = _fname;

                        s.Scope = Symbol.PROTECTED;
                    }
                    else if (token == "private")
                    {
                        line = line.Replace("private ", "");
                        line = line.Trim();
                        _meta.PrivateMember = _fname;

                        s.Scope = Symbol.PRIVATE;
                    }
                    else if (token == "override")
                    {
                        line = line.Replace("override ", "virtual ");
                        line = line.Trim();
                        _meta.VirtualMember = _fname;

                        s.Virtual = true;
                    }
                }

                _meta.Source = line;

                _symtab.Add(s);
            }
            else
            {
                _meta.Source = line;
            }
            return line;
        }

        private string OnImport(string line)
        {
            if (!_inComment)
            {
                line = line.Replace("import ", "#include \"");
                line = line.Replace(".", "\\");
                line = line.Replace(";", ".h\"");
                line = line.Trim();
                _meta.Include = line;
            }
            else
            {
                _meta.Source = line;
            }
            return line;
        }

        private string OnPackage(string line)
        {
            if (!_inComment)
            {
                line = line.Replace("package", "");
                line = line.Replace(".", "::");
                line = line.Trim();
                int ws = line.IndexOfAny(WS, "namespace".Length);
                if (ws > 0)
                {
                    _meta.Namespace = line.Substring(0, ws);
                }
                else
                {
                    _meta.Namespace = line.Substring(0);
                }
            }
            else
            {
                _meta.Source = line;
            }
            return line;
        }

        private string OnClass(string line)
        {
            if (!_inComment)
            {
                int classPos = line.IndexOf("class");
                if (line.IndexOf("{") > 0)
                {
                    _brace += 1;
                }

                int p1 = line.IndexOfAny(WS, classPos);
                int p2 = line.IndexOfAny(ALPHANUMERIC, p1);
                int p3 = line.IndexOfAny(WS, p2);

                if (p3 < 0)
                {
                    _className = line.Substring(p2);
                }
                else
                {
                    _className = line.Substring(p2, p3 - p2);
                }

                line = line.Replace("public", "");
                line = line.Replace("extends", ": public");
                line = line.Replace("implements", ", ");
                _inClass = true;

                line = line.Trim();

                _meta.Header = line;
            }
            else
            {
                _meta.Source = line;
            }

            return line;
        }

        private string OnStatic(string line)
        {
            _symbolModifier = -1;
            _symbolStorage = -1;

            if (!_inComment && !_inFunction)
            {
                int colon = line.IndexOf(":");

                string variable = "";
                string type = "";

                string delim = " \t;";
                char[] anyOf = delim.ToCharArray();
                int ws;

                int kVar = line.IndexOf("var");
                int kConst = line.IndexOf("const");
                int kStatic = line.IndexOf("static");

                if (kVar != -1)
                {
                    kVar += 3;
                    variable = line.Substring(kVar, colon - kVar);
                    ws = line.IndexOfAny(anyOf, colon);
                    type = line.Substring(colon + 1, (ws - colon) - 1);
                    line = line.Replace(type, "");
                    line = line.Replace("var", "");
                }
                else if (kConst != -1)
                {
                    kConst += 5;
                    variable = line.Substring(kConst, colon - kConst);
                    ws = line.IndexOfAny(anyOf, colon);
                    type = line.Substring(colon + 1, (ws - colon) - 1);
                    line = line.Replace(type, "");
                    line = line.Replace("const", "const " + type);
                }
                else if (kStatic != -1)
                {
                    kStatic += 6;
                    variable = line.Substring(kStatic, colon - kStatic);
                    ws = line.IndexOfAny(anyOf, colon);
                    type = line.Substring(colon + 1, (ws - colon) - 1);
                    line = line.Replace(type, "");
                    line = line.Replace("static", "static " + type);
                }

                line = line.Replace(":", " ");

                if (line.Contains("public"))
                {
                    line = line.Replace("public", "");
                    line = line.Trim();
                    _symbolScope = PUBLIC;
                }
                else if (line.Contains("protected"))
                {
                    line = line.Replace("protected", "");
                    line = line.Trim();
                    _symbolScope = PROTECTED;
                }
                else if (line.Contains("private"))
                {
                    line = line.Replace("private", "");
                    line = line.Trim();
                    _symbolScope = PRIVATE;
                }

                string symbol = "";

                if (line.Contains("static"))
                {
                    line = line.Replace("static", "");
                    line = line.Replace(type + " ", _className + "::");
                    _symbolStorage = STATIC;
                    symbol += " static";
                }

                if (line.Contains("const"))
                {
                    line = line.Replace("const", "");
                    _symbolModifier = CONST;
                    symbol += " const";
                }

                line = line.Trim();

                symbol += " ";
                symbol += type;
                symbol += " ";
                symbol += variable;

                if (line.Contains("=") && (CONST != _symbolModifier && STATIC != _symbolModifier))
                {
                    _meta.Initializer = line;
                }

                if (_symbolStorage == STATIC)
                {
                    _meta.Static = line;
                }

                if (_symbolScope == PUBLIC)
                {
                    _meta.PublicField = symbol;
                }
                else if (_symbolScope == PROTECTED)
                {
                    _meta.ProtectedField = symbol;
                }
                else if (_symbolScope == PRIVATE)
                {
                    _meta.PrivateField = symbol;
                }
                else
                {
                    _meta.Source = line;
                }
            }
            return line;
        }

        private string OnBrace(string line)
        {
            if (line.Contains("}"))
            {
                _brace -= 1;
            }
            if (line.Contains("{"))
            {
                _brace += 1;
            }
            if (_inFunction)
            {
                if (_brace == 1)
                {
                    _inFunction = false;
                }
            }
            else if (_inClass)
            {
                if (_brace == 0)
                {
                    _inClass = false;
                }
            }

            return line;
        }

        private int Match(string line, char value)
        {
            int commentStart = line.IndexOf("//");
            int index = line.IndexOf(value);
            return (index < commentStart) ? index : -1;
        }

        private int Match(string line, string value)
        {
            int commentStart = line.IndexOf("//");
            int index = line.IndexOf(value);
            return (index < commentStart) ? index : -1;
        }

        private string ParseVar(string line)
        {
            _symbolModifier = -1;
            _symbolStorage = -1;

            if (!_inComment && !_inFunction)
            {
                StringBuilder sb = new StringBuilder();

                Symbol s = new Symbol();

                s.EnclosingType = _className;

                string[] tokens = line.Split(new char[] { ' ','\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (token == "public")
                    {
                        s.Scope = Symbol.PUBLIC;
                    }
                    else if (token == "protected")
                    {
                        s.Scope = Symbol.PROTECTED;
                    }
                    else if (token == "private")
                    {
                        s.Scope = Symbol.PRIVATE;
                    }
                    else if (token == "var")
                    {
                        _state = State.VARDEF;
                    }
                    else if (token == "const")
                    {
                        s.Modifier = Symbol.CONST;
                    }
                    else if (token == "new")
                    {
                        s.Storage = Symbol.ALLOCATED;
                        _state = State.ALLOCATING;
                    }
                    else if (token == "=")
                    {
                        _state = State.ASSIGNMENT;
                    }
                    else
                    {
                        switch (_state)
                        {
                            case State.ASSIGNMENT:
                                string[] assigntokens = token.Split(new char[] { ' ', '\t', '=',';' }, StringSplitOptions.RemoveEmptyEntries);
                                s.DefaultValue = assigntokens[0];
                                sb.Append(s.DefaultValue);
                                break;
                            case State.VARDEF:
                                string[] vartokens = token.Split(new char[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);
                                s.Name = vartokens[0];
                                s.Type = vartokens[1];
                                break;
                            case State.ALLOCATING:
                                sb.AppendFormat("= {0};", s.Type);
                                break;
                        }
                        _state = State.START;
                    }
                }

                int colon = line.IndexOf(":");

                string variable = "";
                string type = "";

                char[] anyOf = new char[] { ' ', '\t', ';' };
                int ws;

                int kVar = line.IndexOf("var");
                int kConst = line.IndexOf("const");
                int kStatic = line.IndexOf("static");

                

                if (kVar != -1)
                {
                    kVar += 3;
                    variable = line.Substring(kVar, colon - kVar);
                    ws = line.IndexOfAny(anyOf, colon);
                    if (ws < 0)
                    {
                        type = line.Substring(colon + 1, colon - 1);
                    }
                    else
                    {
                        type = line.Substring(colon + 1, (ws - colon) - 1);
                    }
                    line = line.Replace(type+" ", "");
                    line = line.Replace("var", "");
                }
                else if (kConst != -1)
                {
                    kConst += 5;
                    variable = line.Substring(kConst, colon - kConst);
                    ws = line.IndexOfAny(anyOf, colon);
                    type = line.Substring(colon + 1, (ws - colon) - 1);
                    line = line.Replace(type, "");
                    line = line.Replace("const", "const " + type);
                }
                else if (kStatic != -1)
                {
                    kStatic += 6;
                    variable = line.Substring(kStatic, colon - kStatic);
                    ws = line.IndexOfAny(anyOf, colon);
                    type = line.Substring(colon + 1, (ws - colon) - 1);
                    line = line.Replace(type, "");
                    line = line.Replace("static", "static " + type);
                }

                s.Name = variable;
                s.Type = type;
                s.EnclosingType = _className;

                int kEquals = Match(line, '=');
                int kSemi = Match(line, ';');

                if (kEquals != -1)
                {
                    if (kSemi != -1)
                    {
                        s.DefaultValue = line.Substring(kEquals + 1, kSemi - kEquals - 1);
                    }
                    else
                    {
                        // array initializer
                        if (line.IndexOf('[') != -1)
                        {
                            line = line.Replace('[', '{');
                            s.DefaultValue = line.Substring(kEquals + 1);
                            if (s.Type == "Array")
                            {
                                _state = State.ARRAY_INITALIZER;
                            }
                        }
                        else
                        {
                            s.DefaultValue = line.Substring(kEquals + 1);
                        }
                    }
                }
                else
                {
                    s.DefaultValue = null;
                }

                line = line.Replace(":", " ");

                if (line.Contains("public"))
                {
                    line = line.Replace("public", "");
                    line = line.Trim();
                    _meta.ProtectedField = type + " " + variable;
                    _symbolScope = PUBLIC;
                    s.Scope = Symbol.PUBLIC;
                }
                else if (line.Contains("protected"))
                {
                    line = line.Replace("protected", "");
                    line = line.Trim();
                    _meta.ProtectedField = type + " " + variable;
                    _symbolScope = PROTECTED;
                    s.Scope = Symbol.PROTECTED;
                }
                else if (line.Contains("private"))
                {
                    line = line.Replace("private", "");
                    line = line.Trim();
                    _meta.PrivateField = type + " " + variable;
                    _symbolScope = PRIVATE;
                    s.Scope = Symbol.PRIVATE;
                }

                if (line.Contains("static"))
                {
                    line = line.Replace("static", "");
                    line = line.Replace(type + " ", _className + "::");
                    _symbolStorage = STATIC;
                    s.Storage = Symbol.STATIC;
                }

                if (line.Contains("const"))
                {
                    line = line.Replace("const", "");
                    _symbolModifier = CONST;
                    s.Modifier = Symbol.CONST;
                }

                line = line.Trim();

                if (line.Contains("new"))
                {
                    s.Allocator = Symbol.ALLOCATED;
                }

                if (line.Contains("="))
                {
                    _meta.Initializer = line;
                    _meta.Source = line;
                }
                else if (_symbolStorage == STATIC)
                {
                    _meta.Static = line;
                }
                else if (_symbolScope == PUBLIC)
                {
                    _meta.PublicField = type + " " + variable;
                }
                else if (_symbolScope == PROTECTED)
                {
                    _meta.ProtectedField = type + " " + variable;
                }
                else if (_symbolScope == PRIVATE)
                {
                    _meta.PrivateField = type + " " + variable;
                }
                else
                {
                    _meta.Source = line;
                }


                _symtab.Add(s);
            }
            else
            {
                if (_inFunction)
                {
                    int colon = line.IndexOf(":");

                    string variable = "";
                    string type = "";

                    char[] anyOf = new char[] { ' ', '\t', ';' };
                    int ws;

                    int kVar = line.IndexOf("var");

                    Symbol s = new Symbol();

                    if (kVar != -1)
                    {
                        kVar += 3;
                        variable = line.Substring(kVar, colon - kVar);
                        ws = line.IndexOfAny(anyOf, colon);
                        if (ws < 0)
                        {
                            type = line.Substring(colon + 1, colon - 1);
                        }
                        else
                        {
                            type = line.Substring(colon + 1, (ws - colon) - 1);
                        }
                        line = line.Replace(":"+type, "");
                        line = line.Replace(variable, "");
                        line = line.Replace("var", type + "*"+variable);
                    }

                    s.Name = variable;
                    s.Type = type;
                    s.EnclosingType = _fname;

                    if (line.Contains("new"))
                    {
                        s.Allocator = Symbol.ALLOCATED;
                    }

                    _meta.Source = line;

                    _symtab.Add(s);
                }
            }
            return line;
        }
    }
}
