using libthumbnailer2;
using System.Drawing.Text;

namespace Thumbnailer2
{
    public partial class Form1 : Form
    {
        Config _currentConfig;
        Color _infoColor, _timeColor, _shadowColor, _backgroundColor;
        FontFamily _infoFont, _timeFont;
        bool _isFullScreen;
        int _curFile;


        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _currentConfig = await Config.Load("config.json");

            _isFullScreen = false;

            TsProcessing.Visible = false;
            TsPbar.Visible = false;
            TsCurrentFile.Visible = false;
            TsOf.Visible = false;
            TsTotalFiles.Visible = false;
            TsFiles.Visible = false;
            _curFile = 0;

            ApplyConfig();
            PopulateFonts();
        }

        private void BtnInfoColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.InfoFontColor = colorDialog1.Color.ToArgb();
                _infoColor = colorDialog1.Color;
            }
        }

        private void BtnBackgroundColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.BackgroundColor = colorDialog1.Color.ToArgb();
                _backgroundColor = colorDialog1.Color;
            }
        }

        private void BtnTimeColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.TimeFontColor = colorDialog1.Color.ToArgb();
                _timeColor = colorDialog1.Color;
            }
        }

        private void BtnShadowColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.ShadowColor = colorDialog1.Color.ToArgb();
                _shadowColor = colorDialog1.Color;
            }
        }

        private void BtnInfoColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (BtnInfoColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(_infoColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void BtnBackgroundColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (BtnBackgroundColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(_backgroundColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void BtnTimeColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (BtnTimeColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(_timeColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void BtnShadowColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (BtnShadowColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(_shadowColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void RowsSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Rows = (int)RowsSelect.Value;
        }

        private void ColsSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Columns = (int)ColsSelect.Value;
        }

        private void WidthSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Width = (int)WidthSelect.Value;
        }

        private void GapSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Gap = (int)GapSelect.Value;
        }

        private void CbInfoFontSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            _infoFont = FontFamily.Families[CbInfoFontSelect.SelectedIndex];
            _currentConfig.InfoFont = _infoFont.Name;
        }

        private void CbTimeFontSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            _timeFont = FontFamily.Families[CbInfoFontSelect.SelectedIndex];
            _currentConfig.TimeFont = _timeFont.Name;
        }

        private void CbPrintInfo_CheckedChanged(object sender, EventArgs e)
        {
            _currentConfig.PrintInfo = CbPrintInfo.Checked;
        }

        private void CbPrintTime_CheckedChanged(object sender, EventArgs e)
        {
            _currentConfig.PrintTime = CbPrintTime.Checked;
        }

        private async void TsLoadConfig_Click(object sender, EventArgs e)
        {
            OpenConfigDialog.InitialDirectory = Application.StartupPath;
            if (OpenConfigDialog.ShowDialog() == DialogResult.OK)
            {
                _currentConfig = await Config.Load(OpenConfigDialog.FileName);
                ApplyConfig();
            }
        }

        private async void TsSaveConfig_Click(object sender, EventArgs e)
        {
            await _currentConfig.Save();
        }

        private async void TsSaveConfigAs_Click(object sender, EventArgs e)
        {
            SaveConfigDialog.InitialDirectory = Application.ExecutablePath;
            if (SaveConfigDialog.ShowDialog() == DialogResult.OK)
            {
                await _currentConfig.SaveAs(SaveConfigDialog.FileName);
            }
        }

        private void InfoFontSizeSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.InfoFontSize = (int)InfoFontSizeSelect.Value;
        }

        private void TimeFontSizeSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.TimeFontSize = (int)TimeFontSizeSelect.Value;
        }

        void UpdateState()
        {
            if (false)
            {
                BtnStart.Enabled = true;
                //BtnRemoveSelected.Enabled = true;
                //BtnResetSelection.Enabled = true;
            }
            else
            {
                BtnStart.Enabled = false;
                //BtnRemoveSelected.Enabled = false;
                //BtnResetSelection.Enabled = false;
            }
            //lblItemsCount.Text = fileListBox.Items.Count.ToString() + " items";
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {

        }

        void PopulateFonts()
        {
            FontFamily[] fontFamilies;

            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            fontFamilies = installedFontCollection.Families;

            int count = fontFamilies.Length;
            //logger.LogInfo($"Loading {count} fonts...");
            for (int j = 0; j < count; ++j)
            {
                CbInfoFontSelect.Items.Add(fontFamilies[j].Name);
                CbTimeFontSelect.Items.Add(fontFamilies[j].Name);
            }
        }

        private void ApplyConfig()
        {
            RowsSelect.Value = _currentConfig.Rows;
            ColsSelect.Value = _currentConfig.Columns;
            WidthSelect.Value = _currentConfig.Width;
            GapSelect.Value = _currentConfig.Gap;
            CbInfoFontSelect.SelectedIndex = CbInfoFontSelect.FindString(_currentConfig.InfoFont);
            CbTimeFontSelect.SelectedIndex = CbInfoFontSelect.FindString(_currentConfig.TimeFont);
            CbInfoPositionSelect.SelectedIndex = 0;
            CbTimePositionSelect.SelectedIndex = 0;
            _infoColor = Color.FromArgb(_currentConfig.InfoFontColor);
            _timeColor = Color.FromArgb(_currentConfig.TimeFontColor);
            _shadowColor = Color.FromArgb(_currentConfig.ShadowColor);
            _backgroundColor = Color.FromArgb(_currentConfig.BackgroundColor);
            //_infoFont = FontFamily.Families[CbInfoFontSelect.SelectedIndex];
            //_timeFont = FontFamily.Families[CbTimeFontSelect.SelectedIndex];
            CbPrintInfo.Checked = _currentConfig.PrintInfo;
            CbPrintTime.Checked = _currentConfig.PrintTime;
        }
    }
}