using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Android_Resource_Handlers
{
    public class Language
    {
        public string Code { get; private set; }
        public string Name { get; private set; }

        public Language(string code, string name)
        {
            this.Code = code;
            this.Name = name;
        }
    }
}
