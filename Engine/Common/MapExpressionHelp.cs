using System.Collections.Generic;

namespace x360ce.Engine
{
	/// <summary>
	/// What the Help page shows about expressions: every operator, every function, every worked example.
	/// </summary>
	/// <remarks>
	/// Held here rather than written into the page so the two cannot drift apart. A test compiles every
	/// example and checks this catalogue against the parser in both directions, so a function that exists
	/// without being listed, or a listing for something the parser will not accept, fails the build.
	/// </remarks>
	public static class MapExpressionHelp
	{

		/// <summary>Operators that combine two values.</summary>
		public static readonly IList<MapExpressionOperator> BinaryOperators = new[]
		{
			new MapExpressionOperator("+", "Add", "=a1+a2", "Both sources move the same output."),
			new MapExpressionOperator("-", "Subtract", "=a1-a2", "One source opposes the other, as two pedals on one axis."),
			new MapExpressionOperator("*", "Multiply", "=a1*1.5", "Makes a source travel further for the same movement."),
			new MapExpressionOperator("/", "Divide", "=a1/2", "Makes a source travel less, for finer control."),
			new MapExpressionOperator("%", "Remainder", "=a1%0.25", "What is left after dividing, useful for repeating steps."),
			new MapExpressionOperator("^", "Power", "=a1^2", "Raises to a power. 2^3^2 is 2^9, and -2^2 is -4."),
		};

		/// <summary>Operators that act on a single value.</summary>
		public static readonly IList<MapExpressionOperator> UnaryOperators = new[]
		{
			new MapExpressionOperator("-", "Negate", "=-a1", "Reverses the direction of a source."),
		};

		/// <summary>Grouping, and the separator between the values a function takes.</summary>
		public static readonly IList<MapExpressionOperator> Punctuation = new[]
		{
			new MapExpressionOperator("( )", "Group", "=(a1+a2)*0.5", "Work out the part in brackets first."),
			new MapExpressionOperator(",", "Separate", "=min(a1,a2)", "Separates the values a function takes."),
		};

		/// <summary>Functions, with how many values each one takes.</summary>
		public static readonly IList<MapExpressionFunction> Functions = new[]
		{
			new MapExpressionFunction("abs", 1, "Size of a value, ignoring its direction.", "=abs(a1)"),
			new MapExpressionFunction("sign", 1, "Direction alone: -1, 0 or 1.", "=sign(a1)"),
			new MapExpressionFunction("sqrt", 1, "Square root.", "=sqrt(a1)"),
			new MapExpressionFunction("exp", 1, "The number e raised to this power.", "=exp(a1)-1"),
			new MapExpressionFunction("floor", 1, "Rounds down to a whole number.", "=floor(a1*10)/10"),
			new MapExpressionFunction("ceil", 1, "Rounds up to a whole number.", "=ceil(a1*10)/10"),
			new MapExpressionFunction("round", 1, "Rounds to a whole number. A half goes to the even neighbour, so round(2.5) is 2 and round(3.5) is 4.", "=round(a1*10)/10"),
			new MapExpressionFunction("sin", 1, "Sine. Angles are in degrees, so sin(90) is 1.", "=sin(a1*90)"),
			new MapExpressionFunction("cos", 1, "Cosine. Angles are in degrees.", "=cos(a1*90)"),
			new MapExpressionFunction("tan", 1, "Tangent. Angles are in degrees.", "=tan(a1*45)"),
			new MapExpressionFunction("asin", 1, "The angle, in degrees, whose sine is this value.", "=asin(a1)/90"),
			new MapExpressionFunction("acos", 1, "The angle, in degrees, whose cosine is this value.", "=acos(a1)/180"),
			new MapExpressionFunction("atan", 1, "The angle, in degrees, whose tangent is this value.", "=atan(a1)/45"),
			new MapExpressionFunction("min", 2, "The smaller of two values.", "=min(a1,0.8)"),
			new MapExpressionFunction("max", 2, "The larger of two values.", "=max(a1,0)"),
			new MapExpressionFunction("pow", 2, "The first value raised to the power of the second, the same as ^.", "=pow(a1,2)"),
			new MapExpressionFunction("log", 2, "Logarithm of the first value in the base given by the second.", "=log(a1+1,2)"),
			new MapExpressionFunction("clamp", 3, "Holds a value between a low and a high limit.", "=clamp(a1*2,0,1)"),
			new MapExpressionFunction("deadzone", 2, "Ignores the first part of a movement, then stretches what is left over the full travel.", "=sign(a1)*deadzone(abs(a1),0.1)"),
			new MapExpressionFunction("antideadzone", 2, "Lifts any movement above a floor, so a game that ignores small values still notices. Nothing is lifted at rest.", "=antideadzone(a1,0.15)"),
			new MapExpressionFunction("curve", 2, "Bends the middle of the travel and leaves both ends alone, the same as the Sensitivity setting.", "=curve(a1,0.5)"),
		};

