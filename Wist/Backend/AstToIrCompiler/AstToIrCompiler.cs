using System.Text;
using Wist.Backend.IrToAsmCompiler;
using Wist.Backend.IrToAsmCompiler.AsmGenerators;
using Wist.Backend.IrToAsmCompiler.TypeSystem;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Statistics.Logger;
using static Wist.Backend.IrToAsmCompiler.TypeSystem.AsmValueType;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Backend.AstToIrCompiler;

public class AstToIrCompiler(ILogger logger) : IAstToIrCompiler
{
    private readonly CompilerHelper _helper = new();

    private readonly IrImage _image = new([], new DllsManager(), [], []);

    private readonly ImprovedStack<AsmValueType> _stack = new();
    private readonly AstVisitor _visitor = new();

    private IrFunction _function = null!;

    private List<IrInstruction> Instructions => _function.Instructions;

    public IrImage Compile(AstNode root)
    {
        _visitor.Visit(root, DefineStructures, _ => true);
        _visitor.Visit(root, DefineFunctions, _ => true);
        _visitor.Visit(root, HandleNode, _helper.NeedToVisitChildren);

        Log();

        return _image;
    }

    private void DefineFunctions(AstNode node)
    {
        if (node.Lexeme.LexemeType == FunctionDeclaration)
            CreateFunction(node);
    }

    private void DefineStructures(AstNode node)
    {
        if (node.Lexeme.LexemeType != StructDeclaration) return;

        var fields = node.Children[1].Children
            .Where(x => x.Lexeme.LexemeType != Comma)
            .Select(x => new IrStructureField(x.Lexeme.Text, x.Children[0].Lexeme.Text))
            .ToList();
        var structDecl = new IrStructure(node.Children[0].Lexeme.Text, fields);
        _image.Structures.Add(structDecl.Name, structDecl);
    }

    private void Log()
    {
        var logString = new StringBuilder();
        logString.Append("Imports: ");
        logString.AppendJoin(", ", _image.DllsManager.ImportsPaths.Select(x => '"' + x + '"'));
        logString.Append('\n');

        logString.Append('\n');
        foreach (var function in _image.Functions)
        {
            logString.Append(function);
            logString.Append("\n    ");
            logString.AppendJoin("\n    ", function.Instructions);
            logString.Append('\n');
        }

        logString.Remove(logString.Length - 1, 1);
        logger.Log(logString.ToString());
    }

