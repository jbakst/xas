using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.redwine.xas
{
    class TypeMapping
    {
        public static string Convert(string value)
        {
            string type;
            if (value == "Boolean")
            {
                type = "bool";
            }
            else if (value == "*")
            {
                type = "void*";
            }
            else if (value == "int")
            {
                type = "int";
            }
            else if (value == "Number")
            {
                type = "long";
            }
            else if (value == "String")
            {
                type = "char *";
            }
            else
            {
                type = value;
            }

            return type;
        }
    }
}