		/// <summary>Source letters an expression may read, and what each one means.</summary>
		/// <remarks>
		/// These are the letters stored mappings already use, with 'b' added for a button. A button is
		/// stored as a bare number, which inside an expression cannot be told apart from the number
		/// itself, so within an expression the letter is always required.
		/// </remarks>
		public static readonly IList<MapExpressionSource> Sources = new[]
		{
			new MapExpressionSource('a', "Axis", "-1 to 1",
				"A stick or wheel that rests in the middle and moves both ways.", "a1"),
			new MapExpressionSource('b', "Button", "0 or 1",
				"A button: 0 while released, 1 while held.", "b1"),
			new MapExpressionSource('s', "Slider", "0 to 1",
				"A throttle, pedal or dial that rests at one end.", "s1"),
			new MapExpressionSource('x', "Half axis", "0 to 1",
				"One half of an axis on its own, so the two halves can drive different things.", "x1"),
			new MapExpressionSource('h', "Half slider", "0 to 1",
				"One half of a slider on its own.", "h1"),
			new MapExpressionSource('p', "D-pad", "0 to 1",
				"The hat switch read as a direction rather than as separate buttons.", "p1"),
			new MapExpressionSource('d', "D-pad button", "0 or 1",
				"One direction of the hat switch: 0 while released, 1 while held.", "d1"),
		};

		/// <summary>Worked examples, each written as the thing a player wanted.</summary>
		public static readonly IList<MapExpressionExample> Examples = new[]
		{
			new MapExpressionExample("Aim", "Fine control near centre, full speed at the edge", "=a1*abs(a1)"),
			new MapExpressionExample("Aim", "More sensitive everywhere", "=a1*1.5"),
			new MapExpressionExample("Aim", "Less sensitive, for aiming through a scope", "=a1*0.5"),
			new MapExpressionExample("Aim", "Walk slowly, run when the trigger is held", "=a1*(0.5+a2*0.5)"),
			new MapExpressionExample("Aim", "Correct a stick that drifts off centre", "=a1-0.05"),
			new MapExpressionExample("Triggers", "Full throttle before the trigger bottoms out", "=a1*1.5"),
			new MapExpressionExample("Triggers", "A firmer press before anything happens", "=a1*a1"),
			new MapExpressionExample("Wheels", "One pedal axis split into the accelerator", "=max(a1,0)"),
			new MapExpressionExample("Wheels", "The same axis, its braking half", "=-min(a1,0)"),
			new MapExpressionExample("Wheels", "Separate accelerator and brake onto one axis", "=a1-a2"),
			new MapExpressionExample("Buttons", "Only while both buttons are held", "=b1*b2"),
			new MapExpressionExample("Buttons", "While either button is held", "=max(b1,b2)"),
			new MapExpressionExample("Buttons", "While the button is not held", "=1-b1"),
			new MapExpressionExample("Buttons", "While one button is held but not the other", "=abs(b1-b2)"),
			new MapExpressionExample("Buttons", "Full speed only while the button is held", "=a1*b1"),
			new MapExpressionExample("Buttons", "Half speed until the button is held", "=a1*(0.5+b1*0.5)"),
		};

