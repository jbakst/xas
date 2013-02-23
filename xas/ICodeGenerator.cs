using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.redwine.xas
{
    interface ICodeGenerator
    {
        void generate(string outputpath, Metadata m, SymbolTable s);
    }
}