    private void HandleNode(AstNode node)
    {
        var text = node.Lexeme.Text;
        switch (node.Lexeme.LexemeType)
        {
            case Set:
                var type = _function.Locals.First(x => x.Name == node.Children[0].Lexeme.Text).Type;
                _stack.Pop1(type);
                Instructions.Add(new IrInstruction(
                    IrType.SetLocal,
                    type,
                    node.Children[0].Lexeme.Text
                ));
                break;
            case Identifier:
                if (node.Parent?.Lexeme.LexemeType is Set or StructDeclaration or Dot) break;
                if (node.Children.Count > 0 && node.Children[0].Lexeme.LexemeType == LexemeType.Type) break;
                LoadLocal(text);
                break;
            case Import:
                // remove "
                _image.DllsManager.Import(node.Children[0].Lexeme.Text[1..^1]);
                break;
            case GettingRef:
                LoadReference(node.Children[0].Lexeme.Text);
                break;
            case FunctionDeclaration:
                _function = _image.Functions.First(x => x.Name == node.Lexeme.Text);
                DefineLocalsInFunction(node);
                EmitBodyForFunction(node);
                break;
            case LexemeType.Int64:
                Instructions.Add(new IrInstruction(IrType.Push, I64, text.ToLong()));
                _stack.Push(I64);
                break;
            case Character:
                // 1, 'cause first and last symbols are '
                Instructions.Add(new IrInstruction(IrType.Push, I64, (long)text[1]));
                _stack.Push(I64);
                break;
            case LexemeType.Float64:
                Instructions.Add(new IrInstruction(IrType.Push, F64, text.ToDouble()));
                _stack.Push(F64);
                break;
            case Plus:
                Instructions.Add(new IrInstruction(IrType.Add, _stack.Pop2AndPush1Same()));
                break;
            case Minus:
                Instructions.Add(new IrInstruction(IrType.Sub, _stack.Pop2AndPush1Same()));
                break;
            case Mul:
                Instructions.Add(new IrInstruction(IrType.Mul, _stack.Pop2AndPush1Same()));
                break;
            case Div:
                Instructions.Add(new IrInstruction(IrType.Div, _stack.Pop2AndPush1Same()));
                break;
            case Modulo:
                Instructions.Add(new IrInstruction(IrType.Mod, _stack.Pop2AndPush1Same()));
                break;
            case LessThan:
                Instructions.Add(new IrInstruction(IrType.CheckLessThan, _stack.Pop2AndPush1Same()));
                break;
            case LessOrEquals:
                Instructions.Add(new IrInstruction(IrType.CheckLessOrEquals, _stack.Pop2AndPush1Same()));
                break;
            case GreaterThan:
                Instructions.Add(new IrInstruction(IrType.CheckGreaterThan, _stack.Pop2AndPush1Same()));
                break;
            case GreaterOrEquals:
                Instructions.Add(new IrInstruction(IrType.CheckGreaterOrEquals, _stack.Pop2AndPush1Same()));
                break;
            case Equal:
                Instructions.Add(new IrInstruction(IrType.CheckEquality, _stack.Pop2AndPush1Same()));
                break;
            case NotEqual:
                Instructions.Add(new IrInstruction(IrType.CheckInequality, _stack.Pop2AndPush1Same()));
                break;
            case Ret:
                Instructions.Add(new IrInstruction(IrType.Ret, _stack.Pop()));
                break;
            case If:
                var endOfIfElifElse = $"end_if_elif_else_{Guid.NewGuid()}";

                EmitIf(node, endOfIfElifElse);

                Instructions.Add(new IrInstruction(IrType.DefineLabel, Invalid, endOfIfElifElse));
                break;
            case Negation:
                _stack.Pop1(I64);
                Instructions.Add(new IrInstruction(IrType.Negate, Invalid));
                _stack.Push(I64);
                break;
            case Goto:
                Instructions.Add(new IrInstruction(IrType.Br, Invalid, node.Children[0].Lexeme.Text));
                break;
            case Label:
                Instructions.Add(new IrInstruction(IrType.DefineLabel, Invalid, text[..^1]));
                break;
            case WriteToMem:
                Instructions.Add(new IrInstruction(IrType.WriteToMem, _stack.Pop()));
                break;
            case ReadMem:
                _visitor.Visit(node.Children[1], HandleNode, _helper.NeedToVisitChildren);
                Instructions.Add(new IrInstruction(IrType.ReadMem, node.Children[0].Lexeme.Text.ToAsmValueType()));
                break;
            case FunctionCall:
                var fullFunctionName = MakeFullFunctionName(node);

                var sp = _stack.Count;

                var irInstruction = new IrInstruction(IrType.Invalid, I64, 0L);
                Instructions.Add(irInstruction);

                if (node.Parent?.Lexeme.LexemeType == Dot)
                {
                    var local = GetIdentifierFromDotsChain(node);

                    var firstParam = _image.Functions.First(x => x.Name == fullFunctionName).Parameters[0];
                    if (firstParam.TypeAsStr.IsReferenceType())
                        LoadReference(local.Lexeme.Text);
                    else LoadLocal(local.Lexeme.Text);
                }


                _visitor.Visit(node.Children[0], HandleNode, _helper.NeedToVisitChildren);
                var endSp = _stack.Count;
                var needToPushStub = (endSp - sp) % 2 == 1;
                var bytesToDrop = (endSp - sp) * 8 + (needToPushStub ? 8 : 0);
                irInstruction.Instruction = !needToPushStub ? IrType.Nop : IrType.Push;

                var isSharpFunc = _image.DllsManager.HaveFunction(fullFunctionName);
                var isJustFunc = _image.Functions.Any(x => x.Name == fullFunctionName);
                if (!isSharpFunc && !isJustFunc)
                    throw new InvalidOperationException(
                        $"Function {fullFunctionName} is unknown. Did you forgot write namespace?");

                var condNeedToPop = isSharpFunc ? needToPushStub ? 8L : 0L : bytesToDrop;
                Instructions.Add(isSharpFunc
                    ? new IrInstruction(IrType.CallSharpFunction, Invalid, fullFunctionName, condNeedToPop)
                    : new IrInstruction(IrType.CallFunction, Invalid, fullFunctionName, condNeedToPop)
                );

                while (condNeedToPop >= 8)
                {
                    _stack.Pop();
                    condNeedToPop -= 8;
                }

                _stack.Push(isSharpFunc
                    ? _image.DllsManager.GetPointerOf(fullFunctionName).returnType.SharpTypeToAsmValueType()
                    : _image.Functions.First(x => x.Name == fullFunctionName).ReturnType
                );
                break;
            case LexemeType.String:
                var stringPointerName = $"string_{text}_{Guid.NewGuid()}";
                var len = BitConverter.GetBytes((long)text.Length - 2);
                var str = text[1..^1].ToCharArray().SelectMany(BitConverter.GetBytes).ToArray();
                _image.StaticData.Add(stringPointerName, len.Concat(str).ToArray());
                Instructions.Add(new IrInstruction(IrType.GetReference, Invalid, stringPointerName));
                Instructions.Add(new IrInstruction(IrType.Push, I64, 8L));
                Instructions.Add(new IrInstruction(IrType.Add, I64));
                _stack.Push(I64);
                break;
            case Scope:
            case Arrow:
            case Comment:
            case LexemeType.Type:
            case Comma:
            case StructDeclaration:
            case Dot:
                break;
            case As:
            case Alias:
            case Is:
            case LeftPar:
            case RightPar:
            case LeftBrace:
            case RightBrace:
            case LexemeType.Int32:
            case LeftRectangle:
            case RightRectangle:
            case Elif:
            case Else:
            case Spaces:
            case NewLine:
            case For:
            default:
                throw new InvalidOperationException($"{node.Lexeme}");
        }
    }

