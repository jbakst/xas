using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace com.redwine.xas
{
    class Parser4
    {
        private enum State {
            START,
            ARRAY_INITALIZER,
            FUNCTION_ARGLIST,
            ASSIGNMENT,
            VARDEF,
            ALLOCATING,
            ALLOCTYPE,
            COMMENT,
            FUNCDEF,
            SET,
            GET,
            ARGSDEF,
            CLASSDEF,
            EXTENDS,
            IMPLEMENTS,
            STATICDEF,
            PACKAGEDEF,
            LBRACE,
            RBRACE,
            IMPORTDEF,
            BEGINCOMMENT,
            ENDCOMMENT,
            IGNORE,
            CONSUME,
            NEWDEF,
            IFDEF,
            RETURNDEF,
            ELSEDEF,
            STMTDEF,
            CALLDEF,
            QUOTEDEF,
            REFCALL

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
            string name = "";

            while (!sr.EndOfStream)
            {
                _prev = line;
                line = sr.ReadLine();
                _setter = false;
                _getter = false;
                line = line.Trim();

                _sb.Clear();

                // start exp
                string[] tokens = line.Split(new char[] { ' ', '\t', }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    switch (_state)
                    {
                        case State.IGNORE:
                            _sb.Append(token);
                            continue;
                        case State.ASSIGNMENT:
                            break;
                        case State.NEWDEF:
                            line = OnNew(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.PACKAGEDEF:
                            name = token.Replace(".", "::");
                            name = name.Trim();
                            _sb.AppendFormat("namespace {0}", name);
                            _state = State.LBRACE;
                            continue;
                        case State.STATICDEF:
                            line = OnStatic(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.CLASSDEF:
                            line = OnClass(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.STMTDEF:
                            line = OnStatement(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.CONSUME:
                            continue;
                        case State.VARDEF:
                            line = OnVarKeyword(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.LBRACE:
                            _sb.Append(" {");
                            _state = State.START;
                            continue;
                        case State.RBRACE:
                            line = OnBrace(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.FUNCDEF:
                            line = OnFunction(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.IMPORTDEF:
                            name = token.Replace(".", "\\");
                            name = name.Replace(";", ".h\"");
                            _sb.AppendFormat("#include \"{0}\"", name);
                            _state = State.START;
                            continue;
                        case State.IFDEF:
                            line = OnIf(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.ELSEDEF:
                            line = OnElse(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.RETURNDEF:
                            line = OnReturn(line);
                            _sb.Append(line);
                            _state = State.CONSUME;
                            continue;
                        case State.BEGINCOMMENT:
                            if (token == "*/")
                            {
                                _state = State.ENDCOMMENT;
                            }
                            _sb.Append(token);
                            continue;
                        default:
                            break;
                    }

                    if (token == "function")
                    {
                        _state = State.FUNCDEF;
                    }
                    else if (token == "}")
                    {
                        _state = State.RBRACE;
                    }
                    else if (token == "const")
                    {
                        _state = State.START;
                    }
                    else if (token == "{")
                    {
                        _state = State.LBRACE;
                    }
                    else if (token == "is")
                    {
                        _state = State.START;
                    }
                    else if (token == "var")
                    {
                        _state = State.VARDEF;
                    }
                    else if (token == "new")
                    {
                        _state = State.NEWDEF;
                    }
                    else if (token == "import")
                    {
                        _state = State.IMPORTDEF;
                        //line = OnImport(line);
                    }
                    else if (token.StartsWith("[Event"))
                    {
                        _sb.AppendFormat("// (Not supported) {0}", token);
                        _state = State.IGNORE;
                    }
                    else if (token == "package")
                    {
                        _state = State.PACKAGEDEF;
                    }
                    else if (token == "class")
                    {
                        _state = State.CLASSDEF;
                    }
                    else if (token == "public")
                    {
                        _state = State.START;
                    }
                    else if (token == "protected")
                    {
                        _state = State.START;
                    }
                    else if (token == "private")
                    {
                        _state = State.START;
                    }
                    else if (token.StartsWith("if")) 
                    {
                        _state = State.IFDEF;
                    }
                    else if (token == "return")
                    {
                        _state = State.RETURNDEF;
                    }
                    else if (token.StartsWith("else"))
                    {
                        _state = State.ELSEDEF;
                    }
                    else if (token == "/*" || token == "/**")
                    {
                        _inComment = true;
                        _sb.Append(token);
                        _state = State.BEGINCOMMENT;
                    }
                    else if (token == "*/")
                    {
                        _inComment = false;
                        _sb.Append(token);
                        _state = State.ENDCOMMENT;
                    }
                    else if (token == "static")
                    {
                        _state = State.STATICDEF;
                    }
                    else if (token == "=")
                    {
                        _state = State.ASSIGNMENT;
                    }
                    else
                    {
                        _state = State.STMTDEF;

                        // lookup in Symbol table
                        Symbol sym = _symtab.Lookup(token);
                        if (null != sym)
                        {
                            //continue;
                        }
                        //string[] stmttokens = line.Split(new Char[] { ' ', '\t', '.' }, StringSplitOptions.RemoveEmptyEntries);

                        if (line.Contains("}"))
                        {
                            _brace -= 1;
                        }
                        if (line.Contains("{"))
                        {
                            _brace += 1;
                        }

                        continue;
                    }
                }

                switch (_state)
                {
                    case State.ENDCOMMENT:
                        _state = State.START;
                        break;
                    case State.IGNORE:
                    case State.CONSUME:
                        _state = State.START;
                        break;
                    case State.RBRACE:
                        _sb.Append(line);
                        _state = State.START;
                        break;
                    case State.STMTDEF:
                        _sb.Append(line);
                        _state = State.START;
                        break;

                }

                _meta.Source = _sb.ToString();
                // end exp

#if REV3
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
                    case State.COMMENT:
                        _meta.Source = line;
                        if (line.StartsWith("*/"))
                        {
                            _state = State.START;
                            _inComment = false;
                        }
                        continue;
                    default:
                        break;
                }

                if (line.Contains(" function "))
                {
                    _inFunction = true;
                    line = OnFunction(line);
                    _meta.Source = line;
                }
                else if (line.StartsWith("}"))
                {
                    line = OnBrace(line);

                    if (line.Contains(" is "))
                    {
                        // typeid(x) == typeid(y)
                        string keyword = KeywordMapping.Convert("is");
                        line = line.Replace("is", keyword);
                    }

                    _meta.Source = line;
                    if (_addDTOR)
                    {
                        _meta.Source = "\n~" + _className + "::" + _fname + "\n{\n}\n";
                        _addDTOR = false;
                    }
                }
                else if (line.Contains("var "))
                {
                    switch (_state)
                    {
                        case State.COMMENT:
                            continue;
                    }
                    line = OnVarKeyword(line);
                    _meta.Source = line;
                }
                else if (line.Contains(" new "))
                {
                    OnNew(line);
                }
                else if (!_inComment && (_inFunction || _inClass))
                {
                    string[] tokens2 = line.Split(new Char[] { ' ', '\t','.' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string tok in tokens2)
                    {
                        // lookup in Symbol table
                        Symbol sym = _symtab.Lookup(tok);
                        if (sym.Allocator == 1)
                        {

                        }
                    }

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
                        _state = State.COMMENT;
                    }
                    if (line.StartsWith("*/"))
                    {
                        _inComment = false;
                        _state = State.START;
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
                else if (line.Contains("class "))
                {
                    line = OnClass(line);
                }
                else if (line.StartsWith("/*"))
                {
                    _inComment = true;
                    _state = State.COMMENT;
                    _meta.Source = line;
                }
                else if (line.StartsWith("*/"))
                {
                    _inComment = false;
                    _state = State.START;
                    _meta.Source = line;
                }
                else if (line.Contains(" static "))
                {
                    line = OnStatic(line);
                }
                else
                {
                    string[] tokens2 = line.Split(new Char[] { ' ','\t' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string s in tokens2)
                    {

                    }
                    if (line.StartsWith("/*"))
                    {
                        _inComment = true;
                        _state = State.COMMENT;
                    }
                    if (line.StartsWith("*/"))
                    {
                        _inComment = false;
                        _state = State.START;
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
#endif
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
                    if (arguments.Length > 1)
                    {
                        string[] initializer = arguments[1].Split(new char[] { '=' });
                        if (initializer.Length > 1)
                        {
                            argList.Add(arguments[0] + "=" + initializer[1], initializer[0]);
                        }
                        else
                        {
                            argList.Add(arguments[0], TypeMapping.Convert(arguments[1]));
                        }
                    }
                    else
                    {
                        arguments[0] = arguments[0].Replace(".", "::");
                        argList.Add(arguments[0], TypeMapping.Convert(arguments[0]));
                    }
                }
            }

            return argList;
        }

        private string OnNew(string line)
        {
            StringBuilder sb = new StringBuilder();

            string[] tokens = line.Split(new char[] { ' ', '\t', }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                switch (_state)
                {
                    case State.COMMENT:
                        sb.Append(token);
                        continue;
                    default:
                        break;
                }

                if (token == "new")
                {
                    _state = State.ALLOCATING;
                }
                else if (token == "=")
                {
                    continue;
                }
                else
                {
                    string[] newtokens;
                    switch (_state)
                    {
                        case State.ALLOCATING:
                            newtokens = token.Split(new char[] { '(',')',';' }, StringSplitOptions.RemoveEmptyEntries);
                            sb.AppendFormat("new {0}", newtokens[0]);
                            string args = "";
                            if (newtokens.Length > 1)
                            {
                                args = RewriteArgs(doArguments(newtokens[1]));
                            }
                            sb.AppendFormat("({0});", args);
                            _state = State.START;
                            break;
                        default:
                            sb.AppendFormat("{0} = ",  token);
                            Symbol sym = _symtab.Lookup(token);
                            break;
                    }
                }
                    
            }

            line = sb.ToString();
            _meta.Source = line;

            return line;
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

        public static string RewriteArgs(Dictionary<string, string> args)
        {
            StringBuilder sb = new StringBuilder();

            if (null != args)
            {
                bool hasMore = false;
                foreach (KeyValuePair<string, string> kvp in args)
                {
                    if (hasMore) sb.Append(",");
                    if (kvp.Value == kvp.Key)
                    {
                        // constant
                        sb.Append(kvp.Value);
                    }
                    else
                    {
                        sb.Append(kvp.Value).Append(" ").Append(kvp.Key);
                    }
                    hasMore = true;
                }
            }

            return sb.ToString();
        }

        private string OnIf(string line)
        {

            return line;
        }

        private string OnReturn(string line)
        {

            return line;
        }

        private string OnElse(string line)
        {

            return line;
        }

        private string OnMethodCall(string line)
        {
            StringBuilder sb = new StringBuilder();
            Symbol sym;

            string[] tokens = line.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                if (tokens.Length > 1)
                {
                    sym = _symtab.Lookup(token);
                    if (null != sym)
                    {
                        if (Symbol.ALLOCATED == sym.Allocator)
                        {
                            sb.AppendFormat("{0}->", token);
                            _state = State.ALLOCTYPE;
                        }
                        else
                        {
                            sb.Append(token);
                            _state = State.REFCALL;
                        }
                    }
                    else
                    {
                        switch (_state)
                        {
                            case State.ALLOCTYPE:
                                sb.AppendFormat("{0}", token);
                                _state = State.START;
                                break;
                            case State.REFCALL:
                                sb.AppendFormat(".{0}", token);
                                _state = State.START;
                                break;
                            case State.START:
                                sb.AppendFormat("{0}", token);
                                if (line.Contains(token+"."))
                                {
                                    _state = State.REFCALL;
                                }
                                else
                                {
                                    _state = State.START;
                                }
                                break;
                        }

                    }
                }
                else
                {
                    sb.Append(token);
                }
            }

            return sb.ToString();
        }

        private string OnStatement(string line)
        {
            if (_inComment)
            {
                return line;
            }

            StringBuilder sb = new StringBuilder();

            Symbol s = new Symbol();

            string[] tokenss = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokenss)
            {
                switch (_state)
                {
                    case State.STMTDEF:
                        _state = State.START;
                        string str = OnMethodCall(token);
                        sb.Append(str);
                        continue;
                    case State.NEWDEF:
                        _state = State.CALLDEF;
                        sb.Append("new ");
                        break;
                    case State.ASSIGNMENT:
                        _state = State.START;
                        sb.Append(" = ");
                        break;
                    case State.QUOTEDEF:
                        sb.Append(token);
                        continue;
                    default:
                        break;
                }

                if (token == "new")
                {
                    _state = State.NEWDEF;
                }
                else if (token.Contains('"'))
                {
                    sb.Append(token);
                    _state = State.QUOTEDEF;
                }
                else if (token == "=")
                {
                    _state = State.ASSIGNMENT;
                }
                else
                {
                    switch (_state)
                    {
                        case State.CALLDEF:
                            string[] argtokens;
                            string[] functokens = token.Split(new char[] { '(' }, StringSplitOptions.RemoveEmptyEntries);


                            if (functokens[1].StartsWith(")"))
                            {
                                sb.Append(token);
                            }
                            else
                            {
                                Dictionary<string, string> args = null;
                                argtokens = functokens[1].Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
                                if (argtokens.Length == 0)
                                {
                                    args = doArguments("");
                                }
                                else if (argtokens.Length == 1)
                                {
                                    args = doArguments(argtokens[0]);
                                }
                                else if (argtokens.Length == 2)
                                {
                                    args = doArguments(argtokens[0]);
                                }
                                sb.AppendFormat("{0}({1});", functokens[0], RewriteArgs(args));
                            }
                            break;
                        case State.START:
                            //@string str = OnMethodCall(token);
                            sb.Append(token);
                            break;
                    }
                }
            }

            return sb.ToString();
        }

        private string OnFunction(string line)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder args = new StringBuilder();

            Symbol s = new Symbol();

            s.EnclosingType = _className;
            s.Kind = Symbol.FUNCTION;

            string[] tokenss = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokenss)
            {
                switch (_state)
                {
                    case State.COMMENT:
                        sb.Append(token);
                        continue;
                    default:
                        break;
                }

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
                else if (token == "function")
                {
                    _state = State.FUNCDEF;
                }
                else if (token == "const")
                {
                    s.Modifier = Symbol.CONST;
                }
                else if (token == "static")
                {
                    s.Modifier = Symbol.STATIC;
                }
                else if (token == "override")
                {
                    s.Modifier = Symbol.OVERRIDE;
                }
                else if (token == "set")
                {
                    _state = State.SET;
                    s.Kind = Symbol.SETTER;
                }
                else if (token == "get")
                {
                    _state = State.GET;
                    s.Kind = Symbol.GETTER;
                }
                else if (token == "=")
                {
                    _state = State.ASSIGNMENT;
                }
                else if (token == "//" || token == "/*")
                {
                    _state = State.COMMENT;
                    sb.Append(token);
                }
                else
                {
                    string[] functokens;
                    string[] argtokens;
                    switch (_state)
                    {
                        case State.ASSIGNMENT:
                            string[] assigntokens = token.Split(new char[] { ' ', '\t', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            s.DefaultValue = assigntokens[0];
                            sb.AppendFormat("{0} {1} = {2};", s.Type, s.Name, s.DefaultValue);
                            _state = State.START;
                            break;
                        case State.FUNCDEF:
                            functokens = token.Split(new char[] { '(' }, StringSplitOptions.RemoveEmptyEntries);
                            s.Name = functokens[0];
                            if (s.Name == _className)
                            {
                                s.Constructor = true;
                            }
                            _fname = functokens[0];
                            if (functokens[1].StartsWith(")"))
                            {
                                string[] rettype = functokens[1].Split(new char[] { ')',':' }, StringSplitOptions.RemoveEmptyEntries);
                                s.Arguments = doArguments("");
                                if (rettype.Length > 0)
                                {
                                    s.ReturnType = TypeMapping.Convert(rettype[0].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0]); ; ;
                                }
                                else
                                {
                                    s.ReturnType = "";
                                }
                                _state = State.START;
                            }
                            else
                            {
                                argtokens = functokens[1].Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
                                if (argtokens.Length == 0)
                                {
                                    s.Arguments = doArguments("");
                                    s.ReturnType = "";
                                }
                                else if (argtokens.Length == 1)
                                {
                                    s.Arguments = doArguments(argtokens[0]);
                                    s.ReturnType = "";
                                }
                                else if (argtokens.Length == 2)
                                {
                                    s.Arguments = doArguments(argtokens[0]);
                                    s.ReturnType = TypeMapping.Convert(argtokens[1].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0]); ;
                                }
                                _state = State.ARGSDEF;
                            }
                            break;

                        case State.ARGSDEF:
                            args.Append(token);
                            break;

                        case State.SET:
                            functokens = token.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                            s.Name = functokens[0];
                            s.Arguments = doArguments(functokens[1]);
                            s.ReturnType = TypeMapping.Convert(functokens[2].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                            _state = State.ARGSDEF;
                            break;

                        case State.GET:
                            functokens = token.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                            s.Name = functokens[0];
                            s.ReturnType = TypeMapping.Convert(functokens[1].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                            sb.AppendFormat("{0} {1}::{2}()", s.ReturnType, s.EnclosingType, s.Name);
                            sb.Append(" {");
                            _state = State.START;
                            break;
                    }
                }
            }

            switch (_state)
            {
                case State.ASSIGNMENT:
                    sb.AppendFormat("{0} {1};", s.Type, s.Name);
                    _state = State.START;
                    break;
                case State.COMMENT:
                    _state = State.START;
                    break;
                case State.ARGSDEF:
                    switch (s.Kind)
                    {
                        case Symbol.FUNCTION:
                            sb.AppendFormat("{0} {1}::{2}({3})", s.ReturnType, s.EnclosingType, s.Name, RewriteArgs(s.Arguments));
                            sb.Append(" {");
                            break;
                        case Symbol.SETTER:
                            sb.AppendFormat("{0} {1}::{2}({3})", s.ReturnType, s.EnclosingType, s.Name, RewriteArgs(s.Arguments));
                            sb.Append(" {");
                            break;
                    }
                    _state = State.START;
                    break;
            }

            _symtab.Add(s);

            line = sb.ToString();

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
            StringBuilder sb = new StringBuilder();

            Symbol s = new Symbol();

            s.Kind = Symbol.CLASS;

            string[] tokenss = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokenss)
            {
                switch (_state)
                {
                    case State.COMMENT:
                        sb.Append(token);
                        continue;
                    default:
                        break;
                }
                if (token == "public")
                {
                    s.Scope = Symbol.PUBLIC;
                }
                else if(token == "protected")
                {
                    s.Scope = Symbol.PROTECTED;
                }
                else if (token == "private")
                {
                    s.Scope = Symbol.PRIVATE;
                }
                else if (token == "final")
                {
                    s.Modifier = Symbol.FINAL;
                }
                else if (token == "class")
                {
                    _state = State.CLASSDEF;
                }
                else if (token == "extends")
                {
                    _state = State.EXTENDS;
                }
                else if (token == "implements")
                {
                    _state = State.IMPLEMENTS;
                }
                else
                {
                    string[] classtokens;
                    //@string[] argtokens;
                    switch (_state)
                    {
                        case State.CLASSDEF:
                            classtokens = token.Split(new char[] { '(' }, StringSplitOptions.RemoveEmptyEntries);
                            s.Name = classtokens[0];
                            _className = classtokens[0];
                            sb.AppendFormat("class {0}", classtokens[0]);
                            break;
                        case State.EXTENDS:
                            classtokens = token.Split(new char[] { '(' }, StringSplitOptions.RemoveEmptyEntries);
                            sb.AppendFormat(" : public {0}", classtokens[0]);
                            s.ExtendedType = classtokens[0];
                            _state = State.START;
                            break;
                        case State.IMPLEMENTS:
                            classtokens = token.Split(new char[] { '(' }, StringSplitOptions.RemoveEmptyEntries);
                            sb.AppendFormat(", {0}", classtokens[0]);
                            s.ImplementedType = classtokens[0];
                            _state = State.START;
                            break;
                    }
                }
            }

            switch (_state)
            {
                case State.START:
                    sb.Append(" {");
                    break;
            }

            _symtab.Add(s);

            line = sb.ToString();

            _meta.Source = line;

            return line;
        }

        private string OnStatic(string line)
        {
            _symbolModifier = -1;
            _symbolStorage = -1;

            StringBuilder sb = new StringBuilder();

            Symbol s = new Symbol();

            if (_inFunction)
            {
                s.EnclosingType = _fname;
            }
            else
            {
                s.EnclosingType = _className;
            }

            string[] tokens = line.Split(new char[] { ' ', '\t', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                switch (_state)
                {
                    case State.COMMENT:
                        sb.Append(token);
                        continue;
                    case State.CONSUME:
                        continue;
                    default:
                        break;
                }

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
                else if (token == "static")
                {
                    _state = State.STATICDEF;
                    s.Storage = Symbol.STATIC;
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
                else if (token == "//" || token == "/*")
                {
                    _state = State.COMMENT;
                    sb.Append(token);
                }
                else if (token == "function")
                {
                    line = OnFunction(line);
                    sb.Append(line);
                    _state = State.CONSUME;
                    continue;
                }
                else if (token == "var")
                {
                    line = OnVarKeyword(line);
                    sb.Append(line);
                    _state = State.CONSUME;
                    continue;
                }
                else
                {
                    switch (_state)
                    {
                        case State.ASSIGNMENT:
                            string[] assigntokens = token.Split(new char[] { ' ', '\t', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            s.DefaultValue = assigntokens[0];
                            sb.AppendFormat("{0} {1} = {2};", s.Type, s.Name, s.DefaultValue);
                            _state = State.START;
                            break;
                        case State.STATICDEF:
                            string[] vartokens = token.Split(new char[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            s.Name = vartokens[0];
                            s.Type = TypeMapping.Convert(vartokens[1]);
                            _state = State.ASSIGNMENT;
                            break;
                        case State.ALLOCATING:
                            sb.AppendFormat("{0} *{1} = new {2};", s.Type, s.Name, token);
                            _state = State.START;
                            break;
                    }
                }
            }

            switch (_state)
            {
                case State.ASSIGNMENT:
                    sb.AppendFormat("{0} {1};", s.Type, s.Name);
                    break;
                case State.COMMENT:
                    _state = State.START;
                    break;
            }

            line = sb.ToString();

            _symtab.Add(s);

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

        private string OnVarKeyword(string line)
        {
            _symbolModifier = -1;
            _symbolStorage = -1;

            StringBuilder sb = new StringBuilder();

            Symbol s = new Symbol();
            s.Kind = Symbol.VARIABLE;

            if (_inFunction)
            {
                s.EnclosingType = _fname;
            }
            else 
            {
                s.EnclosingType = _className;
            }

                string[] tokens = line.Split(new char[] { ' ','\t','=',';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    switch (_state)
                    {
                        case State.COMMENT:
                            sb.Append(token);
                            continue;
                        default:
                            break;
                    }

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
                    else if (token == "static")
                    {
                        s.Storage = Symbol.STATIC;
                    }
                    else if (token == "=")
                    {
                        _state = State.ASSIGNMENT;
                    }
                    else if (token == "//" || token == "/*")
                    {
                        _state = State.COMMENT;
                        sb.Append(token);
                    }
                    else
                    {
                        switch (_state)
                        {
                            case State.ASSIGNMENT:
                                string[] assigntokens = token.Split(new char[] { ' ', '\t', '=',';' }, StringSplitOptions.RemoveEmptyEntries);
                                s.DefaultValue = assigntokens[0];
                                sb.AppendFormat("{0} {1} = {2};", s.Type, s.Name, s.DefaultValue);
                                _state = State.START;
                                break;
                            case State.VARDEF:
                                string[] vartokens = token.Split(new char[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);
                                s.Name = vartokens[0];
                                s.Type = TypeMapping.Convert(vartokens[1]);
                                _state = State.ASSIGNMENT;
                                break;
                            case State.ALLOCATING:
                                sb.AppendFormat("{0} *{1} = new {2}", s.Type, s.Name, token);
                                s.Allocator = Symbol.ALLOCATED;
                                _state = State.START;
                                break;
                            case State.START:
                                sb.AppendFormat("{0}", token);
                                break;
                        }
                    }
                }

                switch (_state)
                {
                    case State.ASSIGNMENT:
                        sb.AppendFormat("{0} {1};", s.Type, s.Name);
                        break;
                    case State.COMMENT:
                        _state = State.START;
                        break;
                    case State.START:
                        if (!sb.ToString().EndsWith(";"))
                        {
                            sb.Append(";");
                        }
                        break;
                }

                line = sb.ToString();

                //_meta.Source = line;

                _symtab.Add(s);

                return line;
        }
    }
}
