module Types

type GrammarAST<'a> =
    | Word     of string * string * 'a option
    | Optional of GrammarAST<'a>
    | Sequence of GrammarAST<'a> list
    | Choice   of GrammarAST<'a> list
    | Dictation

type Mode = Normal | VisualMode | Replace | Insert | Windows

