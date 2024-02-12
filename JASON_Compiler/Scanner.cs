using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public enum Token_Class
{
    Number, Stirng_Body, INT, FLOAT, STRING, READ, WRITE,
    REPEAT, UNTIL, IF, ELSEIF, ELSE, THEN, RETURN, ENDL,
    IDENTFIER, PLUS_OP, MINUS_OP, MULTIPLY_OP, DEVISON_OP,
    ASSIGNMENT_OP, LESS_THAN_OP, GREATER_THAN_OP, EQUAL_OP,
    NOT_EQUAL_OP, AND, OR, RIGHT_CURLY_BRACE, LEFT_CURLY_BRACE,
    RIGHT_PARANTHESIS, LEFT_PARANTHESIS, SEMICOLON, COMMA, END, MAIN
}

namespace JASON_Compiler
{
    public class Token
    {
        public string lex { set; get; }
        public Token_Class token_type { set; get; }
    }

    public class Error
    {
        public string lex { set; get; }
    }

    public class Scanner
    {
        public static List<Token> Tokens = new List<Token>();
        public static List<Error> Errors = new List<Error>();

        private static Dictionary<string, Token_Class> ReservedWords = new Dictionary<string, Token_Class>();
        private static Dictionary<string, Token_Class> Operators = new Dictionary<string, Token_Class>();
        private static Regex Number, Identfier;
        private static bool FirstFit = true;

        public Scanner()
        {

        }

        public static void GenerateRegex()
        {
            ///insures this code is run only once
            if (!FirstFit)
            {
                return;
            }

            FirstFit = false;

            ///adding reserved words
            ReservedWords.Add("int", Token_Class.INT);
            ReservedWords.Add("float", Token_Class.FLOAT); ;
            ReservedWords.Add("string", Token_Class.STRING);
            ReservedWords.Add("read", Token_Class.READ);
            ReservedWords.Add("write", Token_Class.WRITE);
            ReservedWords.Add("repeat", Token_Class.REPEAT);
            ReservedWords.Add("until", Token_Class.UNTIL);
            ReservedWords.Add("if", Token_Class.IF);
            ReservedWords.Add("else", Token_Class.ELSE);
            ReservedWords.Add("elseif", Token_Class.ELSEIF);
            ReservedWords.Add("then", Token_Class.THEN);
            ReservedWords.Add("return", Token_Class.RETURN);
            ReservedWords.Add("endl", Token_Class.ENDL);
            ReservedWords.Add("end", Token_Class.END);
            ReservedWords.Add("main", Token_Class.MAIN);

            ///adding operators
            Operators.Add("+", Token_Class.PLUS_OP);
            Operators.Add("-", Token_Class.MINUS_OP);
            Operators.Add("*", Token_Class.MULTIPLY_OP);
            Operators.Add("/", Token_Class.DEVISON_OP);
            Operators.Add(":=", Token_Class.ASSIGNMENT_OP);
            Operators.Add("<", Token_Class.LESS_THAN_OP);
            Operators.Add(">", Token_Class.GREATER_THAN_OP);
            Operators.Add("=", Token_Class.EQUAL_OP);
            Operators.Add("<>", Token_Class.NOT_EQUAL_OP);
            Operators.Add("&&", Token_Class.AND);
            Operators.Add("||", Token_Class.OR);
            Operators.Add("{", Token_Class.LEFT_CURLY_BRACE);
            Operators.Add("}", Token_Class.RIGHT_CURLY_BRACE);
            Operators.Add("(", Token_Class.LEFT_PARANTHESIS);
            Operators.Add(")", Token_Class.RIGHT_PARANTHESIS);
            Operators.Add(";", Token_Class.SEMICOLON);
            Operators.Add(",", Token_Class.COMMA);

            ///adding other regexes
            Number = new Regex(@"^[0-9]+(\.[0-9]+)?$");
            Identfier = new Regex(@"^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*$");

        }

        private static void addToken(string lex, Token_Class token_type)
        {
            Token token = new Token();
            token.lex = lex;
            token.token_type = token_type;
            Tokens.Add(token);
        }

        private static void addError(string lex)
        {
            Error error = new Error();
            error.lex = lex;
            Errors.Add(error);
        }


