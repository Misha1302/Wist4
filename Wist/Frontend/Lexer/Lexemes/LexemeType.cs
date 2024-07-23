﻿namespace Wist.Frontend.Lexer.Lexemes;

public enum LexemeType
{
    Import,
    String,
    As,
    Identifier,
    Alias,
    Is,
    NativeType,
    PointerType,
    Set,
    FunctionCall,
    LeftPar,
    RightPar,
    LeftBrace,
    RightBrace,
    Int32,
    Int64,
    LeftRectangle,
    RightRectangle,
    Dot,
    Plus,
    Minus,
    Mul,
    Div,
    If,
    Elif,
    Else,
    Label,
    Goto,
    Ret,
    Spaces,
    NewLine,
    Comma,
    LessThan,
    LessOrEquals,
    GreaterThan,
    GreaterOrEquals,
    Equal,
    NotEqual,


    Scope
}