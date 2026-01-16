using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Postica.BindingSystem.Utility
{
    /// <summary>
    /// This class is used to serialize mathematical expressions and to evaluate them with variables.
    /// </summary>
    [Serializable]
    public class MathExpressionValue
    {
        public string expression;
        public List<Bind<float>> variables = new();

        [SerializeField]
        private string _label;
        [SerializeField]
        private string[] _inputVariables;
        
        private double[] _tempInputs = new double[1];
        
        private bool? _canBeCached;
        private ParsedExpression _parsedExpression;
        private ParsingException _parsingException;
        private readonly Dictionary<string, double> _variablesDict = new(StringComparer.OrdinalIgnoreCase);
        
        public string Label => _label;
        public string[] InputVariables => _inputVariables;

        public bool HasDynamicVariables => _canBeCached == false || variables.Any(v => v.IsBound);

        public MathExpressionValue(string label, params string[] inputVariables)
        {
            _label = label;
            _inputVariables = inputVariables;
        }
        
        public double Evaluate(double input = double.NaN)
        {
            _tempInputs[0] = input;
            return Evaluate(_tempInputs);
        }
        
        public double Evaluate(double[] inputs)
        {
            PrepareExpression(inputs);
            return _parsedExpression.Evaluate(_variablesDict);
        }
        
        public double EvaluateFast(double input = double.NaN)
        {
            _tempInputs[0] = input;
            return EvaluateFast(_tempInputs);
        }
        
        public double EvaluateFast(double[] inputs)
        {
            PrepareExpression(inputs);
            return _parsedExpression.EvaluateFast(_variablesDict);
        }

        private void PrepareExpression(double[] inputs)
        {
            _parsedExpression ??= MathExpressionParser.GetOrParse(expression, out _parsingException);

            if (_parsingException != null)
            {
                throw new Exception($"Error parsing expression: {_parsingException.Message}");
            }

            _canBeCached = _parsedExpression.Evaluator.CanBeCached;

            _variablesDict.Clear();
            var maxVariables = Mathf.Min(variables.Count, _parsedExpression.Variables.Count);
            for (int i = 0; i < maxVariables; i++)
            {
                _variablesDict[_parsedExpression.Variables[i]] = variables[i].Value;
            }
            
            var minLength = Mathf.Min(inputs.Length, _inputVariables.Length);
            for (int i = 0; i < minLength; i++)
            {
                _variablesDict[_inputVariables[i]] = inputs[i];
            }
            if(inputs.Length > 0 && !double.IsNaN(inputs[0]))
            {
                _variablesDict["input"] = inputs[0];
            }
        }

        public void Invalidate() => _parsedExpression = null;
    }
    
    // Exception class for parsing errors
    public class ParsingException : Exception
    {
        public int Position { get; }
        public string ErrorType { get; }

        public ParsingException(int position, string message, string errorType)
            : base($"Error at position {position}: {message}")
        {
            Position = position;
            ErrorType = errorType;
        }
    }

    // Base class for all expressions
    public abstract class MathExpression
    {
        public abstract double Evaluate(Dictionary<string, double> variables);
    }

    // Class for constant values
    public class ConstantExpression : MathExpression
    {
        private readonly double _value;

        public ConstantExpression(double value)
        {
            _value = value;
        }

        public override double Evaluate(Dictionary<string, double> variables)
        {
            return _value;
        }
    }
    
    // Class for constant values
    public class SystemVariableExpression : MathExpression
    {
        private readonly Func<double> _value;

        public SystemVariableExpression(Func<double> value)
        {
            _value = value;
        }

        public override double Evaluate(Dictionary<string, double> variables)
        {
            return _value();
        }
    }

    // Class for variables
    public class VariableExpression : MathExpression
    {
        private readonly string _name;

        public VariableExpression(string name)
        {
            _name = name.ToLowerInvariant();
        }

        public override double Evaluate(Dictionary<string, double> variables)
        {
            if (!variables.TryGetValue(_name, out double value))
                throw new Exception($"Variable '{_name}' not found.");
            return value;
        }
    }

    // Class for binary operations
    public class BinaryExpression : MathExpression
    {
        private readonly MathExpression _left;
        private readonly MathExpression _right;
        private readonly Func<double, double, double> _operation;

        public BinaryExpression(MathExpression left, MathExpression right, Func<double, double, double> operation)
        {
            _left = left;
            _right = right;
            _operation = operation;
        }

        public override double Evaluate(Dictionary<string, double> variables)
        {
            double leftVal = _left.Evaluate(variables);
            double rightVal = _right.Evaluate(variables);
            return _operation(leftVal, rightVal);
        }
    }

    // Class for unary operations
    public class UnaryExpression : MathExpression
    {
        private readonly MathExpression _operand;
        private readonly Func<double, double> _operation;

        public UnaryExpression(MathExpression operand, Func<double, double> operation)
        {
            _operand = operand;
            _operation = operation;
        }

        public override double Evaluate(Dictionary<string, double> variables)
        {
            double val = _operand.Evaluate(variables);
            return _operation(val);
        }
    }

    // Delegate for functions
    public delegate double FunctionDelegate(List<double> args);

    // Function registry to manage supported functions
    public static class FunctionRegistry
    {
        internal static readonly Dictionary<string, FunctionDelegate> Functions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "sin", args => Math.Sin(args[0]) },
            { "cos", args => Math.Cos(args[0]) },
            { "tan", args => Math.Tan(args[0]) },
            { "asin", args => Math.Asin(args[0]) },
            { "acos", args => Math.Acos(args[0]) },
            { "atan", args => Math.Atan(args[0]) },
            { "sqrt", args => Math.Sqrt(args[0]) },
            { "abs", args => Math.Abs(args[0]) },
            { "log", args => args.Count == 1 ? Math.Log(args[0]) : Math.Log(args[0], args[1]) },
            { "mod", args => args[0] % args[1] },
            { "clamp", args => Math.Min(Math.Max(args[0], args[1]), args[2]) },
            { "max", args => args.Max() },
            { "min", args => args.Min() },
            { "lerp", args => args[0] + (args[1] - args[0]) * args[2] },
            { "round", args => Math.Round(args[0]) },
            { "floor", args => Math.Floor(args[0]) },
            { "ceil", args => Math.Ceiling(args[0]) },
            { "pow", args => Math.Pow(args[0], args[1]) },
            { "exp", args => Math.Exp(args[0]) },
            { "sign", args => Math.Sign(args[0]) },
            { "rand", args => UnityEngine.Random.Range((float)args[0], (float)args[1]) },
            // Add more functions as needed
        };

        public static bool TryGetFunction(string name, out FunctionDelegate function)
        {
            return Functions.TryGetValue(name, out function);
        }
    }

    // Class for function calls
    public class FunctionExpression : MathExpression
    {
        private readonly string _functionName;
        private readonly List<MathExpression> _arguments;
        private readonly List<double> _argValues = new(16);

        public FunctionExpression(string functionName, List<MathExpression> arguments)
        {
            _functionName = functionName.ToLowerInvariant();
            _arguments = arguments;
        }

        public override double Evaluate(Dictionary<string, double> variables)
        {
            if (!FunctionRegistry.TryGetFunction(_functionName, out FunctionDelegate func))
                throw new Exception($"Function '{_functionName}' is not defined.");

            _argValues.Clear();
            foreach (var arg in _arguments)
            {
                _argValues.Add(arg.Evaluate(variables));
            }

            return func(_argValues);
        }
    }

    // Tokenizer components

    public enum TokenType
    {
        Number,
        Identifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        Power,
        OpenParen,
        CloseParen,
        Comma,
        EndOfInput,
        Unknown
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Text { get; }
        public int Position { get; }

        public Token(TokenType type, string text, int position)
        {
            Type = type;
            Text = text;
            Position = position;
        }
    }

    public class Tokenizer
    {
        private readonly string _expression;
        private int _position;
        private readonly int _length;
        
        internal List<Token> Tokens { get; } = new();

        public Tokenizer(string expression)
        {
            _expression = expression;
            _length = _expression.Length;
            _position = 0;
        }
        
        public Token GetNextToken()
        {
            SkipWhitespace();

            if (_position >= _length)
            {
                return NewToken(TokenType.EndOfInput, "", _position);
            }

            char current = _expression[_position];

            if (char.IsDigit(current) || current == '.')
            {
                int start = _position;
                bool hasDot = false;
                while (_position < _length && (char.IsDigit(_expression[_position]) || _expression[_position] == '.'))
                {
                    if (_expression[_position] == '.')
                    {
                        if (hasDot)
                            break; // Second dot encountered
                        hasDot = true;
                    }

                    _position++;
                }

                string number = _expression.Substring(start, _position - start);
                return NewToken(TokenType.Number, number, start);
            }

            if (char.IsLetter(current) || current == '_')
            {
                int start = _position;
                while (_position < _length &&
                       (char.IsLetterOrDigit(_expression[_position]) || _expression[_position] == '_'))
                {
                    _position++;
                }

                string identifier = _expression.Substring(start, _position - start);
                return NewToken(TokenType.Identifier, identifier, start);
            }

            // Single character tokens
            switch (current)
            {
                case '+':
                    _position++;
                    return NewToken(TokenType.Plus, "+", _position - 1);
                case '-':
                    _position++;
                    return NewToken(TokenType.Minus, "-", _position - 1);
                case '*':
                    _position++;
                    return NewToken(TokenType.Multiply, "*", _position - 1);
                case '/':
                    _position++;
                    return NewToken(TokenType.Divide, "/", _position - 1);
                case '^':
                    _position++;
                    return NewToken(TokenType.Power, "^", _position - 1);
                case '(':
                    _position++;
                    return NewToken(TokenType.OpenParen, "(", _position - 1);
                case ')':
                    _position++;
                    return NewToken(TokenType.CloseParen, ")", _position - 1);
                case ',':
                    _position++;
                    return NewToken(TokenType.Comma, ",", _position - 1);
                default:
                    _position++;
                    return NewToken(TokenType.Unknown, current.ToString(), _position - 1);
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _length && char.IsWhiteSpace(_expression[_position]))
            {
                _position++;
            }
        }
        
        private Token NewToken(TokenType type, string text, int position)
        {
            Token token = new Token(type, text, position);
#if UNITY_EDITOR
            Tokens.Add(token);
#endif
            return token;
        }
    }

    // Parser to convert tokens into an expression tree

    public class Parser
    {
        private readonly Tokenizer _tokenizer;
        private Token _currentToken;
        
        internal Tokenizer Tokenizer => _tokenizer;

        public bool CanBeCached { get; private set; } = true;

        internal static readonly Dictionary<string, double> Constants = new(StringComparer.OrdinalIgnoreCase)
        {
            { "pi", Math.PI },
            { "e", Math.E },
            // Add more constants if needed
        };
        
        internal static readonly Dictionary<string, Func<double>> SystemVariables = new(StringComparer.OrdinalIgnoreCase)
        {
            { "t", () => Time.time },
            { "dt", () => Time.deltaTime },
            { "fps", () => 1 / Time.deltaTime },
            { "frame", () => Time.frameCount },
            { "rand", () => UnityEngine.Random.value },
            { "ft", () => Time.fixedTime },
            { "fdt", () => Time.fixedDeltaTime },
            // Add more constants if needed
        };

        public Parser(string expression)
        {
            _tokenizer = new Tokenizer(expression);
            _currentToken = _tokenizer.GetNextToken();
        }
        
        public MathExpression Parse() => Parse(out _);

        public MathExpression Parse(out List<string> variables)
        {
            var unorderedVariables = new HashSet<string>();
            MathExpression expr = ParseExpression(unorderedVariables);
            if (_currentToken.Type != TokenType.EndOfInput)
                throw new ParsingException(_currentToken.Position, "Unexpected characters at end of expression.",
                    "SyntaxError");
            
            variables = unorderedVariables.ToList();
            variables.Sort();
            return expr;
        }

        private MathExpression ParseExpression(HashSet<string> variables, int parentPrecedence = 0)
        {
            MathExpression left = ParseUnary(variables);

            while (true)
            {
                int precedence = GetPrecedence(_currentToken.Type);
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                TokenType op = _currentToken.Type;
                _currentToken = _tokenizer.GetNextToken();
                MathExpression right = ParseExpression(variables, precedence);
                left = CreateBinaryExpression(left, right, op);
            }

            return left;
        }

        private MathExpression ParseUnary(HashSet<string> variables)
        {
            if (_currentToken.Type == TokenType.Plus)
            {
                _currentToken = _tokenizer.GetNextToken();
                return ParseUnary(variables);
            }
            else if (_currentToken.Type == TokenType.Minus)
            {
                _currentToken = _tokenizer.GetNextToken();
                MathExpression operand = ParseUnary(variables);
                return new UnaryExpression(operand, a => -a);
            }
            else
            {
                return ParsePrimary(variables);
            }
        }

        private MathExpression ParsePrimary(HashSet<string> variables)
        {
            Token token = _currentToken;

            switch (token.Type)
            {
                case TokenType.Number:
                    _currentToken = _tokenizer.GetNextToken();
                    if (!double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out double number))
                        throw new ParsingException(token.Position, $"Invalid number '{token.Text}'.",
                            "NumberFormatError");
                    return new ConstantExpression(number);

                case TokenType.Identifier:
                    _currentToken = _tokenizer.GetNextToken();
                    string identifier = token.Text.ToLowerInvariant();

                    if (_currentToken.Type == TokenType.OpenParen)
                    {
                        // Function call
                        return ParseFunction(variables, identifier);
                    }
                    else
                    {
                        // Variable or constant
                        if (Constants.TryGetValue(identifier, out double constValue))
                        {
                            return new ConstantExpression(constValue);
                        }
                        
                        if (SystemVariables.TryGetValue(identifier, out Func<double> systemVariableValue))
                        {
                            CanBeCached = false;
                            return new SystemVariableExpression(systemVariableValue);
                        }

                        variables.Add(identifier);
                        return new VariableExpression(identifier);
                    }

                case TokenType.OpenParen:
                    _currentToken = _tokenizer.GetNextToken();
                    MathExpression expr = ParseExpression(variables);
                    if (_currentToken.Type != TokenType.CloseParen)
                        throw new ParsingException(_currentToken.Position, "Expected closing parenthesis.",
                            "SyntaxError");
                    _currentToken = _tokenizer.GetNextToken();
                    return expr;

                default:
                    throw new ParsingException(token.Position, $"Unexpected token '{token.Text}'.", "SyntaxError");
            }
        }

        private MathExpression ParseFunction(HashSet<string> variables, string functionName)
        {
            // Assume current token is '('
            _currentToken = _tokenizer.GetNextToken(); // Skip '('

            List<MathExpression> arguments = new List<MathExpression>();
            if (_currentToken.Type != TokenType.CloseParen)
            {
                while (true)
                {
                    arguments.Add(ParseExpression(variables));
                    if (_currentToken.Type == TokenType.Comma)
                    {
                        _currentToken = _tokenizer.GetNextToken();
                        if (_currentToken.Type == TokenType.CloseParen)
                            throw new ParsingException(_currentToken.Position, "Trailing comma in argument list.",
                                "SyntaxError");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (_currentToken.Type != TokenType.CloseParen)
                throw new ParsingException(_currentToken.Position,
                    "Expected closing parenthesis after function arguments.", "SyntaxError");
            _currentToken = _tokenizer.GetNextToken(); // Skip ')'

            return new FunctionExpression(functionName, arguments);
        }

        private int GetPrecedence(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.Power => 4,
                TokenType.Multiply or TokenType.Divide => 3,
                TokenType.Plus or TokenType.Minus => 2,
                _ => 0
            };
        }

        private BinaryExpression CreateBinaryExpression(MathExpression left, MathExpression right, TokenType op)
        {
            return op switch
            {
                TokenType.Plus => new BinaryExpression(left, right, (a, b) => a + b),
                TokenType.Minus => new BinaryExpression(left, right, (a, b) => a - b),
                TokenType.Multiply => new BinaryExpression(left, right, (a, b) => a * b),
                TokenType.Divide => new BinaryExpression(left, right, (a, b) => a / b),
                TokenType.Power => new BinaryExpression(left, right, Math.Pow),
                _ => throw new ParsingException(_currentToken.Position, $"Unsupported binary operator '{op}'.",
                    "SyntaxError")
            };
        }
    }

    // Expression Evaluator with Caching and Thread-Safety

    public class MathExpressionEvaluator
    {
        private readonly MathExpression _expression;
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private Dictionary<string, double> _lastVariables;
        private double _lastResult;
        private bool _hasCachedResult;
        private readonly bool _canBeCached;
        
        public bool CanBeCached => _canBeCached;

        public MathExpressionEvaluator(MathExpression expression, bool canBeCached = true)
        {
            _expression = expression;
            _canBeCached = canBeCached;
        }

        /// <summary>
        /// Thread-safe evaluation of the expression with the given variables.
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public double Evaluate(Dictionary<string, double> variables)
        {
            // Copy variables to ensure thread safety
            var varsCopy = new Dictionary<string, double>(variables, StringComparer.OrdinalIgnoreCase);

            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (_canBeCached && _hasCachedResult && AreVariablesEqual(_lastVariables, varsCopy))
                {
                    return _lastResult;
                }

                _cacheLock.EnterWriteLock();
                try
                {
                    double result = _expression.Evaluate(varsCopy);
                    _lastResult = result;
                    _lastVariables = varsCopy;
                    _hasCachedResult = true;
                    return result;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
        }
        
        /// <summary>
        /// Non-thread-safe but faster evaluation of the expression with the given variables.
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public double EvaluateFast(Dictionary<string, double> variables)
        {
            return _expression.Evaluate(variables);
        }

        private bool AreVariablesEqual(Dictionary<string, double> vars1, Dictionary<string, double> vars2)
        {
            if (vars1 == null && vars2 == null)
                return true;
            if (vars1 == null || vars2 == null)
                return false;
            if (vars1.Count != vars2.Count)
                return false;
            foreach (var kvp in vars1)
            {
                if (!vars2.TryGetValue(kvp.Key, out double value))
                    return false;
                if (!kvp.Value.Equals(value))
                    return false;
            }

            return true;
        }
    }

    // Parser Factory with Caching for Immutable Expressions

    public static class MathExpressionParser
    {
        private static readonly ConcurrentDictionary<string, ParsedExpression> _expressionCache = new();

        public static ParsedExpression Parse(string expressionText, out ParsingException parsingException)
        {
            parsingException = null;
            try
            {
                var parser = new Parser(expressionText);
                MathExpression expr = parser.Parse(out List<string> variables);
                var evaluator = new MathExpressionEvaluator(expr, parser.CanBeCached);
                var parsedExpr = new ParsedExpression(expressionText, expr, evaluator, variables);
                _expressionCache[expressionText] = parsedExpr;
                return parsedExpr;
            }
            catch (ParsingException ex)
            {
                parsingException = ex;
                return null;
            }
            catch (Exception ex)
            {
                parsingException = new ParsingException(0, ex.Message, "UnknownError");
                return null;
            }
        }

        public static ParsedExpression GetOrParse(string expressionText, out ParsingException parsingException)
        {
            if (_expressionCache.TryGetValue(expressionText, out ParsedExpression cached))
            {
                parsingException = null;
                return cached;
            }

            return Parse(expressionText, out parsingException);
        }
    }

    // Wrapper class for parsed expressions
    public class ParsedExpression
    {
        public string ExpressionText { get; }
        public MathExpression ExpressionTree { get; }
        public MathExpressionEvaluator Evaluator { get; }
        public IReadOnlyList<string> Variables { get; }

        public ParsedExpression(string expressionText, MathExpression expressionTree, MathExpressionEvaluator evaluator, IReadOnlyList<string> variables)
        {
            ExpressionText = expressionText;
            ExpressionTree = expressionTree;
            Evaluator = evaluator;
            Variables = variables;
        }

        public double Evaluate(Dictionary<string, double> variables)
        {
            return Evaluator.Evaluate(variables);
        }
        
        public double EvaluateFast(Dictionary<string, double> variables)
        {
            return Evaluator.EvaluateFast(variables);
        }
    }
}