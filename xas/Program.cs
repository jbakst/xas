using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Options;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Antlr3.ST;
//using Antlr4.StringTemplate;
//using Antlr4.StringTemplate.Compiler;
//using Antlr4.StringTemplate;
using CommandLine;
using CommandLine.Text;


namespace com.redwine.xas
{
    class Program
    {
        private static readonly HeadingInfo _headingInfo = new HeadingInfo("xas ActionScript Translator Version", "0.9.1");

        private sealed class Options
        {
        
            [Option('n', "name",
                    Required = true,
                    HelpText = "Input file.")]
            public string InputFile { get; set; }

            [Option('R', "recurse",
                    HelpText = "Recurse looking for file(s).")]
            public bool Recurse { get; set; }

            [Option('v', "verbose",
                    HelpText = "Verbose level. Range: from 0 to 2.")]
            public int VerboseLevel {get; set;}

            [Option('t', "target",
                HelpText = "Specify the output target (default C++)")]
            public string Target { get; set; }

            [Option('p', "prefix",
                HelpText = "Prefix accessors with set_ or get_")]
            public bool Prefix { get; set; }

            [Option('i', "ignore",
                   HelpText = "If file has errors don't stop.")]
            public bool IgnoreErrors { get; set; }

            [Option('o', "output",
                HelpText = "Specify the directory where translated files will be placed (default is xas_output in current folder)")]
            public string OutputDir { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption(HelpText = "Display this help screen.")]
            public string GetUsage()
            {
                var help = new HelpText(Program._headingInfo);
                help.AdditionalNewLineAfterOption = true;
                int[] years = {2011,2014};
                help.Copyright = new CopyrightInfo("Jeff Bakst. All rights reserved.", years);
                this.HandleParsingErrorsInHelp(help);
                help.AddPreOptionsLine("Usage: xas -nSomeFile.as -o .");
                help.AddPreOptionsLine(string.Format("       xas -R -o ."));
                help.AddOptions(this);

                return help;
            }

            private void HandleParsingErrorsInHelp(HelpText help)
            {
                if (this.LastParserState.Errors.Count > 0)
                {
                }
                string errors = help.RenderParsingErrorsText(this, 2); // indent with two spaces
                if (!string.IsNullOrEmpty(errors))
                {
                    help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                    help.AddPreOptionsLine(errors);
                }
            }
            
        }

        static void Main(string[] args)
        {
            List<string> names = new List<string>();
            int _errorCount = 0;

            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(1);
            }
#if !ANTLR
#if REV4
            Parser4 p = new Parser4();
#else
            Parser3 p = new Parser3();
#endif

            p.OutputPath = options.OutputDir;
#endif
            Console.WriteLine("Initializing...");

            StringTemplateGroup loader = new StringTemplateGroup("cpp2", path);
            //loader.LoadGroup("cpp2");
//            TemplateGroupString templates = loader.Compile() ;//"cpp2");//, typeof(TemplateLexer), null);
//            templates.Listener = new ErrorListener();

            if (options.Recurse)
            {
                string rootpath = options.InputFile;
                if (!rootpath.EndsWith("\\"))
                {
                    rootpath += @"\";
                }

                Console.WriteLine("Processing...");

                AS3TParser parser;

                string[] files = Directory.GetFiles(options.InputFile, "*.as", SearchOption.AllDirectories);
                foreach (string f in files)
                {
#if ANTLR
                    AS3TLexer lex = new AS3TLexer(new ANTLRFileStream(f));
                    //CommonTokenStream tokens = new CommonTokenStream(lex);
                    TokenRewriteStream tokens = new TokenRewriteStream(lex);

                    //CommonTokenStream tokens = new CommonTokenStream(lex);

                    parser = new AS3TParser(tokens);
                    parser.TemplateGroup = loader;
                    parser.OutputPath = options.OutputDir;

                    try
                    {
                        parser.program();
                        if (options.VerboseLevel > 2)
                        {
                            Console.WriteLine("calling generateSource {0}", parser.Classname);
                        }
                        generateSource(options, tokens, parser.Classname);
                        if (options.VerboseLevel > 2)
                        {
                            Console.WriteLine("calling generateHeader {0}", parser.Classname);
                        }
                        generateHeader(options, tokens, parser.Classname, parser.Basetype, parser.SymTable);
                        parser.Reset();
                    }
                    catch (NoViableAltException e)
                    {
                        Console.WriteLine("{0} {1}", e.Line, e.Message);
                        _errorCount += 1;
                    }
                    catch (RewriteEmptyStreamException e)
                    {
                        if (options.VerboseLevel > 2)
                        {
                            Console.WriteLine(e.Message);
                        }

                        _errorCount += 1;
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine("{0} {1}", e.Source, e.Message);
                        _errorCount += 1;
                    }
                    catch (NullReferenceException e)
                    {
                        Console.WriteLine("{0} {1}", e.Source, e.Message);
                        _errorCount += 1;
                    }
#else
                    Console.WriteLine("{0}", f);
                    p.Parse(f);
#endif
                }

                Console.WriteLine("xas done. {0} file(s) processed, {1} Error(s)", files.Length, _errorCount);
#if DEBUG
                Console.ReadKey();
#endif
            }