		/// <summary>
		/// How the ordinary operators do the work of and, or and not, for sources that are 0 or 1.
		/// </summary>
		/// <remarks>
		/// A button is 0 or 1, so logic needs no operators of its own: multiplying is and, taking the
		/// larger is or, and subtracting from one is not. Adding words for these would be three more
		/// things to learn, three more shapes in the grammar, and no new ability.
		/// </remarks>
		public static readonly IList<MapExpressionExample> ButtonLogic = new[]
		{
			new MapExpressionExample("Logic", "and, true only when both are held", "=b1*b2"),
			new MapExpressionExample("Logic", "or, true when either is held", "=max(b1,b2)"),
			new MapExpressionExample("Logic", "not, true while it is released", "=1-b1"),
			new MapExpressionExample("Logic", "either but not both", "=abs(b1-b2)"),
		};

		/// <summary>Rules a person needs to know before writing one, in the order they need them.</summary>
		public static readonly IList<string> Rules = new[]
		{
			"An expression starts with = and works out a number from other sources.",
			"Sources are written as a letter and a number, the same way mappings are stored: a1 is axis 1, b2 is button 2.",
			"The one source that is not a control is written as a word: now, the number of milliseconds since the program started. Divide it by 1000 for seconds, or by 60000 for minutes.",
			"Values are already scaled for you: an axis runs from -1 to 1, a trigger or slider from 0 to 1, a button is 0 or 1.",
			"The result is fitted to whatever it drives, so going past the limit is safe and simply reaches full travel.",
			"Anything that is not a real number, such as dividing by zero, becomes 0.",
			"Older versions of the program ignore expressions, so a configuration using one loses that mapping when opened in them.",
			"A button is 0 or 1, so multiplying acts as and, taking the larger acts as or, and one minus it acts as not.",
		};

	}

	/// <summary>One operator, as the Help page shows it.</summary>
	public struct MapExpressionOperator
	{
		public MapExpressionOperator(string symbol, string name, string example, string meaning)
		{
			Symbol = symbol;
			Name = name;
			Example = example;
			Meaning = meaning;
		}

		public readonly string Symbol;
		public readonly string Name;
		public readonly string Example;
		public readonly string Meaning;
	}

	/// <summary>One function, as the Help page shows it.</summary>
	public struct MapExpressionFunction
	{
		public MapExpressionFunction(string name, int arity, string meaning, string example)
		{
			Name = name;
			Arity = arity;
			Meaning = meaning;
			Example = example;
		}

		public readonly string Name;

		/// <summary>How many values the function takes.</summary>
		public readonly int Arity;

		public readonly string Meaning;
		public readonly string Example;
	}

	/// <summary>One kind of source an expression may read.</summary>
	public struct MapExpressionSource
	{
		public MapExpressionSource(char letter, string name, string range, string meaning, string example)
		{
			Letter = letter;
			Name = name;
			Range = range;
			Meaning = meaning;
			Example = example;
		}

		/// <summary>The letter that names this kind of source inside an expression.</summary>
		public readonly char Letter;

		public readonly string Name;

		/// <summary>The value range this source arrives as, once scaled.</summary>
		public readonly string Range;

		/// <summary>What the source is, for somebody who has not met the word before.</summary>
		public readonly string Meaning;

		public readonly string Example;
	}

	/// <summary>One worked example, written as the thing a player wanted.</summary>
	public struct MapExpressionExample
	{
		public MapExpressionExample(string group, string goal, string expression)
		{
			Group = group;
			Goal = goal;
			Expression = expression;
		}

		public readonly string Group;

		/// <summary>What the player wanted, not what the operator does.</summary>
		public readonly string Goal;

		public readonly string Expression;
	}
}
