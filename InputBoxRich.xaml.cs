using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FACTOVA_Launcher
{
    public partial class InputBoxRich : Window
    {
        public string ResponseText { get; private set; }

        public InputBoxRich(Window owner, string title = "입력", string defaultText = "", bool showDescription = false)
        {
            InitializeComponent();
            this.Owner = owner;
            this.Title = title;
            InputTextBox.Text = defaultText;
            InputTextBox.Focus();

            if (showDescription)
            {
                CreateDescription();
                DescriptionRichTextBox.Visibility = Visibility.Visible;
                this.Height = 300; // 높이를 늘려 안내문구가 보이도록 함
            }
            else
            {
                DescriptionRichTextBox.Visibility = Visibility.Collapsed;
                this.Height = 150; // 기본 높이
            }
        }

        private void CreateDescription()
        {
            Paragraph paragraph = new Paragraph();

            paragraph.Inlines.Add(new Run("폴더 이름을 입력하세요.\n\n폴더 이름은 사업부 혹은 사업부_로 시작해야 합니다.\n\nhttp://"));

            Run runAc = new Run("ac")
            {
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold
            };
            paragraph.Inlines.Add(runAc);

            paragraph.Inlines.Add(new Run(".gmes2.lge.com, http://"));

            Run runKc = new Run("kc")
            {
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold
            };
            paragraph.Inlines.Add(runKc);

            paragraph.Inlines.Add(new Run(".gmes2.lge.com\n\n예시)\nㆍAC\nㆍAC_RPT\nㆍKC_KR3_BOX라벨발행"));

            DescriptionRichTextBox.Document = new FlowDocument(paragraph);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            this.DialogResult = true;
        }
    }
}