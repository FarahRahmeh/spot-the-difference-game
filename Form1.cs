using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace SpotDiffGame
{
    public partial class Form1 : Form
    {
        private PictureBox picLeft, picRight;
        private Label lblFound, lblRemaining, lblLives, lblTimer;
        private ComboBox cmbPlayMode, cmbLevel;
        private Button btnStartGame;
        private Button btnRestart;
        private Button btnQuit;

        private Bitmap bmpLeft, bmpRight;
        private List<Point> differencePoints = new List<Point>();
        private int foundCount = 0;
        private int totalDifferences = 0;
        private int lives = 0;
        private List<Point> foundPoints = new List<Point>();    

        private SoundPlayer correctSound;
        private SoundPlayer wrongSound;
        private SoundPlayer winSound;
        private SoundPlayer failSound;

        private Timer gameTimer;
        private int timeLeft; //in seconds

        public Form1()
        {
            InitializeComponent();

            Text = "Spot The Difference";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.WhiteSmoke;

            correctSound = new SoundPlayer("Sounds\\correct.wav");
            wrongSound = new SoundPlayer("Sounds\\wrong.wav");
            winSound = new SoundPlayer("Sounds\\win.wav");
            failSound = new SoundPlayer("Sounds\\fail.wav");

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 75)); // images
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // level
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // play mode
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // stats
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // start
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // restart
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // quit
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // show solved
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5)); // spacer

            // Images
            picLeft = new PictureBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.LightGray };
            picRight = new PictureBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.LightGray };
            picRight.MouseClick += PicRight_MouseClick;
            mainLayout.Controls.Add(picLeft, 0, 0);
            mainLayout.Controls.Add(picRight, 1, 0);

            // Level selector
            cmbLevel = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLevel.Items.AddRange(Directory.GetDirectories("images").Select(Path.GetFileName).ToArray());
            if (cmbLevel.Items.Count > 0) cmbLevel.SelectedIndex = 0;
            mainLayout.Controls.Add(new Label { Text = "Level:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 1);
            mainLayout.Controls.Add(cmbLevel, 1, 1);

            // Play mode
            cmbPlayMode = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPlayMode.Items.AddRange(new string[] { "Timer Mode", "Attempt Limit Mode" });
            cmbPlayMode.SelectedIndex = 0;
            mainLayout.Controls.Add(new Label { Text = "Mode:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 2);
            mainLayout.Controls.Add(cmbPlayMode, 1, 2);

            // Stats
            var statsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            lblFound = new Label { Text = "Found: 0", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            lblRemaining = new Label { Text = "Remaining: 0", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            lblLives = new Label { Text = "Lives: 0", AutoSize = true, ForeColor = Color.DarkRed, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblTimer = new Label { Text = "Time: 0s", AutoSize = true, ForeColor = Color.DarkBlue, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            statsPanel.Controls.Add(lblFound, 0, 0);
            statsPanel.Controls.Add(lblRemaining, 1, 0);
            statsPanel.Controls.Add(lblLives, 2, 0);
            statsPanel.Controls.Add(lblTimer, 3, 0);
            mainLayout.Controls.Add(statsPanel, 0, 3);
            mainLayout.SetColumnSpan(statsPanel, 2);

            // Start button
            btnStartGame = new Button
            {
                Text = "Start Game",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnStartGame.Click += BtnStartGame_Click;
            mainLayout.Controls.Add(btnStartGame, 0, 4);
            mainLayout.SetColumnSpan(btnStartGame, 2);

            // Restart button
            btnRestart = new Button
            {
                Text = "Restart",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Enabled = false,
                BackColor = Color.LightSkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRestart.Click += BtnRestart_Click;
            mainLayout.Controls.Add(btnRestart, 0, 5);
            mainLayout.SetColumnSpan(btnRestart, 2);

            //Show solved
            Button btnShowSolvedLevels = new Button
            {
                Text = "Show Solved Levels",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.HotPink,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnShowSolvedLevels.Click += BtnShowSolvedLevels_Click;
            mainLayout.Controls.Add(btnShowSolvedLevels, 0, 6);
            mainLayout.SetColumnSpan(btnShowSolvedLevels, 2);


            // Quit button
            btnQuit = new Button
            {
                Text = "Quit",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Visible = false,
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnQuit.Click += BtnQuit_Click;
            mainLayout.Controls.Add(btnQuit, 0, 7);
            mainLayout.SetColumnSpan(btnQuit, 2);

            //Add everything to the layout
            Controls.Add(mainLayout);

            // Timer
            gameTimer = new Timer();
            gameTimer.Interval = 1000;
            gameTimer.Tick += GameTimer_Tick;
        }

        private void BtnStartGame_Click(object sender, EventArgs e)
        {
            string level = cmbLevel.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(level)) return;

            string leftPath = Path.Combine("images", level, "left.png");
            string rightPath = Path.Combine("images", level, "right.png");

            if (!File.Exists(leftPath) || !File.Exists(rightPath))
            {
                MessageBox.Show("Image files for selected level are missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRestart.Enabled = false;
            cmbPlayMode.Enabled = false; // لمنع التبديل
            cmbPlayMode.Visible = false; // إخفاء القائمة بعد بدء اللعبة

            bmpLeft = new Bitmap(leftPath);
            bmpRight = new Bitmap(rightPath);
            picLeft.Image = bmpLeft;
            picRight.Image = bmpRight;

            foundCount = 0;
            differencePoints.Clear();
            totalDifferences = 0;
            lives = 0;

            int width = Math.Min(bmpLeft.Width, bmpRight.Width);
            int height = Math.Min(bmpLeft.Height, bmpRight.Height);

            var rectangles = detectDifferences_ByColor(leftPath, rightPath);
            differencePoints = rectangles.Select(r => new Point(r.X + r.Width / 2, r.Y + r.Height / 2)).ToList();
            totalDifferences = differencePoints.Count;


            string mode = cmbPlayMode.SelectedItem?.ToString() ?? "Timer Mode";
            if (mode == "Attempt Limit Mode")
            {
                lives = 3;
                lblLives.Text = $"Lives: {lives}";
                lblLives.Visible = true;
                lblTimer.Text = "";
                gameTimer.Stop();
                btnQuit.Visible = true;
            }
            else if (mode == "Timer Mode")
            {
                timeLeft = 60;
                lblTimer.Text = $"Time Left: {timeLeft}s";
                lblTimer.Visible = true;
                lblLives.Visible = false;
                lives = int.MaxValue;
                gameTimer.Start();
                btnQuit.Visible = true;
            }


            lblRemaining.Text = $"Differences remaining: {totalDifferences}";
            lblFound.Text = $"Differences found: 0";

        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void BtnRestart_Click(object sender, EventArgs e)
        {
            btnStartGame.PerformClick();
        }

        private void BtnShowSolvedLevels_Click(object sender, EventArgs e)
        {
            Form solvedForm = new Form
            {
                Text = "Solved Levels",
                Size = new Size(1000, 700), 
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };

            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true
            };

            string solvedDir = Path.Combine(Application.StartupPath, "solved_levels");
            if (!Directory.Exists(solvedDir))
            {
                MessageBox.Show("No solved levels found yet.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var files = Directory.GetFiles(solvedDir, "*.png");
            foreach (var file in files)
            {
                PictureBox pb = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 450, 
                    Height = 375, 
                    Margin = new Padding(10)
                };

                // Load image into memory to avoid file lock
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (var img = Image.FromStream(fs))
                    {
                        pb.Image = new Bitmap(img); // Make a copy
                    }
                }

                ContextMenuStrip contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Delete").Click += (s, ea) =>
                {
                    if (MessageBox.Show("Delete this solved image?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        try
                        {
                            if (pb.Image != null)
                            {
                                pb.Image.Dispose(); // Dispose image before deleting
                                pb.Image = null;
                            }

                            File.Delete(file);
                            panel.Controls.Remove(pb);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed to delete image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };

                pb.ContextMenuStrip = contextMenu;
                panel.Controls.Add(pb);
            }

            solvedForm.Controls.Add(panel);
            solvedForm.ShowDialog();
        }


        private void PicRight_MouseClick(object sender, MouseEventArgs e)
        {
            if (differencePoints == null || differencePoints.Count == 0) return;

            Point clicked = e.Location;
            Point imagePoint = TranslateToImageCoordinates(picRight, bmpRight, clicked);

            bool found = false;

            for (int i = 0; i < differencePoints.Count; i++)
            {
                var diffPoint = differencePoints[i];
                if (Distance(diffPoint, imagePoint) < 25)
                {
                    foundCount++;
                    foundPoints.Add(diffPoint); // Store found point

                    differencePoints.RemoveAt(i);

                    lblFound.Text = $"Differences found: {foundCount}";
                    lblRemaining.Text = $"Differences remaining: {differencePoints.Count}";

                    DrawCircle(picRight, diffPoint);
                    correctSound?.Play();
                    found = true;

                    if (differencePoints.Count == 0)
                    {
                        gameTimer.Stop();
                        OnGameCompleted();
                    }
                    break;
                }
            }

            if (!found)
            {
                string mode = cmbPlayMode.SelectedItem?.ToString() ?? "Timer Mode";

                if (mode == "Attempt Limit Mode")
                {
                    lives--;
                    lblLives.Text = $"Lives: {lives}";

                    DrawRedCircleAtClick(picRight, e.Location);
                    wrongSound?.Play();

                    if (lives <= 0)
                    {
                        gameTimer.Stop();
                        OnGameFailed();
                    }
                }
                else if (mode == "Timer Mode")
                {
                    // In timer mode, no lives lost on wrong attempts
                    wrongSound?.Play();
                    DrawRedCircleAtClick(picRight, e.Location);
                }
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            if (timeLeft <= 0)
            {
                gameTimer.Stop();
                OnGameFailed();
            }
            else
            {
                lblTimer.Text = $"Time Left: {timeLeft}s";
            }
        }

        private void DrawRedCircleAtClick(PictureBox pictureBox, Point controlClickPoint)
        {
            using (Graphics g = pictureBox.CreateGraphics())
            {
                int radius = 30;
                g.DrawEllipse(new Pen(Color.Red, 2),
                    controlClickPoint.X - radius / 2,
                    controlClickPoint.Y - radius / 2,
                    radius,
                    radius);
            }
        }

        private void BtnQuit_Click(object sender, EventArgs e)
        {
            gameTimer.Stop();
            MessageBox.Show("Thanks for playing! Come back soon!.", "Quit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnRestart.Enabled = true;
            cmbPlayMode.Enabled = true;
            cmbPlayMode.Visible = true;
            btnQuit.Visible = false;

        }


        private void OnGameCompleted()
        {
            winSound?.Play();
            MessageBox.Show("🎉🎉 You have sharp eyes! All differences found!", "Victory", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SaveSolvedLevelImage();
            btnRestart.Enabled = true;
            cmbPlayMode.Enabled = true;
            cmbPlayMode.Visible = true;
            btnQuit.Visible = false;

        }

        private void OnGameFailed()
        {
            failSound?.Play();
            MessageBox.Show("🙁 Almost there! Try again!.", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            btnRestart.Enabled = true;
            cmbPlayMode.Enabled = true;
            cmbPlayMode.Visible = true;
            btnQuit.Visible = false;
        }

        private void SaveSolvedLevelImage()
        {
            if (bmpRight == null || foundPoints.Count == 0) return;

            string solvedDir = Path.Combine("solved_levels");
            Directory.CreateDirectory(solvedDir);

            string levelName = cmbLevel.SelectedItem?.ToString() ?? "unknown_level";
            string fileName = $"{levelName}_solved.png";
            string savePath = Path.Combine(solvedDir, fileName);

            // Check if the solved image already exists for this level
            if (File.Exists(savePath))
            {
                MessageBox.Show("This level has already been solved and saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return; // Exit without saving again
            }

            Bitmap solvedImage = new Bitmap(bmpRight);
            using (Graphics g = Graphics.FromImage(solvedImage))
            {
                int radius = 30;
                using (Pen greenPen = new Pen(Color.LimeGreen, 3))
                {
                    foreach (Point p in foundPoints)
                    {
                        g.DrawEllipse(greenPen, p.X - radius / 2, p.Y - radius / 2, radius, radius);
                    }
                }
            }

            solvedImage.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
        }


 

        private int Distance(Point p1, Point p2)
        {
            return (int)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private Point TranslateToImageCoordinates(PictureBox pictureBox, Bitmap image, Point controlPoint)
        {
            if (pictureBox.Image == null) return Point.Empty;

            float imgAspect = (float)image.Width / image.Height;
            float boxAspect = (float)pictureBox.Width / pictureBox.Height;

            int imgX = 0, imgY = 0;
            if (imgAspect > boxAspect)
            {
                float scale = (float)pictureBox.Width / image.Width;
                imgX = (int)(controlPoint.X / scale);
                float yOffset = (pictureBox.Height - image.Height * scale) / 2;
                imgY = (int)((controlPoint.Y - yOffset) / scale);
            }
            else
            {
                float scale = (float)pictureBox.Height / image.Height;
                float xOffset = (pictureBox.Width - image.Width * scale) / 2;
                imgX = (int)((controlPoint.X - xOffset) / scale);
                imgY = (int)(controlPoint.Y / scale);
            }

            return new Point(Clamp(imgX, 0, image.Width - 1), Clamp(imgY, 0, image.Height - 1));
        }

        private int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private List<Rectangle> detectDifferences_ByColor(string path1, string path2)
        {
            Mat img1 = CvInvoke.Imread(path1, ImreadModes.Color);
            Mat img2 = CvInvoke.Imread(path2, ImreadModes.Color);

            if (img1.Size != img2.Size)
                CvInvoke.Resize(img2, img2, img1.Size);

            VectorOfMat channels1 = new VectorOfMat();
            VectorOfMat channels2 = new VectorOfMat();
            CvInvoke.Split(img1, channels1);
            CvInvoke.Split(img2, channels2);

            Mat diff = new Mat();

            for (int i = 0; i < 3; i++)
            {
                Mat c1 = channels1[i];
                Mat c2 = channels2[i];
                Mat diffChannel = new Mat();
                CvInvoke.AbsDiff(c1, c2, diffChannel);

                if (i == 0)
                    diff = diffChannel;
                else
                    CvInvoke.Add(diff, diffChannel, diff);
            }

            // Thresholding
            CvInvoke.Threshold(diff, diff, 60, 255, ThresholdType.Binary);

            // Morphological operations
            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
            CvInvoke.MorphologyEx(diff, diff, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());

            // Find contours
            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(diff, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                List<Rectangle> boundingRects = new List<Rectangle>();
                for (int i = 0; i < contours.Size; i++)
                {
                    Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                    if (rect.Width > 10 && rect.Height > 10) // ignore noise
                        boundingRects.Add(rect);
                }
                return boundingRects;
            }
        }

        private void DrawCircle(PictureBox pictureBox, Point imagePoint)
        {
            if (pictureBox.Image == null) return;

            float imgAspect = (float)pictureBox.Image.Width / pictureBox.Image.Height;
            float boxAspect = (float)pictureBox.Width / pictureBox.Height;

            float scale;
            float offsetX = 0, offsetY = 0;

            if (imgAspect > boxAspect)
            {
                // Image is wider relative to box
                scale = (float)pictureBox.Width / pictureBox.Image.Width;
                offsetY = (pictureBox.Height - pictureBox.Image.Height * scale) / 2;
            }
            else
            {
                // Image is taller relative to box
                scale = (float)pictureBox.Height / pictureBox.Image.Height;
                offsetX = (pictureBox.Width - pictureBox.Image.Width * scale) / 2;
            }

            int x = (int)(imagePoint.X * scale + offsetX);
            int y = (int)(imagePoint.Y * scale + offsetY);

            int radius = 30;
            using (Graphics g = pictureBox.CreateGraphics())
            {
                using (Pen pen = new Pen(Color.LimeGreen, 3))
                {
                    g.DrawEllipse(pen, x - radius / 2, y - radius / 2, radius, radius);
                }
            }
        }

    }
}
