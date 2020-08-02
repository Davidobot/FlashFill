using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/12/popl11-synthesis.pdf
namespace FlashFill {
	public static class Tokens {
		// https://github.com/MikaelMayer/StringSolver/blob/2d3e4476c5fe71a4b17387dd68f8c772c52dcdc2/src/main/scala/ch/epfl/lara/synthesis/stringsolver/ScalaRegExp.scala#L84
		// Canonical to paper
		public static Regex NumTok = new Regex(@"\d+");
		public static Regex AlphTok = new Regex(@"[a-zA-Z]+");
		public static Regex LowerTok = new Regex(@"[a-z]+");
		public static Regex UpperTok = new Regex(@"[A-Z]+");
		public static Regex AccentTok = new Regex(@"[\u00C0-\u00FF]+");
		public static Regex AlphNumTok = new Regex(@"[a-zA-Z0-9]+");
		public static Regex WhiteSpaceTok = new Regex(@"\s+");
		public static Regex CommaTok = new Regex(@",+");
		public static Regex SlashTok = new Regex(@"(\\|/)+");
		public static Regex HyphenTok = new Regex(@"-+");
		public static Regex LeftParenTok = new Regex(@"\(+");
		public static Regex RightParenTok = new Regex(@"\)+");
		public static Regex AnyTok = new Regex(@".+");
		public static Regex CharTok = new Regex(@"\w+");
		public static Regex Epsilon = new Regex(@"");
		public static Regex NonWhiteSpaceTok = new Regex(@"\S+");
		public static Regex StartTok = new Regex(@"\A");
		public static Regex EndTok = new Regex(@"\Z");
		// "[^\.]+"

		// Extra
		public static Regex DotTok = new Regex(@"\.+");

		public static Regex TokenSeq(Regex r1, Regex r2) {
			return new Regex(r1.ToString() + r2.ToString());
		}

		public static Regex NonTokenSeq(Regex r1, Regex r2) {
			return new Regex(@"\b" + r1.ToString() + r2.ToString() + @"\b");
		}

		public static Regex Non(Regex r) {
			return new Regex(@"(?!" + r.ToString() + @")");
		}
	}

	public abstract class Position {
		public abstract int? Eval(string s);
	}

	public abstract class Expression {
		public abstract string Eval(string s);

		public abstract string Eval();
	}

	public abstract class TraceExpression {
		public abstract string Eval(string s);

		public abstract string Eval();
	}

	public abstract class AtomicExpression : TraceExpression {
		public override abstract string Eval(string s);

		public override abstract string Eval();
	}

	public abstract class Conditional {
		public abstract bool Eval(string s);

		public abstract bool Eval();
	}

	public class CPos : Position {
		int k;
		public CPos(int k) {
			this.k = k;
		}

		public override int? Eval(string s) {
			if (k < 0)
				return s.Length + k + 1;
			return k;
		}
	}

	public class Pos : Position {
		Regex r_1, r_1s, r_2, r_2s;
		int c;
		private bool IsEps(Regex r) {
			return r.ToString() == Tokens.Epsilon.ToString();
		}

		public Pos(Regex r_1, Regex r_2, int cp) {
			if (cp > 0) {
				this.r_1 = new Regex(r_1.ToString());
				this.r_2 = r_2;
				this.r_2s = new Regex("^" + r_2.ToString());
				this.c = cp;
			} else {
				this.r_1 = new Regex(r_1.ToString() + "$");
				this.r_2 = new Regex(r_2.ToString(), RegexOptions.RightToLeft);
				this.c = cp;
			}
		}

		public override int? Eval(string s) {
			int t;
			int c = this.c;
			if (c > 0) {
				var exclusions = r_2.Matches(s).Select(x => Tuple.Create(x.Index, x.Index + x.Length));
				foreach (System.Text.RegularExpressions.Match match in r_1.Matches(s)) {
					t = match.Index + match.Length;

					bool skip = false;
					if (IsEps(r_1)) {
						foreach (var ex in exclusions) {
							if (t > ex.Item1 && t <= ex.Item2) {
								skip = true; break;
							}
						}
					}

					if (!skip && r_2s.Match(s.Substring(t)).Success) {
						c--;
						if (c > 0)
							continue;
						return t;
					}
				}
			} else if (c < 0) {
				c = -c;
				foreach (System.Text.RegularExpressions.Match match in r_2.Matches(s)) {
					t = match.Index;
					if (r_1.Match(s.Substring(0, t)).Success) {
						c--;
						if (c > 0)
							continue;
						return t;
					}
				}
			}
			return null;
		}
	}

	public class SubStr : AtomicExpression {
		string v_i;
		Position p_1, p_2;
		public SubStr(string v_i, Position p_1, Position p_2) {
			this.v_i = v_i; this.p_1 = p_1; this.p_2 = p_2;
		}

		public override string Eval() {
			int i1 = p_1.Eval(v_i) ?? -1;
			int i2 = p_2.Eval(v_i) ?? -1;
			if (i1 >= v_i.Length || i1 < 0 || i2 > v_i.Length || i2 < 0)
				return null;

			return v_i[i1..i2];
		}

