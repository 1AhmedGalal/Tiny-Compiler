using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace JASON_Compiler
{
    public class Node
    {
        public List<Node> Children = new List<Node>();
        
        public string Name;
        public Node(string N)
        {
            this.Name = N;
        }
    }
    public class Parser
    {
        int InputPointer = 0;
        List<Token> TokenStream;
        public  Node root;
        
        public Node StartParsing(List<Token> TokenStream)
        {
            this.InputPointer = 0;
            this.TokenStream = TokenStream;
            root = PROGRAM();
            return root;
        }

        Node DataType()
        {
            Node datatype = new Node("Datatype");

            if (InputPointer < TokenStream.Count() && (Token_Class.INT) == TokenStream[InputPointer].token_type)
                datatype.Children.Add(match(Token_Class.INT));
            else if (InputPointer < TokenStream.Count() && (Token_Class.FLOAT) == TokenStream[InputPointer].token_type)
                datatype.Children.Add(match(Token_Class.FLOAT));
            else 
                datatype.Children.Add(match(Token_Class.STRING));

            return datatype;
        }

        Node PROGRAM()
        {
            Node program = new Node("PROGRAM");

            program.Children.Add(MORE_FUNCTION_STATEMENTS());
            program.Children.Add(MAIN_FUNCTION());

            //MessageBox.Show("Success");
            return program;
        }

        Node FUNCTION_CALL()
        {
            Node ret = new Node("FUNCTION_CALL");
            
            ret.Children.Add(match(Token_Class.IDENTFIER));
            ret.Children.Add(match(Token_Class.LEFT_PARANTHESIS));

            if(InputPointer < TokenStream.Count() && Token_Class.RIGHT_PARANTHESIS != TokenStream[InputPointer].token_type) //there are arguments
                ret.Children.Add(ID_LIST());

            ret.Children.Add(match(Token_Class.RIGHT_PARANTHESIS));

            return ret;
        }

        Node ID_LIST()
        {
            Node ret = new Node("ID_LIST");

            if (InputPointer < TokenStream.Count() && Token_Class.LEFT_PARANTHESIS != TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(EXPRESSION());

                //first make sure that there can be another argument passed
                if (InputPointer-1 > -1 && Token_Class.RIGHT_PARANTHESIS != TokenStream[InputPointer-1].token_type && Token_Class.COMMA != TokenStream[InputPointer-1].token_type && Token_Class.LEFT_PARANTHESIS != TokenStream[InputPointer].token_type)
                    ret.Children.Add(IDEN_LIST());
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node IDEN_LIST()
        {
            Node ret = new Node("IDEN_LIST");


            if (InputPointer < TokenStream.Count() && Token_Class.COMMA == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.COMMA));
                ret.Children.Add(EXPRESSION());

                //first make sure that there can be another argument passed
                if (InputPointer-1 > -1 && Token_Class.RIGHT_PARANTHESIS != TokenStream[InputPointer-1].token_type && Token_Class.COMMA != TokenStream[InputPointer-1].token_type && Token_Class.LEFT_PARANTHESIS != TokenStream[InputPointer].token_type)
                    ret.Children.Add(IDEN_LIST());
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node TERM()
        {
            Node ret = new Node("TERM");

            if (InputPointer < TokenStream.Count() && (Token_Class.Number) == TokenStream[InputPointer].token_type)
                ret.Children.Add(match(Token_Class.Number));
            else if (InputPointer < TokenStream.Count() && (Token_Class.IDENTFIER) == TokenStream[InputPointer].token_type && InputPointer + 1 < TokenStream.Count() && (Token_Class.LEFT_PARANTHESIS) == TokenStream[InputPointer + 1].token_type)
                ret.Children.Add(FUNCTION_CALL());
            else 
                ret.Children.Add(match(Token_Class.IDENTFIER));

            return ret;
        }

        Node EQUATION_OR_TERM()
        {
            int oldPointer = InputPointer;
            int oldErrorCnt = Errors.Error_List.Count;
            bool foundOP = false, foundPARANTHESIS = false;

            Node eq = EQUATION(ref foundOP, ref foundPARANTHESIS);
            int newErrorCnt = Errors.Error_List.Count;

            if(!foundOP && !foundPARANTHESIS) //this should be a term not an equation at all
            {
                /// 1) reset data 
                InputPointer = oldPointer;
                while (newErrorCnt != oldErrorCnt)
                {
                    newErrorCnt--;
                    Errors.Error_List.RemoveAt(newErrorCnt);
                }

                /// 2) try term
                return TERM();
            }

            if(!foundOP && foundPARANTHESIS) //this error isn't handled by default
            {
                Errors.Error_List.Add("Parsing Error: Expected "
                      + "At least One Aritmetic Operation" + "\r\n");
            }

            return eq;
        }

        Node EQUATION(ref bool foundOP, ref bool foundPARANTHESIS)
        {
            Node ret = new Node("EQUATION");

            ret.Children.Add(TERM_EQ(ref foundOP, ref foundPARANTHESIS));
            ret.Children.Add(EQUATION_COMP(ref foundOP, ref foundPARANTHESIS));

            return ret;
        }

        Node EQUATION_COMP(ref bool foundOP, ref bool foundPARANTHESIS)
        {
            Node ret = new Node("EQUATION_COMP");

            bool isAddOp = InputPointer < TokenStream.Count() && ((Token_Class.PLUS_OP) == TokenStream[InputPointer].token_type || (Token_Class.MINUS_OP) == TokenStream[InputPointer].token_type);
            if (isAddOp)
            {
                ret.Children.Add(ADDOP(ref foundOP));
                ret.Children.Add(EQUATION(ref foundOP, ref foundPARANTHESIS));
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node TERM_EQ(ref bool foundOP, ref bool foundPARANTHESIS)
        {
            Node ret = new Node("TERM_EQ");
            ret.Children.Add(FACTOR(ref foundOP, ref foundPARANTHESIS)); 
            ret.Children.Add(TERM_EQ_COMP(ref foundOP, ref foundPARANTHESIS));
            return ret;

        }

        Node TERM_EQ_COMP(ref bool foundOP, ref bool foundPARANTHESIS)
        {
            Node ret = new Node("TERM_EQ_COMP");

            bool isMulOp = InputPointer < TokenStream.Count() && ((Token_Class.MULTIPLY_OP) == TokenStream[InputPointer].token_type || (Token_Class.DEVISON_OP) == TokenStream[InputPointer].token_type);
            if (isMulOp)
            {
                ret.Children.Add(MULOP(ref foundOP));
                ret.Children.Add(TERM_EQ(ref foundOP, ref foundPARANTHESIS));
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node FACTOR(ref bool foundOP, ref bool foundPARANTHESIS)
        {
            Node ret = new Node("FACTOR");

            if (InputPointer < TokenStream.Count() && (Token_Class.LEFT_PARANTHESIS) == TokenStream[InputPointer].token_type)
            {
                foundPARANTHESIS = true;
                ret.Children.Add(match(Token_Class.LEFT_PARANTHESIS));
                ret.Children.Add(EQUATION(ref foundOP, ref foundPARANTHESIS));
                ret.Children.Add(match(Token_Class.RIGHT_PARANTHESIS));

            }
            else 
            {
                ret.Children.Add(TERM());
            }

            return ret;
        }

        Node ADDOP(ref bool foundOP)
        {
            Node ret = new Node("ADDOP");

            if (InputPointer < TokenStream.Count() && (Token_Class.PLUS_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.PLUS_OP));
                foundOP = true;
            }
            else if (InputPointer < TokenStream.Count() && (Token_Class.MINUS_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.MINUS_OP));
                foundOP = true;
            }

            return ret;
        }

        Node MULOP(ref bool foundOP)
        {
            Node ret = new Node("MULOP");

            if (InputPointer < TokenStream.Count() && (Token_Class.MULTIPLY_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.MULTIPLY_OP));
                foundOP = true;
            }
            else if (InputPointer < TokenStream.Count() && (Token_Class.DEVISON_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.DEVISON_OP));
                foundOP = true;
            }

            return ret;
        }

        Node EXPRESSION()
        {
            Node ret = new Node("EXPRESSION");

            if (InputPointer < TokenStream.Count() && (Token_Class.Stirng_Body) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.Stirng_Body));
            }
            else
            {
                ret.Children.Add(EQUATION_OR_TERM());
            }

            return ret;

        }

        Node ASSIGNMENT_STATEMENT()
        {
            Node ret = new Node("ASSIGNMENT_STATEMENT");
            ret.Children.Add(match(Token_Class.IDENTFIER));
            ret.Children.Add(match(Token_Class.ASSIGNMENT_OP));
            ret.Children.Add(EXPRESSION());

            return ret;
        }

        Node DECLARATION_STATEMENT()
        {
            Node ret = new Node("DECLARATION_STATEMENT");

            ret.Children.Add(DataType());
            ret.Children.Add(ID_LIST_DEC());
            ret.Children.Add(match(Token_Class.SEMICOLON));

            return ret;
        }

        Node ID_LIST_DEC()
        {
            Node ret = new Node("ID_LIST_DEC");


            ret.Children.Add(VARIABLE_DEC());
            ret.Children.Add(IDEN_LIST_DEC());

            return ret;
        }

        Node IDEN_LIST_DEC()
        {
            Node ret = new Node("IDEN_LIST_DEC");

            if (InputPointer < TokenStream.Count() && (Token_Class.COMMA) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.COMMA));
                ret.Children.Add(VARIABLE_DEC());
                ret.Children.Add(IDEN_LIST_DEC());
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node VARIABLE_DEC()
        {
            Node ret = new Node("VARIABLE_DEC");

            //if this is an assignment statemnt or just the identefier
            if (InputPointer + 1 < TokenStream.Count() && (Token_Class.ASSIGNMENT_OP) == TokenStream[InputPointer + 1].token_type)
            {
                //ret.Children.Add(match(Token_Class.IDENTFIER));
                ret.Children.Add(ASSIGNMENT_STATEMENT());
            }
            else
                ret.Children.Add(match(Token_Class.IDENTFIER));

            return ret;
        }

        Node WRITE_STATEMENT()
        {
            Node ret = new Node("WRITE_STATEMENT");

            ret.Children.Add(match(Token_Class.WRITE));
            ret.Children.Add(WRITE_STATEMENT_CONTENT());
            ret.Children.Add(match(Token_Class.SEMICOLON));

            return ret;
        }

        Node WRITE_STATEMENT_CONTENT()
        {
            Node ret = new Node("WRITE_STATEMENT_CONTENT");

            if (InputPointer < TokenStream.Count() && (Token_Class.ENDL) == TokenStream[InputPointer].token_type)
                ret.Children.Add(match(Token_Class.ENDL));
            else
                ret.Children.Add(EXPRESSION());

            return ret;
        }

        Node READ_STATEMENT()
        {
            Node ret = new Node("READ_STATEMENT");

            ret.Children.Add(match(Token_Class.READ));
            ret.Children.Add(match(Token_Class.IDENTFIER));
            ret.Children.Add(match(Token_Class.SEMICOLON));

            return ret;
        }

        Node RETURN_STATEMENT()
        {
            Node ret = new Node("RETURN_STATEMENT");

            ret.Children.Add(match(Token_Class.RETURN));
            ret.Children.Add(EXPRESSION());
            ret.Children.Add(match(Token_Class.SEMICOLON));

            return ret;
        }

        Node CONDITION()
        {
            Node ret = new Node("CONDITION");
            ret.Children.Add(match(Token_Class.IDENTFIER));
            ret.Children.Add(CONDITIONAL_OP());
            ret.Children.Add(TERM());

            return ret;
        }

        Node CONDITIONAL_OP()
        {
            Node ret = new Node("CONDITIONAL_OP");

            if (InputPointer < TokenStream.Count() && (Token_Class.LESS_THAN_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.LESS_THAN_OP));
            }
            else if (InputPointer < TokenStream.Count() && (Token_Class.GREATER_THAN_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.GREATER_THAN_OP));
            }
            else if (InputPointer < TokenStream.Count() && (Token_Class.EQUAL_OP) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.EQUAL_OP));
            }
            else
            {
                ret.Children.Add(match(Token_Class.NOT_EQUAL_OP));
            }

            return ret;
        }

        Node CONDITION_STATEMENT()
        {
            Node ret = new Node("CONDITION_STATEMENT");

            ret.Children.Add(TERM_COND());
            ret.Children.Add(CONDITION_COMP());

            return ret;
        }

        Node CONDITION_COMP()
        {
            Node ret = new Node("CONDITION_COMP");

            if (InputPointer < TokenStream.Count() && (Token_Class.OR) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.OR));
                ret.Children.Add(CONDITION_STATEMENT());
            }
            else
            {
                return null;
            }
            return ret;
        }

        Node TERM_COND()
        {
            Node ret = new Node("TERM_COND");

            ret.Children.Add(CONDITION());
            ret.Children.Add(TERM_COND_COMP());

            return ret;
        }

        Node TERM_COND_COMP()
        {
            Node ret = new Node("TERM_COND_COMP");

            if (InputPointer < TokenStream.Count() && (Token_Class.AND) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(match(Token_Class.AND));
                ret.Children.Add(TERM_COND());
            }
            else
            {
                return null;
            }
            return ret;
        }


        Node IF_STATEMENT()
        {
            Node ret = new Node("IF_STATEMENT");

            ret.Children.Add(match(Token_Class.IF));
            ret.Children.Add(CONDITION_STATEMENT());
            ret.Children.Add(match(Token_Class.THEN));
            ret.Children.Add(STATEMENTS()); 
            ret.Children.Add(CLOSING_IF());

            return ret;
        }

        Node CLOSING_IF()
        {
            Node ret = new Node("CLOSING_IF");

            if (InputPointer < TokenStream.Count() && (Token_Class.ELSEIF) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(ELSE_IF_STATMENT());
            }
            else if(InputPointer < TokenStream.Count() && (Token_Class.ELSE) == TokenStream[InputPointer].token_type)
            {
                ret.Children.Add(ELSE_STATMENT());
            }
            else
            {
                ret.Children.Add(match(Token_Class.END));
            }

            return ret;
        }

        Node ELSE_IF_STATMENT()
        {
            Node ret = new Node("ELSE_IF_STATMENT");

            ret.Children.Add(match(Token_Class.ELSEIF));
            ret.Children.Add(CONDITION_STATEMENT());
            ret.Children.Add(match(Token_Class.THEN));
            ret.Children.Add(STATEMENTS());
            ret.Children.Add(CLOSING_IF());

            return ret;
        }

        Node ELSE_STATMENT()
        {
            Node ret = new Node("ELSE_STATMENT");

            ret.Children.Add(match(Token_Class.ELSE));
            ret.Children.Add(STATEMENTS());
            ret.Children.Add(match(Token_Class.END));

            return ret;
        }


        Node REPEAT_STATEMENT()
        {
            Node ret = new Node("REPEAT_STATEMENT");

            ret.Children.Add(match(Token_Class.REPEAT));
            ret.Children.Add(STATEMENTS());
            ret.Children.Add(match(Token_Class.UNTIL));
            ret.Children.Add(CONDITION_STATEMENT());


            return ret;
        }

        Node PARAMETER()
        {
            Node ret = new Node("PARAMETER");

            ret.Children.Add(DataType());
            ret.Children.Add(match(Token_Class.IDENTFIER));

            return ret;
        }

        Node FUNCTION_DECLARATION()
        {
            Node ret = new Node("FUNCTION_DECLARATION");

            if (InputPointer + 1 < TokenStream.Count() && Token_Class.MAIN == TokenStream[InputPointer + 1].token_type)
                return null;

            ret.Children.Add(DataType());
            ret.Children.Add(match(Token_Class.IDENTFIER));
            ret.Children.Add(match(Token_Class.LEFT_PARANTHESIS));
            ret.Children.Add(PARAMETER_LIST());
            ret.Children.Add(match(Token_Class.RIGHT_PARANTHESIS));

            return ret;
        }

        Node PARAMETER_LIST()
        {
            Node ret = new Node("PARAMETER_LIST");

            bool nextIsDataType = InputPointer < TokenStream.Count() && (Token_Class.INT == TokenStream[InputPointer].token_type
                                                                          || Token_Class.FLOAT == TokenStream[InputPointer].token_type
                                                                          || Token_Class.STRING == TokenStream[InputPointer].token_type);

            if (nextIsDataType)
            {
                ret.Children.Add(PARAMETER());
                ret.Children.Add(OTHER_PARAMETERS());
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node OTHER_PARAMETERS()
        {
            Node ret = new Node("OTHER_PARAMETERS");

            bool hasNext = InputPointer < TokenStream.Count() && (Token_Class.COMMA == TokenStream[InputPointer].token_type);

            if (hasNext)
            {
                ret.Children.Add(match(Token_Class.COMMA));
                ret.Children.Add(PARAMETER());
                ret.Children.Add(OTHER_PARAMETERS());
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node FUNCTION_BODY()
        {
            Node ret = new Node("FUNCTION_BODY");

            ret.Children.Add(match(Token_Class.LEFT_CURLY_BRACE));
            ret.Children.Add(STATEMENTS(true));
            ret.Children.Add(RETURN_STATEMENT());
            ret.Children.Add(match(Token_Class.RIGHT_CURLY_BRACE));

            return ret;
        }

        Node FUNCTION_STATEMENT()
        {
            Node ret = new Node("FUNCTION_STATEMENT");

            ret.Children.Add(FUNCTION_DECLARATION());
            ret.Children.Add(FUNCTION_BODY());

            return ret;
        }
        Node MAIN_FUNCTION()
        {
            Node ret = new Node("MAIN_FUNCTION");

            ret.Children.Add(DataType());
            ret.Children.Add(match(Token_Class.MAIN));
            ret.Children.Add(match(Token_Class.LEFT_PARANTHESIS));
            ret.Children.Add(match(Token_Class.RIGHT_PARANTHESIS));
            ret.Children.Add(FUNCTION_BODY());

            if(InputPointer < TokenStream.Count()) //there are more statement after MAIN which is invalid 
            {
                Errors.Error_List.Add("Parsing Error:  "
                        + "No Code Should Be Written After the Main()" + "\r\n");
            }

            return ret;
        }

        Node MORE_FUNCTION_STATEMENTS()
        {
            Node ret = new Node("MORE_FUNCTION_STATEMENTS");

            bool nextIsNormalFunc = InputPointer < TokenStream.Count() && (  Token_Class.INT == TokenStream[InputPointer].token_type
                                                                          || Token_Class.FLOAT == TokenStream[InputPointer].token_type
                                                                          || Token_Class.STRING == TokenStream[InputPointer].token_type)
                                                                       && (InputPointer + 1 < TokenStream.Count() && Token_Class.MAIN != TokenStream[InputPointer + 1].token_type);


            if (nextIsNormalFunc)
            {
                ret.Children.Add(FUNCTION_STATEMENT());
                ret.Children.Add(MORE_FUNCTION_STATEMENTS());
            }
            else
            {
                return null;
            }

            return ret;
        }

        bool isDataType()
        {
            return Token_Class.INT == TokenStream[InputPointer].token_type || Token_Class.FLOAT == TokenStream[InputPointer].token_type || Token_Class.STRING == TokenStream[InputPointer].token_type;
        }

        bool isBeginingOfStatement(bool fromFunctionBody, ref int returnStatementsCnt)
        {
            if (InputPointer >= TokenStream.Count())
                return false;

            if (Token_Class.RETURN == TokenStream[InputPointer].token_type && !fromFunctionBody)
            {

                returnStatementsCnt++;
                return true;
            }

            if (Token_Class.IF == TokenStream[InputPointer].token_type)
            {
                return true;
            }

            if (Token_Class.READ == TokenStream[InputPointer].token_type)
            {
                return true;
            }

            if (Token_Class.WRITE == TokenStream[InputPointer].token_type)
            {
                return true;
            }

            if (Token_Class.REPEAT == TokenStream[InputPointer].token_type)
            {
                return true;
            }

            if (Token_Class.IDENTFIER == TokenStream[InputPointer].token_type)
            {
                return true;
            }

            if (isDataType())
            {
                return true;
            }

            return false;

        }

        Node STATEMENTS(bool fromFunctionBody = false)
        {
            Node ret = new Node("STATEMENTS");

            int returnStatementsCnt = 0;

            if (isBeginingOfStatement(fromFunctionBody, ref returnStatementsCnt))
            {
                ret.Children.Add(MORE_STATEMENTS(fromFunctionBody, ref returnStatementsCnt));
            }
            else
            {
                return null;
            }

            return ret;
        }

        Node MORE_STATEMENTS(bool fromFunctionBody, ref int returnStatementsCnt)
        {
            Node ret = new Node("MORE_STATEMENTS");


            /*
             *  when the call from a function body we can't accept the return statemnt here as there must exist only one return in that case
             *  but if we are at an if condition for example we can add only one return statement in this branch...
             */


            if (isBeginingOfStatement(fromFunctionBody, ref returnStatementsCnt))
            {
                ret.Children.Add(SINGLE_STATEMENT(fromFunctionBody, ref returnStatementsCnt));
                ret.Children.Add(MORE_STATEMENTS(fromFunctionBody, ref returnStatementsCnt));

            }
            else
            {
                return null;
            }

            return ret;
        }

        Node SINGLE_STATEMENT(bool fromFunctionBody, ref int returnStatementsCnt)
        {
            Node ret = new Node("SINGLE_STATEMENT");


            /*
             *  when the call from a function body we can't accept the return statemnt here as there must exist only one return in that case
             *  but if we are at an if condtion for example we can add only one return statement in this branch...
             */

            if (isBeginingOfStatement(fromFunctionBody, ref returnStatementsCnt))
            {


                //if(returnStatementsCnt > 1 && !fromFunctionBody)
                //{
                //    Errors.Error_List.Add("Parsing Error:  "
                //                        + "More Than One Return Statement was Found in The Same Scope!" + "\r\n");

                //    ret.Children.Add(RETURN_STATEMENT());
                //}
                if (Token_Class.RETURN == TokenStream[InputPointer].token_type)
                {
                    if (fromFunctionBody)
                        return null;
                    else
                        ret.Children.Add(RETURN_STATEMENT());
                }
                else if (Token_Class.IF == TokenStream[InputPointer].token_type)
                {
                    ret.Children.Add(IF_STATEMENT());
                }
                else if (Token_Class.REPEAT == TokenStream[InputPointer].token_type)
                {
                    ret.Children.Add(REPEAT_STATEMENT());
                }
                else if (Token_Class.READ == TokenStream[InputPointer].token_type)
                {
                    ret.Children.Add(READ_STATEMENT());
                }
                else if (Token_Class.WRITE == TokenStream[InputPointer].token_type)
                {
                    ret.Children.Add(WRITE_STATEMENT());
                }
                else if (isDataType() && InputPointer + 2 < TokenStream.Count() && Token_Class.IDENTFIER == TokenStream[InputPointer + 1].token_type && Token_Class.LEFT_PARANTHESIS == TokenStream[InputPointer + 2].token_type)
                {
                    ret.Children.Add(FUNCTION_STATEMENT());
                }
                else if (Token_Class.IDENTFIER == TokenStream[InputPointer].token_type && InputPointer + 1 < TokenStream.Count() && Token_Class.ASSIGNMENT_OP == TokenStream[InputPointer + 1].token_type)
                {
                    ret.Children.Add(ASSIGNMENT_STATEMENT());
                    ret.Children.Add(match(Token_Class.SEMICOLON));
                }
                else //if(isDataType())
                {
                    ret.Children.Add(DECLARATION_STATEMENT());
                }

                //else //if(Token_Class.IDENTFIER == TokenStream[InputPointer].token_type)
                //    ret.Children.Add(CONDITION_STATEMENT());

            }
            else
            {
                return null;
            }

            return ret;
        }


        public Node match(Token_Class ExpectedToken)
        {

            if (InputPointer < TokenStream.Count)
            {
                if (ExpectedToken == TokenStream[InputPointer].token_type)
                {
                    InputPointer++;
                    Node newNode = new Node(ExpectedToken.ToString());

                    return newNode;
                }
                else
                {
                    Errors.Error_List.Add("Parsing Error: Expected "
                        + ExpectedToken.ToString() + " and " +
                        TokenStream[InputPointer].token_type.ToString() +
                        "  found\r\n");
                    InputPointer++;
                    return null;
                }
            }
            else
            {
                Errors.Error_List.Add("Parsing Error: Expected "
                        + ExpectedToken.ToString()  + "\r\n");
                InputPointer++;
                return null;
            }
        }

        public static TreeNode PrintParseTree(Node root)
        {
            TreeNode tree = new TreeNode("Parse Tree");
            TreeNode treeRoot = PrintTree(root);
            if (treeRoot != null)
                tree.Nodes.Add(treeRoot);
            return tree;
        }
        static TreeNode PrintTree(Node root)
        {
            if (root == null || root.Name == null)
                return null;
            TreeNode tree = new TreeNode(root.Name);
            if (root.Children.Count == 0)
                return tree;
            foreach (Node child in root.Children)
            {
                if (child == null)
                    continue;
                tree.Nodes.Add(PrintTree(child));
            }
            return tree;
        }
    }
}
