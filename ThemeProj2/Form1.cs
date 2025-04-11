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
        }

        private void SetupDataGridViews()
        {
            dataGridView1.Columns["ID_Author"].Visible = false;

            //dataGridView1.Columns["photo"].Visible = false;

            if (dataGridView1.Columns.Contains("Name_Author")) dataGridView1.Columns["Name_Author"].HeaderText = "Name Author";

            if (dataGridView1.Columns.Contains("Style_Music")) dataGridView1.Columns["Style_Music"].HeaderText = "Style Music";

            if (dataGridView1.Columns.Contains("country")) dataGridView1.Columns["country"].HeaderText = "country";

            if (dataGridView1.Columns.Contains("start_year")) dataGridView1.Columns["start_year"].HeaderText = "start year";

            dataGridView2.Columns["ID_Album"].Visible = false;

            if (dataGridView2.Columns.Contains("Name_Album")) dataGridView2.Columns["Name_Album"].HeaderText = "Name Album";

            if (dataGridView2.Columns.Contains("Year_Album")) dataGridView2.Columns["Year_Album"].HeaderText = "Year Album";

            if (dataGridView2.Columns.Contains("tracks_number")) dataGridView2.Columns["tracks_number"].HeaderText = "tracks number";

            if (dataGridView2.Columns.Contains("duration")) dataGridView2.Columns["duration"].HeaderText = "duration";

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
            UpdateGenreAssignments(rowIndex);
            //LoadGameImage(rowIndex);
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Check if the row index is valid and the column name is "assigned"
            if (e.RowIndex < 0 || dataGridView2.Columns[e.ColumnIndex].Name != "assigned")
                return;

            // Get the selected game row and the genre row
            DataGridViewRow currentGameRow = dataGridView1.CurrentRow;
            DataRowView genreRow = dataGridView2.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (currentGameRow == null || genreRow == null)
                return;

            // Get the selected game id and the genre id
            int? selectedGameId = (int?)currentGameRow.Cells["ID_Author"].Value;
            int? genreId = (int?)genreRow["ID_Album"];
            if (selectedGameId == null || genreId == null)
                return;

            // Revert flag because envent is triggered before the value is changed
            bool isChecked = !Convert.ToBoolean(dataGridView2.Rows[e.RowIndex].Cells["assigned"].Value);

            // Filter the GameGenres table to check if the genre is already assigned to the game
            dvArtistsAlbums.RowFilter = $"ID_Author = '{selectedGameId}' AND ID_Album = '{genreId}'";

            if (isChecked && dvArtistsAlbums.Count == 0)
            {
                DataRowView newRow = dvArtistsAlbums.AddNew();
                newRow["ID_Author"] = selectedGameId;
                newRow["ID_Album"] = genreId;
                newRow.EndEdit();
            }
            else if (!isChecked && dvArtistsAlbums.Count > 0)
            {
                dvArtistsAlbums.Delete(0);
            }

            dvArtistsAlbums.RowFilter = "";
            UpdateGenreAssignments(dataGridView1.CurrentRow.Index);
        }

        private void UpdateGenreAssignments(int gameRowIndex)
        {
            // Clear the assigned column
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                row.Cells["assigned"].Value = false;
            }

            // Check if the row index is valid
            if (gameRowIndex < 0) return;

            // Get selected game id
            int? selectedGameId = (int?)dataGridView1.Rows[gameRowIndex].Cells["ID_Author"].Value;
            if (selectedGameId == null) return;

            // Filter the GameGenres table to get the genres assigned to the selected game
            dvArtistsAlbums.RowFilter = $"ID_Author = '{selectedGameId}'";
            List<string> genreIds = new List<string>();
            foreach (DataRowView row in dvArtistsAlbums)
                genreIds.Add(row["ID_Album"].ToString());

            // Check the assigned genres
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                DataRowView drv = row.DataBoundItem as DataRowView;
                if (drv != null)
                    row.Cells["assigned"].Value = genreIds.Contains(drv["ID_Album"].ToString());
            }

            dvArtistsAlbums.RowFilter = "";
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

        // Game context menu

        private void deleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            DataRowView drv = dataGridView1.CurrentRow.DataBoundItem as DataRowView;
            if (drv != null)
            {
                int? gameId = (int?)drv["ID_Author"];
                dvArtistsAlbums.RowFilter = $"ID_Author = {gameId}";

                foreach (DataRowView row in dvArtistsAlbums)
                {
                    row.Delete();
                }

                drv.Delete();
                ds.Tables["Artists"].AcceptChanges();
                ds.Tables["ArtAlb"].AcceptChanges();

                UpdateGenreAssignments(dataGridView1.CurrentRow?.Index ?? -1);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InfoForm infoForm = new InfoForm();
            infoForm.Show();
        }

        // Genre context menu
        private void deleteRowToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridView2.CurrentRow == null) return;

            DataRowView drv = dataGridView2.CurrentRow.DataBoundItem as DataRowView;
            if (drv != null)
            {
                string genreId = drv["ID_Album"].ToString();
                dvArtistsAlbums.RowFilter = $"ID_Album = {genreId}";

                foreach (DataRowView row in dvArtistsAlbums)
                {
                    row.Delete();
                }

                drv.Delete();
                ds.Tables["Artists"].AcceptChanges();
                ds.Tables["Albums"].AcceptChanges();

                UpdateGenreAssignments(dataGridView1.CurrentRow?.Index ?? -1);
            }
        }

        //private void LoadGameImage(int gameRowIndex)
        //{
        //    pictureBox1.Image = Resources.photo_camera_32dp_B7B7B7_FILL0_wght400_GRAD0_opsz40;

        //    if (gameRowIndex < 0) return;

        //    string photoFileName = dataGridView1.Rows[gameRowIndex].Cells["photo"].Value?.ToString();
        //    if (string.IsNullOrEmpty(photoFileName)) return;

        //    string photoPath = Path.Combine(Application.StartupPath, "Assets", photoFileName);

        //    if (File.Exists(photoPath))
        //        pictureBox1.ImageLocation = photoPath;
        //}

        private int GetMaxId(DataTable dt)
        {
            int maxId = dt.Rows.Count;

            return maxId + 1;
        }
    }
}
