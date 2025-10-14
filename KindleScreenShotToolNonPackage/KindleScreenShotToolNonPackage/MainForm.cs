using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using static KindleScreenShotToolNonPackage.TaskbarProgress;

namespace KindleScreenShotToolNonPackage
{
    public partial class MainForm : Form
    {
        #region �萔

        /// <summary>
        /// �S�p���l�z��
        /// </summary>
        private readonly string[] FullWidthDigitsAr =
        {
            "�O",
            "�P",
            "�Q",
            "�R",
            "�S",
            "�T",
            "�U",
            "�V",
            "�W",
            "�X"
        };

        /// <summary>
        /// ���ꃊ�X�g
        /// </summary>
        private readonly List<string> LangList =
        [
            "jpn",
            "jpn_vert",
            "eng",
            "jpn+eng",
            "jpn_vert+eng"
        ];

        #endregion

        #region �v���p�e�B

        /// <summary>
        /// �X�N���[���V���b�g���W�b�N
        /// </summary>
        private ScreenShotLogic ScreenShotLogic { get; } = new();

        /// <summary>
        /// �^�X�N�o�[�v���O���X
        /// </summary>
        private TaskbarProgress TaskbarProgress { get; } = new();

        #endregion

        #region �R���X�g���N�^

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            PressKeyComboBox.SelectedIndex = 0;
            LangComboBox.SelectedIndex = 0;
            ConnectionDirectionComboBox.SelectedIndex = 0;
            PageDirectionComboBox.SelectedIndex = 0;
            ImageDisplayComboBox.SelectedIndex = 0;

            // Microsoft Print to PDF�̗L���𔻒肷��B
            if (!PrinterSettings.InstalledPrinters.Cast<string>().Any(printer => printer == "Microsoft Print to PDF"))
            {
                // ���݂��Ȃ��ꍇ
                PrinterWarningLabel.Visible = true;
                PDFExeButton.Enabled = false;
            }
        }

        #endregion

        #region �X�N���[���V���b�g�C�x���g

        /// <summary>
        /// ���s�{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private async void ScreenShotExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                ScreenShotExeButton.Enabled = false;

                if (string.IsNullOrEmpty(KindleTitleTextBox.Text))
                {
                    ShowMessage("�G���[", "Kindle�̃^�C�g���������͂ł��B", MessageBoxIcon.Error);
                    return;
                }

                if (!ScreenShotLogic.IsKindleRunning(KindleTitleTextBox.Text))
                {
                    ShowMessage("�G���[", "���͂��ꂽ�^�C�g����Kindle���N�����Ă��܂���B", MessageBoxIcon.Error);
                    return;
                }

