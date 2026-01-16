using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Postica.BindingSystem.Utility
{
    [TestFixture]
    public class ExpressionEvaluatorTests
    {
        [Test]
        public void TestConstantExpression()
        {
            var expr = new ConstantExpression(42);
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(42, result);
        }

        [Test]
        public void TestVariableExpression()
        {
            var expr = new VariableExpression("x");
            var variables = new Dictionary<string, double> { { "x", 10 } };
            var result = expr.Evaluate(variables);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void TestBinaryExpression_Addition()
        {
            var expr = new BinaryExpression(
                new ConstantExpression(5),
                new ConstantExpression(3),
                (a, b) => a + b);
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(8, result);
        }

        [Test]
        public void TestBinaryExpression_Multiplication()
        {
            var expr = new BinaryExpression(
                new ConstantExpression(5),
                new ConstantExpression(3),
                (a, b) => a * b);
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(15, result);
        }

        [Test]
        public void TestUnaryExpression_Negation()
        {
            var expr = new UnaryExpression(new ConstantExpression(5), a => -a);
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(-5, result);
        }

        [Test]
        public void TestFunctionExpression_Sin()
        {
            var expr = new FunctionExpression("sin", new List<MathExpression> { new ConstantExpression(0) });
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestParser_SimpleExpression()
        {
            var parser = new Parser("2 + 3 * 4");
            var expr = parser.Parse();
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(14, result);
        }

        [Test]
        public void TestParser_ExpressionWithVariables()
        {
            var parser = new Parser("x * y + 3");
            var expr = parser.Parse();
            var variables = new Dictionary<string, double> { { "x", 2 }, { "y", 5 } };
            var result = expr.Evaluate(variables);
            Assert.AreEqual(13, result);
        }

        [Test]
        public void TestParser_Functions()
        {
            var parser = new Parser("sin(pi / 2)");
            var expr = parser.Parse();
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(1, result, 1e-10);
        }

        [Test]
        public void TestParser_FunctionWithVariables()
        {
            var parser = new Parser("sqrt(x^2 + y^2)");
            var expr = parser.Parse();
            var variables = new Dictionary<string, double> { { "x", 3 }, { "y", 4 } };
            var result = expr.Evaluate(variables);
            Assert.AreEqual(5, result);
        }

        [Test]
        public void TestParser_WithConstants()
        {
            var parser = new Parser("e^1");
            var expr = parser.Parse();
            var result = expr.Evaluate(new Dictionary<string, double>());
            Assert.AreEqual(Math.E, result, 1e-10);
        }

        [Test]
        public void TestExpressionEvaluator_Caching()
        {
            var parser = new Parser("x + y");
            var expr = parser.Parse();
            var evaluator = new MathExpressionEvaluator(expr);

            var variables = new Dictionary<string, double> { { "x", 1 }, { "y", 2 } };
            var result1 = evaluator.Evaluate(variables);
            Assert.AreEqual(3, result1);

            // Evaluate again with same variables (should use cache)
            var result2 = evaluator.Evaluate(variables);
            Assert.AreEqual(3, result2);

            // Modify variables
            variables["x"] = 2;
            var result3 = evaluator.Evaluate(variables);
            Assert.AreEqual(4, result3);
        }

        [Test]
        public void TestParsingException_UnmatchedParenthesis()
        {
            var parser = new Parser("2 + (3 * 4");
            Assert.Throws<ParsingException>(() => parser.Parse());
        }

        [Test]
        public void TestParsingException_UnknownFunction()
        {
            var parser = new Parser("unknownFunc(5)");
            Assert.Throws<Exception>(() => parser.Parse().Evaluate(new Dictionary<string, double>()));
        }

        [Test]
        public void TestThreadSafety()
        {
            var parser = new Parser("x * 2 + y");
            var expr = parser.Parse();
            var evaluator = new MathExpressionEvaluator(expr);

            var variables1 = new Dictionary<string, double> { { "x", 1 }, { "y", 2 } };
            var variables2 = new Dictionary<string, double> { { "x", 3 }, { "y", 4 } };

            double result1 = 0, result2 = 0;
            var thread1 = new System.Threading.Thread(() =>
            {
                result1 = evaluator.Evaluate(variables1);
            });
            var thread2 = new System.Threading.Thread(() =>
            {
                result2 = evaluator.Evaluate(variables2);
            });

            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();

            Assert.AreEqual(4, result1);
            Assert.AreEqual(10, result2);
        }

        [Test]
        public void TestComplexExpression()
        {
            var parser = new Parser("3 * sin(pi * x) + log(y, 10) - clamp(z, 0, 100)");
            var expr = parser.Parse();
            var variables = new Dictionary<string, double>
            {
                { "x", 0.5 },
                { "y", 100 },
                { "z", 50 }
            };
            var result = expr.Evaluate(variables);
            double expected = 3 * Math.Sin(Math.PI * 0.5) + Math.Log(100, 10) - Math.Clamp(50, 0, 100);
            Assert.AreEqual(expected, result, 1e-10);
        }
    }
}