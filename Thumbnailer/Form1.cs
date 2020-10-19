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

namespace Thumbnailer
{
    public partial class Form1 : Form
    {
        Config _currentConfig;
        string[] fileNames;
        string outputPath;
        Color InfoColor, TimeColor, ShadowColor, BackgroundColor;
        FontFamily infoFont, timeFont;
        bool IsFullscreen;
        int curFile;
        Logger logger;
        List<ContactSheet> sheets;

        public Form1()
        {
            _currentConfig = Config.Load(Settings.Default.PreviousConfigPath);
            logger = new Logger();
            InitializeComponent();
            IsFullscreen = false;
            outputPath = "";
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

        void SheetCreated(object sender, EventArgs e)
        {
            pbLoadItems.PerformStep();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (openVideoDialog.ShowDialog() == DialogResult.OK)
            {
                pbLoadItems.Value = 0;
                pbLoadItems.Visible = true;
                fileNames = openVideoDialog.FileNames;
                logger.LogInfo($"Loading {fileNames.Length} files...");
                pbLoadItems.Maximum = fileNames.Length;

                var newSheets = ContactSheet.BuildSheets(fileNames, logger);
                sheets.AddRange(newSheets);

                foreach(var s in newSheets)
                {
                    fileListBox.Items.Add(s.FilePath);
                }
                fileListBox.Refresh();
                lblItemsCount.Text = fileListBox.Items.Count + " items";

                //foreach (string fileName in fileNames)
                //{
                //    logger.LogInfo($"Creating contact sheet object for file {fileName}...");
                //    try
                //    {
                //        ContactSheet cs = new ContactSheet(fileName, logger);
                //        sheets.Add(cs);
                //        fileListBox.Items.Add(fileName);
                //        lblItemsCount.Text = fileListBox.Items.Count + " items";
                //        pbLoadItems.PerformStep();
                //        fileListBox.Refresh();
                //    }
                //    catch(Exception ex)
                //    {
                //        MessageBox.Show(ex.Message);
                //        return;
                //    }
                //    logger.LogInfo($"Object created for file {fileName}");
                //}
                //logger.LogInfo($"Loaded {fileNames.Length} files");
            }
            pbLoadItems.Visible = false;
            UpdateState();
        }

        private void btnLoadFolder_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                var files = Loader.LoadFiles(folderBrowserDialog1.SelectedPath);
                logger.LogInfo($"Loading {files.Length} files...");
                pbLoadItems.Maximum = files.Length;

                var newSheets = ContactSheet.BuildSheets(files, logger);
                sheets.AddRange(newSheets);

                foreach (var s in newSheets)
                {
                    fileListBox.Items.Add(s.FilePath);
                }
                fileListBox.Refresh();
                lblItemsCount.Text = fileListBox.Items.Count + " items";
            }
            pbLoadItems.Visible = false;
            UpdateState();
        }

        void SheetPrinted(object sender, EventArgs e)
        {
            //tsPbar.PerformStep();
            //tsCurrentFile.Text = (++curFile).ToString();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var start = DateTime.Now;
            logger.LogInfo($"Starting conversion");
            int count = fileListBox.Items.Count;
            if (count > 0)
            {
                logger.LogInfo($"Converting {count} files...");
                btnStart.Enabled = false;
                tsPbar.Value = 0;
                tsProcessing.Visible = true;
                tsPbar.Visible = true;
                tsCurrentFile.Visible = true;
                tsOf.Visible = true;
                tsTotalFiles.Visible = true;
                tsFiles.Visible = true;
                tsTotalFiles.Text = count.ToString();
                tsPbar.Maximum = count;
                tsCurrentFile.Text = curFile.ToString();

                Task<bool>[] results = new Task<bool>[count];
                tsProcessing.Text = "Starting frame capture";
                int i = 0;

                TaskFactory ts = new TaskFactory();

                foreach (ContactSheet cs in sheets)
                {
                    cs.SheetPrinted += SheetPrinted;
                    cs.Rows = (int)rowsSelect.Value;
                    cs.Columns = (int)colsSelect.Value;
                    cs.Width = (int)widthSelect.Value;
                    cs.Gap = (int)gapSelect.Value;
                    var t = new Task<bool>(() =>
                    {
                        if (cbSameDir.Checked)
                        {
                            outputPath = cs.FilePath;
                        }
                        else
                        {
                            outputPath = tbOutput.Text + "/" + new FileInfo(cs.FilePath.ToString()).Name;
                        }
                        GC.Collect();

                        return cs.PrintSheet(outputPath, cbPrintInfo.Checked, infoFont, (int)infoFontSizeSelect.Value,
                                             InfoColor, cbPrintTime.Checked, timeFont, (int)timeFontSizeSelect.Value,
                                             TimeColor, ShadowColor, BackgroundColor);
                    });
                    
                    results[i] = t;
                    t.Start();
                    i++;
                    //tsPbar.PerformStep();
                    tsCurrentFile.Text = (++curFile).ToString();
                    Refresh();
                }

                tsProcessing.Text = "Building sheets";
                tsPbar.Value = 0;
                curFile = 0;
                Refresh();
                Task.WhenAll(results).Wait();

                bool success = true;
                logger.LogInfo($"Validating tasks...");
                foreach (Task<bool> task in results)
                {
                    if (!task.Result)
                    {
                        success = false;
                    }
                }
                if (results.Length != sheets.Count)
                {
                    success = false;
                    logger.LogError($"Expected {sheets.Count} sheets, but only got {results.Length}");
                }
                //logger.LogInfo($"All tasks finished successfully: {success}");

                if (success)
                {
                    MessageBox.Show(this, "Done!");
                }
                else
                {
                    MessageBox.Show(this, "Done with errors!");
                }

                tsProcessing.Visible = false;
                tsPbar.Visible = false;
                tsCurrentFile.Visible = false;
                tsOf.Visible = false;
                tsTotalFiles.Visible = false;
                tsFiles.Visible = false;

                UpdateState();

                GC.Collect();

                logger.LogInfo($"*** Done in {DateTime.Now.Subtract(start).TotalSeconds} seconds ***");
            }
            else
            {
                logger.LogError($"No files loaded.");
                MessageBox.Show("No files loaded.");
            }
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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
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
                    catch(Exception ex)
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
            ListBox.SelectedObjectCollection selectedItems = new ListBox.SelectedObjectCollection(fileListBox);
            selectedItems = fileListBox.SelectedItems;
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
                fileNames = openVideoDialog.FileNames;
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

        private void Form1_Paint(object sender, PaintEventArgs e)
        {

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
            Settings.Default.PreviousConfigPath = _currentConfig.Path;
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

        private void btnInfoColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.InfoColor = colorDialog1.Color.ToArgb();
                InfoColor = colorDialog1.Color;
            }
        }

        private void btnTimeColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentConfig.TimeColor = colorDialog1.Color.ToArgb();
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
            InfoColor = Color.FromArgb(_currentConfig.InfoColor);
            TimeColor = Color.FromArgb(_currentConfig.TimeColor);
            ShadowColor = Color.FromArgb(_currentConfig.ShadowColor);
            BackgroundColor = Color.FromArgb(_currentConfig.BackgroundColor);
            infoFont = FontFamily.Families[cbInfoFontSelect.SelectedIndex];
            timeFont = FontFamily.Families[cbTimeFontSelect.SelectedIndex];
            cbPrintInfo.Checked = _currentConfig.PrintInfo;
            cbPrintTime.Checked = _currentConfig.PrintTime;
        }
    }
}