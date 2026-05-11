using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class JsMinifier {
    public static string RemoveComments(string js) {
        js = js.Replace("\r\n", "\n").Replace("\r", "\n");
        var sb = new StringBuilder(js.Length);
        var stack = new Stack<string>();
        string ctx = "normal";
        int exprBraceDepth = 0;

        for (int i = 0; i < js.Length; i++) {
            char c = js[i];
            char n = (i + 1 < js.Length) ? js[i + 1] : '\0';

            if (ctx == "lineComment") {
                if (c == '\n') { sb.Append(c); ctx = Restore(stack, ref exprBraceDepth); }
                continue;
            }
            if (ctx == "blockComment") {
                if (c == '*' && n == '/') { i++; ctx = Restore(stack, ref exprBraceDepth); }
                else if (c == '\n') sb.Append(c);
                continue;
            }
            if (ctx == "singleQ") {
                sb.Append(c);
                if (c == '\\' && i + 1 < js.Length) { i++; sb.Append(js[i]); }
                else if (c == '\'') ctx = Restore(stack, ref exprBraceDepth);
                continue;
            }
            if (ctx == "doubleQ") {
                sb.Append(c);
                if (c == '\\' && i + 1 < js.Length) { i++; sb.Append(js[i]); }
                else if (c == '"') ctx = Restore(stack, ref exprBraceDepth);
                continue;
            }
            if (ctx == "template") {
                sb.Append(c);
                if (c == '\\' && i + 1 < js.Length) { i++; sb.Append(js[i]); }
                else if (c == '`') ctx = Restore(stack, ref exprBraceDepth);
                else if (c == '$' && n == '{') {
                    sb.Append('{'); i++;
                    stack.Push("template");
                    ctx = "templateExpr";
                    exprBraceDepth = 1;
                }
                continue;
            }

            bool inExpr = (ctx == "templateExpr");
            if (c == '/' && n == '/') {
                stack.Push(inExpr ? "templateExpr:" + exprBraceDepth : "normal");
                ctx = "lineComment"; i++;
            } else if (c == '/' && n == '*') {
                stack.Push(inExpr ? "templateExpr:" + exprBraceDepth : "normal");
                ctx = "blockComment"; i++;
            } else if (c == '\'') {
                sb.Append(c);
                stack.Push(inExpr ? "templateExpr:" + exprBraceDepth : "normal");
                ctx = "singleQ";
            } else if (c == '"') {
                sb.Append(c);
                stack.Push(inExpr ? "templateExpr:" + exprBraceDepth : "normal");
                ctx = "doubleQ";
            } else if (c == '`') {
                sb.Append(c);
                stack.Push(inExpr ? "templateExpr:" + exprBraceDepth : "normal");
                ctx = "template";
            } else if (inExpr && c == '{') {
                exprBraceDepth++; sb.Append(c);
            } else if (inExpr && c == '}') {
                exprBraceDepth--; sb.Append(c);
                if (exprBraceDepth == 0) ctx = Restore(stack, ref exprBraceDepth);
            } else {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string Restore(Stack<string> stack, ref int depth) {
        if (stack.Count == 0) return "normal";
        string s = stack.Pop();
        if (s.StartsWith("templateExpr:")) {
            depth = int.Parse(s.Substring(13));
            return "templateExpr";
        }
        return s;
    }

    public static string CompressBlankLines(string text) {
        return Regex.Replace(text, @"\n{3,}", "\n\n");
    }
}