		public override string Eval(string v) {
			int i1 = p_1.Eval(v) ?? -1;
			int i2 = p_2.Eval(v) ?? -1;
			if (i1 >= v.Length || i1 < 0 || i2 > v.Length || i2 < 0)
				return null;

			return v[i1..i2];
		}
	}

	public class SubStr2 : AtomicExpression {
		string v_i;
		Regex r;
		int c;
		public SubStr2(string v_i, Regex r, int c) {
			this.v_i = v_i; this.r = r; this.c = c;
		}

		public override string Eval() {
			return new SubStr(v_i, new Pos(Tokens.Epsilon, r, c), new Pos(r, Tokens.Epsilon, c)).Eval();
		}

		public override string Eval(string v) {
			return new SubStr(v, new Pos(Tokens.Epsilon, r, c), new Pos(r, Tokens.Epsilon, c)).Eval();
		}
	}

	public class ConstStr : AtomicExpression {
		string s;
		public ConstStr(string s) {
			this.s = s;
		}

		public override string Eval(string _) {
			return s;
		}

		public override string Eval() {
			return s;
		}
	}

	public class Loop : AtomicExpression {
		Func<int, TraceExpression> e;

		public Loop(Func<int, TraceExpression> e) {
			this.e = e;
		}

		public override string Eval(string s) {
			return new LoopR(e, 1, s).Eval();
		}

		public override string Eval() {
			throw new Exception("Loop needs string to be evaluatued");
		}
	}

	public class LoopR {
		Func<int, TraceExpression> e;
		int k;
		string s;

		public LoopR(Func<int, TraceExpression> e, int k, string s) {
			this.e = e; this.k = k; this.s = s;
		}

		public string Eval() {
			string t = e(k).Eval(s);
			if (t == null)
				return null;
			return t + (new LoopR(e, k + 1, s).Eval());
		}
	}

	public class Match : Conditional {
		string v_i;
		Regex r;
		int k;

		public Match(string v_i, Regex r, int k) {
			this.v_i = v_i; this.r = r; this.k = k;
		}

		public Match(string v_i, Regex r) {
			this.v_i = v_i; this.r = r; this.k = 1;
		}

		public override bool Eval() {
			if (v_i == null)
				return false;
			return r.Matches(v_i).Count >= k;
		}

		public override bool Eval(string v) {
			if (v == null)
				return false;
			return r.Matches(v).Count >= k;
		}
	}

	public class NotMatch : Match {
		public NotMatch(string v_i, Regex r, int k) : base(v_i, r, k) {
			;
		}

		public NotMatch(string v_i, Regex r) : base(v_i, r) {
			;
		}

		public override bool Eval() {
			return !base.Eval();
		}

		public override bool Eval(string v) {
			return !base.Eval(v);
		}
	}

	public class Conjunct : Conditional {
		List<Match> matches;

		public Conjunct(List<Match> matches) {
			this.matches = matches;
		}

		public override bool Eval() {
			foreach (Match m in matches) {
				if (!m.Eval())
					return false;
			}
			return true;
		}

		public override bool Eval(string v) {
			foreach (Match m in matches) {
				if (!m.Eval(v))
					return false;
			}
			return true;
		}
	}

	public class Disjunct : Conditional {
		List<Match> matches;

		public Disjunct(List<Match> matches) {
			this.matches = matches;
		}

		public override bool Eval() {
			foreach (Match m in matches) {
				if (m.Eval())
					return true;
			}
			return false;
		}

		public override bool Eval(string v) {
			foreach (Match m in matches) {
				if (m.Eval(v))
					return true;
			}
			return false;
		}
	}

	public class Switch : Expression {
		List<Tuple<Conditional, TraceExpression>> tuples;

		public Switch(List<Tuple<Conditional, TraceExpression>> tuples) {
			this.tuples = tuples;
		}

		public override string Eval() {
			foreach (Tuple<Conditional, TraceExpression> t in tuples) {
				if (t.Item1.Eval())
					return t.Item2.Eval();
			}
			return null;
		}

		public override string Eval(string s) {
			foreach (Tuple<Conditional, TraceExpression> t in tuples) {
				if (t.Item1.Eval(s))
					return t.Item2.Eval(s);
			}
			return null;
		}
	}

	public class Concatenate : TraceExpression {
		List<AtomicExpression> fs;

		public Concatenate(List<AtomicExpression> fs) {
			this.fs = fs;
		}

		public override string Eval() {
			StringBuilder stringBuilder = new StringBuilder();
			foreach (AtomicExpression f in fs) {
				string t = f.Eval();
				if (t == null)
					return null;
				stringBuilder.Append(t);
			}
			return stringBuilder.ToString();
		}

		public override string Eval(string s) {
			StringBuilder stringBuilder = new StringBuilder();
			foreach (AtomicExpression f in fs) {
				string t = f.Eval(s);
				if (t == null)
					return null;
				stringBuilder.Append(t);
			}
			return stringBuilder.ToString();
		}
	}

	public class Epsilon : TraceExpression {
		public override string Eval(string s) {
			return null;
		}

		public override string Eval() {
			return null;
		}
	}

	class Program {
		static void Main(string[] args) {
			Console.WriteLine("Hello World!");
		}
	}
}
