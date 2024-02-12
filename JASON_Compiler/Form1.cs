using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JASON_Compiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox2.Clear();
            string Code=textBox1.Text.ToLower();
            JASON_Compiler.Start_Compiling(Code);
            PrintTokens();
            treeView1.Nodes.Add(Parser.PrintParseTree(JASON_Compiler.treeroot));
            PrintErrors();
        }
        void PrintTokens()
        {
            for (int i = 0; i < Scanner.Tokens.Count; i++)
            {
               dataGridView1.Rows.Add(Scanner.Tokens.ElementAt(i).lex, Scanner.Tokens.ElementAt(i).token_type);
            }
        }

        void PrintErrors()
        {
            foreach (var err in Scanner.Errors)
            {
                textBox2.Text += "Unknown Token: " + err.lex + "\r\n";
            }

            for (int i=0; i<Errors.Error_List.Count; i++)
            {
                textBox2.Text += Errors.Error_List[i];
            }

            
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            JASON_Compiler.TokenStream.Clear();
            dataGridView1.Rows.Clear();
            treeView1.Nodes.Clear();
            Errors.Error_List.Clear();
            Scanner.Tokens.Clear();
            Scanner.Errors.Clear();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
