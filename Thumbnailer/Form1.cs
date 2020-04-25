using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Thumbnailer
{
    public partial class Form1 : Form
    {
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
            logger = new Logger();
            InitializeComponent();
            IsFullscreen = false;
            outputPath = "";
            PopulateFonts();
            cbInfoFontSelect.SelectedIndex = 0;
            cbTimeFontSelect.SelectedIndex = 0;
            cbInfoPositionSelect.SelectedIndex = 0;
            cbTimePositionSelect.SelectedIndex = 0;
            InfoColor = Color.White;
            TimeColor = Color.White;
            ShadowColor = Color.Black;
            BackgroundColor = Color.Black;
            infoFont = FontFamily.Families[0];
            timeFont = FontFamily.Families[0];
            tsProcessing.Visible = false;
            tsPbar.Visible = false;
            tsCurrentFile.Visible = false;
            tsOf.Visible = false;
            tsTotalFiles.Visible = false;
            tsFiles.Visible = false;
            curFile = 0;
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

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pbLoadItems.Value = 0;
                pbLoadItems.Visible = true;
                fileNames = openFileDialog1.FileNames;
                logger.LogInfo($"Loading {fileNames.Length} files...");
                pbLoadItems.Maximum = fileNames.Length;
                foreach (string fileName in fileNames)
                {
                    logger.LogInfo($"Creating contact sheet object for file {fileName}...");
                    try
                    {
                        ContactSheet cs = new ContactSheet(fileName, logger);
                        sheets.Add(cs);
                        fileListBox.Items.Add(fileName);
                        lblItemsCount.Text = fileListBox.Items.Count + " items";
                        pbLoadItems.PerformStep();
                        fileListBox.Refresh();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                    logger.LogInfo($"Object created for file {fileName}");
                }
                logger.LogInfo($"Loaded {fileNames.Length} files");
            }
            pbLoadItems.Visible = false;
            UpdateState();
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

                foreach(ContactSheet cs in sheets)
                {
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

                        return cs.PrintSheet(outputPath, cbPrintInfo.Checked, infoFont, (int)infoFontSizeSelect.Value,
                                             InfoColor, cbPrintTime.Checked, timeFont, (int)timeFontSizeSelect.Value,
                                             TimeColor, ShadowColor, BackgroundColor);
                    });
                    results[i++] = t;
                    t.Start();

                    tsPbar.PerformStep();
                    tsCurrentFile.Text = (++curFile).ToString();
                    Refresh();
                    GC.Collect();
                }

                tsProcessing.Text = "Building sheets";
                Refresh();
                Task.WhenAll(results).Wait();

                tsPbar.Value = 0;
                curFile = 0;

                bool success = true;
                logger.LogInfo($"Validating tasks...");
                foreach (Task<bool> task in results)
                {
                    if (!task.Result)
                    {
                        success = false;
                    }
                }
                logger.LogInfo($"All tasks finished successfully: {success}");

                if (success)
                {
                    MessageBox.Show("Done!");
                }
                else
                {
                    MessageBox.Show("Done with errors!");
                }

                tsProcessing.Visible = false;
                tsPbar.Visible = false;
                tsCurrentFile.Visible = false;
                tsOf.Visible = false;
                tsTotalFiles.Visible = false;
                tsFiles.Visible = false;

                UpdateState();

                GC.Collect();
                GC.WaitForPendingFinalizers();

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
        }

        private void cbTimeFontSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            timeFont = FontFamily.Families[cbInfoFontSelect.SelectedIndex];
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            ListBox.SelectedObjectCollection selectedItems = new ListBox.SelectedObjectCollection(fileListBox);
            selectedItems = fileListBox.SelectedItems;

            if (fileListBox.SelectedIndex != -1)
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                    fileListBox.Items.Remove(selectedItems[i]);
            }
            UpdateState();
        }

        private void btnResetSelection_Click(object sender, EventArgs e)
        {
            fileListBox.Items.Clear();
            UpdateState();
        }

        private void fileListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                ListBox.SelectedObjectCollection selectedItems = new ListBox.SelectedObjectCollection(fileListBox);
                selectedItems = fileListBox.SelectedItems;

                if (fileListBox.SelectedIndex != -1)
                {
                    for (int i = selectedItems.Count - 1; i >= 0; i--)
                        fileListBox.Items.Remove(selectedItems[i]);
                }
            }
            UpdateState();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileNames = openFileDialog1.FileNames;
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

        private void btnBackgroundColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                BackgroundColor = colorDialog1.Color;
            }
        }

        private void btnInfoColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                InfoColor = colorDialog1.Color;
            }
        }

        private void btnTimeColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                TimeColor = colorDialog1.Color;
            }
        }

        private void btnShadowColorSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                ShadowColor = colorDialog1.Color;
            }
        }
    }
}