                if (Equals(DialogResult.OK, ScreenShotSaveFolderBrowserDialog.ShowDialog(this)))
                {
                    // Kindle�^�C�g����ێ�����B
                    string kindleTitle = KindleTitleTextBox.Text;

                    // �B�e����ێ�����B
                    int captureStartX = (int)CaptureStartXNumericUpDown.Value;
                    int captureStartY = (int)CaptureStartYNumericUpDown.Value;
                    int captureWidth = (int)CaptureWidthNumericUpDown.Value;
                    int captureHeight = (int)CaptureHeightNumericUpDown.Value;

                    // �B�e������ێ�����B
                    int captureCount = (int)CaptureCountNumericUpDown.Value;
                    ulong captureCountULong = (ulong)captureCount;

                    // �t�@�C�����A�Ԍ�����ێ�����B
                    int maxLength = (int)FileNameSerialNumberNumericUpDown.Value;

                    // �B�e�ҋ@���Ԃ�ێ�����B
                    int waitingTime = (int)WaitingTimeNumericUpDown.Value;

                    // ��������L�[�R�[�h��ێ�����B
                    int keyCode = ScreenShotLogic.VK_LEFT;
                    if (PressKeyComboBox.SelectedIndex == 1)
                    {
                        keyCode = ScreenShotLogic.VK_RIGHT;
                    }

                    // Kindle�E�B���h�E���A�N�e�B�u�ɂ���B
                    ScreenShotLogic.ActivateKindleWindow(kindleTitle);

                    // �ۑ��_�C�A���O��������O�ɁA�X�N���[���V���b�g�̎B�e���n�܂邽�߁A�ҋ@������B
                    Thread.Sleep(100);

                    await Task.Run(async () =>
                    {
                        // �^�X�N�o�[�̃v���O���X��Ԃ�ݒ肷��B
                        Invoke(() => TaskbarProgress.SetProgressState(Handle, TaskbarProgressState.Normal));

                        for (int index = 0; index < captureCount; index++)
                        {
                            // ��������ƁA���܂��B�e�o���Ȃ����߁A�ҋ@������B
                            await Task.Delay(waitingTime);

                            // �X�N���[���V���b�g���B�e����B
                            ScreenShotLogic.SaveScreenShot(
                                captureStartX,
                                captureStartY,
                                captureWidth,
                                captureHeight,
                                GetSaveFullPath(ScreenShotSaveFolderBrowserDialog.SelectedPath, index, maxLength));

                            // �L�[����������B
                            ScreenShotLogic.KeyDown(kindleTitle, keyCode);

                            // �v���O���X�l��ݒ肷��B
                            Invoke(() => TaskbarProgress.SetProgressValue(Handle, (ulong)(index + 1), captureCountULong));
                        }
                    });

                    // �v���O���X����������B
                    TaskbarProgress.SetProgressState(Handle, TaskbarProgressState.NoProgress);

                    // Kindle�E�B���h�E���A�N�e�B�u�ɂȂ��Ă���̂ŁA���g���A�N�e�B�u�ɂ���B
                    Activate();

                    ShowMessage("�X�N���[���V���b�g�B�e����", "�X�N���[���V���b�g�̎B�e���������܂����B", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("�G���[", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                ScreenShotExeButton.Enabled = true;
            }
        }

        /// <summary>
        /// �e�X�g�B�e�{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void TestScreenShotButton_Click(object sender, EventArgs e)
        {
            if (Equals(DialogResult.OK, TestScreenShotSaveFileDialog.ShowDialog(this)))
            {
                // �ۑ��_�C�A���O��������O�ɁA�X�N���[���V���b�g�̎B�e���n�܂邽�߁A�ҋ@������B
                Thread.Sleep(100);

                // �X�N���[���V���b�g���B�e����B
                ScreenShotLogic.SaveScreenShot(
                    (int)CaptureStartXNumericUpDown.Value,
                    (int)CaptureStartYNumericUpDown.Value,
                    (int)CaptureWidthNumericUpDown.Value,
                    (int)CaptureHeightNumericUpDown.Value,
                    TestScreenShotSaveFileDialog.FileName);

                ShowMessage("�e�X�g�X�N���[���V���b�g�B�e����", "�e�X�g�X�N���[���V���b�g�̎B�e���������܂����B", MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// �^�C�g���擾�{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void GetTitleButton_Click(object sender, EventArgs e)
        {
            KindleTitleTextBox.Text = ScreenShotLogic.GetKindleTitle();
        }

        /// <summary>
        /// �t�@�C�����A�Ԍ����j���[�����b�N�A�b�v�_�E���l�ύX����
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void FileNameSerialNumberNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            FileNameSampleLabel.Text = $@"�T���v���t�@�C�����F{new string('0', (int)FileNameSerialNumberNumericUpDown.Value)}.png";
        }

        #endregion

        #region OCR�C�x���g

        /// <summary>
        /// ���s�{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private async void OCRExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                OCRExeButton.Enabled = false;

                if (string.IsNullOrEmpty(OCRImageFolderPathTextBox.Text))
                {
                    ShowMessage("�G���[", "�摜�t�H���_�p�X�������͂ł��B", MessageBoxIcon.Error);
                    return;
                }

                // �t�H���_�p�X�𐳋K�����A�ێ�����B
                string imageFolderPath = NormalizePath(OCRImageFolderPathTextBox.Text);

                if (!Directory.Exists(imageFolderPath))
                {
                    ShowMessage("�G���[", "�摜�t�H���_�p�X�����݂��܂���B", MessageBoxIcon.Error);
                    return;
                }

                if (SingleRadioButton.Checked)
                {
                    // �t�H���_�I���_�C�A���O��\������B
                    if (!Equals(DialogResult.OK, OCROutputFolderPathFolderBrowserDialog.ShowDialog(this)))
                    {
                        return;
                    }
                }
                else
                {
                    // �t�@�C���ۑ��_�C�A���O��\������B
                    if (!Equals(DialogResult.OK, OCRTextSaveFileDialog.ShowDialog(this)))
                    {
                        return;
                    }
                }

                // �o�͌`����ێ�����B
                bool outputFormatFlg = SingleRadioButton.Checked;

                // �����ێ�����B
                string lngStr = LangList[LangComboBox.SelectedIndex];

                // png�t�@�C���̃t���p�X�̈ꗗ�����X�g�ɂ���B
                List<string> pngPathList = [.. Directory.GetFiles(imageFolderPath, "*.png").OrderBy(path => Path.GetFileName(path))];

                string maxCount = ConvertNumberWide(pngPathList.Count);

                // �����ݒ肷��B
                string lang = string.Equals("���{��", LangComboBox.Text) ? "ja-JP" : "en-US";

                string outputText = string.Empty;
                int count = 0;

                await Task.Run(async () =>
                {
                    foreach (var pngPath in pngPathList)
                    {
                        // OCRengine������������B
                        // �iWindows OCR�́A���{��E�p�ꂵ���Ή����Ă��Ȃ��B�j
                        var ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(lang));

                        // �摜��Bitmap�Ƃ��ēǂݍ��ށB
                        using var bitmap = (Bitmap)Image.FromFile(pngPath);
                        var softwareBitmap = await ConvertToSoftwareBitmap(bitmap);

                        // �摜��ǂݍ��݁A�e�L�X�g�𒊏o����B
                        var result = await ocrEngine.RecognizeAsync(softwareBitmap);

                        if (outputFormatFlg)
                        {
                            File.WriteAllText(
                                string.Concat(OCROutputFolderPathFolderBrowserDialog.SelectedPath, @"\", Path.GetFileNameWithoutExtension(pngPath), ".txt"),
                                result.Text,
                                Encoding.UTF8);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(outputText))
                            {
                                outputText = result.Text;
                            }
                            else
                            {
                                outputText = string.Concat(outputText, Environment.NewLine, result.Text);
                            }
                        }

                        count++;

                        // �^�C�g���ɏ���������ݒ肷��B
                        Invoke(() => Text = $"���������F{ConvertNumberWide(count)}�^{maxCount}�t�@�C������");
                    }
                    ;

                    if (!outputFormatFlg)
                    {
                        File.WriteAllText(
                            OCRTextSaveFileDialog.FileName,
                            outputText,
                            Encoding.UTF8);
                    }
                });

                ShowMessage("OCR��������", "OCR�������������܂����B", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage("�G���[", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                Text = "Kindle�X�N���[���V���b�g�c�[��";
                OCRExeButton.Enabled = true;
            }
        }

        /// <summary>
        /// �t�H���_�I���{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void OCRImageFolderPathSelectButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog_Show(OCRImageFolderPathFolderBrowserDialog, OCRImageFolderPathTextBox);
        }

        #endregion

        #region PDF�C�x���g

        /// <summary>
        /// ���s�{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void PDFExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PDFImageFolderPathTextBox.Text))
                {
                    ShowMessage("�G���[", "�摜�t�H���_�p�X�������͂ł��B", MessageBoxIcon.Error);
                    return;
                }

                // PDF�摜�t�H���_�p�X���擾����B
                string pdfImageFolderPath = NormalizePath(PDFImageFolderPathTextBox.Text);

                if (!Directory.Exists(pdfImageFolderPath))
                {
                    ShowMessage("�G���[", "�摜�t�H���_�p�X�����݂��܂���B", MessageBoxIcon.Error);
                    return;
                }

                if (Equals(DialogResult.OK, PDFSaveFileDialog.ShowDialog(this)))
                {
                    // �g���q���A�y.png�z�i�啶���E��������킸�j�̃t���p�X���擾���A���X�g�ɂ���B
                    List<string> pngPathList = [..Directory.EnumerateFiles(pdfImageFolderPath, "*.png", SearchOption.TopDirectoryOnly)
                        .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(path => Path.GetFileName(path))
                        .ToList()];

                    // �y�[�W�̌�����ێ�����B
                    bool landscapeFlg = string.Equals("������", PageDirectionComboBox.Text);

                    // �摜�\����ێ�����B
                    // �itrue�F�S��ʁEfalse�F�����\���j
                    bool imageDisplayFlg = string.Equals("�S�\��", ImageDisplayComboBox.Text);

                    PrintDocument doc = new()
                    {
                        PrinterSettings = new PrinterSettings()
                        {
                            PrinterName = "Microsoft Print to PDF",
                            MaximumPage = pngPathList.Count,
                            ToPage = pngPathList.Count,
                            DefaultPageSettings = { Landscape = landscapeFlg, },
                            PrintToFile = true, // �t�@�C���ɏo�͂���B
                            PrintFileName = Path.Combine(PDFSaveFileDialog.FileName),
                        }
                    };

                    // �]���𖳂��ɂ���B
                    doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    int currentPageIndex = 0;

                    // �y�[�W�ݒ���s���B
                    doc.PrintPage += (s, e) =>
                    {
                        using (var image = Image.FromFile(pngPathList[currentPageIndex]))
                        {
                            Rectangle pageRect;
                            int x;
                            int y;

                            if (imageDisplayFlg)
                            {
                                // �S��ʂ̏ꍇ
                                pageRect = e.PageBounds;

                                // �����E�c���̔{�����Z�o����B
                                float ratioX = (float)pageRect.Width / image.Width;
                                float ratioY = (float)pageRect.Height / image.Height;

                                // �摜�̏c�����ۂ��߁A��菬�����{����ێ�����B
                                float ratio = Math.Min(ratioX, ratioY);

                                // �`�悷��摜�T�C�Y��{���ɉ����ĎZ�o����B
                                int drawWidth = (int)(image.Width * ratio);
                                int drawHeight = (int)(image.Height * ratio);

                                // �y�[�W�����ɉ摜��z�u���邽�߂̍��W���Z�o����B
                                x = pageRect.X + (pageRect.Width - drawWidth) / 2;
                                y = pageRect.Y + (pageRect.Height - drawHeight) / 2;

                                // �Z�o�����ʒu�ƃT�C�Y�ŉ摜��`�悷��B
                                e.Graphics?.DrawImage(image, x, y, drawWidth, drawHeight);
                            }
                            else
                            {
                                // �����\���̏ꍇ
                                pageRect = e.MarginBounds;

                                // �摜�T�C�Y��ێ�����B
                                int imgWidth = image.Width;
                                int imgHeight = image.Height;

                                // �����ɔz�u���邽�߁A�ʒu���Z�o����B
                                x = pageRect.X + (pageRect.Width - imgWidth) / 2;
                                y = pageRect.Y + (pageRect.Height - imgHeight) / 2;

                                // �����T�C�Y�ŕ`�悷��B
                                e.Graphics?.DrawImage(image, x, y, imgWidth, imgHeight);
                            }
                        }

                        currentPageIndex++;
                        e.HasMorePages = currentPageIndex < pngPathList.Count;
                    };


                    // PDF�t�@�C���Ƃ��Ĉ���i�ۑ��j����B
                    doc.Print();

                    ShowMessage("PDF�쐬����", "PDF�̍쐬���������܂����B", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("�G���[", ex.Message, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �t�H���_�I���{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void PDFImageFolderPathSelectButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog_Show(PDFImageFolderPathFolderBrowserDialog, PDFImageFolderPathTextBox);
        }

        #endregion

        #region �摜�A���C�x���g

        /// <summary>
        /// ���s�{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void ImageConcatenationExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(ImageConcatenationImageFolderPathTextBox.Text))
                {
                    ShowMessage("�G���[", "�摜�t�H���_�p�X�������͂ł��B", MessageBoxIcon.Error);
                    return;
                }

                // �摜�t�H���_�p�X���擾����B
                string imageFolderPath = NormalizePath(ImageConcatenationImageFolderPathTextBox.Text);

                if (!Directory.Exists(imageFolderPath))
                {
                    ShowMessage("�G���[", "�摜�t�H���_�p�X�����݂��܂���B", MessageBoxIcon.Error);
                    return;
                }

                if (Equals(DialogResult.OK, ConnectionImageSaveFolderBrowserDialog.ShowDialog(this)))
                {

                    // �A���������擾����B
                    int connectionDirection = ConnectionDirectionComboBox.SelectedIndex;
                    bool verticalFlg = connectionDirection == 0 || connectionDirection == 1;

                    // �A�������擾����B
                    int connectionCount = (int)ConnectionCountNumericUpDown.Value;

                    // �t�@�C�����A�Ԍ�����ێ�����B
                    int maxLength = (int)ImageConcatenationFileNameSerialNumberNumericUpDown.Value;

                    // �g���q���A�y.png�z�i�啶���E��������킸�j�̑S�t�@�C�����擾���A���X�g�ɂ���B
                    var pngFileList = Directory.EnumerateFiles(imageFolderPath, "*.png", SearchOption.TopDirectoryOnly)
                        .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(Path.GetFileName)
                        .ToList();

                    string maxCount = ConvertNumberWide(pngFileList.Count);

                    // ���X�g�𕪊�����B
                    string[][] splitArray = pngFileList.Chunk(connectionCount).ToArray();

                    int count = 0;

                    for (int index = 0; index < splitArray.Length; index++)
                    {
                        var pngFilePathAr = splitArray[index];

                        // �A�������𔻒肷��B
                        if (Equals(1, connectionDirection) || Equals(3, connectionDirection))
                        {
                            pngFilePathAr = [.. pngFilePathAr.OrderByDescending(pngFilePath => pngFilePath)];
                        }

                        // �摜��A������B
                        MergeImage(
                            pngFilePathAr,
                            GetSaveFullPath(ConnectionImageSaveFolderBrowserDialog.SelectedPath, index, maxLength),
                            verticalFlg);

                        count += pngFilePathAr.Length;

                        // �^�C�g���ɏ���������ݒ肷��B
                        Invoke(() => Text = $"���������F{ConvertNumberWide(count)}�^{maxCount}�t�@�C������");
                    }

                    ShowMessage("�摜�A������", "�摜�̘A�����������܂����B", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("�G���[", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                Text = "Kindle�X�N���[���V���b�g�c�[��";
            }
        }

        /// <summary>
        /// �t�H���_�I���{�^����������
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void ImageConcatenationImageFolderPathSelectButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog_Show(ConnectionImageSaveFolderBrowserDialog, ImageConcatenationImageFolderPathTextBox);
        }

        /// <summary>
        /// �t�@�C�����A�Ԍ����j���[�����b�N�A�b�v�_�E���l�ύX����
        /// </summary>
        /// <param name="sender">�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g</param>
        private void ImageConcatenationFileNameSerialNumberNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            ImageConcatenationFileNameSampleLabel.Text = $@"�T���v���t�@�C�����F{new string('0', (int)ImageConcatenationFileNameSerialNumberNumericUpDown.Value)}.png";
        }

        #endregion

        #region �w���p�[���\�b�h

        /// <summary>
        /// SoftwareBitmap�ϊ�����
        /// </summary>
        /// <param name="bitmap">�r�b�g�}�b�v</param>
        /// <returns>SoftwareBitmap</returns>
        private async Task<SoftwareBitmap> ConvertToSoftwareBitmap(Bitmap bitmap)
        {
            // �r�b�g�}�b�v��MemoryStream�ɕۑ�����B
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;

            // UWP API�ŁA�g�p�\��InMemoryRandomAccessStream�ɕۑ�����B
            var randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(ms.ToArray().AsBuffer());
            randomAccessStream.Seek(0);

            // �摜���f�R�[�h����B
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

            // SoftwareBitmap�ɕϊ����A�ԋp����B
            return await decoder.GetSoftwareBitmapAsync();
        }

        /// <summary>
        /// �摜�A������
        /// 
        /// �O��Ƃ��āA�A��������摜�T�C�Y�͑S�ē���T�C�Y
        /// </summary>
        /// <param name="pngPathAr">Png�t�@�C���p�X�z��</param>
        /// <param name="saveFullPath">�ۑ��t���p�X�i�g���q���܂ށj</param>
        /// <param name="verticalFlg">�����t���O�itrue�F�c�A���Efalse�F���A���j</param>
        private void MergeImage(string[] pngPathAr, string saveFullPath, bool verticalFlg)
        {
            // �S�摜��ǂݍ��ށB
            Image[] imageAr = pngPathAr.Select(Image.FromFile).ToArray();

            try
            {
                // �摜�T�C�Y��ێ�����B
                int width = imageAr[0].Width;
                int height = imageAr[0].Height;

                // �A���摜�T�C�Y���Z�o���A�ێ�����B
                int totalWidth = verticalFlg ? width : width * imageAr.Length;
                int totalHeight = verticalFlg ? height * imageAr.Length : height;

                using Bitmap mergeBitmap = new(totalWidth, totalHeight);
                using Graphics g = Graphics.FromImage(mergeBitmap);
                g.Clear(Color.White);

                // �摜��A������B
                for (int index = 0; index < imageAr.Length; index++)
                {
                    int x = verticalFlg ? 0 : index * width;
                    int y = verticalFlg ? index * height : 0;
                    g.DrawImage(imageAr[index], x, y, width, height);
                }

                // �A���摜��ۑ�����B
                mergeBitmap.Save(saveFullPath, ImageFormat.Png);
            }
            finally
            {
                foreach (var img in imageAr)
                {
                    img.Dispose();
                }
            }
        }

        /// <summary>
        /// �t�H���_�I���_�C�A���O�\������
        /// </summary>
        /// <param name="folderBrowserDialog">�t�H���_�I���_�C�A���O</param>
        /// <param name="textbox">�e�L�X�g�{�b�N�X</param>
        private void FolderBrowserDialog_Show(FolderBrowserDialog folderBrowserDialog, TextBox textbox)
        {
            // �p�X�����͂���Ă���ꍇ�́A���̓p�X�������l�ɐݒ肷��B
            // �i�u�����N�⑶�݂��Ȃ��p�X�̏ꍇ�A�f�t�H���g�A�������͑O�ɑI�������p�X���ݒ肳���B�j
            folderBrowserDialog.InitialDirectory = textbox.Text;
            if (Equals(DialogResult.OK, folderBrowserDialog.ShowDialog(this)))
            {
                textbox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// �p�X���K������
        /// </summary>
        /// <param name="path">�p�X</param>
        /// <returns>���K�����ꂽ�p�X</returns>
        private static string NormalizePath(string path)
        {
            string fullPath = string.Empty;
            if (path.Length <= 3)
            {
                // �h���C�u�����݂̂̏ꍇ
                fullPath = string.Concat(char.ToUpperInvariant(path[0]), path[1..]);
            }
            else
            {
                fullPath = Path.GetFullPath(path);

                // ���[�g�����擾����B�i��FC:\�j
                var root = Path.GetPathRoot(fullPath);

                if (!string.IsNullOrEmpty(root) && Path.IsPathRooted(fullPath))
                {
                    // �h���C�u������啶���ɂ���B
                    fullPath = string.Concat(char.ToUpperInvariant(root[0]), fullPath[1..]);
                }
            }

            // �����́y\�z���폜����B
            return fullPath.TrimEnd('\\');
        }

        /// <summary>
        /// �ۑ��t���p�X�擾����
        /// </summary>
        /// <param name="saveFolderPath">�ۑ��t�H���_�p�X</param>
        /// <param name="setNumber">�ݒ�ԍ�</param>
        /// <param name="maxLength">�ő包��</param>
        /// <returns>�ۑ��t���p�X</returns>
        private string GetSaveFullPath(string saveFolderPath, int setNumber, int maxLength)
        {
            if (setNumber.ToString().Length > maxLength)
            {
                setNumber = int.Parse(new string('9', maxLength));
            }

            return $@"{saveFolderPath}\{setNumber.ToString().PadLeft(maxLength, '0')}.png";
        }

        /// <summary>
        /// ���p���l���S�p���l�ϊ�����
        /// </summary>
        /// <param name="narrowNumber">���p���l</param>
        /// <returns>�ϊ������S�p���l</returns>
        private string ConvertNumberWide(int narrowNumber)
        {
            var sb = new StringBuilder();
            foreach (char ch in narrowNumber.ToString())
            {
                sb.Append(FullWidthDigitsAr[int.Parse(ch.ToString())]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// ���b�Z�[�W�\������
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        private void ShowMessage(string title, string message, MessageBoxIcon icon)
        {
            MessageBox.Show(
                this,
                message,
                title,
                MessageBoxButtons.OK,
                icon);
        }

        #endregion
    }
}
