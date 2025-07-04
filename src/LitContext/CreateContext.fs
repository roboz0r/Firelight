namespace Lit.Context

/// The Context type defines a type brand to associate a key value with the context value type
[<AllowNullLiteral>]
type Context<'ValueType> =
    abstract __context__: 'ValueType with get, set
