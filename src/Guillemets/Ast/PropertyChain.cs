using System.Collections.ObjectModel;

namespace Guillemets.Ast;

internal class PropertyChain(IList<string> properties)
    : ReadOnlyCollection<string>(properties);