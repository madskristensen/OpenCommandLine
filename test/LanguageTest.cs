using System;
using System.Linq;
using System.Text.RegularExpressions;
using MadsKristensen.OpenCommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class LanguageTest
    {
        [TestMethod, TestCategory("Language")]
        public void KeywordTest()
        {
            Regex regex = CmdLanguage.KeywordRegex;
            Assert.AreEqual("call", regex.Match("call notepad.exe").Value);
            Assert.AreEqual("not", regex.Matches("if not exist")[1].Value);
            Assert.AreEqual("", regex.Match("%call").Value);
            Assert.AreEqual("", regex.Match("ifnot").Value);
        }

        [TestMethod, TestCategory("Language")]
        public void IdentifierTest()
        {
            Regex regex = CmdLanguage.IdentifierRegex;
            Assert.AreEqual("foo", regex.Match("set foo = hat").Value);
            Assert.AreEqual("foo", regex.Match("set foo=hat").Value);
            Assert.AreEqual("%foo%", regex.Match("if %foo% = hat").Value);

            Assert.AreNotEqual("foo", regex.Match("setfoo=hat").Value);
        }

        [TestMethod, TestCategory("Language")]
        public void CommentTest()
        {
            Regex regex = CmdLanguage.CommentRegex;
            Assert.AreEqual(":: hat", regex.Match("   :: hat").Value);
            Assert.AreEqual(":: hat", regex.Match(":: hat").Value);
            Assert.AreEqual("&:: hat", regex.Match("foo &:: hat").Value);
            Assert.AreEqual("&:: hat &:: test", regex.Match("foo &:: hat &:: test").Value);

            Assert.AreEqual("REM hat", regex.Match("   REM hat").Value);
            Assert.AreEqual("rem hat", regex.Match("rem hat").Value);
            Assert.AreEqual("&rem hat", regex.Match("foo &rem hat").Value);
            Assert.AreEqual("&rem hat &REM test", regex.Match("foo &rem hat &REM test").Value);
        }

        [TestMethod, TestCategory("Language")]
        public void StringTest()
        {
            Regex regex = CmdLanguage.StringRegex;
            Assert.AreEqual("'hat'", regex.Match(" test  'hat'  foo").Value);
            Assert.AreEqual("\"hat\"", regex.Match(" test  \"hat\"  foo").Value);
            Assert.AreEqual("\"foo 'test' bar\"", regex.Match("\"foo 'test' bar\"").Value);
            Assert.AreEqual("foo bar", regex.Match("echo foo bar").Value);
            Assert.AreEqual("", regex.Match(" test  \"hat  ").Value);
        }

        [TestMethod, TestCategory("Language")]
        public void LabelTest()
        {
            Regex regex = CmdLanguage.LabelRegex;
            Assert.AreEqual(":foo", regex.Match(":foo").Value);
            Assert.AreEqual("foo", regex.Match("goto:foo").Value);
            Assert.AreEqual("foo", regex.Match("goto foo").Value);
            Assert.AreEqual("foo", regex.Match("   goto:foo").Value);

            Assert.AreEqual("", regex.Match("notgoto:foo").Value);
            Assert.AreEqual("", regex.Match("notgoto foo").Value);
        }

        [TestMethod, TestCategory("Language")]
        public void ParameterTest()
        {
            Regex regex = CmdLanguage.ParameterRegex;
            Assert.AreEqual("/f", regex.Match(" /f ").Value);
            Assert.AreEqual("-f", regex.Match(" -f ").Value);
            Assert.AreEqual("--f", regex.Match(" --f ").Value);

            Assert.AreEqual("", regex.Match("fo/f").Value);
            Assert.AreEqual("", regex.Match("fo-f").Value);
            Assert.AreEqual("", regex.Match("fo--f").Value);
            Assert.AreEqual("", regex.Match("-- f").Value);
        }
    }
}
