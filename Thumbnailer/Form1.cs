using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Thumbnailer.Properties;
using libthumbnailer;
using System.Linq;

namespace Thumbnailer
{
    public partial class Form1 : Form
    {
        Config _currentConfig;
        Color InfoColor, TimeColor, ShadowColor, BackgroundColor;
        FontFamily infoFont, timeFont;
        bool IsFullscreen;
        int curFile;
        readonly Logger logger;
        readonly List<ContactSheet> sheets;

        public Form1()
        {
            _currentConfig = Config.Load(Settings.Default.PreviousConfigPath);
            logger = new Logger();
            InitializeComponent();
            IsFullscreen = false;
            PopulateFonts();
            ApplyConfig();
            tsProcessing.Visible = false;
            tsPbar.Visible = false;
            tsCurrentFile.Visible = false;
            tsOf.Visible = false;
            tsTotalFiles.Visible = false;
            tsFiles.Visible = false;
            curFile = 0;

            ContactSheet.SheetCreated += SheetCreated;

            UpdateState();

            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");

            sheets = new List<ContactSheet>();
        }

        void UpdateState()
        {
            if (fileListBox.Items.Count > 0)
            {
                btnStart.Enabled = true;
                btnRemoveSelected.Enabled = true;
                btnResetSelection.Enabled = true;
            }
            else
            {
                btnStart.Enabled = false;
                btnRemoveSelected.Enabled = false;
                btnResetSelection.Enabled = false;
            }
            lblItemsCount.Text = fileListBox.Items.Count.ToString() + " items";
            GC.Collect();
        }

        void SheetCreated(object sender, string e)
        {
            pbLoadItems.PerformStep();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (openVideoDialog.ShowDialog() == DialogResult.OK)
            {
                pbLoadItems.Value = 0;
                pbLoadItems.Visible = true;
                var fileNames = openVideoDialog.FileNames;
                logger.LogInfo($"Loading {fileNames.Length} files...");
                pbLoadItems.Maximum = fileNames.Length;

                fileListBox.Items.AddRange(fileNames);
                //var newSheets = ContactSheet.BuildSheets(fileNames, logger);
                //sheets.AddRange(newSheets);

                //foreach(var s in newSheets)
                //{
                //    fileListBox.Items.Add(s.FilePath);
                //}
                fileListBox.Refresh();
                lblItemsCount.Text = fileListBox.Items.Count + " items";
            }
            pbLoadItems.Visible = false;
            UpdateState();
        }

        private void btnLoadFolder_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                pbLoadItems.Value = 0;
                pbLoadItems.Visible = true;
                var fileNames = Loader.LoadFiles(folderBrowserDialog1.SelectedPath);
                logger.LogInfo($"Loading {fileNames.Length} files...");
                pbLoadItems.Maximum = fileNames.Length;

                fileListBox.Items.AddRange(fileNames);

                //var newSheets = ContactSheet.BuildSheets(files, logger);
                //sheets.AddRange(newSheets);

