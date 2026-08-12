using Guillemets.Filters;

namespace Guillemets.Ast;

internal record FilterNode(IFilter Filter, string? Arg);