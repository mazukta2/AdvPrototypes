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
    /// This class is used to serialize boolean expressions and to evaluate them with boolean variables.
    /// Supports operators in both symbolic (&&, ||, !, ==, !=) and word forms (and, or, not, equals, notequals).
    /// </summary>
    [Serializable]
    public class BoolExpressionValue
    {
        public string expression;
        public List<Bind<bool>> variables = new();

        [SerializeField]
        private string _label;
        [SerializeField]
        private string[] _inputVariables;
        
        private bool[] _tempInputs = new bool[1];
        
        private ParsedBoolExpression _parsedExpression;
        private ParsingException _parsingException;
        private readonly Dictionary<string, bool> _variablesDict = new(StringComparer.OrdinalIgnoreCase);
        
        public string Label => _label;
        public string[] InputVariables => _inputVariables;
        
        public BoolExpressionValue(string label, params string[] inputVariables)
        {
            _label = label;
            _inputVariables = inputVariables;
        }

        public bool HasDynamicVariables => variables.Any(v => v.IsBound);
        
        public bool Evaluate(bool input = false)
        {
            _tempInputs[0] = input;
            return Evaluate(_tempInputs);
        }
        
        public bool Evaluate(bool[] inputs)
        {
            PrepareExpression(inputs);
            return _parsedExpression.Evaluate(_variablesDict);
        }
        
        public bool EvaluateFast(bool input = false)
        {
            _tempInputs[0] = input;
            return EvaluateFast(_tempInputs);
        }
        
        public bool EvaluateFast(bool[] inputs)
        {
            PrepareExpression(inputs);
            return _parsedExpression.EvaluateFast(_variablesDict);
        }

        private void PrepareExpression(bool[] inputs)
        {
            _parsedExpression ??= BoolExpressionParser.GetOrParse(expression, out _parsingException);

            if (_parsingException != null)
            {
                throw new Exception($"Error parsing boolean expression: {_parsingException.Message}");
            }

            _variablesDict.Clear();
            int maxVariables = Mathf.Min(variables.Count, _parsedExpression.Variables.Count);
            for (int i = 0; i < maxVariables; i++)
            {
                _variablesDict[_parsedExpression.Variables[i]] = variables[i].Value;
            }
            
            int minLength = Mathf.Min(inputs.Length, _inputVariables.Length);
            for (int i = 0; i < minLength; i++)
            {
                _variablesDict[_inputVariables[i]] = inputs[i];
            }
            if (inputs.Length > 0)
            {
                _variablesDict["input"] = inputs[0];
            }
        }

        public void Invalidate() => _parsedExpression = null;
    }

    // --------------------- Boolean Expression Tree -----------------------------

    public abstract class BoolExpression
    {
        public abstract bool Evaluate(Dictionary<string, bool> variables);
    }

    public class ConstantBoolExpression : BoolExpression
    {
        private readonly bool _value;

        public ConstantBoolExpression(bool value)
        {
            _value = value;
        }

        public override bool Evaluate(Dictionary<string, bool> variables)
        {
            return _value;
        }
    }

    public class VariableBoolExpression : BoolExpression
    {
        private readonly string _name;

        public VariableBoolExpression(string name)
        {
            _name = name.ToLowerInvariant();
        }

        public override bool Evaluate(Dictionary<string, bool> variables)
        {
            if (!variables.TryGetValue(_name, out bool value))
                throw new Exception($"Variable '{_name}' not found.");
            return value;
        }
    }

    public class UnaryBoolExpression : BoolExpression
    {
        private readonly BoolExpression _operand;
        private readonly Func<bool, bool> _operation;

        public UnaryBoolExpression(BoolExpression operand, Func<bool, bool> operation)
        {
            _operand = operand;
            _operation = operation;
        }

        public override bool Evaluate(Dictionary<string, bool> variables)
        {
            return _operation(_operand.Evaluate(variables));
        }
    }

    public class BinaryBoolExpression : BoolExpression
    {
        private readonly BoolExpression _left;
        private readonly BoolExpression _right;
        private readonly Func<bool, bool, bool> _operation;

        public BinaryBoolExpression(BoolExpression left, BoolExpression right, Func<bool, bool, bool> operation)
        {
            _left = left;
            _right = right;
            _operation = operation;
        }

        public override bool Evaluate(Dictionary<string, bool> variables)
        {
            return _operation(_left.Evaluate(variables), _right.Evaluate(variables));
        }
    }

    // --------------------- Tokenizer for Boolean Expressions -----------------------------

    public enum BoolTokenType
    {
        Identifier,
        BooleanLiteral, // for "true" and "false"
        And,          // && or "and"
        Or,           // || or "or"
        Not,          // ! or "not"
        Equals,       // == or "equals"
        NotEquals,    // != or "notequals"/"neq"
        OpenParen,
        CloseParen,
        EndOfInput,
        Unknown
    }

    public class BoolToken
    {
        public BoolTokenType Type { get; }
        public string Text { get; }
        public int Position { get; }

        public BoolToken(BoolTokenType type, string text, int position)
        {
            Type = type;
            Text = text;
            Position = position;
        }
    }

    public class BoolTokenizer
    {
        private readonly string _expression;
        private int _position;
        private readonly int _length;

        internal List<BoolToken> Tokens { get; } = new();

        public BoolTokenizer(string expression)
        {
            _expression = expression;
            _length = _expression.Length;
            _position = 0;
        }

        public BoolToken GetNextToken()
        {
            SkipWhitespace();

            if (_position >= _length)
            {
                return NewToken(BoolTokenType.EndOfInput, "", _position);
            }

            char current = _expression[_position];

            // Check for multi-character operators first (&&, ||, !=, ==)
            if (current == '&')
            {
                int start = _position;
                _position++;
                if (_position < _length && _expression[_position] == '&')
                {
                    _position++;
                    return NewToken(BoolTokenType.And, "&&", start);
                }
                else
                {
                    return NewToken(BoolTokenType.Unknown, "&", start);
                }
            }
            if (current == '|')
            {
                int start = _position;
                _position++;
                if (_position < _length && _expression[_position] == '|')
                {
                    _position++;
                    return NewToken(BoolTokenType.Or, "||", start);
                }
                else
                {
                    return NewToken(BoolTokenType.Unknown, "|", start);
                }
            }
            if (current == '!')
            {
                int start = _position;
                _position++;
                if (_position < _length && _expression[_position] == '=')
                {
                    _position++;
                    return NewToken(BoolTokenType.NotEquals, "!=", start);
                }
                else
                {
                    return NewToken(BoolTokenType.Not, "!", start);
                }
            }
            if (current == '=')
            {
                int start = _position;
                _position++;
                if (_position < _length && _expression[_position] == '=')
                {
                    _position++;
                    return NewToken(BoolTokenType.Equals, "==", start);
                }
                else
                {
                    return NewToken(BoolTokenType.Unknown, "=", start);
                }
            }
            if (current == '(')
            {
                _position++;
                return NewToken(BoolTokenType.OpenParen, "(", _position - 1);
            }
            if (current == ')')
            {
                _position++;
                return NewToken(BoolTokenType.CloseParen, ")", _position - 1);
            }

            // Identifiers or keywords
            if (char.IsLetter(current) || current == '_')
            {
                int start = _position;
                while (_position < _length &&
                       (char.IsLetterOrDigit(_expression[_position]) || _expression[_position] == '_'))
                {
                    _position++;
                }

                string word = _expression.Substring(start, _position - start);
                switch (word.ToLowerInvariant())
                {
                    case "and":
                        return NewToken(BoolTokenType.And, word, start);
                    case "or":
                        return NewToken(BoolTokenType.Or, word, start);
                    case "not":
                        return NewToken(BoolTokenType.Not, word, start);
                    case "true":
                        return NewToken(BoolTokenType.BooleanLiteral, "true", start);
                    case "false":
                        return NewToken(BoolTokenType.BooleanLiteral, "false", start);
                    case "equals":
                        return NewToken(BoolTokenType.Equals, word, start);
                    case "notequals":
                    case "not_equal":
                    case "neq":
                        return NewToken(BoolTokenType.NotEquals, word, start);
                    default:
                        return NewToken(BoolTokenType.Identifier, word, start);
                }
            }

            // Unknown character
            int pos = _position;
            _position++;
            return NewToken(BoolTokenType.Unknown, current.ToString(), pos);
        }

        private void SkipWhitespace()
        {
            while (_position < _length && char.IsWhiteSpace(_expression[_position]))
            {
                _position++;
            }
        }
        
        private BoolToken NewToken(BoolTokenType type, string text, int position)
        {
            BoolToken token = new BoolToken(type, text, position);
#if UNITY_EDITOR
            Tokens.Add(token);
#endif
            return token;
        }
    }

    // --------------------- Parser for Boolean Expressions -----------------------------

    public class BoolParser
    {
        private readonly BoolTokenizer _tokenizer;
        private BoolToken _currentToken;
        private readonly HashSet<string> _variables = new(StringComparer.OrdinalIgnoreCase);

        public bool CanBeCached { get; private set; } = true;
        
        internal BoolTokenizer Tokenizer => _tokenizer;

        public BoolParser(string expression)
        {
            _tokenizer = new BoolTokenizer(expression);
            _currentToken = _tokenizer.GetNextToken();
        }

        public BoolExpression Parse() => ParseExpression(0);

        public BoolExpression Parse(out List<string> variables)
        {
            BoolExpression expr = ParseExpression(0);
            if (_currentToken.Type != BoolTokenType.EndOfInput)
                throw new ParsingException(_currentToken.Position, "Unexpected characters at end of expression.", "SyntaxError");
            variables = _variables.ToList();
            variables.Sort();
            return expr;
        }

        private BoolExpression ParseExpression(int parentPrecedence)
        {
            BoolExpression left = ParseUnary();

            while (true)
            {
                int precedence = GetPrecedence(_currentToken.Type);
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                BoolToken op = _currentToken;
                _currentToken = _tokenizer.GetNextToken();
                BoolExpression right = ParseExpression(precedence);
                left = CreateBinaryExpression(left, right, op);
            }

            return left;
        }

        private BoolExpression ParseUnary()
        {
            if (_currentToken.Type == BoolTokenType.Not)
            {
                _currentToken = _tokenizer.GetNextToken();
                BoolExpression operand = ParseUnary();
                return new UnaryBoolExpression(operand, a => !a);
            }
            return ParsePrimary();
        }

        private BoolExpression ParsePrimary()
        {
            BoolToken token = _currentToken;

            switch (token.Type)
            {
                case BoolTokenType.BooleanLiteral:
                    _currentToken = _tokenizer.GetNextToken();
                    bool value = token.Text.ToLowerInvariant() == "true";
                    return new ConstantBoolExpression(value);

                case BoolTokenType.Identifier:
                    _currentToken = _tokenizer.GetNextToken();
                    string identifier = token.Text.ToLowerInvariant();
                    _variables.Add(identifier);
                    return new VariableBoolExpression(identifier);

                case BoolTokenType.OpenParen:
                    _currentToken = _tokenizer.GetNextToken();
                    BoolExpression expr = ParseExpression(0);
                    if (_currentToken.Type != BoolTokenType.CloseParen)
                        throw new ParsingException(_currentToken.Position, "Expected closing parenthesis.", "SyntaxError");
                    _currentToken = _tokenizer.GetNextToken();
                    return expr;

                default:
                    throw new ParsingException(token.Position, $"Unexpected token '{token.Text}'.", "SyntaxError");
            }
        }

        private int GetPrecedence(BoolTokenType type)
        {
            return type switch
            {
                BoolTokenType.Equals or BoolTokenType.NotEquals => 3,
                BoolTokenType.And => 2,
                BoolTokenType.Or => 1,
                _ => 0,
            };
        }

        private BinaryBoolExpression CreateBinaryExpression(BoolExpression left, BoolExpression right, BoolToken op)
        {
            return op.Type switch
            {
                BoolTokenType.And => new BinaryBoolExpression(left, right, (a, b) => a && b),
                BoolTokenType.Or => new BinaryBoolExpression(left, right, (a, b) => a || b),
                BoolTokenType.Equals => new BinaryBoolExpression(left, right, (a, b) => a == b),
                BoolTokenType.NotEquals => new BinaryBoolExpression(left, right, (a, b) => a != b),
                _ => throw new ParsingException(op.Position, $"Unsupported binary operator '{op.Text}'.", "SyntaxError")
            };
        }
    }

    // --------------------- Expression Evaluator with Caching -----------------------------

    public class BooleanExpressionEvaluator
    {
        private readonly BoolExpression _expression;
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private Dictionary<string, bool> _lastVariables;
        private bool _lastResult;
        private bool _hasCachedResult;
        private bool _canBeCached;

        public BooleanExpressionEvaluator(BoolExpression expression, bool canBeCached = true)
        {
            _expression = expression;
            _canBeCached = canBeCached;
        }

        /// <summary>
        /// Thread-safe evaluation of the boolean expression with the given variables.
        /// </summary>
        public bool Evaluate(Dictionary<string, bool> variables)
        {
            var varsCopy = new Dictionary<string, bool>(variables, StringComparer.OrdinalIgnoreCase);

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
                    bool result = _expression.Evaluate(varsCopy);
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
        /// Non-thread-safe but faster evaluation of the boolean expression with the given variables.
        /// </summary>
        public bool EvaluateFast(Dictionary<string, bool> variables)
        {
            return _expression.Evaluate(variables);
        }

        private bool AreVariablesEqual(Dictionary<string, bool> vars1, Dictionary<string, bool> vars2)
        {
            if (vars1 == null && vars2 == null)
                return true;
            if (vars1 == null || vars2 == null)
                return false;
            if (vars1.Count != vars2.Count)
                return false;
            foreach (var kvp in vars1)
            {
                if (!vars2.TryGetValue(kvp.Key, out bool value))
                    return false;
                if (!kvp.Value.Equals(value))
                    return false;
            }
            return true;
        }
    }

    // --------------------- Parser Factory with Caching -----------------------------

    public class BoolExpressionParser
    {
        private static readonly ConcurrentDictionary<string, ParsedBoolExpression> _expressionCache = new();

        public static ParsedBoolExpression Parse(string expressionText, out ParsingException parsingException)
        {
            parsingException = null;
            try
            {
                var parser = new BoolParser(expressionText);
                BoolExpression expr = parser.Parse(out List<string> variables);
                var evaluator = new BooleanExpressionEvaluator(expr, parser.CanBeCached);
                var parsedExpr = new ParsedBoolExpression(expressionText, expr, evaluator, variables);
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

        public static ParsedBoolExpression GetOrParse(string expressionText, out ParsingException parsingException)
        {
            if (_expressionCache.TryGetValue(expressionText, out ParsedBoolExpression cached))
            {
                parsingException = null;
                return cached;
            }

            return Parse(expressionText, out parsingException);
        }
    }

    // --------------------- Parsed Boolean Expression Wrapper -----------------------------

    public class ParsedBoolExpression
    {
        public string ExpressionText { get; }
        public BoolExpression ExpressionTree { get; }
        public BooleanExpressionEvaluator Evaluator { get; }
        public IReadOnlyList<string> Variables { get; }

        public ParsedBoolExpression(string expressionText, BoolExpression expressionTree, BooleanExpressionEvaluator evaluator, IReadOnlyList<string> variables)
        {
            ExpressionText = expressionText;
            ExpressionTree = expressionTree;
            Evaluator = evaluator;
            Variables = variables;
        }

        public bool Evaluate(Dictionary<string, bool> variables)
        {
            return Evaluator.Evaluate(variables);
        }
        
        public bool EvaluateFast(Dictionary<string, bool> variables)
        {
            return Evaluator.EvaluateFast(variables);
        }
    }
}