                //foreach (var s in newSheets)
                //{
                //    fileListBox.Items.Add(s.FilePath);
                //}
                fileListBox.Refresh();
                lblItemsCount.Text = fileListBox.Items.Count + " items";
            }
            pbLoadItems.Visible = false;
            UpdateState();
        }

        void SheetPrinted(object sender, string e)
        {
            tsPbar.PerformStep();
            tsCurrentFile.Text = (++curFile).ToString();
        }

        void AllSheetsPrinted(object sender, string e)
        {
            EnableItems();
            MessageBox.Show(e);
            ContactSheet.AllSheetsPrinted -= AllSheetsPrinted;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            logger.LogInfo($"Starting conversion");
            int count = fileListBox.Items.Count;
            if (count > 0)
            {
                logger.LogInfo($"Building {count} sheets...");
                tsProcessing.Text = "Starting image capture...";
                foreach (var f in fileListBox.Items)
                {
                    sheets.Add(ContactSheetFactory.CreateContactSheet(f.ToString(), logger));
                }

                tsProcessing.Text = "Processing...";
                Refresh();

                curFile = 0;
                tsPbar.Value = 0;
                tsPbar.Maximum = count;
                tsCurrentFile.Text = curFile.ToString();
                tsTotalFiles.Text = count.ToString();
                DisableItems();
                sheets.ForEach(x => x.SheetPrinted += SheetPrinted);
                ContactSheet.AllSheetsPrinted += AllSheetsPrinted;
                logger.LogInfo($"Converting {count} files...");
                _ = ContactSheet.PrintSheets(sheets, _currentConfig, logger, tbOutput.Text);
            }
            else
            {
                MessageBox.Show("No files loaded.");
            }
        }

        private void DisableItems()
        {
            tsStatusLabel.Text = "Working...";

            rowsSelect.Enabled = false;
            colsSelect.Enabled = false;
            widthSelect.Enabled = false;
            gapSelect.Enabled = false;

            cbPrintInfo.Enabled = false;
            infoFontSizeSelect.Enabled = false;
            cbInfoFontSelect.Enabled = false;
            btnBackgroundColorSelect.Enabled = false;
            btnInfoColorSelect.Enabled = false;

            cbPrintTime.Enabled = false;
            timeFontSizeSelect.Enabled = false;
            cbTimeFontSelect.Enabled = false;
            btnTimeColorSelect.Enabled = false;
            btnShadowColorSelect.Enabled = false;

            fileListBox.Enabled = false;
            btnLoad.Enabled = false;
            btnLoadFolder.Enabled = false;
            btnRemoveSelected.Enabled = false;
            btnResetSelection.Enabled = false;

            cbSameDir.Enabled = false;
            tbOutput.Enabled = false;
            btnOutputSelect.Enabled = false;
            btnStart.Enabled = false;

            tsProcessing.Visible = true;
            tsPbar.Visible = true;
            tsCurrentFile.Visible = true;
            tsOf.Visible = true;
            tsTotalFiles.Visible = true;
            tsFiles.Visible = true;
        }

        private void EnableItems()
        {
            tsStatusLabel.Text = "Ready";

            rowsSelect.Enabled = true;
            colsSelect.Enabled = true;
            widthSelect.Enabled = true;
            gapSelect.Enabled = true;

            cbPrintInfo.Enabled = true;
            infoFontSizeSelect.Enabled = true;
            cbInfoFontSelect.Enabled = true;
            btnBackgroundColorSelect.Enabled = true;
            btnInfoColorSelect.Enabled = true;

            cbPrintTime.Enabled = true;
            timeFontSizeSelect.Enabled = true;
            cbTimeFontSelect.Enabled = true;
            btnTimeColorSelect.Enabled = true;
            btnShadowColorSelect.Enabled = true;

            fileListBox.Enabled = true;
            btnLoad.Enabled = true;
            btnLoadFolder.Enabled = true;
            btnRemoveSelected.Enabled = true;
            btnResetSelection.Enabled = true;

            cbSameDir.Enabled = true;
            tbOutput.Enabled = true;
            btnOutputSelect.Enabled = true;
            btnStart.Enabled = true;

            tsProcessing.Visible = false;
            tsPbar.Visible = false;
            tsCurrentFile.Visible = false;
            tsOf.Visible = false;
            tsTotalFiles.Visible = false;
            tsFiles.Visible = false;
        }

        private void btnOutputSelect_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                tbOutput.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void cbSameDir_CheckedChanged(object sender, EventArgs e)
        {
            tbOutput.Enabled = !tbOutput.Enabled;
            btnOutputSelect.Enabled = !btnOutputSelect.Enabled;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.LogInfo($"Form closing...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            logger.LogInfo($"Beginning clean-up...");
            foreach (string d in Directory.GetDirectories("temp"))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Failed to delete file {f}: {ex.Message}");
                    }
                }
                try
                {
                    Directory.Delete(d);
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Failed to delete directory {d}: {ex.Message}");
                }
            }
            logger.LogInfo($"Clean-up done!");
            logger.Close();
            Thread.Sleep(100);
        }

        void PopulateFonts()
        {
            FontFamily[] fontFamilies;

            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            fontFamilies = installedFontCollection.Families;

            int count = fontFamilies.Length;
            logger.LogInfo($"Loading {count} fonts...");
            for (int j = 0; j < count; ++j)
            {
                cbInfoFontSelect.Items.Add(fontFamilies[j].Name);
                cbTimeFontSelect.Items.Add(fontFamilies[j].Name);
            }
        }

        private void cbInfoFontSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            infoFont = FontFamily.Families[cbInfoFontSelect.SelectedIndex];
            _currentConfig.InfoFont = infoFont.Name;
        }

        private void cbTimeFontSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            timeFont = FontFamily.Families[cbInfoFontSelect.SelectedIndex];
            _currentConfig.TimeFont = timeFont.Name;
        }

        private void RemoveSelectedItems()
        {
            ListBox.SelectedObjectCollection selectedItems = fileListBox.SelectedItems;
            List<ContactSheet> tempSheetList = new List<ContactSheet>();

            if (fileListBox.SelectedIndex != -1)
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    foreach (var item in sheets)
                    {
                        if (item.FilePath == selectedItems[i].ToString())
                        {
                            tempSheetList.Add(item);
                        }
                    }
                    fileListBox.Items.Remove(selectedItems[i]);
                }
                foreach(var item in tempSheetList)
                {
                    sheets.Remove(item);
                }
            }
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            RemoveSelectedItems();
            UpdateState();
        }

        private void btnResetSelection_Click(object sender, EventArgs e)
        {
            fileListBox.Items.Clear();
            sheets.Clear();
            UpdateState();
        }

        private void fileListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedItems();
            }
            UpdateState();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openVideoDialog.ShowDialog() == DialogResult.OK)
            {
                var fileNames = openVideoDialog.FileNames;
                foreach (string fileName in fileNames)
                {
                    fileListBox.Items.Add(fileName);
                    fileListBox.Refresh();
                }
            }
            UpdateState();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void fullscreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsFullscreen)
            {
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                fullscreenToolStripMenuItem.Checked = true;
                IsFullscreen = true;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.Sizable;
                fullscreenToolStripMenuItem.Checked = false;
                IsFullscreen = false;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //pbPreview.Size = scrollableControl1.Size;
            //pbPreview.Location = scrollableControl1.Location;
        }

        private void SetDimensions(int rows, int cols)
        {
            rowsSelect.Value = rows;
            colsSelect.Value = cols;
            _currentConfig.Rows = rows;
            _currentConfig.Columns = cols;
            Refresh();
        }

        private void x1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetDimensions(1, 1);
        }

        private void x2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetDimensions(1, 2);
        }

        private void x3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetDimensions(1, 3);
        }

        private void x4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetDimensions(1, 4);
        }

        private void x5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetDimensions(1, 5);
        }

        private void x1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetDimensions(2, 1);
        }

        private void x2ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetDimensions(2, 2);
        }

        private void x3ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetDimensions(2, 3);
        }

        private void x4ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetDimensions(2, 4);
        }

        private void x5ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetDimensions(2, 5);
        }

        private void x1ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SetDimensions(3, 1);
        }

        private void x2ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SetDimensions(3, 2);
        }

        private void x3ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SetDimensions(3, 3);
        }

        private void x4ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SetDimensions(3, 4);
        }

        private void x5ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SetDimensions(3, 5);
        }

        private void x1ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SetDimensions(4, 1);
        }

        private void x2ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SetDimensions(4, 2);
        }

        private void x3ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SetDimensions(4, 3);
        }

        private void x4ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SetDimensions(4, 4);
        }

        private void x5ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SetDimensions(4, 5);
        }

        private void x1ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SetDimensions(5, 1);
        }

        private void x2ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SetDimensions(5, 2);
        }

        private void x3ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SetDimensions(5, 3);
        }

        private void x4ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SetDimensions(5, 4);
        }

        private void x5ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SetDimensions(5, 5);
        }

        private void btnInfoColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (btnInfoColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(InfoColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void btnTimeColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (btnTimeColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(TimeColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void btnShadowColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (btnShadowColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(ShadowColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void btnBackgroundColorSelect_Paint(object sender, PaintEventArgs e)
        {
            Size size = new Size(12, 12);
            int offset = (btnBackgroundColorSelect.Height / 2) - (size.Height / 2);
            Rectangle r = new Rectangle(new Point(3 * offset, offset), size);
            Pen pen = new Pen(Color.Black);
            SolidBrush infoBrush = new SolidBrush(BackgroundColor);
            e.Graphics.FillRectangle(infoBrush, r);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void saveConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _currentConfig.Save();
            Settings.Default.PreviousConfigPath = _currentConfig.ConfigPath;
            Settings.Default.Save();
        }

        private void saveConfigurationAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openConfigDialog.InitialDirectory = Application.ExecutablePath;
            if(saveConfigDialog.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.SaveAs(saveConfigDialog.FileName);
                Settings.Default.PreviousConfigPath = saveConfigDialog.FileName;
                Settings.Default.Save();
            }
        }

        private void loadConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openConfigDialog.InitialDirectory = Application.ExecutablePath;
            if(openConfigDialog.ShowDialog() == DialogResult.OK)
            {
                _currentConfig = Config.Load(openConfigDialog.FileName);
                Settings.Default.PreviousConfigPath = openConfigDialog.FileName;
                Settings.Default.Save();
                ApplyConfig();
            }
        }

        private void btnBackgroundColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.BackgroundColor = colorDialog1.Color.ToArgb();
                BackgroundColor = colorDialog1.Color;
            }
        }

        private void rowsSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Rows = (int)rowsSelect.Value;
        }

        private void colsSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Columns = (int)colsSelect.Value;
        }

        private void widthSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Width = (int)widthSelect.Value;
        }

        private void gapSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.Gap = (int)gapSelect.Value;
        }

        private void infoFontSizeSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.InfoFontSize = (int)infoFontSizeSelect.Value;
        }

        private void timeFontSizeSelect_ValueChanged(object sender, EventArgs e)
        {
            _currentConfig.TimeFontSize = (int)timeFontSizeSelect.Value;
        }

        private void cbPrintInfo_CheckedChanged(object sender, EventArgs e)
        {
            _currentConfig.PrintInfo = cbPrintInfo.Checked;
        }

        private void cbPrintTime_CheckedChanged(object sender, EventArgs e)
        {
            _currentConfig.PrintTime = cbPrintTime.Checked;
        }

        private void btnInfoColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.InfoFontColor = colorDialog1.Color.ToArgb();
                InfoColor = colorDialog1.Color;
            }
        }

        private void btnTimeColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.TimeFontColor = colorDialog1.Color.ToArgb();
                TimeColor = colorDialog1.Color;
            }
        }

        private void btnShadowColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.ShadowColor = colorDialog1.Color.ToArgb();
                ShadowColor = colorDialog1.Color;
            }
        }

        void ApplyConfig()
        {
            rowsSelect.Value = _currentConfig.Rows;
            colsSelect.Value = _currentConfig.Columns;
            widthSelect.Value = _currentConfig.Width;
            gapSelect.Value = _currentConfig.Gap;
            cbInfoFontSelect.SelectedIndex = cbInfoFontSelect.FindString(_currentConfig.InfoFont);
            cbTimeFontSelect.SelectedIndex = cbInfoFontSelect.FindString(_currentConfig.TimeFont);
            cbInfoPositionSelect.SelectedIndex = 0;
            cbTimePositionSelect.SelectedIndex = 0;
            InfoColor = Color.FromArgb(_currentConfig.InfoFontColor);
            TimeColor = Color.FromArgb(_currentConfig.TimeFontColor);
            ShadowColor = Color.FromArgb(_currentConfig.ShadowColor);
            BackgroundColor = Color.FromArgb(_currentConfig.BackgroundColor);
            infoFont = FontFamily.Families[cbInfoFontSelect.SelectedIndex];
            timeFont = FontFamily.Families[cbTimeFontSelect.SelectedIndex];
            cbPrintInfo.Checked = _currentConfig.PrintInfo;
            cbPrintTime.Checked = _currentConfig.PrintTime;
        }
    }
}