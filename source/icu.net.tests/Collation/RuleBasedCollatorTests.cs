﻿// Copyright (c) 2013-2025 SIL Global
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Globalization;
using Icu.Collation;
using NUnit.Framework;

namespace Icu.Tests.Collation
{
	[TestFixture]
	public class RuleBasedCollatorTests
	{
		private const string SerbianRules = "& C < č <<< Č < ć <<< Ć";
		// with UCA:			after Tailoring:
		// --------------	   ----------------
		// CUKIĆ RADOJICA	   CUKIĆ RADOJICA
		// ČUKIĆ SLOBODAN	   CUKIĆ SVETOZAR
		// CUKIĆ SVETOZAR	   CURIĆ MILOŠ
		// ČUKIĆ ZORAN		  CVRKALJ ÐURO
		// CURIĆ MILOŠ		  ČUKIĆ SLOBODAN
		// ĆURIĆ MILOŠ		  ČUKIĆ ZORAN
		// CVRKALJ ÐURO		 ĆURIĆ MILOŠ

		private const string DanishRules = "&V <<< w <<< W";

		//UCA 	&V <<< w <<< W
		//---   ---
		//va	va
		//Va	Va
		//VA	VA
		//vb	wa
		//Vb	Wa
		//VB	WA
		//vz	vb
		//Vz	Vb
		//VZ	VB
		//wa	wb
		//Wa	Wb
		//WA	WB
		//wb	vz
		//Wb	Vz
		//WB	VZ
		//wz	wz
		//Wz	Wz
		//WZ	WZ

		[Test]
		public void Construct_EmptyRules_UCAcollator()
		{
			using (var collator = new RuleBasedCollator(string.Empty))
			{
				Assert.That(collator, Is.Not.Null);
			}
		}

		[Test]
		public void Construct_Rules_Okay()
		{
			using (var collator = new RuleBasedCollator(SerbianRules))
			{
				Assert.That(collator, Is.Not.Null);
			}
		}

