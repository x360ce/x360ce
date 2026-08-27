using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace x360ce.Engine
{
	/// <summary>
	/// A mapping value that computes its result from other sources, written as "=a1*abs(a1)".
	/// </summary>
	/// <remarks>
	/// Expressions arrive from a shared database and are therefore written by strangers. The grammar is
	/// deliberately closed: numbers, source references, four operators, brackets, unary minus, and a fixed
	/// table of functions. It has no way to name a type, reach a member, loop, or recurse, so the only cost
	/// an author can impose is arithmetic, and the node cap bounds that.
	///
	/// Values are normalised before evaluation and clamped afterwards by the caller, so an expression is
	/// written in plain numbers and never has to know a device's range.
	/// </remarks>
	public sealed class MapExpression
	{

		#region Limits

		/// <summary>Marks a value as an expression rather than a plain source reference.</summary>
		public const string Prefix = "=";

		/// <summary>
		/// Longest accepted expression. Checked against the raw text before anything parses it.
		/// </summary>
		/// <remarks>
		/// This is the width of the column an expression is stored in, not a number chosen here.
		/// Storage has to be the binding limit: a cap larger than the column would let somebody write
		/// an expression that passes every check and is then silently cut short when it is saved,
		/// which is worse than refusing it while they are still looking at it.
		///
		/// Sixteen is short. It is enough for the simple things people actually write, such as
		/// "=a5*2" or "=a1*0.5", and not enough to write a dead zone and a curve out in full. The
		/// column is widened in a later release and this number moves with it; until then the two
		/// must agree, and a test holds them together.
		/// </remarks>
		public const int MaxLength = 16;

		/// <summary>
		/// Deepest accepted nesting. Counted by scanning the raw text, then again while parsing.
		/// </summary>
		/// <remarks>
		/// A stack overflow cannot be caught: it ends the process with no handler and no chance to save.
		/// The raw scan is therefore the real defence, because it runs before any recursion starts.
		/// </remarks>
		public const int MaxDepth = 16;

		/// <summary>Most nodes an expression may build. Bounds evaluation cost and compiled size.</summary>
		public const int MaxNodes = 128;

		/// <summary>Most distinct sources one expression may read.</summary>
		public const int MaxReferences = 8;

		#endregion

		/// <summary>Source letters, matching the stored mapping format, plus 'b' for a button.</summary>
		/// <remarks>
		/// Storage writes a button as a bare number, which cannot be told from a number literal inside an
		/// expression. Within an expression the letter is therefore required and buttons take 'b'.
		/// </remarks>
		internal const string TypeLetters = "abdhpsx";

		/// <summary>The one source that is not a control: how long the program has been running.</summary>
		/// <remarks>
		/// Written as a word rather than a letter and a number, because there is only ever one of it.
		/// Its type letter is deliberately not among the ones above, so that "t1" is still refused: a
		/// person who wants the clock writes what they mean.
		/// </remarks>
		public const string TimeName = "now";

		/// <summary>Reference type used for <see cref="TimeName"/>.</summary>
		public const char TimeType = 't';

		/// <summary>Angles are in degrees, because a person writing sin(90) means a quarter turn.</summary>
		internal const double DegreesToRadians = Math.PI / 180.0;

		private MapExpression(string text, Func<float[], float> compiled, IList<MapReference> references)
		{
			Text = text;
			_compiled = compiled;
			References = new System.Collections.ObjectModel.ReadOnlyCollection<MapReference>(references);
		}

		private readonly Func<float[], float> _compiled;

		/// <summary>The expression as written, including its prefix.</summary>
		public string Text { get; private set; }

		/// <summary>
		/// Sources this expression reads, in the order <see cref="Evaluate"/> expects their values.
		/// </summary>
		public IList<MapReference> References { get; private set; }

		/// <summary>True when a stored value is an expression rather than a plain source reference.</summary>
		public static bool IsExpression(string value)
		{
			return !string.IsNullOrEmpty(value) && value[0] == '=';
		}

		/// <summary>
		/// Computes the result. Values are supplied in the order given by <see cref="References"/>.
		/// </summary>
		/// <remarks>
		/// A result that is not a finite number becomes zero, so a division by zero or a square root of a
		/// negative number reaches the pad as a resting value rather than as rubbish.
		/// </remarks>
		public float Evaluate(float[] values)
		{
			// A caller that supplies too few values gets a resting result rather than an exception.
			// This runs on the thread that feeds the pad, where a throw would end the feed entirely.
			if (values == null || values.Length < References.Count)
				return 0f;
			var result = _compiled(values);
			return float.IsNaN(result) || float.IsInfinity(result) ? 0f : result;
		}

		/// <summary>
		/// Parses and compiles an expression. Returns false for anything it does not fully understand.
		/// </summary>
		/// <param name="value">The stored value, including the leading '=' prefix.</param>
		/// <param name="result">The compiled expression, or null.</param>
		/// <param name="error">Why it was rejected, or null.</param>
		/// <param name="position">Where in the text the fault is, or -1.</param>
		/// <remarks>
		/// Bad input is expected input, so this never throws for it. Every rejection happens before the
		/// expression is compiled, and compilation is reached only by text the grammar fully accepts.
		/// </remarks>
		public static bool TryParse(string value, out MapExpression result, out string error, out int position)
		{
			result = null;
			error = null;
			position = -1;
			if (!IsExpression(value))
			{
				error = "Not an expression.";
				return false;
			}
			return Read(value, out result, out error, out position);
		}

		/// <summary>
		/// Reads the text. Called once for each distinct mapping, not once per poll.
		/// </summary>
		/// <remarks>
		/// Nothing is remembered here on purpose. A store of readings would hold every compiled method
		/// alive for as long as the program runs, and a compiled method cannot be unloaded. The place
		/// that stops this being read repeatedly is the pad's own list of mappings, which is rebuilt
		/// only when a mapping actually changes; a formula is therefore read once when it is set, and
		/// the compiled result lives exactly as long as the mapping that uses it.
		/// </remarks>
		private static bool Read(string value, out MapExpression result, out string error, out int position)
		{
			result = null;
			error = null;
			position = -1;
			// Guard the raw text first. Everything below this point recurses, and the checks that
			// prevent a stack overflow cannot themselves be allowed to overflow.
			if (!CheckRawText(value, ref error, ref position))
				return false;
			var body = value.Substring(Prefix.Length);
			try
			{
				var parser = new Parser(body);
				var tree = parser.ParseExpression();
				parser.ExpectEnd();
				var lambda = Expression.Lambda<Func<float[], float>>(
					Expression.Convert(tree, typeof(float)), parser.Values);
				result = new MapExpression(value, lambda.Compile(), parser.References);
				return true;
			}
			catch (FormatException ex)
			{
				error = ex.Message;
				position = ex.Data.Contains("pos") ? (int)ex.Data["pos"] + Prefix.Length : -1;
				return false;
			}
		}

		/// <summary>
		/// Rejects text that is too long, too deeply nested, or contains a character the grammar has no
		/// use for, before any parsing begins.
		/// </summary>
		private static bool CheckRawText(string value, ref string error, ref int position)
		{
			if (value.Length > MaxLength)
			{
				error = string.Format("Longer than {0} characters.", MaxLength);
				position = MaxLength;
				return false;
			}
			var depth = 0;
			// Where each unclosed bracket was opened, so an unfinished expression can point at the
			// bracket that is waiting rather than at the end of the text, which tells nobody anything.
			var opened = new int[MaxDepth + 1];
			for (int i = Prefix.Length; i < value.Length; i++)
			{
				var c = value[i];
				if (c == '(')
				{
					if (depth <= MaxDepth)
						opened[depth] = i;
					depth++;
					if (depth > MaxDepth)
					{
						error = string.Format("Nested deeper than {0} brackets.", MaxDepth);
						position = i;
						return false;
					}
				}
				else if (c == ')')
				{
					depth--;
					if (depth < 0)
					{
						error = "Closing bracket without an opening one.";
						position = i;
						return false;
					}
				}
				else if (!IsAllowed(c))
				{
					error = string.Format("'{0}' has no meaning here.", c);
					position = i;
					return false;
				}
			}
			if (depth != 0)
			{
				error = "Opening bracket without a closing one.";
				position = opened[Math.Min(depth, MaxDepth) - 1];
				return false;
			}
			return true;
		}

		/// <summary>Characters the grammar can use. Everything else is refused outright.</summary>
		private static bool IsAllowed(char c)
		{
			if (c >= '0' && c <= '9') return true;
			if (c >= 'a' && c <= 'z') return true;
			if (c >= 'A' && c <= 'Z') return true;
			// Note what is absent: '!' has no meaning here, so a factorial cannot be written. It is the
			// one arithmetic operation whose cost grows without bound from a short input, and it earned
			// another expression library a published advisory for exactly that.
			return c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '^'
				|| c == '.' || c == ',' || c == ' ';
		}

		#region Functions

		private static readonly Dictionary<string, FunctionInfo> Functions = CreateFunctions();

		/// <summary>Every function the grammar accepts, with how many values each takes.</summary>
		/// <remarks>
		/// Exposed so the Help page can be checked against the parser rather than written beside it. A
		/// function that exists here without being documented is a capability nobody was told about.
		/// </remarks>
		public static IDictionary<string, int> FunctionArity
		{
			get
			{
				var map = new SortedDictionary<string, int>(StringComparer.Ordinal);
				foreach (var pair in Functions)
					map.Add(pair.Key, pair.Value.Arity);
				return map;
			}
		}

		/// <summary>Source letters an expression may read.</summary>
		public static string SourceLetters { get { return TypeLetters; } }

		private sealed class FunctionInfo
		{
			public int Arity;
			public Func<Expression[], Expression> Build;
		}

		private static Dictionary<string, FunctionInfo> CreateFunctions()
		{
			// Ordinal comparison, because under some languages a case-insensitive compare maps 'I' to a
			// letter that no longer matches, and 'min' would stop being found.
			var map = new Dictionary<string, FunctionInfo>(StringComparer.Ordinal);
			Add(map, "abs", 1, a => Call("Abs", a[0]));
			// Not Math.Sign, which throws for a value that is not a number rather than returning one.
			// A throw here would leave the input thread through Evaluate, past the sanitiser.
			Add(map, "sign", 1, a => Expression.Condition(
				Expression.GreaterThan(a[0], Expression.Constant(0.0)), Expression.Constant(1.0),
				Expression.Condition(
					Expression.LessThan(a[0], Expression.Constant(0.0)), Expression.Constant(-1.0),
					Expression.Constant(0.0))));
			Add(map, "sqrt", 1, a => Call("Sqrt", a[0]));
			Add(map, "exp", 1, a => Call("Exp", a[0]));
			Add(map, "floor", 1, a => Call("Floor", a[0]));
			Add(map, "ceil", 1, a => Call("Ceiling", a[0]));
			Add(map, "round", 1, a => Call("Round", a[0]));
			// Angles are degrees throughout, so a person writing sin(90) gets the quarter turn they
			// meant. Conversions between degrees and radians are deliberately absent: with degree
			// trigonometry, sin(rad(90)) would silently compute the sine of 1.57 degrees.
			Add(map, "sin", 1, a => Call("Sin", ToRadians(a[0])));
			Add(map, "cos", 1, a => Call("Cos", ToRadians(a[0])));
			Add(map, "tan", 1, a => Call("Tan", ToRadians(a[0])));
			Add(map, "asin", 1, a => ToDegrees(Call("Asin", a[0])));
			Add(map, "acos", 1, a => ToDegrees(Call("Acos", a[0])));
			Add(map, "atan", 1, a => ToDegrees(Call("Atan", a[0])));
			Add(map, "min", 2, a => Call("Min", a[0], a[1]));
			Add(map, "max", 2, a => Call("Max", a[0], a[1]));
			Add(map, "pow", 2, a => Call("Pow", a[0], a[1]));
			Add(map, "log", 2, a => Call("Log", a[0], a[1]));
			// The limits are sorted, so writing them the wrong way round holds the value between them
			// rather than silently pinning every input to one number.
			Add(map, "clamp", 3, a => Call("Min", Call("Max", a[0], Call("Min", a[1], a[2])), Call("Max", a[1], a[2])));
			// The three shaping steps a mapping row already offers, written as functions so a row that
			// is switched to an expression reads as what it does rather than as a wall of arithmetic.
			// Spelled out, the busiest row came to over three hundred characters because each part had
			// to be repeated; named, it fits in sixty and can be read aloud.
			Add(map, "deadzone", 2, a => DeadZone(a[0], a[1]));
			Add(map, "antideadzone", 2, a => AntiDeadZone(a[0], a[1]));
			Add(map, "curve", 2, a => Curve(a[0], a[1]));
			return map;
		}

		private static void Add(Dictionary<string, FunctionInfo> map, string name, int arity, Func<Expression[], Expression> build)
		{
			map.Add(name, new FunctionInfo { Arity = arity, Build = build });
		}

		private static Expression ToRadians(Expression degrees)
		{
			return Expression.Multiply(degrees, Expression.Constant(DegreesToRadians));
		}

		private static Expression ToDegrees(Expression radians)
		{
			return Expression.Divide(radians, Expression.Constant(DegreesToRadians));
		}

		/// <summary>Direction of a value as -1, 0 or 1, without throwing for one that is not a number.</summary>
		private static Expression SignOf(Expression value)
		{
			return Expression.Condition(
				Expression.GreaterThan(value, Expression.Constant(0.0)), Expression.Constant(1.0),
				Expression.Condition(
					Expression.LessThan(value, Expression.Constant(0.0)), Expression.Constant(-1.0),
					Expression.Constant(0.0)));
		}

		/// <summary>
		/// Ignores the first part of a movement, then stretches what is left back over the full travel.
		/// </summary>
		private static Expression DeadZone(Expression value, Expression amount)
		{
			return Expression.Divide(
				Call("Max", Expression.Subtract(value, amount), Expression.Constant(0.0)),
				Expression.Subtract(Expression.Constant(1.0), amount));
		}

		/// <summary>
		/// Lifts any movement above a floor, so a game that ignores small values still notices.
		/// </summary>
		/// <remarks>
		/// Nothing is lifted at rest. Multiplying by the direction does that, because the direction of
		/// nought is nought - otherwise a stick sitting still would drive the game constantly.
		/// </remarks>
		private static Expression AntiDeadZone(Expression value, Expression amount)
		{
			return Expression.Multiply(SignOf(value),
				Expression.Add(amount, Expression.Multiply(
					Call("Abs", value), Expression.Subtract(Expression.Constant(1.0), amount))));
		}

		/// <summary>
		/// Bends the middle of the travel while leaving both ends where they are.
		/// </summary>
		/// <remarks>
		/// The same curve the Sensitivity setting applies. A positive amount makes the start gentler,
		/// a negative one makes it quicker.
		/// </remarks>
		private static Expression Curve(Expression value, Expression amount)
		{
			var v = Call("Abs", value);
			var one = Expression.Constant(1.0);
			var gentle = Expression.Add(v, Expression.Multiply(
				Expression.Subtract(Expression.Subtract(one, Call("Sqrt", Expression.Subtract(one, Expression.Multiply(v, v)))), v),
				Call("Abs", amount)));
			var quick = Expression.Add(v, Expression.Multiply(
				Expression.Subtract(Call("Sqrt", Expression.Subtract(one,
					Expression.Multiply(Expression.Subtract(one, v), Expression.Subtract(one, v)))), v),
				Call("Abs", amount)));
			return Expression.Multiply(SignOf(value),
				Expression.Condition(Expression.GreaterThanOrEqual(amount, Expression.Constant(0.0)), gentle, quick));
		}

		/// <summary>
		/// Binds one named method on <see cref="Math"/>, chosen here at build time.
		/// </summary>
		/// <remarks>
		/// The name comes from this file and never from the expression, so an author cannot reach a method
		/// the table does not already list.
		/// </remarks>
		private static Expression Call(string method, params Expression[] args)
		{
			var types = new Type[args.Length];
			for (int i = 0; i < args.Length; i++)
				types[i] = typeof(double);
			if (method == "Sign")
				return Expression.Call(typeof(Math).GetMethod("Sign", types), args);
			return Expression.Call(typeof(Math).GetMethod(method, types), args);
		}

		#endregion

		#region Parser

		/// <summary>Recursive descent over the closed grammar, counting nodes and depth as it goes.</summary>
		private sealed class Parser
		{
			public Parser(string text)
			{
				_text = text;
				Values = Expression.Parameter(typeof(float[]), "v");
				References = new List<MapReference>();
			}

			private readonly string _text;
			private int _at;
			private int _nodes;
			private int _depth;

			public ParameterExpression Values { get; private set; }
			public IList<MapReference> References { get; private set; }

			public Expression ParseExpression()
			{
				return ParseAdditive();
			}

			public void ExpectEnd()
			{
				SkipSpace();
				if (_at < _text.Length)
					throw Fault(string.Format("'{0}' was not expected here.", _text[_at]), _at);
			}

			// Additive and multiplicative both loop rather than recurse on their right operand, so
			// "10-4-3" is 3 and "100/10/2" is 5. Recursing right is silently wrong and never throws.
			private Expression ParseAdditive()
			{
				var left = ParseMultiplicative();
				while (true)
				{
					SkipSpace();
					if (Peek() == '+') { _at++; left = Node(Expression.Add(left, ParseMultiplicative())); }
					else if (Peek() == '-') { _at++; left = Node(Expression.Subtract(left, ParseMultiplicative())); }
					else return left;
				}
			}

			private Expression ParseMultiplicative()
			{
				var left = ParseUnary();
				while (true)
				{
					SkipSpace();
					if (Peek() == '*') { _at++; left = Node(Expression.Multiply(left, ParseUnary())); }
					else if (Peek() == '/') { _at++; left = Node(Expression.Divide(left, ParseUnary())); }
					else if (Peek() == '%') { _at++; left = Node(Expression.Modulo(left, ParseUnary())); }
					else return left;
				}
			}

			private Expression ParseUnary()
			{
				SkipSpace();
				if (Peek() == '-')
				{
					_at++;
					// Counted like any other nesting. A run of signs descends once per sign and opens no
					// bracket, so a cap that watched only brackets would miss the very shape that was
					// measured ending another parser's process.
					Enter();
					var negated = Node(Expression.Negate(ParseUnary()));
					Leave();
					return negated;
				}
				// There is deliberately no unary plus. It computes nothing that its absence does not,
				// and every operator the grammar carries is one more shape to reason about.
				return ParsePower();
			}

			/// <summary>
			/// Raising to a power, which binds tighter than multiplying and looser than negating.
			/// </summary>
			/// <remarks>
			/// Two conventions have to hold together and neither raises an error when wrong. It groups
			/// to the right, so 2^3^2 is 2^9 rather than 8^2. And negation applies to the result, so
			/// -2^2 is -4 rather than 4. Reading the exponent as a unary term gives both, and also lets
			/// 2^-1 mean a half.
			/// </remarks>
			private Expression ParsePower()
			{
				var left = ParsePrimary();
				SkipSpace();
				if (Peek() != '^')
					return left;
				_at++;
				return Node(Call("Pow", left, ParseUnary()));
			}

			private Expression ParsePrimary()
			{
				SkipSpace();
				if (_at >= _text.Length)
					throw Fault("The expression ends before it is complete.", _at);
				var c = Peek();
				if (c == '(')
					return ParseBracketed();
				if (c >= '0' && c <= '9' || c == '.')
					return ParseNumber();
				if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z')
					return ParseName();
				throw Fault(string.Format("'{0}' was not expected here.", c), _at);
			}

			private Expression ParseBracketed()
			{
				Enter();
				_at++;
				var inner = ParseAdditive();
				SkipSpace();
				if (Peek() != ')')
					throw Fault("A closing bracket is missing.", _at);
				_at++;
				Leave();
				return inner;
			}

			private Expression ParseNumber()
			{
				var start = _at;
				while (_at < _text.Length && (_text[_at] >= '0' && _text[_at] <= '9' || _text[_at] == '.'))
					_at++;
				var raw = _text.Substring(start, _at - start);
				double number;
				// AllowDecimalPoint alone, so a thousands separator is refused rather than silently
				// dropped: with the wider styles "1,000" parses as 1000 and no error is ever raised.
				if (!double.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number))
					throw Fault(string.Format("'{0}' is not a number.", raw), start);
				return Node(Expression.Constant(number));
			}

			private Expression ParseName()
			{
				var start = _at;
				while (_at < _text.Length && (_text[_at] >= 'a' && _text[_at] <= 'z' || _text[_at] >= 'A' && _text[_at] <= 'Z'))
					_at++;
				var name = _text.Substring(start, _at - start);
				var digitsAt = _at;
				while (_at < _text.Length && _text[_at] >= '0' && _text[_at] <= '9')
					_at++;
				var digits = _text.Substring(digitsAt, _at - digitsAt);
				// No space is skipped here: a bracket is part of the call, so "abs (1)" is a mistake
				// rather than a call written loosely.
				if (Peek() == '(')
					return ParseCall(name, digits, start);
				if (digits.Length == 0)
				{
					// The clock is the one source written as a word, because there is only one of it.
					if (string.Equals(name, TimeName, StringComparison.OrdinalIgnoreCase))
						return Reference(new MapReference(TimeType, 1), start);
					throw Fault(string.Format("'{0}' is not a source or a function.", name), start);
				}
				return ParseReference(name, digits, start);
			}

			private Expression ParseCall(string name, string digits, int start)
			{
				if (digits.Length != 0)
					throw Fault(string.Format("'{0}{1}' is not a function.", name, digits), start);
				FunctionInfo function;
				if (!Functions.TryGetValue(name.ToLowerInvariant(), out function))
					throw Fault(string.Format("'{0}' is not a function.", name), start);
				Enter();
				_at++;
				var args = new Expression[function.Arity];
				for (int i = 0; i < function.Arity; i++)
				{
					if (i > 0)
					{
						SkipSpace();
						if (Peek() != ',')
							throw Fault(string.Format("'{0}' takes {1} values.", name, function.Arity), _at);
						_at++;
					}
					args[i] = ParseAdditive();
				}
				SkipSpace();
				if (Peek() != ')')
					throw Fault(string.Format("'{0}' takes {1} values.", name, function.Arity), _at);
				_at++;
				Leave();
				return Node(function.Build(args));
			}

			private Expression ParseReference(string name, string digits, int start)
			{
				if (name.Length != 1 || TypeLetters.IndexOf(char.ToLowerInvariant(name[0])) < 0)
					throw Fault(string.Format("'{0}' is not a source.", name), start);
				int index;
				if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out index) || index < 1)
					throw Fault(string.Format("'{0}{1}' has no source number.", name, digits), start);
				return Reference(new MapReference(char.ToLowerInvariant(name[0]), index), start);
			}

			/// <summary>Turns a source into a read of the slot its value will arrive in.</summary>
			private Expression Reference(MapReference reference, int start)
			{
				var slot = References.IndexOf(reference);
				if (slot < 0)
				{
					if (References.Count >= MaxReferences)
						throw Fault(string.Format("More than {0} sources.", MaxReferences), start);
					References.Add(reference);
					slot = References.Count - 1;
				}
				// Resolved to an array slot here, once, so evaluation is an indexed read rather than a
				// lookup by name on every poll.
				return Node(Expression.Convert(
					Expression.ArrayIndex(Values, Expression.Constant(slot)), typeof(double)));
			}

			private Expression Node(Expression expression)
			{
				if (++_nodes > MaxNodes)
					throw Fault(string.Format("More than {0} operations.", MaxNodes), _at);
				return expression;
			}

			// Depth is held by this counter rather than by catching an overflow, because an overflow
			// cannot be caught: the process ends without running any handler.
			private void Enter()
			{
				if (++_depth > MaxDepth)
					throw Fault(string.Format("Nested deeper than {0} brackets.", MaxDepth), _at);
			}

			private void Leave()
			{
				_depth--;
			}

			private char Peek()
			{
				return _at < _text.Length ? _text[_at] : '\0';
			}

			private void SkipSpace()
			{
				while (_at < _text.Length && _text[_at] == ' ')
					_at++;
			}

			private static FormatException Fault(string message, int at)
			{
				var ex = new FormatException(message);
				ex.Data["pos"] = at;
				return ex;
			}
		}

		#endregion

	}

	/// <summary>One source an expression reads, named the way stored mappings name it.</summary>
	public struct MapReference : IEquatable<MapReference>
	{
		public MapReference(char type, int index)
		{
			Type = type;
			Index = index;
		}

		/// <summary>Source letter: a axis, b button, d d-pad button, h half slider, p d-pad, s slider, x half axis.</summary>
		public readonly char Type;

		/// <summary>One-based number of the source, as stored mappings count them.</summary>
		public readonly int Index;

		public bool Equals(MapReference other)
		{
			return Type == other.Type && Index == other.Index;
		}

		public override bool Equals(object obj)
		{
			return obj is MapReference && Equals((MapReference)obj);
		}

		public override int GetHashCode()
		{
			return Type * 397 ^ Index;
		}

		public override string ToString()
		{
			return string.Concat(Type, Index.ToString(CultureInfo.InvariantCulture));
		}
	}
}