    private void LoadReference(string localName)
    {
        Instructions.Add(new IrInstruction(IrType.GetReference, Invalid, localName));
        _stack.Push(I64);
    }

    private void LoadLocal(string text)
    {
        var sourceIrLocalInfo = _function.Locals.FirstOrDefault(x => x.Name == text);
        if (sourceIrLocalInfo == null) throw new InvalidOperationException($"{text} local was not found");
        switch (sourceIrLocalInfo)
        {
            case IrRealLocalInfo:
                var type = sourceIrLocalInfo.Type;
                Instructions.Add(new IrInstruction(IrType.LoadLocalValue, type, text));
                _stack.Push(type);
                break;
            case IrLocalAlias localAlias:
                var localAliasRealLocalsInfo = localAlias.RealLocalsInfo;
                for (var index = localAliasRealLocalsInfo.Count - 1; index >= 0; index--)
                {
                    var irLocalInfo = localAliasRealLocalsInfo[index];
                    Instructions.Add(
                        new IrInstruction(IrType.LoadLocalValue, irLocalInfo.Type, irLocalInfo.Name)
                    );
                    _stack.Push(irLocalInfo.Type);
                }

                break;
            default:
                throw new InvalidOperationException();
        }
    }

    private string MakeFullFunctionName(AstNode node)
    {
        if (node.Parent?.Lexeme.LexemeType != Dot)
            return node.Lexeme.Text;


        var parentChild = GetIdentifierFromDotsChain(node);

        var alias = _function.Locals.First(x => x.Name == parentChild.Lexeme.Text) as IrLocalAlias;
        if (alias == null) throw new InvalidOperationException("Left expression above dot must be alias of local");
        return alias.AliasType + LexerData.StructureAndFieldSeparator + node.Lexeme.Text;
    }

