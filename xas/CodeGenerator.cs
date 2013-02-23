using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.redwine.xas
{
    class CodeGenerator : ICodeGenerator
    {
        private string _basename;

        public CodeGenerator() 
        {
        }

        public string Basename
        {
            set
            {
                _basename = value;
            }
            get
            {
                return _basename;
            }
        }

        public virtual void generate(string outputpath, Metadata m, SymbolTable s)
        {

        }
    }
}
