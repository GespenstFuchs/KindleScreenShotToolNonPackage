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
        #region 定数

        /// <summary>
        /// 全角数値配列
        /// </summary>
        private readonly string[] FullWidthDigitsAr =
        {
            "０",
            "１",
            "２",
            "３",
            "４",
            "５",
            "６",
            "７",
            "８",
            "９"
        };

        /// <summary>
        /// 言語リスト
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

        #region プロパティ

        /// <summary>
        /// スクリーンショットロジック
        /// </summary>
        private ScreenShotLogic ScreenShotLogic { get; } = new();

        /// <summary>
        /// タスクバープログレス
        /// </summary>
        private TaskbarProgress TaskbarProgress { get; } = new();

        #endregion

        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            PressKeyComboBox.SelectedIndex = 0;
            LangComboBox.SelectedIndex = 0;
            ConnectionDirectionComboBox.SelectedIndex = 0;
            PageDirectionComboBox.SelectedIndex = 0;
            ImageDisplayComboBox.SelectedIndex = 0;

            // Microsoft Print to PDFの有無を判定する。
            if (!PrinterSettings.InstalledPrinters.Cast<string>().Any(printer => printer == "Microsoft Print to PDF"))
            {
                // 存在しない場合
                PrinterWarningLabel.Visible = true;
                PDFExeButton.Enabled = false;
            }
        }

        #endregion

        #region スクリーンショットイベント

        /// <summary>
        /// 実行ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private async void ScreenShotExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                ScreenShotExeButton.Enabled = false;

                if (string.IsNullOrEmpty(KindleTitleTextBox.Text))
                {
                    ShowMessage("エラー", "Kindleのタイトルが未入力です。", MessageBoxIcon.Error);
                    return;
                }

                if (!ScreenShotLogic.IsKindleRunning(KindleTitleTextBox.Text))
                {
                    ShowMessage("エラー", "入力されたタイトルのKindleが起動していません。", MessageBoxIcon.Error);
                    return;
                }

                if (Equals(DialogResult.OK, ScreenShotSaveFolderBrowserDialog.ShowDialog(this)))
                {
                    // Kindleタイトルを保持する。
                    string kindleTitle = KindleTitleTextBox.Text;

                    // 撮影情報を保持する。
                    int captureStartX = (int)CaptureStartXNumericUpDown.Value;
                    int captureStartY = (int)CaptureStartYNumericUpDown.Value;
                    int captureWidth = (int)CaptureWidthNumericUpDown.Value;
                    int captureHeight = (int)CaptureHeightNumericUpDown.Value;

                    // 撮影枚数を保持する。
                    int captureCount = (int)CaptureCountNumericUpDown.Value;
                    ulong captureCountULong = (ulong)captureCount;

                    // ファイル名連番桁数を保持する。
                    int maxLength = (int)FileNameSerialNumberNumericUpDown.Value;

                    // 撮影待機時間を保持する。
                    int waitingTime = (int)WaitingTimeNumericUpDown.Value;

                    // 押下するキーコードを保持する。
                    int keyCode = ScreenShotLogic.VK_LEFT;
                    if (PressKeyComboBox.SelectedIndex == 1)
                    {
                        keyCode = ScreenShotLogic.VK_RIGHT;
                    }

                    // Kindleウィンドウをアクティブにする。
                    ScreenShotLogic.ActivateKindleWindow(kindleTitle);

                    // 保存ダイアログが消える前に、スクリーンショットの撮影が始まるため、待機させる。
                    Thread.Sleep(100);

                    // 撮影終了条件フラグを設定する。
                    bool captureEndConditionFlg = DuplicateCountRadioButton.Checked;

                    // 重複枚数を保持する（ＵＩ上分かりやすく２から入力させているので、デクリメントする。）。
                    int duplicateCount = (int)DuplicateCountNumericUpDown.Value;
                    duplicateCount--;

                    await Task.Run(async () =>
                    {
                        // タスクバーのプログレス状態を設定する。
                        Invoke(() => TaskbarProgress.SetProgressState(Handle, TaskbarProgressState.Normal));

                        int index = 0;

                        // 撮影終了条件フラグを判定する。
                        if (captureEndConditionFlg)
                        {
                            string saveFullPath = string.Empty;
                            string beforeSaveFullPath = string.Empty;
                            int fileCount = 0;
                            int matchCount = 0;

                            do
                            {
                                // 保存ファイル名を保持する。
                                saveFullPath = GetSaveFullPath(ScreenShotSaveFolderBrowserDialog.SelectedPath, fileCount, maxLength);

                                // ファイル数をインクリメントする。
                                fileCount++;

                                // 早すぎると、うまく撮影出来ないため、待機させる。
                                await Task.Delay(waitingTime);

                                // スクリーンショットを撮影する。
                                ScreenShotLogic.SaveScreenShot(
                                    captureStartX,
                                    captureStartY,
                                    captureWidth,
                                    captureHeight,
                                    saveFullPath);

                                // 前回保存ファイル名の有無を判定する。
                                if (!string.IsNullOrEmpty(beforeSaveFullPath))
                                {
                                    // 画像ファイルを比較する。
                                    if (ScreenShotLogic.CompareImage(beforeSaveFullPath, saveFullPath))
                                    {
                                        // 一致する場合
                                        matchCount++;
                                    }
                                    else
                                    {
                                        // 一致しない場合
                                        matchCount = 0;
                                    }
                                }

                                // 前回保存ファイル名を保持する。
                                beforeSaveFullPath = saveFullPath;

                                // キーを押下する。
                                ScreenShotLogic.KeyDown(kindleTitle, keyCode);

                                // プログレス値を設定する。
                                Invoke(() => TaskbarProgress.SetProgressValue(Handle, (ulong)(index + 1), 20));
                                if (index == 19)
                                {
                                    index = 0;
                                }
                                else
                                {
                                    index++;
                                }

                            } while (duplicateCount != matchCount);
                        }
                        else
                        {
                            // 撮影枚数の場合
                            for (index = 0; index < captureCount; index++)
                            {
                                // 早すぎると、うまく撮影出来ないため、待機させる。
                                await Task.Delay(waitingTime);

                                // スクリーンショットを撮影する。
                                ScreenShotLogic.SaveScreenShot(
                                    captureStartX,
                                    captureStartY,
                                    captureWidth,
                                    captureHeight,
                                    GetSaveFullPath(ScreenShotSaveFolderBrowserDialog.SelectedPath, index, maxLength));

                                // キーを押下する。
                                ScreenShotLogic.KeyDown(kindleTitle, keyCode);

                                // プログレス値を設定する。
                                Invoke(() => TaskbarProgress.SetProgressValue(Handle, (ulong)(index + 1), captureCountULong));
                            }
                        }
                    });

                    // プログレスを消去する。
                    TaskbarProgress.SetProgressState(Handle, TaskbarProgressState.NoProgress);

                    // Kindleウィンドウがアクティブになっているので、自身をアクティブにする。
                    Activate();

                    ShowMessage("スクリーンショット撮影完了", "スクリーンショットの撮影が完了しました。", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("エラー", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                ScreenShotExeButton.Enabled = true;
            }
        }

        /// <summary>
        /// テスト撮影ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void TestScreenShotButton_Click(object sender, EventArgs e)
        {
            if (Equals(DialogResult.OK, TestScreenShotSaveFileDialog.ShowDialog(this)))
            {
                // 保存ダイアログが消える前に、スクリーンショットの撮影が始まるため、待機させる。
                Thread.Sleep(100);

                // スクリーンショットを撮影する。
                ScreenShotLogic.SaveScreenShot(
                    (int)CaptureStartXNumericUpDown.Value,
                    (int)CaptureStartYNumericUpDown.Value,
                    (int)CaptureWidthNumericUpDown.Value,
                    (int)CaptureHeightNumericUpDown.Value,
                    TestScreenShotSaveFileDialog.FileName);

                ShowMessage("テストスクリーンショット撮影完了", "テストスクリーンショットの撮影が完了しました。", MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// タイトル取得ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void GetTitleButton_Click(object sender, EventArgs e)
        {
            KindleTitleTextBox.Text = ScreenShotLogic.GetKindleTitle();
        }

        /// <summary>
        /// ファイル名連番桁数ニューメリックアップダウン値変更処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void FileNameSerialNumberNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            FileNameSampleLabel.Text = $@"サンプルファイル名：{new string('0', (int)FileNameSerialNumberNumericUpDown.Value)}.png";
        }

        /// <summary>
        /// 重複枚数ラジオボタンチェック変更処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void DuplicateCountRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            DuplicateCountNumericUpDown.Enabled = DuplicateCountRadioButton.Checked;
            CaptureCountNumericUpDown.Enabled = CaptureCountRadioButton.Checked;
        }

        /// <summary>
        /// 撮影枚数ラジオボタンチェック変更処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void CaptureCountRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            DuplicateCountNumericUpDown.Enabled = DuplicateCountRadioButton.Checked;
            CaptureCountNumericUpDown.Enabled = CaptureCountRadioButton.Checked;
        }

        #endregion

        #region OCRイベント

        /// <summary>
        /// 実行ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private async void OCRExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                OCRExeButton.Enabled = false;

                if (string.IsNullOrEmpty(OCRImageFolderPathTextBox.Text))
                {
                    ShowMessage("エラー", "画像フォルダパスが未入力です。", MessageBoxIcon.Error);
                    return;
                }

                // フォルダパスを正規化し、保持する。
                string imageFolderPath = NormalizePath(OCRImageFolderPathTextBox.Text);

                if (!Directory.Exists(imageFolderPath))
                {
                    ShowMessage("エラー", "画像フォルダパスが存在しません。", MessageBoxIcon.Error);
                    return;
                }

                if (SingleRadioButton.Checked)
                {
                    // フォルダ選択ダイアログを表示する。
                    if (!Equals(DialogResult.OK, OCROutputFolderPathFolderBrowserDialog.ShowDialog(this)))
                    {
                        return;
                    }
                }
                else
                {
                    // ファイル保存ダイアログを表示する。
                    if (!Equals(DialogResult.OK, OCRTextSaveFileDialog.ShowDialog(this)))
                    {
                        return;
                    }
                }

                // 出力形式を保持する。
                bool outputFormatFlg = SingleRadioButton.Checked;

                // 言語を保持する。
                string lngStr = LangList[LangComboBox.SelectedIndex];

                // pngファイルのフルパスの一覧をリストにする。
                List<string> pngPathList = [.. Directory.GetFiles(imageFolderPath, "*.png").OrderBy(path => Path.GetFileName(path))];

                string maxCount = ConvertNumberWide(pngPathList.Count);

                // 言語を設定する。
                string lang = string.Equals("日本語", LangComboBox.Text) ? "ja-JP" : "en-US";

                string outputText = string.Empty;
                int count = 0;

                await Task.Run(async () =>
                {
                    foreach (var pngPath in pngPathList)
                    {
                        // OCRengineを初期化する。
                        // （Windows OCRは、日本語・英語しか対応していない。）
                        var ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(lang));

                        // 画像をBitmapとして読み込む。
                        using var bitmap = (Bitmap)Image.FromFile(pngPath);
                        var softwareBitmap = await ConvertToSoftwareBitmap(bitmap);

                        // 画像を読み込み、テキストを抽出する。
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

                        // タイトルに処理件数を設定する。
                        Invoke(() => Text = $"処理件数：{ConvertNumberWide(count)}／{maxCount}ファイル完了");
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

                ShowMessage("OCR処理完了", "OCR処理が完了しました。", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage("エラー", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                Text = "Kindleスクリーンショットツール";
                OCRExeButton.Enabled = true;
            }
        }

        /// <summary>
        /// フォルダ選択ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void OCRImageFolderPathSelectButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog_Show(OCRImageFolderPathFolderBrowserDialog, OCRImageFolderPathTextBox);
        }

        #endregion

        #region PDFイベント

        /// <summary>
        /// 実行ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private async void PDFExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                PDFExeButton.Enabled = false;

                if (string.IsNullOrEmpty(PDFImageFolderPathTextBox.Text))
                {
                    ShowMessage("エラー", "画像フォルダパスが未入力です。", MessageBoxIcon.Error);
                    return;
                }

                // PDF画像フォルダパスを取得する。
                string pdfImageFolderPath = NormalizePath(PDFImageFolderPathTextBox.Text);

                if (!Directory.Exists(pdfImageFolderPath))
                {
                    ShowMessage("エラー", "画像フォルダパスが存在しません。", MessageBoxIcon.Error);
                    return;
                }

                if (Equals(DialogResult.OK, PDFSaveFileDialog.ShowDialog(this)))
                {
                    string pageDirection = PageDirectionComboBox.Text;
                    string imageDisplay = ImageDisplayComboBox.Text;

                    await Task.Run(() =>
                    {
                        // 拡張子が、【.png】（大文字・小文字問わず）のフルパスを取得し、リストにする。
                        List<string> pngPathList = [..Directory.EnumerateFiles(pdfImageFolderPath, "*.png", SearchOption.TopDirectoryOnly)
                        .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(path => Path.GetFileName(path))
                        .ToList()];

                        // ページの向きを保持する。
                        bool landscapeFlg = string.Equals("横向き", pageDirection);

                        // 画像表示を保持する。
                        // （true：全画面・false：中央表示）
                        bool imageDisplayFlg = string.Equals("全表示", imageDisplay);

                        PrintDocument doc = new()
                        {
                            PrinterSettings = new PrinterSettings()
                            {
                                PrinterName = "Microsoft Print to PDF",
                                MaximumPage = pngPathList.Count,
                                ToPage = pngPathList.Count,
                                DefaultPageSettings = { Landscape = landscapeFlg, },
                                PrintToFile = true, // ファイルに出力する。
                                PrintFileName = Path.Combine(PDFSaveFileDialog.FileName),
                            }
                        };

                        // 余白を無しにする。
                        doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                        int currentPageIndex = 0;

                        // ページ設定を行う。
                        doc.PrintPage += (s, e) =>
                        {
                            using (var image = Image.FromFile(pngPathList[currentPageIndex]))
                            {
                                Rectangle pageRect;
                                int x;
                                int y;

                                if (imageDisplayFlg)
                                {
                                    // 全画面の場合
                                    pageRect = e.PageBounds;

                                    // 横幅・縦幅の倍率を算出する。
                                    float ratioX = (float)pageRect.Width / image.Width;
                                    float ratioY = (float)pageRect.Height / image.Height;

                                    // 画像の縦横比を保つため、より小さい倍率を保持する。
                                    float ratio = Math.Min(ratioX, ratioY);

                                    // 描画する画像サイズを倍率に応じて算出する。
                                    int drawWidth = (int)(image.Width * ratio);
                                    int drawHeight = (int)(image.Height * ratio);

                                    // ページ中央に画像を配置するための座標を算出する。
                                    x = pageRect.X + (pageRect.Width - drawWidth) / 2;
                                    y = pageRect.Y + (pageRect.Height - drawHeight) / 2;

                                    // 算出した位置とサイズで画像を描画する。
                                    e.Graphics?.DrawImage(image, x, y, drawWidth, drawHeight);
                                }
                                else
                                {
                                    // 中央表示の場合
                                    pageRect = e.MarginBounds;

                                    // 画像サイズを保持する。
                                    int imgWidth = image.Width;
                                    int imgHeight = image.Height;

                                    // 中央に配置するため、位置を算出する。
                                    x = pageRect.X + (pageRect.Width - imgWidth) / 2;
                                    y = pageRect.Y + (pageRect.Height - imgHeight) / 2;

                                    // 原寸サイズで描画する。
                                    e.Graphics?.DrawImage(image, x, y, imgWidth, imgHeight);
                                }
                            }

                            currentPageIndex++;
                            e.HasMorePages = currentPageIndex < pngPathList.Count;
                        };

                        // PDFファイルとして印刷（保存）する。
                        doc.Print();
                    });

                    ShowMessage("PDF作成完了", "PDFの作成が完了しました。", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("エラー", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                PDFExeButton.Enabled = true;
            }
        }

        /// <summary>
        /// フォルダ選択ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void PDFImageFolderPathSelectButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog_Show(PDFImageFolderPathFolderBrowserDialog, PDFImageFolderPathTextBox);
        }

        #endregion

        #region 画像連結イベント

        /// <summary>
        /// 実行ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private async void ImageConcatenationExeButton_Click(object sender, EventArgs e)
        {
            try
            {
                ImageConcatenationExeButton.Enabled = false;

                if (string.IsNullOrEmpty(ImageConcatenationImageFolderPathTextBox.Text))
                {
                    ShowMessage("エラー", "画像フォルダパスが未入力です。", MessageBoxIcon.Error);
                    return;
                }

                // 画像フォルダパスを取得する。
                string imageFolderPath = NormalizePath(ImageConcatenationImageFolderPathTextBox.Text);

                if (!Directory.Exists(imageFolderPath))
                {
                    ShowMessage("エラー", "画像フォルダパスが存在しません。", MessageBoxIcon.Error);
                    return;
                }

                if (Equals(DialogResult.OK, ConnectionImageSaveFolderBrowserDialog.ShowDialog(this)))
                {

                    // 連結方向を取得する。
                    int connectionDirection = ConnectionDirectionComboBox.SelectedIndex;
                    bool verticalFlg = connectionDirection == 0 || connectionDirection == 1;

                    // 連結数を取得する。
                    int connectionCount = (int)ConnectionCountNumericUpDown.Value;

                    // ファイル名連番桁数を保持する。
                    int maxLength = (int)ImageConcatenationFileNameSerialNumberNumericUpDown.Value;

                    await Task.Run(() =>
                    {
                        // 拡張子が、【.png】（大文字・小文字問わず）の全ファイルを取得し、リストにする。
                        var pngFileList = Directory.EnumerateFiles(imageFolderPath, "*.png", SearchOption.TopDirectoryOnly)
                            .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(Path.GetFileName)
                            .ToList();

                        string maxCount = ConvertNumberWide(pngFileList.Count);

                        // リストを分割する。
                        string[][] splitArray = pngFileList.Chunk(connectionCount).ToArray();

                        int count = 0;

                        for (int index = 0; index < splitArray.Length; index++)
                        {
                            var pngFilePathAr = splitArray[index];

                            // 連結方向を判定する。
                            if (Equals(1, connectionDirection) || Equals(3, connectionDirection))
                            {
                                pngFilePathAr = [.. pngFilePathAr.OrderByDescending(pngFilePath => pngFilePath)];
                            }

                            // 画像を連結する。
                            MergeImage(
                                pngFilePathAr,
                                GetSaveFullPath(ConnectionImageSaveFolderBrowserDialog.SelectedPath, index, maxLength),
                                verticalFlg);

                            count += pngFilePathAr.Length;

                            // タイトルに処理件数を設定する。
                            Invoke(() => Text = $"処理件数：{ConvertNumberWide(count)}／{maxCount}ファイル完了");
                        }
                    });

                    ShowMessage("画像連結完了", "画像の連結が完了しました。", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("エラー", ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                Text = "Kindleスクリーンショットツール";
                ImageConcatenationExeButton.Enabled = true;
            }
        }

        /// <summary>
        /// フォルダ選択ボタン押下処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void ImageConcatenationImageFolderPathSelectButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog_Show(ConnectionImageSaveFolderBrowserDialog, ImageConcatenationImageFolderPathTextBox);
        }

        /// <summary>
        /// ファイル名連番桁数ニューメリックアップダウン値変更処理
        /// </summary>
        /// <param name="sender">オブジェクト</param>
        /// <param name="e">イベント</param>
        private void ImageConcatenationFileNameSerialNumberNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            ImageConcatenationFileNameSampleLabel.Text = $@"サンプルファイル名：{new string('0', (int)ImageConcatenationFileNameSerialNumberNumericUpDown.Value)}.png";
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// SoftwareBitmap変換処理
        /// </summary>
        /// <param name="bitmap">ビットマップ</param>
        /// <returns>SoftwareBitmap</returns>
        private async Task<SoftwareBitmap> ConvertToSoftwareBitmap(Bitmap bitmap)
        {
            // ビットマップをMemoryStreamに保存する。
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;

            // UWP APIで、使用可能なInMemoryRandomAccessStreamに保存する。
            var randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(ms.ToArray().AsBuffer());
            randomAccessStream.Seek(0);

            // 画像をデコードする。
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

            // SoftwareBitmapに変換し、返却する。
            return await decoder.GetSoftwareBitmapAsync();
        }

        /// <summary>
        /// 画像連結処理
        /// 
        /// 前提として、連結させる画像サイズは全て同一サイズ
        /// </summary>
        /// <param name="pngPathAr">Pngファイルパス配列</param>
        /// <param name="saveFullPath">保存フルパス（拡張子も含む）</param>
        /// <param name="verticalFlg">垂直フラグ（true：縦連結・false：横連結）</param>
        private void MergeImage(string[] pngPathAr, string saveFullPath, bool verticalFlg)
        {
            // 全画像を読み込む。
            Image[] imageAr = pngPathAr.Select(Image.FromFile).ToArray();

            try
            {
                // 画像サイズを保持する。
                int width = imageAr[0].Width;
                int height = imageAr[0].Height;

                // 連結画像サイズを算出し、保持する。
                int totalWidth = verticalFlg ? width : width * imageAr.Length;
                int totalHeight = verticalFlg ? height * imageAr.Length : height;

                using Bitmap mergeBitmap = new(totalWidth, totalHeight);
                using Graphics g = Graphics.FromImage(mergeBitmap);
                g.Clear(Color.White);

                // 画像を連結する。
                for (int index = 0; index < imageAr.Length; index++)
                {
                    int x = verticalFlg ? 0 : index * width;
                    int y = verticalFlg ? index * height : 0;
                    g.DrawImage(imageAr[index], x, y, width, height);
                }

                // 連結画像を保存する。
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
        /// フォルダ選択ダイアログ表示処理
        /// </summary>
        /// <param name="folderBrowserDialog">フォルダ選択ダイアログ</param>
        /// <param name="textbox">テキストボックス</param>
        private void FolderBrowserDialog_Show(FolderBrowserDialog folderBrowserDialog, TextBox textbox)
        {
            // パスが入力されている場合は、入力パスを初期値に設定する。
            // （ブランクや存在しないパスの場合、デフォルト、もしくは前に選択したパスが設定される。）
            folderBrowserDialog.InitialDirectory = textbox.Text;
            if (Equals(DialogResult.OK, folderBrowserDialog.ShowDialog(this)))
            {
                textbox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// パス正規化処理
        /// </summary>
        /// <param name="path">パス</param>
        /// <returns>正規化されたパス</returns>
        private static string NormalizePath(string path)
        {
            string fullPath = string.Empty;
            if (path.Length <= 3)
            {
                // ドライブ文字のみの場合
                fullPath = string.Concat(char.ToUpperInvariant(path[0]), path[1..]);
            }
            else
            {
                fullPath = Path.GetFullPath(path);

                // ルート部を取得する。（例：C:\）
                var root = Path.GetPathRoot(fullPath);

                if (!string.IsNullOrEmpty(root) && Path.IsPathRooted(fullPath))
                {
                    // ドライブ文字を大文字にする。
                    fullPath = string.Concat(char.ToUpperInvariant(root[0]), fullPath[1..]);
                }
            }

            // 末尾の【\】を削除する。
            return fullPath.TrimEnd('\\');
        }

        /// <summary>
        /// 保存フルパス取得処理
        /// </summary>
        /// <param name="saveFolderPath">保存フォルダパス</param>
        /// <param name="setNumber">設定番号</param>
        /// <param name="maxLength">最大桁数</param>
        /// <returns>保存フルパス</returns>
        private string GetSaveFullPath(string saveFolderPath, int setNumber, int maxLength)
        {
            if (setNumber.ToString().Length > maxLength)
            {
                setNumber = int.Parse(new string('9', maxLength));
            }

            return $@"{saveFolderPath}\{setNumber.ToString().PadLeft(maxLength, '0')}.png";
        }

        /// <summary>
        /// 半角数値→全角数値変換処理
        /// </summary>
        /// <param name="narrowNumber">半角数値</param>
        /// <returns>変換した全角数値</returns>
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
        /// メッセージ表示処理
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
