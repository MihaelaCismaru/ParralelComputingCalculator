using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CalculatorRapid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _operation;
        private string _result;
        public event PropertyChangedEventHandler PropertyChanged;
        public Thread _calculateThread;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                OnPropertyChanged("Result");
            }
        }
        public string Operation
        {
            get { return _operation; }
            set {
                _operation = value;
                OnPropertyChanged("Operation");
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            MyGrid.DataContext = this;
            _calculateThread = new Thread(new ThreadStart(KeepUpdating));
            _calculateThread.Start();
        }

        private void Add_Element(object sender, RoutedEventArgs e)
        {
            var number = (e.Source as Button).Content.ToString();
            Operation += number;
        }

        private bool CheckFormat(string Operation)
        {
            string legalCharacters = "0123456789./+*-()^";
            string justOne = "./ +*-^";
            string numbers = "0123456789";
            char last_ch = 'o';
            foreach (char c in Operation)
            {
                if (!legalCharacters.Contains(c)) {
                    return false;
                }
                if (justOne.Contains(c) && justOne.Contains(last_ch))
                {
                    return false;
                }
                if (numbers.Contains(c) && last_ch== ')') {
                    return false;
                }
                if (numbers.Contains(last_ch) && c == '(')
                {
                    return false;
                }
                if (c ==')' && last_ch== '(')
                {
                    return false;
                }
                last_ch = c;
            }
            return true;
        }

        Tuple<double?, int> Expresie(string Op, int index)
        {
            Tuple<double?, int> termen = Termen(Op, index);
            double? r = termen.Item1;
            index = termen.Item2;
            while (index < Op.Length && (Op[index] == '+' || Op[index] == '-'))
            {
                if (Op[index] == '+')
                {
                    index++;
                    Tuple<double?, int> result = Termen(Op, index);
                    if (result.Item1 != null)
                    {
                        r += result.Item1;
                    }
                    index = result.Item2;
                }
                else
                {
                    index++;
                    Tuple<double?, int> result = Termen(Op, index);
                    if (result.Item1 != null)
                    {
                        r -= result.Item1;
                    }
                    index = result.Item2;
                }
            }
            return Tuple.Create(r, index);
        }

        Tuple<double?, int> Termen(string Op, int index)
        {
            Tuple<double?, int> factor = Factor(Op, index);
            double? r = factor.Item1;
            index = factor.Item2;
            while (index < Op.Length && (Op[index] == '*' || Op[index] == '/' || Op[index] == '^'))
            {
                if (Op[index] == '*')
                {
                    index++;
                    Tuple<double?,int> result = Factor(Op,index);
                    if (result.Item1 != null)
                    {
                        r *= result.Item1;
                    }          
                    index = result.Item2;    
                }
                else
                if (Op[index] == '/')
                {
                    index++;
                    Tuple<double?, int> result = Factor(Op, index);
                    if (result.Item1 != null)
                    {
                        r /= result.Item1;
                    }
                    index = result.Item2;
                }
                else
                {
                    index++;
                    Tuple<double?, int> result = Factor(Op, index);
                    if (result.Item1 != null)
                    {
                        double a = (double)r;
                        double b = (double)result.Item1;
                        r = Math.Pow (a, b);
                    }
                    index = result.Item2;
                }
            }
            return Tuple.Create(r, index);
        }

        Tuple<double?, int> Factor (string Op, int index)
        {
            Tuple<double?, int> value;
            if (Op.Length == index) {
                return new Tuple<double?, int>(null, index);
            }
            if (Op[index] == '(')
            {
                Tuple<double?, int> result = Expresie(Op, index+1);
                value = new Tuple<double?, int>(result.Item1, result.Item2 + 1);
            }
            else
            {
                double? left = 0;
                double? right = 0;
                bool dot = false;
                while (index < Op.Length && (Op[index] == '.' || (Op[index] >= '0' && Op[index] <= '9')))
                {
                    if (Op[index] == '.') {
                        dot = true;
                    }
                    else
                    if (dot == true)
                    {
                        right = right * 10 + Op[index] - 48;
                    }
                    else
                    {
                        left = left * 10 + Op[index] - 48;
                    }
                    index++;
                }
                while (right > 1)
                {
                    right /= 10;
                }
                value = new Tuple<double?, int>(left + right, index);
            }
            return value;
        }

        private string ExpressionEval (string Op)
        {
        
            int index = 0;
            Tuple<double?, int> result = Expresie(Op, index);
            if (result.Item1 == null)
            {
                return "";
            }
            return result.Item1.ToString();
        }

        private bool CheckParentheses(string Operation)
        {
            string stack = "";
            foreach (char c in Operation)
            {
                if (c == '(')
                {
                    stack += c;
                } 
                if (c == ')')
                {
                    if (stack.Length > 0)
                    {
                        stack = stack.Remove(stack.Length - 1);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (stack.Length == 0)
            {
                return true;
            }
            return false;
        }

        private void KeepUpdating ()
        {
            while (true)
            {
                if (Operation == null)
                {
                    Result = "";
                }
                else
                if (CheckFormat(Operation) && CheckParentheses(Operation)) {
                    Result = ExpressionEval(Operation);
                }
                else
                {
                    Result = "Invalid";
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Operation = "";
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Operation.Length > 0)
            {
                Operation = Operation.Remove(Operation.Length-1);
            }
        }

        private void OperationBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Operation = OperationBox.Text;
        }
    }
}