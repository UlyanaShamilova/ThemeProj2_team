using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThemeProj2.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ThemeProj2
{
    public partial class DetailsForm : Form
    {
        DataRowView row;
        int rowIndex;
        int maxRowIndex;

        public DetailsForm(DataRowView row, int rowIndex, int maxRowIndex)
        {
            InitializeComponent();
            this.row = row;
            this.rowIndex = rowIndex;
            this.maxRowIndex = maxRowIndex;
        }

        private void DetailsForm_Load(object sender, EventArgs e)
        {
            SetFormState(0);

            LoadSummaryData(row);
            LoadPhoto(row);
        }

        private void LoadSummaryData(DataRowView drv)
        {
            textBox1.Text = drv["Name_Author"]?.ToString();
            textBox2.Text = drv["Style_Music"]?.ToString();
            textBox3.Text = drv["country"]?.ToString();
            textBox4.Text = drv["start_year"]?.ToString();

            int artistId = (int)drv["ID_Author"];
            Form1 form1 = this.Owner as Form1;
            Dictionary<string, bool> artistCategories = form1.GetAssignedArtAlb(artistId);

            checkedListBox1.Items.Clear();
            foreach (KeyValuePair<string, bool> item in artistCategories)
            {
                checkedListBox1.Items.Add(item.Key, item.Value);
            }
        }

        private void LoadPhoto(DataRowView drv)
        {
            pictureBox1.Image = Resources.stop_photo;

            string photoFileName = drv["name_media"]?.ToString();
            if (string.IsNullOrEmpty(photoFileName)) return;

            string photoPath = Path.Combine(Application.StartupPath, "Assets", photoFileName);

            if (File.Exists(photoPath)) pictureBox1.ImageLocation = photoPath;
        }

        private void SetFormState(int state)
        {
            switch (state)
            {
                case 0:
                    textBox1.ReadOnly = true;
                    textBox2.ReadOnly = true;
                    textBox3.ReadOnly = true;
                    textBox4.ReadOnly = true;
                    checkedListBox1.Enabled = false;

                    button2.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 1:
                    button2.Enabled = false;

                    textBox1.ReadOnly = false;
                    textBox2.ReadOnly = false;
                    textBox3.ReadOnly = false;
                    textBox4.ReadOnly = false;

                    checkedListBox1.Enabled = true;

                    button3.Enabled = true;
                    button4.Enabled = true;
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SetFormState(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveChanges();
            SetFormState(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            LoadSummaryData(row);
            SetFormState(0);
        }

        private void SaveChanges()
        {
            string artistName = textBox1.Text;
            string style = textBox2.Text;
            string country = textBox3.Text;
            int start_year = Int16.Parse(textBox4.Text);

            row["Name_Author"] = artistName;
            row["Style_Music"] = style;
            row["country"] = country;
            row["start_year"] = start_year;

            Dictionary<string, bool> artistCategories = new Dictionary<string, bool>();
            foreach (string item in checkedListBox1.Items)
            {
                artistCategories.Add(item, checkedListBox1.CheckedItems.Contains(item));
            }

            Form1 form1 = this.Owner as Form1;
            int artistId = Int16.Parse(row["ID_Author"]?.ToString());
            form1.UpdateRowAssignments(artistId, artistCategories);

            row.EndEdit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                File.Copy(ofd.FileName, Path.Combine(Application.StartupPath, "Assets", Path.GetFileName(ofd.FileName)), true);
                string photoFileName = Path.GetFileName(ofd.FileName);
                row["photo"] = photoFileName;
                row.EndEdit();

                pictureBox1.ImageLocation = ofd.FileName;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            --rowIndex;

            if (rowIndex < 0) rowIndex = 0;

            row = (this.Owner as Form1).GetArtistRow(rowIndex);

            LoadSummaryData(row);
            LoadPhoto(row);
            SetFormState(0);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (rowIndex == maxRowIndex) return;

            ++rowIndex;
            row = (this.Owner as Form1).GetArtistRow(rowIndex);

            LoadSummaryData(row);
            LoadPhoto(row);
            SetFormState(0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            rowIndex = maxRowIndex;
            row = (this.Owner as Form1).GetLastArtistRow();

            LoadSummaryData(row);
            LoadPhoto(row);
            SetFormState(0);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Form1 form1 = this.Owner as Form1;
            row = form1.GetFirstArtistRow();
            rowIndex = 0;

            LoadSummaryData(row);
            LoadPhoto(row);
            SetFormState(0);
        }
    }
}
