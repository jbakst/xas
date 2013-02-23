﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace com.redwine.xas
{
    class CppCodeGenerator : CodeGenerator
    {
        public CppCodeGenerator()
        {
            
        }

        public override void generate(string outputpath, Metadata meta, SymbolTable table)
        {
            base.generate(outputpath, meta, table);

            string path = outputpath + @"\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            StreamWriter sourceFile = new StreamWriter(path + Basename + ".cpp", false);
            StreamWriter headerFile = new StreamWriter(path + Basename + ".h", false);

            sourceFile.WriteLine("/* Autogenerated by xas on {0} */", DateTime.Now);

            // write includes
            if (meta.Includes.Count > 0)
            {
                sourceFile.WriteLine("// includes");
                foreach (String s in meta.Includes)
                {
                    sourceFile.WriteLine("{0}", s);
                }
            }
            // write statics
            if (meta.Statics.Count > 0)
            {
                sourceFile.WriteLine("\n\n// statics");
                foreach (String s in meta.Statics)
                {
                    sourceFile.WriteLine("{0}\n", s);
                }
            }

            foreach (Symbol s in table)
            {
                switch (s.Storage)
                {
                    case Symbol.STATIC:
                        if (s.DefaultValue != null)
                        {
                            sourceFile.WriteLine("{0}::{1} = {2};\n", s.EnclosingType, s.Name, s.DefaultValue);
                        }
                        else
                        {
                            sourceFile.WriteLine("{0}::{1};\n", s.EnclosingType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

#if DEBUG
            // dump symtab
            foreach (Symbol z in table)
            {
                sourceFile.WriteLine("//@ {0} {1} {2} {3} {4} ", z.Type, z.Name, z.Modifier, z.Scope, z.Storage);
                if (z.Storage == Symbol.STATIC)
                {
                    sourceFile.WriteLine("//@ {0}::{1}", z.EnclosingName, z.Name);
                }
            }
#endif
            //
            // write source
            //
            if (meta.Sources.Count > 0)
            {
                foreach (String s in meta.Sources)
                {
                    string src = s;
                    foreach (Symbol z in table)
                    {
                        if (s.Contains(z.Name))
                        {
                            if (z.Storage == Symbol.STATIC)
                            {
                                src = src.Replace(z.Name, z.EnclosingName + "::" + z.Name);
                            }
                            break;
                        }
                    }
                    sourceFile.WriteLine("{0}", src);
                }
            }

            StringBuilder sb = new StringBuilder();

            //
            // write header (interface)
            //
            headerFile.WriteLine("/* Autogenerated by xas on {0} */", DateTime.Now);

            headerFile.WriteLine("namespace {0} {1}", meta.Namespace, "{");

            foreach (Symbol s in table)
            {
                switch (s.Kind)
                {
                    case Symbol.CLASS:
                        switch (s.Scope)
                        {
                            case Symbol.PUBLIC:
                                sb.AppendFormat("public class {0}({1}) ", s.Name, Parser3.RewriteArgs(s.Arguments));
                                break;
                            case Symbol.PROTECTED:
                                sb.AppendFormat("protected class {0}({1}) ", s.Name, Parser3.RewriteArgs(s.Arguments));
                                break;
                            case Symbol.PRIVATE:
                                sb.AppendFormat("private class {0}({1}) ", s.Name, Parser3.RewriteArgs(s.Arguments));
                                break;
                            default:
                                break;
                        }
                        if (s.ExtendedType != null)
                        {
                            sb.AppendFormat(": {0}()", s.ExtendedType);
                        }
                        if (s.ImplementedType != null)
                        {
                            sb.AppendFormat(", {0}", s.ImplementedType);
                        }
                        break;
                    default:
                        break;
                }
            }

            sb.Append(" {");

            headerFile.WriteLine(sb.ToString());

            if (meta.Headers.Count > 0)
            {
                foreach (String s in meta.Headers)
                {
                    headerFile.WriteLine("{0}", s);
                }
            }

            headerFile.WriteLine("\n// Fields");
            headerFile.WriteLine("public:\n");
            foreach (Symbol s in table)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PUBLIC)
                        {
                            headerFile.WriteLine("\t{0} {1};", s.Type, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            headerFile.WriteLine("protected:\n");
            foreach (Symbol s in table)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PROTECTED)
                        {
                            headerFile.WriteLine("\t{0} {1};", s.Type, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            headerFile.WriteLine("private:\n");
            foreach (Symbol s in table)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PRIVATE)
                        {
                            headerFile.WriteLine("\t{0} {1};", s.Type, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            headerFile.WriteLine("\n// Constructors");
            headerFile.WriteLine("public:\n");

            foreach (Symbol s in table)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
                        if (s.Constructor)
                        {
                            headerFile.WriteLine("\t{0} {1}({2});\n", s.ReturnType, s.Name, Parser3.RewriteArgs(s.Arguments));
                            // Destructor
                            headerFile.WriteLine("\t~{0}();\n", s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }


            // public members
            headerFile.WriteLine("public:\n");
            foreach (Symbol s in table)
            {
                switch (s.Scope)
                {
                    case Symbol.PUBLIC:
                        switch (s.Kind)
                        {
                            case Symbol.FUNCTION:
                                if (!s.Constructor)
                                {
                                    headerFile.WriteLine("\t{0} {1}({2});\n", s.ReturnType, s.Name, Parser3.RewriteArgs(s.Arguments));
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            // protected members
            headerFile.WriteLine("protected:\n");
            foreach (Symbol s in table)
            {
                switch (s.Scope)
                {
                    case Symbol.PROTECTED:
                        switch (s.Kind)
                        {
                            case Symbol.FUNCTION:
                                if (!s.Constructor)
                                {
                                    headerFile.WriteLine("\t{0} {1}({2});\n", s.ReturnType, s.Name, Parser3.RewriteArgs(s.Arguments));
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            // protected members methods
            headerFile.WriteLine("private:\n");
            foreach (Symbol s in table)
            {
                switch (s.Scope)
                {
                    case Symbol.PRIVATE:
                        switch (s.Kind)
                        {
                            case Symbol.FUNCTION:
                                if (!s.Constructor)
                                {
                                    headerFile.WriteLine("\t{0} {1}({2});\n", s.ReturnType, s.Name, Parser3.RewriteArgs(s.Arguments));
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            // terminate class
            headerFile.WriteLine("};");

            headerFile.WriteLine("{0} // namespace {1}", "}", meta.Namespace);

            sourceFile.Close();
            headerFile.Close();
        }
    }
}