		[Test]
		public void Construct_SyntaxErrorInRules_Throws()
		{
			// Previously "<<<<" was assumed to be syntax.  Now that is Quaternary so we use a longer string
			string badRules = "& C < č <<<<<<<< Č < ć <<< Ć";
			Assert.That(() => new RuleBasedCollator(badRules), Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void Clone()
		{
			using (var danishCollator = new RuleBasedCollator(DanishRules))
			using (var danishCollator2 = (RuleBasedCollator) danishCollator.Clone())
			{
				Assert.That(danishCollator2.Compare("wa", "vb"), Is.EqualTo(-1));
			}
		}

		[TestCase("", null, "a", ExpectedResult = -1)]
		[TestCase("", "a", null, ExpectedResult = 1)]
		[TestCase("", null, null, ExpectedResult = 0)]
		[TestCase("", "ČUKIĆ SLOBODAN", "CUKIĆ SVETOZAR", ExpectedResult = -1)]
		[TestCase(SerbianRules, "ČUKIĆ SLOBODAN", "CUKIĆ SVETOZAR", ExpectedResult = 1)]
		public int Compare(string rules, string string1, string string2)
		{
			using (var ucaCollator = new RuleBasedCollator(rules))
			{
				return ucaCollator.Compare(string1, string2);
			}
		}

		[Test]
		public void GetSortKey()
		{
			using (var serbianCollator = new RuleBasedCollator(SerbianRules))
			{
				var sortKeyČUKIĆ = serbianCollator.GetSortKey("ČUKIĆ SLOBODAN");
				var sortKeyCUKIĆ = serbianCollator.GetSortKey("CUKIĆ SVETOZAR");
				Assert.That(SortKey.Compare(sortKeyČUKIĆ, sortKeyCUKIĆ), Is.EqualTo(1));
			}
		}

		[Test]
		public void GetSortKey_Null()
		{
			using (var ucaCollator = new RuleBasedCollator(string.Empty))
			{
				Assert.That(() => ucaCollator.GetSortKey(null), Throws.TypeOf<ArgumentNullException>());
			}
		}

		[Test]
		public void GetSortKey_emptyString()
		{
			using (var ucaCollator = new RuleBasedCollator(string.Empty))
			{
				SortKey key = ucaCollator.GetSortKey(string.Empty);
				Assert.IsNotNull(key);
				Assert.IsNotNull(key.KeyData);
			}
		}

		[Test]
		public void SetCollatorStrengthToIdentical()
		{
			using (var collator = Collator.Create("zh"))
			{
				collator.Strength = CollationStrength.Identical;

				Assert.AreEqual(CollationStrength.Identical, collator.Strength);
			}
		}

		[TestCase(CollationStrength.Tertiary, AlternateHandling.Shifted, "di Silva", "diSilva", ExpectedResult =
			0)]
		[TestCase(CollationStrength.Tertiary, AlternateHandling.Shifted, "diSilva", "Di Silva", ExpectedResult =
			-1)]
		[TestCase(CollationStrength.Tertiary, AlternateHandling.Shifted, "U.S.A.", "USA", ExpectedResult = 0)]
		[TestCase(CollationStrength.Quaternary, AlternateHandling.Shifted, "di Silva", "diSilva",
			ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, AlternateHandling.Shifted, "diSilva", "Di Silva",
			ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, AlternateHandling.Shifted, "U.S.A.", "USA", ExpectedResult =
			-1)]
		[TestCase(CollationStrength.Tertiary, AlternateHandling.NonIgnorable, "di Silva", "Di Silva",
			ExpectedResult = -1)]
		[TestCase(CollationStrength.Tertiary, AlternateHandling.NonIgnorable, "Di Silva", "diSilva",
			ExpectedResult = -1)]
		[TestCase(CollationStrength.Tertiary, AlternateHandling.NonIgnorable, "U.S.A.", "USA", ExpectedResult =
			-1)]
		public int AlternateHandlingSetting(CollationStrength collationStrength,
			AlternateHandling alternateHandling, string string1, string string2)
		{
			/*  The Alternate attribute is used to control the handling of the so-called
			 * variable characters in the UCA: whitespace, punctuation and symbols. If
			 * Alternate is set to Non-Ignorable (N), then differences among these
			 * characters are of the same importance as differences among letters.
			 * If Alternate is set to Shifted (S), then these characters are of only
			 * minor importance. The Shifted value is often used in combination with
			 * Strength set to Quaternary. In such a case, white-space, punctuation,
			 * and symbols are considered when comparing strings, but only if all other
			 * aspects of the strings (base letters, accents, and case) are identical.
			 * If Alternate is not set to Shifted, then there is no difference between
			 * a Strength of 3 and a Strength of 4.
			  Example:
				  S=3, A=N di Silva < Di Silva < diSilva < U.S.A. < USA
				  S=3, A=S di Silva = diSilva < Di Silva  < U.S.A. = USA
				  S=4, A=S di Silva < diSilva < Di Silva < U.S.A. < USA
			 */
			using (var ucaCollator = new RuleBasedCollator(string.Empty, collationStrength))
			{
				ucaCollator.AlternateHandling = alternateHandling;
				return ucaCollator.Compare(string1, string2);
			}
		}

		[TestCase(CaseFirst.LowerFirst, "china", "China", ExpectedResult = -1)]
		[TestCase(CaseFirst.LowerFirst, "China", "denmark", ExpectedResult = -1)]
		[TestCase(CaseFirst.LowerFirst, "denmark", "Denmark", ExpectedResult = -1)]
		[TestCase(CaseFirst.Off, "china", "China", ExpectedResult = -1)]
		[TestCase(CaseFirst.Off, "China", "denmark", ExpectedResult = -1)]
		[TestCase(CaseFirst.Off, "denmark", "Denmark", ExpectedResult = -1)]
		[TestCase(CaseFirst.UpperFirst, "China", "china", ExpectedResult = -1)]
		[TestCase(CaseFirst.UpperFirst, "china", "Denmark", ExpectedResult = -1)]
		[TestCase(CaseFirst.UpperFirst, "Denmark", "denmark", ExpectedResult = -1)]
		public int CaseFirstSetting(CaseFirst caseFirst, string string1, string string2)
		{
			/* The Case_First attribute is used to control whether uppercase letters
			 * come before lowercase letters or vice versa, in the absence of other
			 * differences in the strings. The possible values are Uppercase_First
			 * (U) and Lowercase_First (L), plus the standard Default and Off. There
			 * is almost no difference between the Off and Lowercase_First options in
			 * terms of results, so typically users will not use Lowercase_First: only
			 * Off or Uppercase_First. (People interested in the detailed differences
			 * between X and L should consult the Collation Customization ).
			 *   Specifying either L or U won't affect string comparison performance,
			 * but will affect the sort key length.
				Example:
					C=X or C=L "china" < "China" < "denmark" < "Denmark"
					C=U "China" < "china" < "Denmark" < "denmark"
			 */
			using (var ucaCollator = new RuleBasedCollator(string.Empty))
			{
				ucaCollator.CaseFirst = caseFirst;
				Assert.That(ucaCollator.CaseFirst, Is.EqualTo(caseFirst));
				return ucaCollator.Compare(string1, string2);
			}
		}

		[TestCase(CaseLevel.Off, "role", "Role", ExpectedResult = 0)]
		[TestCase(CaseLevel.Off, "role", "rôle", ExpectedResult = 0)]
		[TestCase(CaseLevel.On, "role", "rôle", ExpectedResult = 0)]
		[TestCase(CaseLevel.On, "rôle", "Role", ExpectedResult = -1)]
		public int CaseLevelSetting(CaseLevel caseLevel, string string1, string string2)
		{
			/*The Case_Level attribute is used when ignoring accents but not case. In
			 * such a situation, set Strength to be Primary, and Case_Level to be On.
			 * In most locales, this setting is Off by default. There is a small string
			 * comparison performance and sort key impact if this attribute is set to be On.
				Example:
				S=1, E=X role = Role = rôle
				S=1, E=O role = rôle <  Role*/
			using (var ucaCollator = new RuleBasedCollator(string.Empty, CollationStrength.Primary))
			{
				Assert.That(ucaCollator.CaseLevel, Is.EqualTo(CaseLevel.Off));
				ucaCollator.CaseLevel = caseLevel;
				return ucaCollator.Compare(string1, string2);
			}
		}

		[TestCase(FrenchCollation.Off, "cote", "coté", ExpectedResult = -1)]
		[TestCase(FrenchCollation.Off, "coté", "côte", ExpectedResult = -1)]
		[TestCase(FrenchCollation.Off, "côte", "côté", ExpectedResult = -1)]
		[TestCase(FrenchCollation.On, "cote", "côte", ExpectedResult = -1)]
		[TestCase(FrenchCollation.On, "côte", "coté", ExpectedResult = -1)]
		[TestCase(FrenchCollation.On, "coté", "côté", ExpectedResult = -1)]
		public int FrenchCollationSetting(FrenchCollation frenchCollation, string string1,
			string string2)
		{
			/*The French sort strings with different accents from the back of the
			 * string. This attribute is automatically set to On for the French
			 * locales and a few others. Users normally would not need to explicitly
			 * set this attribute. There is a string comparison performance cost
			 * when it is set On, but sort key length is unaffected.
				Example:
				F=X cote < coté < côte < côté
				F=O cote < côte < coté < côté
			 */
			using (var ucaCollator = new RuleBasedCollator(string.Empty))
			{
				Assert.That(ucaCollator.FrenchCollation, Is.EqualTo(FrenchCollation.Off));
				ucaCollator.FrenchCollation = frenchCollation;
				return ucaCollator.Compare(string1, string2);
			}
		}

		private static Collator CreateJaCollator()
		{
			try
			{
				return RuleBasedCollator.Create("ja");
			}
			catch
			{
				return new RuleBasedCollator(JaStandardRules);
			}
		}

		[TestCase(CollationStrength.Tertiary, "きゅう", "キュウ", ExpectedResult = 0)]
		[TestCase(CollationStrength.Tertiary, "キュウ", "きゆう", ExpectedResult = -1)]
		[TestCase(CollationStrength.Tertiary, "きゆう", "キユウ", ExpectedResult = 0)]
		[TestCase(CollationStrength.Quaternary, "きゅう", "キュウ", ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, "キュウ", "きゆう", ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, "きゆう", "キユウ", ExpectedResult = -1)]
		public int HiraganaQuarternarySetting(CollationStrength collationStrength, string string1,
			string string2)
		{
			/* Compatibility with JIS x 4061 requires the introduction of an
			 * additional level to distinguish Hiragana and Katakana characters.
			 * If compatibility with that standard is required, then this attribute
			 * should be set On, and the strength set to Quaternary. This will affect
			 * sort key length and string comparison string comparison performance.
				Example:
				H=X, S=4 きゅう = キュウ < きゆう = キユウ
				H=O, S=4 きゅう < キュウ < きゆう < キユウ
			 */
			using (var jaCollator = CreateJaCollator())
			{
				// In ICU54 the HiraganaQuaternary special feature is deprecated in favor of supporting
				// quaternary sorting as a regular feature.
				jaCollator.Strength = collationStrength;
				return jaCollator.Compare(string1, string2);
			}
		}

		[TestCase(NormalizationMode.Off, "ä", "a\u0308", ExpectedResult = 0)]
		[TestCase(NormalizationMode.Off, "a\u0308", "ä\u0323", ExpectedResult = -1)]
		[TestCase(NormalizationMode.Off, "ä\u0323", "ạ\u0308", ExpectedResult = -1)]
		[TestCase(NormalizationMode.On, "ä", "a\u0308", ExpectedResult = 0)]
		[TestCase(NormalizationMode.On, "a\u0308", "ä\u0323", ExpectedResult = -1)]
		[TestCase(NormalizationMode.On, "ä\u0323", "ạ\u0308", ExpectedResult = 0)]
		public int NormalizationModeSetting(NormalizationMode normalizationMode, string string1,
			string string2)
		{
			/*The Normalization setting determines whether text is thoroughly
			 * normalized or not in comparison. Even if the setting is off (which
			 * is the default for many locales), text as represented in common usage
			 * will compare correctly (for details, see UTN #5 ). Only if the accent
			 * marks are in non-canonical order will there be a problem. If the
			 * setting is On, then the best results are guaranteed for all possible
			 * text input.There is a medium string comparison performance cost if
			 * this attribute is On, depending on the frequency of sequences that
			 * require normalization. There is no significant effect on sort key
			 * length.If the input text is known to be in NFD or NFKD normalization
			 * forms, there is no need to enable this Normalization option.
				Example:
				N=X ä = a + ◌̈ < ä + ◌̣ < ạ + ◌̈
				N=O ä = a + ◌̈ < ä + ◌̣ = ạ + ◌̈
			 */
			using (var ucaCollator =
				new RuleBasedCollator(string.Empty, normalizationMode, CollationStrength.Default))
			{
				return ucaCollator.Compare(string1, string2);
			}
		}

		//  1 < 10 < 2 < 20
		[TestCase(NumericCollation.Off, "1", "10", ExpectedResult = -1)]
		[TestCase(NumericCollation.Off, "10", "2", ExpectedResult = -1)]
		[TestCase(NumericCollation.Off, "2", "20", ExpectedResult = -1)]
		//  1 < 2 < 10 < 20
		[TestCase(NumericCollation.On, "1", "10", ExpectedResult = -1)]
		[TestCase(NumericCollation.On, "10", "2", ExpectedResult = 1)]
		[TestCase(NumericCollation.On, "2", "20", ExpectedResult = -1)]
		public int NumericCollationSetting(NumericCollation numericCollation, string string1,
			string string2)
		{
			using (var ucaCollator = new RuleBasedCollator(string.Empty))
			{
				Assert.AreEqual(NumericCollation.Off, ucaCollator.NumericCollation);
				ucaCollator.NumericCollation = numericCollation;
				return ucaCollator.Compare(string1, string2);
			}
		}

		[TestCase(CollationStrength.Primary, "role", "Role", ExpectedResult = 0)]
		[TestCase(CollationStrength.Primary, "Role", "rôle", ExpectedResult = 0)]
		[TestCase(CollationStrength.Secondary, "role", "Role", ExpectedResult = 0)]
		[TestCase(CollationStrength.Secondary, "Role", "rôle", ExpectedResult = -1)]
		[TestCase(CollationStrength.Tertiary, "role", "Role", ExpectedResult = -1)]
		[TestCase(CollationStrength.Tertiary, "Role", "rôle", ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, "ab", "a c", ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, "a c", "a-c", ExpectedResult = -1)]
		[TestCase(CollationStrength.Quaternary, "a-c", "ac", ExpectedResult = -1)]
		public int StrengthSetting(CollationStrength collationStrength, string string1,
			string string2)
		{
			/*The Strength attribute determines whether accents or case are taken
			 * into account when collating or matching text. ( (In writing systems
			 * without case or accents, it controls similarly important features).
			 * The default strength setting usually does not need to be changed for
			 * collating (sorting), but often needs to be changed when matching
			 * (e.g. SELECT). The possible values include Default (D), Primary
			 * (1), Secondary (2), Tertiary (3), Quaternary (4), and Identical (I).
			 *
			 * For example, people may choose to ignore accents or ignore accents and
			 * case when searching for text.
			 *
			 * Almost all characters are distinguished by the first three levels, and
			 * in most locales the default value is thus Tertiary. However, if
			 * Alternate is set to be Shifted, then the Quaternary strength (4)
			 * can be used to break ties among whitespace, punctuation, and symbols
			 * that would otherwise be ignored. If very fine distinctions among
			 * characters are required, then the Identical strength (I) can be
			 * used (for example, Identical Strength distinguishes between the
			 * Mathematical Bold Small A and the Mathematical Italic Small A. For
			 * more examples, look at the cells with white backgrounds in the
			 * collation charts). However, using levels higher than Tertiary -
			 * the Identical strength - result in significantly longer sort keys,
			 * and slower string comparison performance for equal strings.
				Example:
				S=1 role = Role = rôle
				S=2 role = Role < rôle
				S=3 role < Role < rôle
				A=S  S=4 ab < a c < a-c < ac*/
			using (var ucaCollator = new RuleBasedCollator(string.Empty, collationStrength))
			{
				if (collationStrength == CollationStrength.Quaternary)
					ucaCollator.AlternateHandling = AlternateHandling.Shifted;
				Assert.That(ucaCollator.Strength, Is.EqualTo(collationStrength));
				return ucaCollator.Compare(string1, string2);
			}
		}

		[Test]
		public void GetAvailableLocales_ReturnsList()
		{
			IList<string> locales = RuleBasedCollator.GetAvailableCollationLocales();
			Assert.IsNotNull(locales);
		}

		#region JaKanaRules
		// Rules from ja.ldml collations
		// collation type="private-kana"
		private const string JaKanaRules = @"&ゝ<<<<ヽ # iteration marks \u309D, \u30FD
# The length mark sorts tertiary less-than the
# small version of the preceding vowel.
&[before 3]ぁ # A
<<<ぁ|ー=あ|ー=か|ー=ゕ|ー=が|ー=さ|ー=ざ|ー=た|ー=だ|ー=な|ー=は|ー=ば|ー=ぱ|ー=ま|ー=ゃ|ー=や|ー=ら|ー=ゎ|ー=わ|ー # Hiragana
<<<<ァ|ー=ｧ|ー=ア|ー=ｱ|ー=カ|ー=ｶ|ー=ガ|ー # Katakana
=サ|ー=ｻ|ー=ザ|ー=タ|ー=ﾀ|ー=ダ|ー=ナ|ー=ﾅ|ー=ハ|ー=ﾊ|ー=ㇵ|ー=バ|ー=パ|ー
=マ|ー=ﾏ|ー=ャ|ー=ｬ|ー=ヤ|ー=ﾔ|ー=ラ|ー=ﾗ|ー=ㇻ|ー=ヮ|ー=ワ|ー=ﾜ|ー=ヵ|ー=ヷ|ー
&[before 3]ぃ # I
<<<ぃ|ー=い|ー=き|ー=ぎ|ー=し|ー=じ|ー=ち|ー=ぢ|ー=に|ー=ひ|ー=び|ー=ぴ|ー=み|ー=り|ー=ゐ|ー # Hiragana
<<<<ィ|ー=ｨ|ー=イ|ー=ｲ|ー=キ|ー=ｷ|ー=ギ|ー=シ|ー=ｼ|ー=ㇱ|ー=ジ|ー # Katakana
=チ|ー=ﾁ|ー=ヂ|ー=ニ|ー=ﾆ|ー=ヒ|ー=ﾋ|ー=ㇶ|ー=ビ|ー=ピ|ー
=ミ|ー=ﾐ|ー=リ|ー=ﾘ|ー=ㇼ|ー=ヰ|ー=ヸ|ー
&[before 3]ぅ # U
<<<ぅ|ー=う|ー=く|ー=ぐ|ー=す|ー=ず|ー=っ|ー=つ|ー=づ|ー=ぬ|ー=ふ|ー=ぶ|ー=ぷ|ー=む|ー=ゅ|ー=ゆ|ー=る|ー=ゔ|ー # Hiragana
<<<<ゥ|ー=ｩ|ー=ウ|ー=ｳ|ー=ク|ー=ｸ|ー=ㇰ|ー=グ|ー # Katakana
=ス|ー=ｽ|ー=ㇲ|ー=ズ|ー=ッ|ー=ｯ|ー=ツ|ー=ﾂ|ー=ヅ|ー=ヌ|ー=ﾇ|ー=ㇴ|ー
=フ|ー=ﾌ|ー=ㇷ|ー=ブ|ー=プ|ー=ム|ー=ﾑ|ー=ㇺ|ー=ュ|ー=ｭ|ー=ユ|ー=ﾕ|ー=ル|ー=ﾙ|ー=ㇽ|ー=ヴ|ー
&[before 3]ぇ # E
<<<ぇ|ー=え|ー=け|ー=ゖ|ー=げ|ー=せ|ー=ぜ|ー=て|ー=で|ー=ね|ー=へ|ー=べ|ー=ぺ|ー=め|ー=れ|ー=ゑ|ー # Hiragana
<<<<ェ|ー=ｪ|ー=エ|ー=ｴ|ー=ケ|ー=ｹ|ー=ゲ|ー # Katakana
=セ|ー=ｾ|ー=ゼ|ー=テ|ー=ﾃ|ー=デ|ー=ネ|ー=ﾈ|ー=ヘ|ー=ﾍ|ー=ㇸ|ー=ベ|ー=ペ|ー
=メ|ー=ﾒ|ー=レ|ー=ﾚ|ー=ㇾ|ー=ヱ|ー=ヶ|ー=ヹ|ー
&[before 3]ぉ # O
<<<ぉ|ー=お|ー=こ|ー=ご|ー=そ|ー=ぞ|ー=と|ー=ど|ー=の|ー=ほ|ー=ぼ|ー=ぽ|ー=も|ー=ょ|ー=よ|ー=ろ|ー=を|ー # Hiragana
<<<<ォ|ー=ｫ|ー=オ|ー=ｵ|ー=コ|ー=ｺ|ー=ゴ|ー=ソ|ー=ｿ|ー=ゾ|ー=ト|ー=ﾄ|ー=ㇳ|ー=ド|ー # Katakana
=ノ|ー=ﾉ|ー=ホ|ー=ﾎ|ー=ㇹ|ー=ボ|ー=ポ|ー=モ|ー=ﾓ|ー=ョ|ー=ｮ|ー=ヨ|ー=ﾖ|ー
=ロ|ー=ﾛ|ー=ㇿ|ー=ヲ|ー=ｦ|ー=ヺ|ー
# The iteration mark sorts tertiary-between small and large Kana.
&[before 3]あ # A
<<<あ|ゝ=ぁ|ゝ
<<<<ア|ヽ=ｱ|ヽ=ァ|ヽ=ｧ|ヽ
&[before 3]い # I
<<<い|ゝ=ぃ|ゝ
<<<<イ|ヽ=ｲ|ヽ=ィ|ヽ=ｨ|ヽ
&[before 3]う # U
<<<う|ゝ=ぅ|ゝ=ゔ|ゝ=う|ゞ/゙=ぅ|ゞ/゙=ゔ|ゞ/゙
<<<<ウ|ヽ=ｳ|ヽ=ゥ|ヽ=ｩ|ヽ=ヴ|ヽ=ウ|ヾ/゙=ｳ|ヾ/゙=ゥ|ヾ/゙=ｩ|ヾ/゙=ヴ|ヾ/゙
&[before 3]え # E
<<<え|ゝ=ぇ|ゝ
<<<<エ|ヽ=ｴ|ヽ=ェ|ヽ=ｪ|ヽ
&[before 3]お # O
<<<お|ゝ=ぉ|ゝ
<<<<オ|ヽ=ｵ|ヽ=ォ|ヽ=ｫ|ヽ
&[before 3]か # KA
<<<か|ゝ=ゕ|ゝ
<<<<カ|ヽ=ｶ|ヽ=ヵ|ヽ
&[before 3]が # GA
<<<が|ゝ
<<<<ガ|ヽ
&[before 3]き # KI
<<<き|ゝ=ぎ|ゝ=き|ゞ/゙=ぎ|ゞ/゙
<<<<キ|ヽ=ｷ|ヽ=ギ|ヽ=キ|ヾ/゙=ｷ|ヾ/゙=ギ|ヾ/゙
&[before 3]く # KU
<<<く|ゝ=ぐ|ゝ=く|ゞ/゙=ぐ|ゞ/゙
<<<<ク|ヽ=ｸ|ヽ=ㇰ|ヽ=グ|ヽ=ク|ヾ/゙=ｸ|ヾ/゙=ㇰ|ヾ/゙=グ|ヾ/゙
&[before 3]け # KE
<<<け|ゝ=ゖ|ゝ
<<<<ケ|ヽ=ｹ|ヽ=ヶ|ヽ
&[before 3]げ # GE
<<<げ|ゝ
<<<<ゲ|ヽ
&[before 3]こ # KO
<<<こ|ゝ=ご|ゝ=こ|ゞ/゙=ご|ゞ/゙
<<<<コ|ヽ=ｺ|ヽ=ゴ|ヽ=コ|ヾ/゙=ｺ|ヾ/゙=ゴ|ヾ/゙
&[before 3]さ # SA
<<<さ|ゝ=ざ|ゝ=さ|ゞ/゙=ざ|ゞ/゙
<<<<サ|ヽ=ｻ|ヽ=ザ|ヽ=サ|ヾ/゙=ｻ|ヾ/゙=ザ|ヾ/゙
&[before 3]し # SI
<<<し|ゝ=じ|ゝ=し|ゞ/゙=じ|ゞ/゙
<<<<シ|ヽ=ｼ|ヽ=ㇱ|ヽ=ジ|ヽ=シ|ヾ/゙=ｼ|ヾ/゙=ㇱ|ヾ/゙=ジ|ヾ/゙
&[before 3]す # SU
<<<す|ゝ=ず|ゝ=す|ゞ/゙=ず|ゞ/゙
<<<<ス|ヽ=ｽ|ヽ=ㇲ|ヽ=ズ|ヽ=ス|ヾ/゙=ｽ|ヾ/゙=ㇲ|ヾ/゙=ズ|ヾ/゙
&[before 3]せ # SE
<<<せ|ゝ=ぜ|ゝ=せ|ゞ/゙=ぜ|ゞ/゙
<<<<セ|ヽ=ｾ|ヽ=ゼ|ヽ=セ|ヾ/゙=ｾ|ヾ/゙=ゼ|ヾ/゙
&[before 3]そ # SO
<<<そ|ゝ=ぞ|ゝ=そ|ゞ/゙=ぞ|ゞ/゙
<<<<ソ|ヽ=ｿ|ヽ=ゾ|ヽ=ソ|ヾ/゙=ｿ|ヾ/゙=ゾ|ヾ/゙
&[before 3]た # TA
<<<た|ゝ=だ|ゝ=た|ゞ/゙=だ|ゞ/゙
<<<<タ|ヽ=ﾀ|ヽ=ダ|ヽ=タ|ヾ/゙=ﾀ|ヾ/゙=ダ|ヾ/゙
&[before 3]ち # TI
<<<ち|ゝ=ぢ|ゝ=ち|ゞ/゙=ぢ|ゞ/゙
<<<<チ|ヽ=ﾁ|ヽ=ヂ|ヽ=チ|ヾ/゙=ﾁ|ヾ/゙=ヂ|ヾ/゙
&[before 3]つ # TU
<<<つ|ゝ=っ|ゝ=づ|ゝ=つ|ゞ/゙=づ|ゞ/゙=つ|ゝ=っ|ゞ/゙=つ|ゞ/゙
<<<<ツ|ヽ=ﾂ|ヽ=ッ|ヽ=ｯ|ヽ=ヅ|ヽ=ツ|ヾ/゙=ﾂ|ヾ/゙=ヅ|ヾ/゙=ツ|ヽ=ﾂ|ヽ=ッ|ヾ/゙=ｯ|ヾ/゙=ツ|ヾ/゙=ﾂ|ヾ/゙
&[before 3]て # TE
<<<て|ゝ=で|ゝ=て|ゞ/゙=で|ゞ/゙
<<<<テ|ヽ=ﾃ|ヽ=デ|ヽ=テ|ヾ/゙=ﾃ|ヾ/゙=デ|ヾ/゙
&[before 3]と # TO
<<<と|ゝ=ど|ゝ=と|ゞ/゙=ど|ゞ/゙
<<<<ト|ヽ=ﾄ|ヽ=ㇳ|ヽ=ド|ヽ=ト|ヾ/゙=ﾄ|ヾ/゙=ㇳ|ヾ/゙=ド|ヾ/゙
&[before 3]な # NA
<<<な|ゝ
<<<<ナ|ヽ=ﾅ|ヽ
&[before 3]に # NI
<<<に|ゝ
<<<<ニ|ヽ=ﾆ|ヽ
&[before 3]ぬ # NU
<<<ぬ|ゝ
<<<<ヌ|ヽ=ﾇ|ヽ=ㇴ|ヽ # \u31F4
&[before 3]ね # NE
<<<ね|ゝ
<<<<ネ|ヽ=ﾈ|ヽ
&[before 3]の # NO
<<<の|ゝ
<<<<ノ|ヽ=ﾉ|ヽ
&[before 3]は # HA
<<<は|ゝ=ば|ゝ=は|ゞ/゙=ば|ゞ/゙=ぱ|ゝ=ぱ|ゞ/゙
<<<<ハ|ヽ=ﾊ|ヽ=ㇵ|ヽ=バ|ヽ=ハ|ヾ/゙=ﾊ|ヾ/゙=ㇵ|ヾ/゙=バ|ヾ/゙=パ|ヽ=パ|ヾ/゙
&[before 3]ひ # HI
<<<ひ|ゝ=び|ゝ=ひ|ゞ/゙=び|ゞ/゙=ぴ|ゝ=ぴ|ゞ/゙
<<<<ヒ|ヽ=ﾋ|ヽ=ㇶ|ヽ=ビ|ヽ=ヒ|ヾ/゙=ﾋ|ヾ/゙=ㇶ|ヾ/゙=ビ|ヾ/゙=ピ|ヽ=ピ|ヾ/゙
&[before 3]ふ # HU
<<<ふ|ゝ=ぶ|ゝ=ふ|ゞ/゙=ぶ|ゞ/゙=ぷ|ゝ=ぷ|ゞ/゙
<<<<フ|ヽ=ﾌ|ヽ=ㇷ|ヽ=ブ|ヽ=フ|ヾ/゙=ﾌ|ヾ/゙=ㇷ|ヾ/゙=ブ|ヾ/゙=プ|ヽ=プ|ヾ/゙
&[before 3]へ # HE
<<<へ|ゝ=べ|ゝ=へ|ゞ/゙=べ|ゞ/゙=ぺ|ゝ=ぺ|ゞ/゙
<<<<ヘ|ヽ=ﾍ|ヽ=ㇸ|ヽ=ベ|ヽ=ヘ|ヾ/゙=ﾍ|ヾ/゙=ㇸ|ヾ/゙=ベ|ヾ/゙=ペ|ヽ=ペ|ヾ/゙
&[before 3]ほ # HO
<<<ほ|ゝ=ぼ|ゝ=ほ|ゞ/゙=ぼ|ゞ/゙=ぽ|ゝ=ぽ|ゞ/゙
<<<<ホ|ヽ=ﾎ|ヽ=ㇹ|ヽ=ボ|ヽ=ホ|ヾ/゙=ﾎ|ヾ/゙=ㇹ|ヾ/゙=ボ|ヾ/゙=ポ|ヽ=ポ|ヾ/゙
&[before 3]ま # MA
<<<ま|ゝ
<<<<マ|ヽ=ﾏ|ヽ
&[before 3]み # MI
<<<み|ゝ
<<<<ミ|ヽ=ﾐ|ヽ
&[before 3]む # MU
<<<む|ゝ
<<<<ム|ヽ=ﾑ|ヽ=ㇺ|ヽ # \u31FA
&[before 3]め # ME
<<<め|ゝ
<<<<メ|ヽ=ﾒ|ヽ
&[before 3]も # MO
<<<も|ゝ
<<<<モ|ヽ=ﾓ|ヽ
&[before 3]や # YA
<<<や|ゝ=ゃ|ゝ
<<<<ヤ|ヽ=ﾔ|ヽ=ャ|ヽ=ｬ|ヽ
&[before 3]ゆ # YU
<<<ゆ|ゝ=ゅ|ゝ
<<<<ユ|ヽ=ﾕ|ヽ=ュ|ヽ=ｭ|ヽ
&[before 3]よ # YO
<<<よ|ゝ=ょ|ゝ
<<<<ヨ|ヽ=ﾖ|ヽ=ョ|ヽ=ｮ|ヽ
&[before 3]ら # RA
<<<ら|ゝ
<<<<ラ|ヽ=ﾗ|ヽ=ㇻ|ヽ # \u31FB
&[before 3]り # RI
<<<り|ゝ
<<<<リ|ヽ=ﾘ|ヽ=ㇼ|ヽ # \u31FC
&[before 3]る # RU
<<<る|ゝ
<<<<ル|ヽ=ﾙ|ヽ=ㇽ|ヽ # \u31FD
&[before 3]れ # RE
<<<れ|ゝ
<<<<レ|ヽ=ﾚ|ヽ=ㇾ|ヽ # \u31FE
&[before 3]ろ # RO
<<<ろ|ゝ
<<<<ロ|ヽ=ﾛ|ヽ=ㇿ|ヽ # \u31FF
&[before 3]わ # WA
<<<わ|ゝ=ゎ|ゝ=わ|ゞ/゙=ゎ|ゞ/゙
<<<<ワ|ヽ=ﾜ|ヽ=ヮ|ヽ=ヷ|ヽ=ワ|ヾ/゙=ﾜ|ヾ/゙=ヷ|ヾ/゙=ヮ|ヾ/゙
&[before 3]ゐ # WI
<<<ゐ|ゝ=ゐ|ゞ/゙
<<<<ヰ|ヽ=ヸ|ヽ=ヰ|ヾ/゙=ヸ|ヾ/゙
&[before 3]ゑ # WE
<<<ゑ|ゝ=ゑ|ゞ/゙
<<<<ヱ|ヽ=ヹ|ヽ=ヱ|ヾ/゙=ヹ|ヾ/゙
&[before 3]を # WO
<<<を|ゝ=を|ゞ/゙
<<<<ヲ|ヽ=ｦ|ヽ=ヺ|ヽ=ヲ|ヾ/゙=ｦ|ヾ/゙=ヺ|ヾ/゙
&[before 3]ん # N
<<<ん|ゝ
<<<<ン|ヽ=ﾝ|ヽ
&ぁ<<<<ァ=ｧ # SMALL A
&あ<<<<ア=ｱ # A
&ぃ<<<<ィ=ｨ # SMALL I
&い<<<<イ=ｲ # I
&ぅ<<<<ゥ=ｩ # SMALL U
&う<<<<ウ=ｳ # U
&ぇ<<<<ェ=ｪ # SMALL E
&え<<<<エ=ｴ # E
&ぉ<<<<ォ=ｫ # SMALL O
&お<<<<オ=ｵ # O
&か<<<<カ=ｶ # KA
&き<<<<キ=ｷ # KI
&く<<<<ク=ｸ # KU
&け<<<<ケ=ｹ # KE
&こ<<<<コ=ｺ # KO
&さ<<<<サ=ｻ # SA
&し<<<<シ=ｼ # SI
&す<<<<ス=ｽ # SU
&せ<<<<セ=ｾ # SE
&そ<<<<ソ=ｿ # SO
&た<<<<タ=ﾀ # TA
&ち<<<<チ=ﾁ # TI
&っ<<<<ッ=ｯ # SMALL TU
&つ<<<<ツ=ﾂ # TU
&て<<<<テ=ﾃ # TE
&と<<<<ト=ﾄ # TO
&な<<<<ナ=ﾅ # NA
&に<<<<ニ=ﾆ # NI
&ぬ<<<<ヌ=ﾇ # NU
&ね<<<<ネ=ﾈ # NE
&の<<<<ノ=ﾉ # NO
&は<<<<ハ=ﾊ # HA
&ひ<<<<ヒ=ﾋ # HI
&ふ<<<<フ=ﾌ # HU
&へ<<<<ヘ=ﾍ # HE
&ほ<<<<ホ=ﾎ # HO
&ま<<<<マ=ﾏ # MA
&み<<<<ミ=ﾐ # MI
&む<<<<ム=ﾑ # MU
&め<<<<メ=ﾒ # ME
&も<<<<モ=ﾓ # MO
&ゃ<<<<ャ=ｬ # SMALL YA
&や<<<<ヤ=ﾔ # YA
&ゅ<<<<ュ=ｭ # SMALL YU
&ゆ<<<<ユ=ﾕ # YU
&ょ<<<<ョ=ｮ # SMALL YO
&よ<<<<ヨ=ﾖ # YO
&ら<<<<ラ=ﾗ # RA
&り<<<<リ=ﾘ # RI
&る<<<<ル=ﾙ # RU
&れ<<<<レ=ﾚ # RE
&ろ<<<<ロ=ﾛ # RO
&ゎ<<<<ヮ # SMALL WA
&わ<<<<ワ=ﾜ # WA
&ゐ<<<<ヰ # WI
&ゑ<<<<ヱ # WE
&を<<<<ヲ=ｦ # WO
&ん<<<<ン=ﾝ # N
&ゕ<<<<ヵ # SMALL KA
&ゖ<<<<ヶ # SMALL KE
&より # \u3088\u308A
<<ゟ # \u309F HIRAGANA DIGRAPH YORI
&コト # \u30B3\u30C8
<<ヿ # \u30FF KATAKANA DIGRAPH KOTO
&'\u0020'=*'\u3000'￣
&'!'=！
&'\u0022'=＂
&'\u0023'=＃
&'$'=＄
&'%'=％
&'&'=＆
&''=＇
&'('=（
&')'=）
&'*'=＊
&'+'=＋
&','=，
&'-'=－
&'.'=．
&'/'=／
&0=０
&1=１
&2=２
&3=３
&4=４
&5=５
&6=６
&7=７
&8=８
&9=９
&':'=：
&';'=；
&'<'=＜
&'='=＝
&'>'=＞
&'?'=？
&'@'=＠
&A=Ａ
&B=Ｂ
&C=Ｃ
&D=Ｄ
&E=Ｅ
&F=Ｆ
&G=Ｇ
&H=Ｈ
&I=Ｉ
&J=Ｊ
&K=Ｋ
&L=Ｌ
&M=Ｍ
&N=Ｎ
&O=Ｏ
&P=Ｐ
&Q=Ｑ
&R=Ｒ
&S=Ｓ
&T=Ｔ
&U=Ｕ
&V=Ｖ
&W=Ｗ
&X=Ｘ
&Y=Ｙ
&Z=Ｚ
&'['=［
&'\\'=＼  # \\ unescapes to \ which is not special between 'apostrophes'
&']'=］
&'^'=＾
&'_'=＿
&'`'=｀
&a=ａ
&b=ｂ
&c=ｃ
&d=ｄ
&e=ｅ
&f=ｆ
&g=ｇ
&h=ｈ
&i=ｉ
&j=ｊ
&k=ｋ
&l=ｌ
&m=ｍ
&n=ｎ
&o=ｏ
&p=ｐ
&q=ｑ
&r=ｒ
&s=ｓ
&t=ｔ
&u=ｕ
&v=ｖ
&w=ｗ
&x=ｘ
&y=ｙ
&z=ｚ
&'{'=｛
&'|'=｜
&'}'=｝
&'~'=～
&¢=￠
&£=￡
&¥=￥
&¦=￤
&¬=￢
&ᄀ=ﾡ=ㄱ
&ᄁ=ﾢ=ㄲ
&ᄂ=ﾤ=ㄴ
&ᄃ=ﾧ=ㄷ
&ᄄ=ﾨ=ㄸ
&ᄅ=ﾩ=ㄹ
&ᄆ=ﾱ=ㅁ
&ᄇ=ﾲ=ㅂ
&ᄈ=ﾳ=ㅃ
&ᄉ=ﾵ=ㅅ
&ᄊ=ﾶ=ㅆ
&ᄋ=ﾷ=ㅇ
&ᄌ=ﾸ=ㅈ
&ᄍ=ﾹ=ㅉ
&ᄎ=ﾺ=ㅊ
&ᄏ=ﾻ=ㅋ
&ᄐ=ﾼ=ㅌ
&ᄑ=ﾽ=ㅍ
&ᄒ=ﾾ=ㅎ
&ᄚ=ﾰ=ㅀ
&ᄡ=ﾴ=ㅄ
&ᅠ=ﾠ=ㅤ
&ᅡ=ￂ=ㅏ
&ᅢ=ￃ=ㅐ
&ᅣ=ￄ=ㅑ
&ᅤ=ￅ=ㅒ
&ᅥ=ￆ=ㅓ
&ᅦ=ￇ=ㅔ
&ᅧ=ￊ=ㅕ
&ᅨ=ￋ=ㅖ
&ᅩ=ￌ=ㅗ
&ᅪ=ￍ=ㅘ
&ᅫ=ￎ=ㅙ
&ᅬ=ￏ=ㅚ
&ᅭ=ￒ=ㅛ
&ᅮ=ￓ=ㅜ
&ᅯ=ￔ=ㅝ
&ᅰ=ￕ=ㅞ
&ᅱ=ￖ=ㅟ
&ᅲ=ￗ=ㅠ
&ᅳ=ￚ=ㅡ
&ᅴ=ￛ=ㅢ
&ᅵ=ￜ=ㅣ
&ᆪ=ﾣ=ㄳ
&ᆬ=ﾥ=ㄵ
&ᆭ=ﾦ=ㄶ
&ᆰ=ﾪ=ㄺ
&ᆱ=ﾫ=ㄻ
&ᆲ=ﾬ=ㄼ
&ᆳ=ﾭ=ㄽ
&ᆴ=ﾮ=ㄾ
&ᆵ=ﾯ=ㄿ
&₩=￦
&←=￩
&↑=￪
&→=￫
&↓=￬
&│=￨
&■=￭
&○=￮
&、=､
&。=｡
&「=｢
&」=｣";

		// collation type="standard"
		private const string JaStandardRules = JaKanaRules + @"&[last regular]<*亜唖娃阿哀愛挨姶逢葵茜穐悪握渥旭葦芦鯵梓圧斡扱宛姐虻飴絢綾鮎或粟袷安庵按暗案闇鞍杏以伊位依偉囲夷委威尉惟意慰易椅為畏異移維緯胃萎衣謂違遺医井亥域育郁磯一壱溢逸稲茨芋鰯允印咽員因姻引飲淫胤蔭院陰隠韻吋右宇烏羽迂雨卯鵜窺丑碓臼渦嘘唄欝蔚鰻姥厩浦瓜閏噂云運雲荏餌叡営嬰影映曳栄永泳洩瑛盈穎頴英衛詠鋭液疫益駅悦謁越閲榎厭円園堰奄宴延怨掩援沿演炎焔煙燕猿縁艶苑薗遠鉛鴛塩於汚甥凹央奥往応押旺横欧殴王翁襖鴬鴎黄岡沖荻億屋憶臆桶牡乙俺卸恩温穏音下化仮何伽価佳加可嘉夏嫁家寡科暇果架歌河火珂禍禾稼箇花苛茄荷華菓蝦課嘩貨迦過霞蚊俄峨我牙画臥芽蛾賀雅餓駕介会解回塊壊廻快怪悔恢懐戒拐改魁晦械海灰界皆絵芥蟹開階貝凱劾外咳害崖慨概涯碍蓋街該鎧骸浬馨蛙垣柿蛎鈎劃嚇各廓拡撹格核殻獲確穫覚角赫較郭閣隔革学岳楽額顎掛笠樫橿梶鰍潟割喝恰括活渇滑葛褐轄且鰹叶椛樺鞄株兜竃蒲釜鎌噛鴨栢茅萱粥刈苅瓦乾侃冠寒刊勘勧巻喚堪姦完官寛干幹患感慣憾換敢柑桓棺款歓汗漢澗潅環甘監看竿管簡緩缶翰肝艦莞観諌貫還鑑間閑関陥韓館舘丸含岸巌玩癌眼岩翫贋雁頑顔願企伎危喜器基奇嬉寄岐希幾忌揮机旗既期棋棄機帰毅気汽畿祈季稀紀徽規記貴起軌輝飢騎鬼亀偽儀妓宜戯技擬欺犠疑祇義蟻誼議掬菊鞠吉吃喫桔橘詰砧杵黍却客脚虐逆丘久仇休及吸宮弓急救朽求汲泣灸球究窮笈級糾給旧牛去居巨拒拠挙渠虚許距鋸漁禦魚亨享京供侠僑兇競共凶協匡卿叫喬境峡強彊怯恐恭挟教橋況狂狭矯胸脅興蕎郷鏡響饗驚仰凝尭暁業局曲極玉桐粁僅勤均巾錦斤欣欽琴禁禽筋緊芹菌衿襟謹近金吟銀九倶句区狗玖矩苦躯駆駈駒具愚虞喰空偶寓遇隅串櫛釧屑屈掘窟沓靴轡窪熊隈粂栗繰桑鍬勲君薫訓群軍郡卦袈祁係傾刑兄啓圭珪型契形径恵慶慧憩掲携敬景桂渓畦稽系経継繋罫茎荊蛍計詣警軽頚鶏芸迎鯨劇戟撃激隙桁傑欠決潔穴結血訣月件倹倦健兼券剣喧圏堅嫌建憲懸拳捲検権牽犬献研硯絹県肩見謙賢軒遣鍵険顕験鹸元原厳幻弦減源玄現絃舷言諺限乎個古呼固姑孤己庫弧戸故枯湖狐糊袴股胡菰虎誇跨鈷雇顧鼓五互伍午呉吾娯後御悟梧檎瑚碁語誤護醐乞鯉交佼侯候倖光公功効勾厚口向后喉坑垢好孔孝宏工巧巷幸広庚康弘恒慌抗拘控攻昂晃更杭校梗構江洪浩港溝甲皇硬稿糠紅紘絞綱耕考肯肱腔膏航荒行衡講貢購郊酵鉱砿鋼閤降項香高鴻剛劫号合壕拷濠豪轟麹克刻告国穀酷鵠黒獄漉腰甑忽惚骨狛込此頃今困坤墾婚恨懇昏昆根梱混痕紺艮魂些佐叉唆嵯左差査沙瑳砂詐鎖裟坐座挫債催再最哉塞妻宰彩才採栽歳済災采犀砕砦祭斎細菜裁載際剤在材罪財冴坂阪堺榊肴咲崎埼碕鷺作削咋搾昨朔柵窄策索錯桜鮭笹匙冊刷察拶撮擦札殺薩雑皐鯖捌錆鮫皿晒三傘参山惨撒散桟燦珊産算纂蚕讃賛酸餐斬暫残仕仔伺使刺司史嗣四士始姉姿子屍市師志思指支孜斯施旨枝止死氏獅祉私糸紙紫肢脂至視詞詩試誌諮資賜雌飼歯事似侍児字寺慈持時次滋治爾璽痔磁示而耳自蒔辞汐鹿式識鴫竺軸宍雫七叱執失嫉室悉湿漆疾質実蔀篠偲柴芝屡蕊縞舎写射捨赦斜煮社紗者謝車遮蛇邪借勺尺杓灼爵酌釈錫若寂弱惹主取守手朱殊狩珠種腫趣酒首儒受呪寿授樹綬需囚収周宗就州修愁拾洲秀秋終繍習臭舟蒐衆襲讐蹴輯週酋酬集醜什住充十従戎柔汁渋獣縦重銃叔夙宿淑祝縮粛塾熟出術述俊峻春瞬竣舜駿准循旬楯殉淳準潤盾純巡遵醇順処初所暑曙渚庶緒署書薯藷諸助叙女序徐恕鋤除傷償勝匠升召哨商唱嘗奨妾娼宵将小少尚庄床廠彰承抄招掌捷昇昌昭晶松梢樟樵沼消渉湘焼焦照症省硝礁祥称章笑粧紹肖菖蒋蕉衝裳訟証詔詳象賞醤鉦鍾鐘障鞘上丈丞乗冗剰城場壌嬢常情擾条杖浄状畳穣蒸譲醸錠嘱埴飾拭植殖燭織職色触食蝕辱尻伸信侵唇娠寝審心慎振新晋森榛浸深申疹真神秦紳臣芯薪親診身辛進針震人仁刃塵壬尋甚尽腎訊迅陣靭笥諏須酢図厨逗吹垂帥推水炊睡粋翠衰遂酔錐錘随瑞髄崇嵩数枢趨雛据杉椙菅頗雀裾澄摺寸世瀬畝是凄制勢姓征性成政整星晴棲栖正清牲生盛精聖声製西誠誓請逝醒青静斉税脆隻席惜戚斥昔析石積籍績脊責赤跡蹟碩切拙接摂折設窃節説雪絶舌蝉仙先千占宣専尖川戦扇撰栓栴泉浅洗染潜煎煽旋穿箭線繊羨腺舛船薦詮賎践選遷銭銑閃鮮前善漸然全禅繕膳糎噌塑岨措曾曽楚狙疏疎礎祖租粗素組蘇訴阻遡鼠僧創双叢倉喪壮奏爽宋層匝惣想捜掃挿掻操早曹巣槍槽漕燥争痩相窓糟総綜聡草荘葬蒼藻装走送遭鎗霜騒像増憎臓蔵贈造促側則即息捉束測足速俗属賊族続卒袖其揃存孫尊損村遜他多太汰詑唾堕妥惰打柁舵楕陀駄騨体堆対耐岱帯待怠態戴替泰滞胎腿苔袋貸退逮隊黛鯛代台大第醍題鷹滝瀧卓啄宅托択拓沢濯琢託鐸濁諾茸凧蛸只叩但達辰奪脱巽竪辿棚谷狸鱈樽誰丹単嘆坦担探旦歎淡湛炭短端箪綻耽胆蛋誕鍛団壇弾断暖檀段男談値知地弛恥智池痴稚置致蜘遅馳築畜竹筑蓄逐秩窒茶嫡着中仲宙忠抽昼柱注虫衷註酎鋳駐樗瀦猪苧著貯丁兆凋喋寵帖帳庁弔張彫徴懲挑暢朝潮牒町眺聴脹腸蝶調諜超跳銚長頂鳥勅捗直朕沈珍賃鎮陳津墜椎槌追鎚痛通塚栂掴槻佃漬柘辻蔦綴鍔椿潰坪壷嬬紬爪吊釣鶴亭低停偵剃貞呈堤定帝底庭廷弟悌抵挺提梯汀碇禎程締艇訂諦蹄逓邸鄭釘鼎泥摘擢敵滴的笛適鏑溺哲徹撤轍迭鉄典填天展店添纏甜貼転顛点伝殿澱田電兎吐堵塗妬屠徒斗杜渡登菟賭途都鍍砥砺努度土奴怒倒党冬凍刀唐塔塘套宕島嶋悼投搭東桃梼棟盗淘湯涛灯燈当痘祷等答筒糖統到董蕩藤討謄豆踏逃透鐙陶頭騰闘働動同堂導憧撞洞瞳童胴萄道銅峠鴇匿得徳涜特督禿篤毒独読栃橡凸突椴届鳶苫寅酉瀞噸屯惇敦沌豚遁頓呑曇鈍奈那内乍凪薙謎灘捺鍋楢馴縄畷南楠軟難汝二尼弐迩匂賑肉虹廿日乳入如尿韮任妊忍認濡禰祢寧葱猫熱年念捻撚燃粘乃廼之埜嚢悩濃納能脳膿農覗蚤巴把播覇杷波派琶破婆罵芭馬俳廃拝排敗杯盃牌背肺輩配倍培媒梅楳煤狽買売賠陪這蝿秤矧萩伯剥博拍柏泊白箔粕舶薄迫曝漠爆縛莫駁麦函箱硲箸肇筈櫨幡肌畑畠八鉢溌発醗髪伐罰抜筏閥鳩噺塙蛤隼伴判半反叛帆搬斑板氾汎版犯班畔繁般藩販範釆煩頒飯挽晩番盤磐蕃蛮匪卑否妃庇彼悲扉批披斐比泌疲皮碑秘緋罷肥被誹費避非飛樋簸備尾微枇毘琵眉美鼻柊稗匹疋髭彦膝菱肘弼必畢筆逼桧姫媛紐百謬俵彪標氷漂瓢票表評豹廟描病秒苗錨鋲蒜蛭鰭品彬斌浜瀕貧賓頻敏瓶不付埠夫婦富冨布府怖扶敷斧普浮父符腐膚芙譜負賦赴阜附侮撫武舞葡蕪部封楓風葺蕗伏副復幅服福腹複覆淵弗払沸仏物鮒分吻噴墳憤扮焚奮粉糞紛雰文聞丙併兵塀幣平弊柄並蔽閉陛米頁僻壁癖碧別瞥蔑箆偏変片篇編辺返遍便勉娩弁鞭保舗鋪圃捕歩甫補輔穂募墓慕戊暮母簿菩倣俸包呆報奉宝峰峯崩庖抱捧放方朋法泡烹砲縫胞芳萌蓬蜂褒訪豊邦鋒飽鳳鵬乏亡傍剖坊妨帽忘忙房暴望某棒冒紡肪膨謀貌貿鉾防吠頬北僕卜墨撲朴牧睦穆釦勃没殆堀幌奔本翻凡盆摩磨魔麻埋妹昧枚毎哩槙幕膜枕鮪柾鱒桝亦俣又抹末沫迄侭繭麿万慢満漫蔓味未魅巳箕岬密蜜湊蓑稔脈妙粍民眠務夢無牟矛霧鵡椋婿娘冥名命明盟迷銘鳴姪牝滅免棉綿緬面麺摸模茂妄孟毛猛盲網耗蒙儲木黙目杢勿餅尤戻籾貰問悶紋門匁也冶夜爺耶野弥矢厄役約薬訳躍靖柳薮鑓愉愈油癒諭輸唯佑優勇友宥幽悠憂揖有柚湧涌猶猷由祐裕誘遊邑郵雄融夕予余与誉輿預傭幼妖容庸揚揺擁曜楊様洋溶熔用窯羊耀葉蓉要謡踊遥陽養慾抑欲沃浴翌翼淀羅螺裸来莱頼雷洛絡落酪乱卵嵐欄濫藍蘭覧利吏履李梨理璃痢裏裡里離陸律率立葎掠略劉流溜琉留硫粒隆竜龍侶慮旅虜了亮僚両凌寮料梁涼猟療瞭稜糧良諒遼量陵領力緑倫厘林淋燐琳臨輪隣鱗麟瑠塁涙累類令伶例冷励嶺怜玲礼苓鈴隷零霊麗齢暦歴列劣烈裂廉恋憐漣煉簾練聯蓮連錬呂魯櫓炉賂路露労婁廊弄朗楼榔浪漏牢狼篭老聾蝋郎六麓禄肋録論倭和話歪賄脇惑枠鷲亙亘鰐詫藁蕨椀湾碗腕弌丐丕个丱丶丼丿乂乖乘亂亅豫亊舒弍于亞亟亠亢亰亳亶从仍仄仆仂仗仞仭仟价伉佚估佛佝佗佇佶侈侏侘佻佩佰侑佯來侖儘俔俟俎俘俛俑俚俐俤俥倚倨倔倪倥倅伜俶倡倩倬俾俯們倆偃假會偕偐偈做偖偬偸傀傚傅傴傲僉僊傳僂僖僞僥僭僣僮價僵儉儁儂儖儕儔儚儡儺儷儼儻儿兀兒兌兔兢竸兩兪兮冀冂囘册冉冏冑冓冕冖冤冦冢冩冪冫决冱冲冰况冽凅凉凛几處凩凭凰凵凾刄刋刔刎刧刪刮刳刹剏剄剋剌剞剔剪剴剩剳剿剽劍劔劒剱劈劑辨辧劬劭劼劵勁勍勗勞勣勦飭勠勳勵勸勹匆匈甸匍匐匏匕匚匣匯匱匳匸區卆卅丗卉卍凖卞卩卮夘卻卷厂厖厠厦厥厮厰厶參簒雙叟曼燮叮叨叭叺吁吽呀听吭吼吮吶吩吝呎咏呵咎呟呱呷呰咒呻咀呶咄咐咆哇咢咸咥咬哄哈咨咫哂咤咾咼哘哥哦唏唔哽哮哭哺哢唹啀啣啌售啜啅啖啗唸唳啝喙喀咯喊喟啻啾喘喞單啼喃喩喇喨嗚嗅嗟嗄嗜嗤嗔嘔嗷嘖嗾嗽嘛嗹噎噐營嘴嘶嘲嘸噫噤嘯噬噪嚆嚀嚊嚠嚔嚏嚥嚮嚶嚴囂嚼囁囃囀囈囎囑囓囗囮囹圀囿圄圉圈國圍圓團圖嗇圜圦圷圸坎圻址坏坩埀垈坡坿垉垓垠垳垤垪垰埃埆埔埒埓堊埖埣堋堙堝塲堡塢塋塰毀塒堽塹墅墹墟墫墺壞墻墸墮壅壓壑壗壙壘壥壜壤壟壯壺壹壻壼壽夂夊夐夛梦夥夬夭夲夸夾竒奕奐奎奚奘奢奠奧奬奩奸妁妝佞侫妣妲姆姨姜妍姙姚娥娟娑娜娉娚婀婬婉娵娶婢婪媚媼媾嫋嫂媽嫣嫗嫦嫩嫖嫺嫻嬌嬋嬖嬲嫐嬪嬶嬾孃孅孀孑孕孚孛孥孩孰孳孵學斈孺宀它宦宸寃寇寉寔寐寤實寢寞寥寫寰寶寳尅將專對尓尠尢尨尸尹屁屆屎屓屐屏孱屬屮乢屶屹岌岑岔妛岫岻岶岼岷峅岾峇峙峩峽峺峭嶌峪崋崕崗嵜崟崛崑崔崢崚崙崘嵌嵒嵎嵋嵬嵳嵶嶇嶄嶂嶢嶝嶬嶮嶽嶐嶷嶼巉巍巓巒巖巛巫已巵帋帚帙帑帛帶帷幄幃幀幎幗幔幟幢幤幇幵并幺麼广庠廁廂廈廐廏廖廣廝廚廛廢廡廨廩廬廱廳廰廴廸廾弃弉彝彜弋弑弖弩弭弸彁彈彌彎弯彑彖彗彙彡彭彳彷徃徂彿徊很徑徇從徙徘徠徨徭徼忖忻忤忸忱忝悳忿怡恠怙怐怩怎怱怛怕怫怦怏怺恚恁恪恷恟恊恆恍恣恃恤恂恬恫恙悁悍惧悃悚悄悛悖悗悒悧悋惡悸惠惓悴忰悽惆悵惘慍愕愆惶惷愀惴惺愃愡惻惱愍愎慇愾愨愧慊愿愼愬愴愽慂慄慳慷慘慙慚慫慴慯慥慱慟慝慓慵憙憖憇憬憔憚憊憑憫憮懌懊應懷懈懃懆憺懋罹懍懦懣懶懺懴懿懽懼懾戀戈戉戍戌戔戛戞戡截戮戰戲戳扁扎扞扣扛扠扨扼抂抉找抒抓抖拔抃抔拗拑抻拏拿拆擔拈拜拌拊拂拇抛拉挌拮拱挧挂挈拯拵捐挾捍搜捏掖掎掀掫捶掣掏掉掟掵捫捩掾揩揀揆揣揉插揶揄搖搴搆搓搦搶攝搗搨搏摧摯摶摎攪撕撓撥撩撈撼據擒擅擇撻擘擂擱擧舉擠擡抬擣擯攬擶擴擲擺攀擽攘攜攅攤攣攫攴攵攷收攸畋效敖敕敍敘敞敝敲數斂斃變斛斟斫斷旃旆旁旄旌旒旛旙无旡旱杲昊昃旻杳昵昶昴昜晏晄晉晁晞晝晤晧晨晟晢晰暃暈暎暉暄暘暝曁暹曉暾暼曄暸曖曚曠昿曦曩曰曵曷朏朖朞朦朧霸朮朿朶杁朸朷杆杞杠杙杣杤枉杰枩杼杪枌枋枦枡枅枷柯枴柬枳柩枸柤柞柝柢柮枹柎柆柧檜栞框栩桀桍栲桎梳栫桙档桷桿梟梏梭梔條梛梃檮梹桴梵梠梺椏梍桾椁棊椈棘椢椦棡椌棍棔棧棕椶椒椄棗棣椥棹棠棯椨椪椚椣椡棆楹楷楜楸楫楔楾楮椹楴椽楙椰楡楞楝榁楪榲榮槐榿槁槓榾槎寨槊槝榻槃榧樮榑榠榜榕榴槞槨樂樛槿權槹槲槧樅榱樞槭樔槫樊樒櫁樣樓橄樌橲樶橸橇橢橙橦橈樸樢檐檍檠檄檢檣檗蘗檻櫃櫂檸檳檬櫞櫑櫟檪櫚櫪櫻欅蘖櫺欒欖鬱欟欸欷盜欹飮歇歃歉歐歙歔歛歟歡歸歹歿殀殄殃殍殘殕殞殤殪殫殯殲殱殳殷殼毆毋毓毟毬毫毳毯麾氈氓气氛氤氣汞汕汢汪沂沍沚沁沛汾汨汳沒沐泄泱泓沽泗泅泝沮沱沾沺泛泯泙泪洟衍洶洫洽洸洙洵洳洒洌浣涓浤浚浹浙涎涕濤涅淹渕渊涵淇淦涸淆淬淞淌淨淒淅淺淙淤淕淪淮渭湮渮渙湲湟渾渣湫渫湶湍渟湃渺湎渤滿渝游溂溪溘滉溷滓溽溯滄溲滔滕溏溥滂溟潁漑灌滬滸滾漿滲漱滯漲滌漾漓滷澆潺潸澁澀潯潛濳潭澂潼潘澎澑濂潦澳澣澡澤澹濆澪濟濕濬濔濘濱濮濛瀉瀋濺瀑瀁瀏濾瀛瀚潴瀝瀘瀟瀰瀾瀲灑灣炙炒炯烱炬炸炳炮烟烋烝烙焉烽焜焙煥煕熈煦煢煌煖煬熏燻熄熕熨熬燗熹熾燒燉燔燎燠燬燧燵燼燹燿爍爐爛爨爭爬爰爲爻爼爿牀牆牋牘牴牾犂犁犇犒犖犢犧犹犲狃狆狄狎狒狢狠狡狹狷倏猗猊猜猖猝猴猯猩猥猾獎獏默獗獪獨獰獸獵獻獺珈玳珎玻珀珥珮珞璢琅瑯琥珸琲琺瑕琿瑟瑙瑁瑜瑩瑰瑣瑪瑶瑾璋璞璧瓊瓏瓔珱瓠瓣瓧瓩瓮瓲瓰瓱瓸瓷甄甃甅甌甎甍甕甓甞甦甬甼畄畍畊畉畛畆畚畩畤畧畫畭畸當疆疇畴疊疉疂疔疚疝疥疣痂疳痃疵疽疸疼疱痍痊痒痙痣痞痾痿痼瘁痰痺痲痳瘋瘍瘉瘟瘧瘠瘡瘢瘤瘴瘰瘻癇癈癆癜癘癡癢癨癩癪癧癬癰癲癶癸發皀皃皈皋皎皖皓皙皚皰皴皸皹皺盂盍盖盒盞盡盥盧盪蘯盻眈眇眄眩眤眞眥眦眛眷眸睇睚睨睫睛睥睿睾睹瞎瞋瞑瞠瞞瞰瞶瞹瞿瞼瞽瞻矇矍矗矚矜矣矮矼砌砒礦砠礪硅碎硴碆硼碚碌碣碵碪碯磑磆磋磔碾碼磅磊磬磧磚磽磴礇礒礑礙礬礫祀祠祗祟祚祕祓祺祿禊禝禧齋禪禮禳禹禺秉秕秧秬秡秣稈稍稘稙稠稟禀稱稻稾稷穃穗穉穡穢穩龝穰穹穽窈窗窕窘窖窩竈窰窶竅竄窿邃竇竊竍竏竕竓站竚竝竡竢竦竭竰笂笏笊笆笳笘笙笞笵笨笶筐筺笄筍笋筌筅筵筥筴筧筰筱筬筮箝箘箟箍箜箚箋箒箏筝箙篋篁篌篏箴篆篝篩簑簔篦篥籠簀簇簓篳篷簗簍篶簣簧簪簟簷簫簽籌籃籔籏籀籐籘籟籤籖籥籬籵粃粐粤粭粢粫粡粨粳粲粱粮粹粽糀糅糂糘糒糜糢鬻糯糲糴糶糺紆紂紜紕紊絅絋紮紲紿紵絆絳絖絎絲絨絮絏絣經綉絛綏絽綛綺綮綣綵緇綽綫總綢綯緜綸綟綰緘緝緤緞緻緲緡縅縊縣縡縒縱縟縉縋縢繆繦縻縵縹繃縷縲縺繧繝繖繞繙繚繹繪繩繼繻纃緕繽辮繿纈纉續纒纐纓纔纖纎纛纜缸缺罅罌罍罎罐网罕罔罘罟罠罨罩罧罸羂羆羃羈羇羌羔羞羝羚羣羯羲羹羮羶羸譱翅翆翊翕翔翡翦翩翳翹飜耆耄耋耒耘耙耜耡耨耿耻聊聆聒聘聚聟聢聨聳聲聰聶聹聽聿肄肆肅肛肓肚肭冐肬胛胥胙胝胄胚胖脉胯胱脛脩脣脯腋隋腆脾腓腑胼腱腮腥腦腴膃膈膊膀膂膠膕膤膣腟膓膩膰膵膾膸膽臀臂膺臉臍臑臙臘臈臚臟臠臧臺臻臾舁舂舅與舊舍舐舖舩舫舸舳艀艙艘艝艚艟艤艢艨艪艫舮艱艷艸艾芍芒芫芟芻芬苡苣苟苒苴苳苺莓范苻苹苞茆苜茉苙茵茴茖茲茱荀茹荐荅茯茫茗茘莅莚莪莟莢莖茣莎莇莊荼莵荳荵莠莉莨菴萓菫菎菽萃菘萋菁菷萇菠菲萍萢萠莽萸蔆菻葭萪萼蕚蒄葷葫蒭葮蒂葩葆萬葯葹萵蓊葢蒹蒿蒟蓙蓍蒻蓚蓐蓁蓆蓖蒡蔡蓿蓴蔗蔘蔬蔟蔕蔔蓼蕀蕣蕘蕈蕁蘂蕋蕕薀薤薈薑薊薨蕭薔薛藪薇薜蕷蕾薐藉薺藏薹藐藕藝藥藜藹蘊蘓蘋藾藺蘆蘢蘚蘰蘿虍乕虔號虧虱蚓蚣蚩蚪蚋蚌蚶蚯蛄蛆蚰蛉蠣蚫蛔蛞蛩蛬蛟蛛蛯蜒蜆蜈蜀蜃蛻蜑蜉蜍蛹蜊蜴蜿蜷蜻蜥蜩蜚蝠蝟蝸蝌蝎蝴蝗蝨蝮蝙蝓蝣蝪蠅螢螟螂螯蟋螽蟀蟐雖螫蟄螳蟇蟆螻蟯蟲蟠蠏蠍蟾蟶蟷蠎蟒蠑蠖蠕蠢蠡蠱蠶蠹蠧蠻衄衂衒衙衞衢衫袁衾袞衵衽袵衲袂袗袒袮袙袢袍袤袰袿袱裃裄裔裘裙裝裹褂裼裴裨裲褄褌褊褓襃褞褥褪褫襁襄褻褶褸襌褝襠襞襦襤襭襪襯襴襷襾覃覈覊覓覘覡覩覦覬覯覲覺覽覿觀觚觜觝觧觴觸訃訖訐訌訛訝訥訶詁詛詒詆詈詼詭詬詢誅誂誄誨誡誑誥誦誚誣諄諍諂諚諫諳諧諤諱謔諠諢諷諞諛謌謇謚諡謖謐謗謠謳鞫謦謫謾謨譁譌譏譎證譖譛譚譫譟譬譯譴譽讀讌讎讒讓讖讙讚谺豁谿豈豌豎豐豕豢豬豸豺貂貉貅貊貍貎貔豼貘戝貭貪貽貲貳貮貶賈賁賤賣賚賽賺賻贄贅贊贇贏贍贐齎贓賍贔贖赧赭赱赳趁趙跂趾趺跏跚跖跌跛跋跪跫跟跣跼踈踉跿踝踞踐踟蹂踵踰踴蹊蹇蹉蹌蹐蹈蹙蹤蹠踪蹣蹕蹶蹲蹼躁躇躅躄躋躊躓躑躔躙躪躡躬躰軆躱躾軅軈軋軛軣軼軻軫軾輊輅輕輒輙輓輜輟輛輌輦輳輻輹轅轂輾轌轉轆轎轗轜轢轣轤辜辟辣辭辯辷迚迥迢迪迯邇迴逅迹迺逑逕逡逍逞逖逋逧逶逵逹迸遏遐遑遒逎遉逾遖遘遞遨遯遶隨遲邂遽邁邀邊邉邏邨邯邱邵郢郤扈郛鄂鄒鄙鄲鄰酊酖酘酣酥酩酳酲醋醉醂醢醫醯醪醵醴醺釀釁釉釋釐釖釟釡釛釼釵釶鈞釿鈔鈬鈕鈑鉞鉗鉅鉉鉤鉈銕鈿鉋鉐銜銖銓銛鉚鋏銹銷鋩錏鋺鍄錮錙錢錚錣錺錵錻鍜鍠鍼鍮鍖鎰鎬鎭鎔鎹鏖鏗鏨鏥鏘鏃鏝鏐鏈鏤鐚鐔鐓鐃鐇鐐鐶鐫鐵鐡鐺鑁鑒鑄鑛鑠鑢鑞鑪鈩鑰鑵鑷鑽鑚鑼鑾钁鑿閂閇閊閔閖閘閙閠閨閧閭閼閻閹閾闊濶闃闍闌闕闔闖關闡闥闢阡阨阮阯陂陌陏陋陷陜陞陝陟陦陲陬隍隘隕隗險隧隱隲隰隴隶隸隹雎雋雉雍襍雜霍雕雹霄霆霈霓霎霑霏霖霙霤霪霰霹霽霾靄靆靈靂靉靜靠靤靦靨勒靫靱靹鞅靼鞁靺鞆鞋鞏鞐鞜鞨鞦鞣鞳鞴韃韆韈韋韜韭齏韲竟韶韵頏頌頸頤頡頷頽顆顏顋顫顯顰顱顴顳颪颯颱颶飄飃飆飩飫餃餉餒餔餘餡餝餞餤餠餬餮餽餾饂饉饅饐饋饑饒饌饕馗馘馥馭馮馼駟駛駝駘駑駭駮駱駲駻駸騁騏騅駢騙騫騷驅驂驀驃騾驕驍驛驗驟驢驥驤驩驫驪骭骰骼髀髏髑髓體髞髟髢髣髦髯髫髮髴髱髷髻鬆鬘鬚鬟鬢鬣鬥鬧鬨鬩鬪鬮鬯鬲魄魃魏魍魎魑魘魴鮓鮃鮑鮖鮗鮟鮠鮨鮴鯀鯊鮹鯆鯏鯑鯒鯣鯢鯤鯔鯡鰺鯲鯱鯰鰕鰔鰉鰓鰌鰆鰈鰒鰊鰄鰮鰛鰥鰤鰡鰰鱇鰲鱆鰾鱚鱠鱧鱶鱸鳧鳬鳰鴉鴈鳫鴃鴆鴪鴦鶯鴣鴟鵄鴕鴒鵁鴿鴾鵆鵈鵝鵞鵤鵑鵐鵙鵲鶉鶇鶫鵯鵺鶚鶤鶩鶲鷄鷁鶻鶸鶺鷆鷏鷂鷙鷓鷸鷦鷭鷯鷽鸚鸛鸞鹵鹹鹽麁麈麋麌麒麕麑麝麥麩麸麪麭靡黌黎黏黐黔黜點黝黠黥黨黯黴黶黷黹黻黼黽鼇鼈皷鼕鼡鼬鼾齊齒齔齣齟齠齡齦齧齬齪齷齲齶龕龜龠堯槇遙瑤凜熙";
		#endregion

		// Tests involving Unicode surrogate boundaries pass for ICU versions 50 and up.
		// Prior versions of ICU didn't always throw
		private const string IcuStart = "&[before 1] [first regular] < ";

		[Test]
		[Category("ICULessThan50")]
		// This test fails on trusty (which has ICU 52) because it doesn't throw an exception.
		// I guess it is because invalid surrogates are handled differently, but I haven't
		// investigated this any further. Ignoring for now by adding KnownMonoIssue category.
		[Category("KnownMonoIssue")]
		public void ConvertToIcuRules_SurrogateCharacterLowBound_Throws()
		{
			Assert.Throws<ArgumentException>(
				// Invalid unicode character escape sequence:
				() => new RuleBasedCollator(IcuStart + "\ud800")
			);
		}

		[Test]
		[Category("ICULessThan50")]
		// This test fails on trusty (which has ICU 52) because it doesn't throw an exception.
		// I guess it is because invalid surrogates are handled differently, but I haven't
		// investigated this any further. Ignoring for now by adding KnownMonoIssue category.
		[Category("KnownMonoIssue")]
		public void ConvertToIcuRules_SurrogateCharacterHighBound_Throws()
		{
			Assert.Throws<ArgumentException>(
				// Invalid unicode character escape sequence:
				() => new RuleBasedCollator(IcuStart + "\udfff")
			);
		}

		[Test]
		[Category("ICULessThan50")]
		// This test fails on trusty (which has ICU 52) because it doesn't throw an exception.
		// I guess it is because invalid surrogates are handled differently, but I haven't
		// investigated this any further. Ignoring for now by adding KnownMonoIssue category.
		[Category("KnownMonoIssue")]
		public void ConvertToIcuRules_SurrogateCharactersOutOfOrder_Throws()
		{
			Assert.Throws<ArgumentException>(
				// Invalid unicode character escape sequence:
				() => new RuleBasedCollator(IcuStart + "a << \udc00\ud800")
			);
		}

		/// <summary>
		/// Tailored rules were obtained from:
		/// http://source.icu-project.org/repos/icu/icu/tags/release-56-1/source/data/coll/sr.txt
		/// </summary>
		[Test]
		[Category("Full ICU")]
		public void GetSortRules_Serbian()
		{
			if (string.CompareOrdinal(Wrapper.IcuVersion, "55.1") < 0)
				Assert.Ignore("This test requires ICU 55 or higher");

			const string rules = "[reorder Cyrl][suppressContractions [Ии]]";
			const string language = "sr";
			var locale = new Locale(language);

			var collationRules = Collator.GetCollationRules(locale);
			var collationRules2 = Collator.GetCollationRules(language);

			Assert.AreEqual(rules, collationRules);
			Assert.AreEqual(collationRules, collationRules2);
		}

		/// <summary>
		/// Getting CollationRules for English should return some content.
		/// Before it would return NULL because the ErrorCode returned was
		/// ErrorCode.USING_DEFAULT_WARNING which is not a failure.
		///
		/// Double-check this to make sure the rules are correct.
		/// http://source.icu-project.org/repos/icu/icu/tags/release-56-1/source/data/coll/en.txt
		/// </summary>
		[Test]
		[Category("Full ICU")]
		public void GetSortRules_English()
		{
			var locale = new Locale("en-US");

			var tailoredRules = Collator.GetCollationRules(locale);
			var collationRules = Collator.GetCollationRules(locale, UColRuleOption.UCOL_FULL_RULES);

			Assert.IsEmpty(tailoredRules);
			Assert.IsNotNull(collationRules);
			Assert.IsNotEmpty(collationRules);
		}
	}
}
