using Guillemets.Ast;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal interface IParser
{
    INode Parse(IToken token);
}