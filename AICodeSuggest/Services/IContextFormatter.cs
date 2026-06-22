using System.Collections.Generic;
using System.Text;

namespace AICodeSuggest.Services
{
    public interface IContextFormatter
    {
        string FormatContext(Models.CodeContext context, Options.GeneralOptions options);
    }
}
