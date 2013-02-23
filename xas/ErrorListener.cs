using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.StringTemplate;


namespace com.redwine.xas
{
    class ErrorListener : ITemplateErrorListener
    {
        public void Warning(string msg)
        {

        }

        public void Error(string msg, Exception e)
        {

        }

        public void CompiletimeError(Antlr4.StringTemplate.Misc.TemplateMessage msg)
        {
            throw new NotImplementedException();
        }

        public void IOError(Antlr4.StringTemplate.Misc.TemplateMessage msg)
        {
            throw new NotImplementedException();
        }

        public void InternalError(Antlr4.StringTemplate.Misc.TemplateMessage msg)
        {
            throw new NotImplementedException();
        }

        public void RuntimeError(Antlr4.StringTemplate.Misc.TemplateMessage msg)
        {
            throw new NotImplementedException();
        }
    }
}