        public static void StartScanning(string SourceCode)
        {
            for (int i = 0; i < SourceCode.Length; i++)
            {
                int j = i; ///pointer j will be used before moving pointer i
                string CurrentLexeme = SourceCode[j].ToString(), tmp = "";

                ///skip theses characters
                if (SourceCode[j] == ' ' || SourceCode[j] == '\r' || SourceCode[j] == '\n' || SourceCode[j] == '\t')
                    continue;

                //if you read a character then it can be an identifer or a reserved_word
                if (Char.IsLetter(SourceCode[j]))
                {
                    while (j < SourceCode.Length && (Char.IsLetter(SourceCode[j]) || Char.IsDigit(SourceCode[j])))
                    {
                        tmp += SourceCode[j++];
                    }

                    if (ReservedWords.ContainsKey(tmp)) ///first we check for reserved words
                    {
                        Token_Class token_class = ReservedWords[tmp];
                        addToken(tmp, token_class);
                    }
                    else if (Identfier.IsMatch(tmp)) ///then we check for identifires
                    {
                        addToken(tmp, Token_Class.IDENTFIER);
                    }
                    else
                    {
                        addError(tmp);
                    }

                    tmp = "";
                    i = j - 1;
                }
                else if (Char.IsDigit(SourceCode[j]) || SourceCode[j] == '.') /// then we ckeck for numbers 
                {
                    ///a number can have "." so we continue the loop
                    ///a also read letters and that allows the reading of invalid identifires like "1num"
                    while (j < SourceCode.Length && (Char.IsDigit(SourceCode[j]) || SourceCode[j] == '.' || Char.IsLetter(SourceCode[j])))
                    {
                        tmp += SourceCode[j++];
                    }

                    if (Number.IsMatch(tmp))
                    {
                        addToken(tmp, Token_Class.Number);
                    }
                    else
                    {
                        addError(tmp);
                    }

                    tmp = "";
                    i = j - 1;

                }
                else if (SourceCode[j] == '"') ///detect string bodys
                {
                    tmp += SourceCode[j++];

                    ///reads untill the second double quotation
                    while (j < SourceCode.Length && SourceCode[j] != '"')
                    {
                        if (SourceCode[j] == '\r' || SourceCode[j] == '\n') ///if the line ends you can detect other things
                        {
                            break;
                        }

                        tmp += SourceCode[j++];
                    }

                    if (j < SourceCode.Length && SourceCode[j] != '\r' && SourceCode[j] != '\n')
                    {
                        tmp += SourceCode[j];
                    }

                    if (tmp[0] == '"' && tmp[tmp.Length - 1] == '"' && tmp.Length >= 2) /// a single double quote is not acceptable
                    {
                        addToken(tmp, Token_Class.Stirng_Body);
                    }
                    else
                    {
                        addError(tmp);
                    }

                    i = j;
                    tmp = "";

                }
                else if (j + 1 < SourceCode.Length && SourceCode[j] == '/' && SourceCode[j + 1] == '*')  ///detect comments
                {
                    tmp += "/*";
                    j += 2;

                    while (j < SourceCode.Length && SourceCode[j] != '*')
                    {
                        //if (SourceCode[j] == '\r' || SourceCode[j] == '\n') ///if the line ends you can detect other things
                        //{
                        //    break;
                        //}

                        tmp += SourceCode[j++];
                    }

                    while (j + 1 < SourceCode.Length && (SourceCode[j] != '*' || SourceCode[j + 1] != '/'))
                    {
                        //if (SourceCode[j] == '\r' || SourceCode[j] == '\n')
                        //{
                        //    break;
                        //}

                        tmp += SourceCode[j++];
                    }

                    if (j + 1 < SourceCode.Length /* && SourceCode[j] == '*' && SourceCode[j + 1] == '/' */)
                    {
                        tmp += "*/";
                    }
                    else
                    {
                        if (j < SourceCode.Length /* && SourceCode[j] != '\r' && SourceCode[j] == '\n' */) //edit
                        {
                            tmp += SourceCode[j];
                        }

                        addError(tmp);
                    }

                    i = j + 1;
                    tmp = "";
                }
                else if (IsSymbol(SourceCode[j])) ///detect symbols like +,-,>,<,..........
                {
                    tmp += SourceCode[j]; ///length 1 symbol
                    string tmp2 = tmp; ///symbols with bigger lengths
                    while (j + 1 < SourceCode.Length && IsSymbol(SourceCode[j + 1]))
                        tmp2 += SourceCode[++j];

                    if (tmp2.Length == 2 && Operators.ContainsKey(tmp2)) ///we only have symbols of legth 1,2
                    {
                        i++;
                        Token_Class token_class = Operators[tmp2];
                        addToken(tmp2, token_class);
                    }
                    else if (tmp2.Length >= 2) ///any bigger symbol is an error
                    {
                        i += tmp2.Length - 1;
                        addError(tmp2);
                    }
                    else if (Operators.ContainsKey(tmp)) ///checking for length 1 symbol if a logest match wasn't found
                    {
                        Token_Class token_class = Operators[tmp];
                        addToken(tmp, token_class);
                    }
                    else ///symbol eror like "_" which is not defined in the language
                    {
                        addError(tmp);
                    }

                    tmp = "";

                }
                else ///other things like {,},(,)
                {
                    tmp += SourceCode[j]; ///they must be of length 1 

                    if (Operators.ContainsKey(tmp)) ///checking for length 1 symbol if a logest match wasn't found
                    {
                        Token_Class token_class = Operators[tmp];
                        addToken(tmp, token_class);
                    }
                    else ///symbol eror like "_" which is not defined in the language
                    {
                        addError(tmp);
                    }

                    tmp = "";
                }
            }
        }

        private static bool IsSymbol(char c)
        {
            bool ret = false;
            ret |= (c == '+') | (c == '-') | (c == '*') | (c == '/');
            ret |= (c == ':') | (c == '>') | (c == '<') | (c == '=');
            ret |= (c == '&') | (c == '|');

            return ret;
        }

    }



}