    private static AstNode GetIdentifierFromDotsChain(AstNode node)
    {
        var parentChild = node.Parent!.Children[0];
        while (parentChild.Lexeme.LexemeType == Dot)
            parentChild = parentChild.Children[0];
        return parentChild;
    }

    private void EmitIf(AstNode node, string endOfIfElifElse)
    {
        var elseLabel = $"else_{Guid.NewGuid()}";

        _visitor.Visit(node.Children[0], HandleNode, _helper.NeedToVisitChildren);
        Instructions.Add(new IrInstruction(IrType.BrFalse, Invalid, elseLabel));

        _visitor.Visit(node.Children[1], HandleNode, _helper.NeedToVisitChildren);
        Instructions.Add(new IrInstruction(IrType.Br, Invalid, endOfIfElifElse));

        Instructions.Add(new IrInstruction(IrType.DefineLabel, Invalid, elseLabel));

        if (node.Lexeme.LexemeType != If) return;
        foreach (var elifOrElse in node.Children.Skip(2))
            if (elifOrElse.Lexeme.LexemeType == Elif)
                EmitIf(elifOrElse, endOfIfElifElse);
            else _visitor.Visit(elifOrElse.Children[0], HandleNode, _helper.NeedToVisitChildren);

        Instructions.Add(new IrInstruction(IrType.Br, Invalid, endOfIfElifElse));
    }

    private void CreateFunction(AstNode node)
    {
        var parameters = node.Children[0].Children.SelectMany(
                x => GetExpandedLocal(x.Lexeme.Text, x.Children[0].Lexeme.Text)
            )
            .Where(x => x is IrRealLocalInfo)
            .Cast<IrRealLocalInfo>()
            .ToList();

        var returnType = node.Children[2].Lexeme.Text.ToAsmValueType();
        _image.Functions.Add(new IrFunction(node.Lexeme.Text, [], parameters, [], returnType));
    }

    private void EmitBodyForFunction(AstNode node)
    {
        _visitor.Visit(node.Children[3], HandleNode, _helper.NeedToVisitChildren);
    }

    private void DefineLocalsInFunction(AstNode node)
    {
        _visitor.Visit(node.Children[0], DefineLocal, _ => true);
        _visitor.Visit(node.Children[3], DefineLocal, _ => true);
    }

    private void DefineLocal(AstNode node)
    {
        if (node.Lexeme.LexemeType != LexemeType.Type) return;
        if (node.Parent?.Lexeme.LexemeType != Identifier) return;

        var expandedLocal = GetExpandedLocal(node.Parent!.Lexeme.Text, node.Lexeme.Text);
        _function.Locals.AddRange(expandedLocal);
    }

    private List<IIrLocalInfo> GetExpandedLocal(string localName, string type)
    {
        var locals = (List<IIrLocalInfo>) [];

        if (!_image.Structures.TryGetValue(type, out var structure))
        {
            locals.Add(new IrRealLocalInfo(localName, type));
        }
        else
        {
            // why I can't use forward for and forward list without reverse? 

            // reversed to give an opportunity to get fields via +, not -
            // &tuple + 8 <-> tuple:item2
            for (var index = structure.Fields.Count - 1; index >= 0; index--)
            {
                var field = structure.Fields[index];
                var fieldName = localName + LexerData.StructureAndFieldSeparator + field.Name;
                locals.Add(new IrRealLocalInfo(fieldName, field.TypeAsStr));
            }

            // add an alias for all fields
            // vec -> vec:x, vec:y, vec:z
            var realLocalsInfo = (List<IIrLocalInfo>) [..locals];
            realLocalsInfo.Reverse();
            locals.Add(new IrLocalAlias(localName, realLocalsInfo, type));
        }

        return locals;
    }
}