            else
            {
#if ANTLR
                try
                {
                    AS3TLexer lex = new AS3TLexer(new ANTLRFileStream(options.InputFile));
                    TokenRewriteStream tokens = new TokenRewriteStream(lex);

                    AS3TParser parser = new AS3TParser(tokens);
                    parser.TemplateGroup = loader;
                    parser.OutputPath = options.OutputDir;

                    parser.program();
                    generateSource(options, tokens, parser.Classname);
                    generateHeader(options, tokens, parser.Classname, parser.Basetype, parser.SymTable);
                    parser.Reset();
                }
                catch(FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    Environment.Exit(2);
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    Environment.Exit(2);
                }
                catch (NoViableAltException e)
                {
                    if (options.VerboseLevel > 2)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                catch (RewriteEmptyStreamException e)
                {
                    if (options.VerboseLevel > 2)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                catch (ArgumentException e)
                {
                    if (options.VerboseLevel > 2)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                catch (NullReferenceException e)
                {
                    if (options.VerboseLevel > 2)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                Console.WriteLine("xas done. {0} file(s) processed, {1} Error(s)", 1, _errorCount);

#if DEBUG
                Console.ReadKey();
#endif

#else
                p.Parse(options.InputFile);
#endif
            }
        }

        private static void generateSource(Options options, TokenRewriteStream tokens, string className)
        {
            if (options.VerboseLevel > 2)
            {
                Console.WriteLine("generateSource()");
            }
            string path = options.OutputDir + @"\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            StreamWriter sourceFile = new StreamWriter(path + className + ".cpp", false);


            sourceFile.WriteLine("/* Autogenerated by xas on {0} */", DateTime.Now);

            sourceFile.WriteLine("#include <cstddef>");
            sourceFile.WriteLine("#include <cstdio>");
            sourceFile.WriteLine("#include <cstdlib>");
            sourceFile.WriteLine("#include <cstring>");

            sourceFile.WriteLine("\n#include \"{0}.h\"", className);

            //Console.WriteLine(tokens.ToString("program"));

            try
            {
                sourceFile.WriteLine(tokens.ToString());
            }
            catch (NoViableAltException e)
            {
                if (options.VerboseLevel > 2)
                {
                    Console.WriteLine(e.Message);
                }
            }
            catch (InvalidCastException e)
            {
                if (options.VerboseLevel > 2)
                {
                    Console.WriteLine(e.Message);
                }
            }
            catch (ArgumentException e)
            {
                if (options.VerboseLevel > 2)
                {
                    Console.WriteLine(e.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected {0}", e.Message);
            }

            sourceFile.Close();
            if (options.VerboseLevel > 2)
            {
                Console.WriteLine("generateSource()--");
            }
        }

        private static void generateHeader(Options options, TokenRewriteStream tokens, string className, string baseName, SymbolTable symtab)
        {
            if (options.VerboseLevel > 2)
            {
                Console.WriteLine("generateHeader()");
            }
            string path = options.OutputDir + @"\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            StreamWriter sw = new StreamWriter(path + className + ".h", false);

            sw.WriteLine("/* Autogenerated by xas on {0} */", DateTime.Now);

            sw.WriteLine("#if !defined({0})", className.ToUpper());
            sw.WriteLine("#define {0}\n", className.ToUpper());

            if (null != baseName && baseName.Length > 0)
            {
                sw.WriteLine("class {0} : public {1} {2}", className, baseName, "{");
            }
            else
            {
                sw.WriteLine("class {0} {1}", className, "{");
            }

            sw.Flush();
//            parser.gen(headerFile);

            sw.WriteLine("\n// instance variables");

            sw.WriteLine("public:\n");
            foreach (Symbol s in symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PUBLIC)
                        {
                            string storage = "";
                            switch (s.Storage)
                            {
                                case Symbol.STATIC:
                                    storage = "static";
                                    break;
                            }
                            sw.WriteLine("\t{0} {1} {2};", storage, s.ReturnType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("protected:\n");
            foreach (Symbol s in symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PROTECTED && s.Storage != Symbol.STATIC)
                        {
                            sw.WriteLine("\t{0} {1};", s.ReturnType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("private:\n");
            foreach (Symbol s in symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PRIVATE && s.Storage != Symbol.STATIC)
                        {
                            sw.WriteLine("\t{0} {1};", s.ReturnType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("\n// methods");
            sw.WriteLine("public:\n");

            foreach (Symbol s in symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
                        if (s.Scope == Symbol.PUBLIC)
                        {
                            string prefix = "";
                            switch (s.Accessor)
                            {
                                case Symbol.SETTER:
                                    prefix = "set_";
                                    break;
                                case Symbol.GETTER:
                                    prefix = "get_";
                                    break;
                            }
                            if (s.Name == className)
                            {
                                sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
                                sw.WriteLine("\tvirtual ~{0}();\n", className);
                            }
                            else
                            {
                                sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("protected:\n");
            foreach (Symbol s in symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
                        if (s.Scope == Symbol.PROTECTED)
                        {
                            string prefix = "";
                            switch (s.Accessor)
                            {
                                case Symbol.SETTER:
                                    prefix = "set_";
                                    break;
                                case Symbol.GETTER:
                                    prefix = "get_";
                                    break;
                            }
                            sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("private:\n");
            foreach (Symbol s in symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
                        if (s.Scope == Symbol.PRIVATE)
                        {
                            string prefix = "";
                            switch (s.Accessor)
                            {
                                case Symbol.SETTER:
                                    prefix = "set_";
                                    break;
                                case Symbol.GETTER:
                                    prefix = "get_";
                                    break;
                            }
                            sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("{0}\n", "};");
            sw.WriteLine("\n#endif // {0}", className.ToUpper());

            sw.Close();
            if (options.VerboseLevel > 2)
            {
                Console.WriteLine("generateHeader()--");
            }
        }

    }
}
