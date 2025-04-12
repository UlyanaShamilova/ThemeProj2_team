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

namespace ThemeProj2
{
    public partial class Form1 : Form
    {
        DataSet ds = new DataSet();
        DataView dvArtists, dvAlbums, dvArtistsAlbums;
        const string DefaultXMLFileName = "Database.xml";
        const string DefaultXMLSchemaFileName = "Database.xsd";
        private string currentXMLFilePath = Path.Combine(Application.StartupPath, DefaultXMLFileName);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadXMLData(currentXMLFilePath);
            SetupDataGridViews();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void SetupDataGridViews()
        {
            dataGridView1.Columns["ID_Author"].Visible = false;
            dataGridView2.Columns["ID_Album"].Visible = false;
            dataGridView1.Columns["name_media"].Visible = false;
            dataGridView2.Columns["cover"].Visible = false;

            if (dataGridView1.Columns.Contains("Name_Author")) dataGridView1.Columns["Name_Author"].HeaderText = "Name author";

            if (dataGridView1.Columns.Contains("Style_Music")) dataGridView1.Columns["Style_Music"].HeaderText = "Style music";

            if (dataGridView1.Columns.Contains("country")) dataGridView1.Columns["country"].HeaderText = "Country";

            if (dataGridView1.Columns.Contains("start_year")) dataGridView1.Columns["start_year"].HeaderText = "Start year";

            if (dataGridView2.Columns.Contains("Name_Album")) dataGridView2.Columns["Name_Album"].HeaderText = "Name album";

            if (dataGridView2.Columns.Contains("Year_Album")) dataGridView2.Columns["Year_Album"].HeaderText = "Year album";

            if (dataGridView2.Columns.Contains("tracks_number")) dataGridView2.Columns["tracks_number"].HeaderText = "Tracks number";

            if (dataGridView2.Columns.Contains("duration")) dataGridView2.Columns["duration"].HeaderText = "Duration";

            if (!dataGridView2.Columns.Contains("assigned"))
            {
                var assignedColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "assigned",
                    HeaderText = "Assigned",
                    Width = 60
                };
                dataGridView2.Columns.Insert(0, assignedColumn);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Files (*.xml)|*.xml";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentXMLFilePath = ofd.FileName;
                LoadXMLData(currentXMLFilePath);
            }
        }

        private void LoadXMLData(string xmlFilePath)
        {
            try
            {
                ds.Clear();

                string xmlSchemaPath = Path.Combine(Application.StartupPath, DefaultXMLSchemaFileName);
                ds.ReadXmlSchema(xmlSchemaPath);

                ds.ReadXml(xmlFilePath);

                #region optional code can be foern in the XML schema file

                DataTable artistsTable = ds.Tables["Виконавці"];
                if (artistsTable != null)
                {
                    artistsTable.Columns["Код_Виконавця"].AutoIncrement = true;
                    artistsTable.Columns["Код_Виконавця"].AutoIncrementSeed = GetMaxId(artistsTable);
                    artistsTable.Columns["Код_Виконавця"].AutoIncrementStep = 1;
                }

                DataTable albumsTable = ds.Tables["Альбоми"];
                if (albumsTable != null)
                {
                    albumsTable.Columns["Код_Альбому"].AutoIncrement = true;
                    albumsTable.Columns["Код_Альбому"].AutoIncrementSeed = GetMaxId(albumsTable);
                    albumsTable.Columns["Код_Альбому"].AutoIncrementStep = 1;
                }

                DataTable artistsAlbumsTable = ds.Tables["Виконавці_Альбоми"];
                if (artistsAlbumsTable != null)
                {
                    artistsAlbumsTable.Columns["Ідентифікатор"].AutoIncrement = true;
                    artistsAlbumsTable.Columns["Ідентифікатор"].AutoIncrementSeed = GetMaxId(artistsAlbumsTable);
                    artistsAlbumsTable.Columns["Ідентифікатор"].AutoIncrementStep = 1;
                }

                #endregion

                dvArtists = new DataView(ds.Tables["Artists"]);
                dvAlbums = new DataView(ds.Tables["Albums"]);
                dvArtistsAlbums = new DataView(ds.Tables["ArtAlb"]);

                dataGridView1.DataSource = dvArtists;
                dataGridView2.DataSource = dvAlbums;

                dataGridView1.AutoResizeColumns();
                dataGridView2.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження XML: {ex.Message}");
            }
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            UpdateAlbumAssignments(rowIndex);
        }

