using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlashFill;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net;
using System;
using System.Numerics;
using System.Text;

namespace FlashFillTests {
	[TestClass]
	public class BaseTests {
		// https://github.com/MikaelMayer/StringSolver/blob/master/src/test/scala/ch/epfl/lara/synthesis/stringsolver/BaseTests.scala#L63
		[TestMethod]
		public void SubStrTest() {
			string v = "abcdefg";
			SubStr ss = new SubStr(v, new CPos(0), new CPos(-1));
			Assert.AreEqual(v, ss.Eval());

			v = "qsdf1234amlkj12345mlkj432  fkj ";
			ss = new SubStr(v, new Pos(Tokens.NumTok, Tokens.AlphTok, 2), new CPos(-1));
			Assert.AreEqual("mlkj432  fkj ", ss.Eval());

			v = "CAMP DRY DBL NDL 3.6 OZ";
			ss = new SubStr(v, new Pos(Tokens.Epsilon, Tokens.NumTok, 1), new CPos(-1));
			Assert.AreEqual("3.6 OZ", ss.Eval());

			v = "My Taylor is Rich";
			SubStr2 ss2 = new SubStr2(v, Tokens.UpperTok, 2);
			Assert.AreEqual("T", ss2.Eval());
			ss2 = new SubStr2(v, Tokens.UpperTok, 3);
			Assert.AreEqual("R", ss2.Eval());

			v = "Dr. Eran Yahav";
			SubStr firstname = new SubStr(v, new Pos(Tokens.Epsilon, Tokens.NonTokenSeq(Tokens.AlphTok, Tokens.Non(Tokens.DotTok)), 1),
				new Pos(Tokens.Epsilon, Tokens.TokenSeq(Tokens.LowerTok, Tokens.Non(Tokens.DotTok)), 1));
			Assert.AreEqual("E", firstname.Eval());

			ss2 = new SubStr2(v, Tokens.AlphTok, -1);
			Assert.AreEqual("Yahav", ss2.Eval());

			v = "Prof. Kathleen S. Fisher";
			firstname = new SubStr(v, new Pos(Tokens.Epsilon, Tokens.NonTokenSeq(Tokens.AlphTok, Tokens.Non(Tokens.DotTok)), 1),
				new Pos(Tokens.Epsilon, Tokens.NonTokenSeq(Tokens.LowerTok, Tokens.Non(Tokens.DotTok)), 1));
			Assert.AreEqual("K", firstname.Eval());
		}

		[TestMethod]
		public void LoopTest() {
			string v = "qsdf1234amlkj12345mlkj432  fkj ";
			var ss = new Concatenate(new List<AtomicExpression> { new ConstStr(" + "),
				new SubStr(v, new Pos(Tokens.NumTok, Tokens.AlphTok, 2), new CPos(-1)) });
			Assert.AreEqual(" + mlkj432  fkj ", ss.Eval());

			v = "My Taylor is Rich";
			Loop l = new Loop((int w) => {
				return new Concatenate(new List<AtomicExpression> { new SubStr2(v, Tokens.UpperTok, w) });
			});
			Assert.AreEqual("MTR", l.Eval(v));
		}

		[TestMethod]
		public void Example2() {
			string v = "";
			SubStr e2 = new SubStr(v, new Pos(Tokens.Epsilon, Tokens.NumTok, 1), new CPos(-1));

			Assert.AreEqual("15Z", e2.Eval("BTR KRNL WK CORN 15Z"));
			Assert.AreEqual("3.6 OZ", e2.Eval("CAMP DRY DBL NDL 3.6 OZ"));
			Assert.AreEqual("1 PK", e2.Eval("CHORE BOY HD SC SPNG 1 PK"));
			Assert.AreEqual("5 Z", e2.Eval("FRENCH WORCESTERSHIRE 5 Z"));
			Assert.AreEqual("6 OZ", e2.Eval("O F TOMATO PASTE 6 OZ"));
		}

		[TestMethod]
		public void Example3() {
			string v = "";
			SubStr e3 = new SubStr(v, new CPos(0), new Pos(Tokens.SlashTok, Tokens.Epsilon, -1));

			Assert.AreEqual("Company\\Code\\", e3.Eval("Company\\Code\\Index.html"));
			Assert.AreEqual("Company\\Docs\\Spec\\", e3.Eval("Company\\Docs\\Spec\\specs.doc"));
		}

		[TestMethod]
		public void Example6() {
			string v = "        Oege    de    Moor";
			var ss = new Concatenate(new List<AtomicExpression> {
				new Loop((int w) => {
					return new Concatenate(new List<AtomicExpression> { new SubStr(v,
						new Pos(Tokens.Epsilon, Tokens.NonWhiteSpaceTok, w),
						new Pos(Tokens.NonWhiteSpaceTok, Tokens.TokenSeq(Tokens.WhiteSpaceTok, Tokens.NonWhiteSpaceTok), w)),
						new ConstStr(" ")
					});
				}),
				new SubStr2(v, Tokens.NonWhiteSpaceTok, -1)
			});
			Assert.AreEqual("Oege de Moor", ss.Eval(v));
		}

		[TestMethod]
		public void Example9() {
			string v = "Prof. Kathleen S. Fisher";
			SubStr firstname = new SubStr(v, new Pos(Tokens.Epsilon, Tokens.NonTokenSeq(Tokens.AlphTok, Tokens.Non(Tokens.DotTok)), 1),
				new Pos(Tokens.Epsilon, Tokens.NonTokenSeq(Tokens.LowerTok, Tokens.Non(Tokens.DotTok)), 1));
			Concatenate e1 = new Concatenate(new List<AtomicExpression> {
				new SubStr(v, new Pos(Tokens.Epsilon, Tokens.TokenSeq(Tokens.AlphTok, Tokens.CommaTok), 1),
				new Pos(Tokens.AlphTok, Tokens.CommaTok, 1)),
				new ConstStr(", "), firstname, new ConstStr(".")
			});
			Concatenate e2 = new Concatenate(new List<AtomicExpression> {
				new SubStr2(v, Tokens.AlphTok, -1), new ConstStr(", "), firstname, new ConstStr(".")
			});
			Switch swi = new Switch(new List<Tuple<Conditional, TraceExpression>> {
				Tuple.Create<Conditional, TraceExpression>(new FlashFill.Match(v, Tokens.CommaTok, 1), e1),
				Tuple.Create<Conditional, TraceExpression>(new FlashFill.Match(v, Tokens.Non(Tokens.CommaTok), 1), e2)
			});

			Assert.AreEqual("Yahav, E.", swi.Eval("Dr. Eran Yahav"));
			Assert.AreEqual("Fisher, K.", swi.Eval("Prof. Kathleen S. Fisher"));
			Assert.AreEqual("Gates, B.", swi.Eval("Bill Gates, Sr."));
			Assert.AreEqual("Necula, G.", swi.Eval("George Ciprian Necula"));
			Assert.AreEqual("McMillan, K.", swi.Eval("Ken McMillan, II"));
		}

		[TestMethod]
		public void Example12() {
			Func<string, string, string> e12 = (string v1, string v2) => {
				return new Concatenate(new List<AtomicExpression> {
					new ConstStr("case "), new ConstStr(v2), new ConstStr(": return �"), new ConstStr(v1), new ConstStr("�;")
				}).Eval();
			};

			Assert.AreEqual("case 355: return �Albania�;", e12("Albania", "355"));
			Assert.AreEqual("case 213: return �Algeria�;", e12("Algeria", "213"));
		}
	}
}