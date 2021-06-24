
using System;
using static System.Console;

namespace com.spaceflint.dbg
{
    public class Parser
    {

        // --------------------------------------------------------------------
        // construct debug object and run its main loop

        public Parser (IDebuggee debuggee)
        {
            this.debuggee = debuggee;
        }

        // --------------------------------------------------------------------
        // parse address string

        public (int, int, string) ParseAddress (string cmd, int defaultSegment)
        {
            int seg, ofs;
            (ofs, cmd) = ParseValue(cmd);
            if (cmd != null)
            {
                if (cmd.Length > 0 && cmd[0] == ':')
                {
                    seg = ofs;
                    (ofs, cmd) = ParseValue(cmd.Substring(1));
                }
                else
                    seg = defaultSegment;
                if (ofs < 0 || ofs > 0xFFFF || seg < 0 || seg > 0xFFFF)
                {
                    seg = ofs = -1;
                    cmd = null;
                }
            }
            else
                seg = ofs = -1;
            return (seg, ofs, cmd);
        }

        // --------------------------------------------------------------------
        // parse optional start address prefixed by '='

        public (int, int, string) ParseStartAddress (string cmd, int defaultSegment)
        {
            int seg, ofs;
            if (cmd != null && cmd.Length > 2 && cmd[0] == '=')
            {
                (seg, ofs, cmd) = ParseAddress(
                                    cmd.Substring(1).TrimStart(), defaultSegment);
            }
            else
                seg = ofs = -1;
            return (seg, ofs, cmd);
        }

        // --------------------------------------------------------------------
        // parse length prefixed by 'L'

        public (int, string) ParseLength (string cmd, bool required = false)
        {
            int len;
            if (cmd != null && cmd.Length > 2 && cmd[0] == 'L')
            {
                (len, cmd) = ParseValue(cmd.Substring(1).TrimStart());
                if (len <= 0)
                    cmd = null;
            }
            else
            {
                len = -1;
                if (required)
                    cmd = null;
            }
            return (len, cmd);
        }

        // --------------------------------------------------------------------
        // parse expression string

        public (int, string) ParseValue (string expr)
        {
            // stops at end of string, or at a point where a plus or a minus
            // operator would be expected but some other char was found.
            //
            // returns the parsed value, and a string containing the rest of
            // the string past the expression.  returns (-1, null) on error.

            if (expr.Length == 0)
                return (-1, null);

            return ParseValueSum(expr);
        }

        // --------------------------------------------------------------------
        // parse sub-expressions and add or subtract them

        private (int, string) ParseValueSum (string expr)
        {
            int valueExpr;
            (valueExpr, expr) = ParseValueProd(expr);

            while (expr != null)
            {
                // if there is no more input, or we reach a 'stop char',
                // then the expression is finished. otherwise, the next
                // input must be a sum operator.

                if (expr.Length == 0)
                    break;

                char op = expr[0];
                if (op != '+' && op != '-')
                    break;
                else
                {
                    expr = expr.Substring(1).TrimStart();
                    if (expr.Length == 0)
                        expr = null;
                    else
                    {
                        int valueTerm;
                        (valueTerm, expr) = ParseValueProd(expr);

                        if (op == '-')
                            valueExpr -= valueTerm;
                        else
                            valueExpr += valueTerm;
                    }
                }
            }

            return (valueExpr, expr);
        }

        // --------------------------------------------------------------------
        // parse sub-expressions and multiply or divide them

        private (int, string) ParseValueProd (string expr)
        {
            int valueExpr;
            (valueExpr, expr) = ParseValueWrap(expr);

            while (expr != null)
            {
                // if there is no more input, or we reach a 'stop char',
                // then the expression is finished. otherwise, the next
                // input must be a product operator.

                if (expr.Length == 0)
                    break;

                char op = expr[0];
                if (op != '*' && op != '/')
                    break;
                else
                {
                    expr = expr.Substring(1).TrimStart();
                    if (expr.Length == 0)
                        expr = null;
                    else
                    {
                        int valueTerm;
                        (valueTerm, expr) = ParseValueWrap(expr);

                        if (op == '*')
                            valueExpr *= valueTerm;
                        else
                            valueExpr /= valueTerm;
                    }
                }
            }

            return (valueExpr, expr);
        }

        // --------------------------------------------------------------------
        // parse terminal value with an optional unary minus and parenthesis

        private (int, string) ParseValueWrap (string expr)
        {
            bool negate;
            if (expr[0] == '-')
            {
                // if unary, indicate to negate value
                expr = expr.Substring(1).TrimStart();
                negate = true;
            }
            else
                negate = false;

            int value;

            if (expr[0] == '(')
            {
                // if parentheses, evaluate sub-expression recursively
                var rest = expr.Substring(1).TrimStart();
                (value, expr) = ParseValueSum(rest);
                if (string.IsNullOrEmpty(expr) || expr[0] != ')')
                    return (-1, null);
                expr = expr.Substring(1).TrimStart();
            }
            else
                (value, expr) = ParseValueTerm(expr);

            if (negate)
                value = -value;

            return (value, expr);
        }

        // --------------------------------------------------------------------
        // parse terminal value as a hex number or register reference

        private (int, string) ParseValueTerm (string expr)
        {
            string token, rest;
            int value = -1;

            if (expr[0] == '@')
            {
                // if specified as @reg, don't try to parse as number
                (token, rest) = SplitNextWord(expr.Substring(1));
                if (debuggee.IsRegister(token))
                    value = debuggee.GetRegister(token);
                else
                    rest = null;
            }
            else
            {
                // otherwise first try to parse as a hex number
                (token, rest) = SplitNextWord(expr);
                try
                {
                    value = Convert.ToInt32(token, 16);
                }
                catch (Exception)
                {
                    // if fails, try to parse as register name
                    bool isReg = debuggee.IsRegister(token);
                    /*
                    if (! isReg && token.Length > 1 && token[0] == 'R')
                    {
                        // allow register name to be prefixed with R
                        token = token.Substring(1);
                        isReg = debuggee.IsRegister(token);
                    }
                    */
                    if (isReg)
                        value = debuggee.GetRegister(token);
                    else
                        rest = null;
                }
            }
            return (value, rest);
        }

        // --------------------------------------------------------------------
        // extract first char from command string

        public static (char, string) SplitFirstChar (string cmd, char quitChar)
        {
            // trim leading spaces, and split command into
            // initial verb, and trimmed rest of string

            char cmdVerb;
            if (cmd == null)
                cmdVerb = quitChar;
            else
            {
                cmd = cmd.TrimStart();
                if (cmd.Length == 0)
                    cmdVerb = '\0';
                else
                {
                    cmd = cmd.ToUpper();
                    cmdVerb = cmd[0];
                    cmd = cmd.Substring(1).TrimStart();
                }
            }

            return (cmdVerb, cmd);
        }

        // --------------------------------------------------------------------
        // extract next word from command string

        public static (string, string) SplitNextWord (string cmd)
        {
            var idx = IndexOfSymbol(cmd);
            string rest;
            if (idx == -1)
                rest = "";
            else
            {
                rest = cmd.Substring(idx).TrimStart();
                cmd = cmd.Substring(0, idx);
            }
            return (cmd, rest);
        }

        // --------------------------------------------------------------------
        // find first non-alphanumeric characters

        private static int IndexOfSymbol (string str)
        {
            for (var idx = 0; idx < str.Length; idx++)
            {
                var ch = str[idx];
                if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z'))
                    continue;
                return idx;
            }
            return -1;
        }

        // --------------------------------------------------------------------

        private IDebuggee debuggee;

    }
}