        private void UpdateAlbumAssignments(int artistRowIndex)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                row.Cells["assigned"].Value = false;
            }

            if (artistRowIndex < 0) return;

            int? selectedArtistId = (int?)dataGridView1.Rows[artistRowIndex].Cells["ID_Author"].Value;
            if (selectedArtistId == null) return;

            Dictionary<string, bool> ArtAlb = GetAssignedArtAlb(selectedArtistId.Value);

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                DataRowView drv = row.DataBoundItem as DataRowView;
                if (drv != null) row.Cells["assigned"].Value = ArtAlb[drv["Name_Album"].ToString()];
            }

            dvArtistsAlbums.RowFilter = "";
        }

        public Dictionary<string, bool> GetAssignedArtAlb(int artistId)
        {
            Dictionary<string, bool> ArtAlb = new Dictionary<string, bool>();
            dvAlbums.RowFilter = "";
            foreach (DataRowView row in dvAlbums)
            {
                var albumId = row["ID_Album"].ToString();
                dvArtistsAlbums.RowFilter = $"ID_Author = '{artistId}' AND ID_Album = '{albumId}'";
                bool isAssigned = dvArtistsAlbums.Count > 0;

                if (!ArtAlb.ContainsKey(row["Name_Album"].ToString()))
                {
                    ArtAlb.Add(row["Name_Album"].ToString(), isAssigned);
                }
            }
            return ArtAlb;
        }

        public void UpdateRowAssignments(int artistId, Dictionary<string, bool> albumAssignments)
        {
            foreach (KeyValuePair<string, bool> albumAssignment in albumAssignments)
            {
                dvAlbums.RowFilter = $"Name_Album = '{albumAssignment.Key}'";
                int albumId = (int)dvAlbums[0]["ID_Album"];
                bool isChecked = albumAssignment.Value;

                dvArtistsAlbums.RowFilter = $"ID_Author = '{artistId}' AND ID_Album = '{albumId}'";
                if (isChecked && dvArtistsAlbums.Count == 0)
                {
                    DataRowView newRow = dvArtistsAlbums.AddNew();
                    newRow["ID_Author"] = artistId;
                    newRow["ID_Album"] = albumId;
                    newRow.EndEdit();
                }
                else if (!isChecked && dvArtistsAlbums.Count > 0)
                {
                    dvArtistsAlbums.Delete(0);
                }
            }

            UpdateAlbumAssignments(dataGridView1.CurrentRow.Index);
        }

        public DataRowView GetFirstArtistRow()
        {
            return GetArtistRow(0);
        }

        public DataRowView GetLastArtistRow()
        {
            return GetArtistRow(dataGridView1.Rows.Count - 2);
        }

        public DataRowView GetArtistRow(int rowIndex)
        {
            return dataGridView1.Rows[rowIndex].DataBoundItem as DataRowView;
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dataGridView2.Columns[e.ColumnIndex].Name != "assigned") return;

            DataGridViewRow currentArtistRow = dataGridView1.CurrentRow;
            DataRowView albumRow = dataGridView2.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (currentArtistRow == null || albumRow == null) return;

            int? selectedArtistId = (int?)currentArtistRow.Cells["ID_Author"].Value;
            int? albumId = (int?)albumRow["ID_Album"];
            if (selectedArtistId == null || albumId == null) return;

            bool isChecked = !Convert.ToBoolean(dataGridView2.Rows[e.RowIndex].Cells["assigned"].Value);

            dvArtistsAlbums.RowFilter = $"ID_Author = '{selectedArtistId}' AND ID_Album = '{albumId}'";

            if (isChecked && dvArtistsAlbums.Count == 0)
            {
                DataRowView newRow = dvArtistsAlbums.AddNew();
                newRow["ID_Author"] = selectedArtistId;
                newRow["ID_Album"] = albumId;
                newRow.EndEdit();
            }
            else if (!isChecked && dvArtistsAlbums.Count > 0)
            {
                dvArtistsAlbums.Delete(0);
            }

            dvArtistsAlbums.RowFilter = "";
            UpdateAlbumAssignments(dataGridView1.CurrentRow.Index);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ds.WriteXml(currentXMLFilePath, XmlWriteMode.IgnoreSchema);
                MessageBox.Show("Changes successfully saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving XML: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            DataRowView drv = dataGridView1.CurrentRow.DataBoundItem as DataRowView;
            if (drv != null)
            {
                int? artistId = (int?)drv["ID_Author"];
                dvArtistsAlbums.RowFilter = $"ID_Author = {artistId}";

                foreach (DataRowView row in dvArtistsAlbums)
                {
                    row.Delete();
                }

                drv.Delete();
                ds.Tables["Artists"].AcceptChanges();
                ds.Tables["ArtAlb"].AcceptChanges();

                UpdateAlbumAssignments(dataGridView1.CurrentRow?.Index ?? -1);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            InfoForm infoForm = new InfoForm();
            infoForm.Show();
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataRowView row = dataGridView1.CurrentRow?.DataBoundItem as DataRowView;
            int rowIndex = dataGridView1.CurrentRow.Index;
            int maxRowIndex = dataGridView1.Rows.Count - 2;

            DetailsForm detailsForm = new DetailsForm(row, rowIndex, maxRowIndex);
            detailsForm.Owner = this;

            detailsForm.ShowDialog();
        }

        private void deleteRowToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridView2.CurrentRow == null) return;

            DataRowView drv = dataGridView2.CurrentRow.DataBoundItem as DataRowView;
            if (drv != null)
            {
                string albumId = drv["ID_Album"].ToString();
                dvArtistsAlbums.RowFilter = $"ID_Album = {albumId}";

                foreach (DataRowView row in dvArtistsAlbums)
                {
                    row.Delete();
                }

                drv.Delete();
                ds.Tables["Artists"].AcceptChanges();
                ds.Tables["Albums"].AcceptChanges();

                UpdateAlbumAssignments(dataGridView1.CurrentRow?.Index ?? -1);
            }
        }

        private int GetMaxId(DataTable dt)
        {
            int maxId = dt.Rows.Count;

            return maxId + 1;
        }

        private void dataGridView2_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataRowView drv = dataGridView2.Rows[e.RowIndex].DataBoundItem as DataRowView;

            LoadPhoto(drv);
        }

        private void LoadPhoto(DataRowView drv)
        {
            pictureBox1.Image = Resources.stop_photo;

            string photoFileName = drv["cover"]?.ToString();

            string photoPath = Path.Combine(Application.StartupPath, "Assets", photoFileName);

            if (File.Exists(photoPath)) pictureBox1.ImageLocation = photoPath;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int currentRowIndex = dataGridView2.CurrentRow.Index;

            if (currentRowIndex < dataGridView2.Rows.Count - 1)
            {
                dataGridView2.CurrentCell = dataGridView2.Rows[currentRowIndex + 1].Cells[0];

                DataRowView drv = (DataRowView)dataGridView2.CurrentRow.DataBoundItem;

                LoadPhoto(drv);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int currentRowIndex = dataGridView2.CurrentRow.Index;

            if (currentRowIndex > 0)
            {
                dataGridView2.CurrentCell = dataGridView2.Rows[currentRowIndex - 1].Cells[0];

                DataRowView drv = (DataRowView)dataGridView2.CurrentRow.DataBoundItem;

                LoadPhoto(drv);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            dataGridView2.CurrentCell = dataGridView2.Rows[0].Cells[0];

            DataRowView drv = (DataRowView)dataGridView2.CurrentRow.DataBoundItem;

            LoadPhoto(drv);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int lastRowIndex = dataGridView2.Rows.Count - 1;
            dataGridView2.CurrentCell = dataGridView2.Rows[lastRowIndex].Cells[0];

            DataRowView drv = (DataRowView)dataGridView2.CurrentRow.DataBoundItem;

            LoadPhoto(drv);
        }
    }
}
