using System.Collections.Generic;

namespace Threax.K8sDeploy.Services
{
    public interface ITokenReplacer
    {
        string ReplaceTokens(string text, Dictionary<string, object> parameters, char openingDelimiter = '{', char closingDelimiter = '}');
    }
}