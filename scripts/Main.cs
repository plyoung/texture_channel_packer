using Godot;
using System;

namespace Game;

public partial class Main : Panel
{
    [Export] private Button saveButton;
    [Export] private Button[] loadButton;
    [Export] private Button[] clearButton;
    [Export] private OptionButton[] channelOption;
    [Export] private SpinBox[] defaultValEd;
    [Export] private TextureRect mainTR;
    [Export] private TextureRect[] subTR;
    [Export] private FileDialog dialog;

    private int dialogueTarget = -1;
    private string[] paths = new string[4];
    private Image mainImage;
    private double updateTimer;

    // ----------------------------------------------------------------------------------------------------------------

    public override void _EnterTree()
    {
        saveButton.Pressed += OnSaveButton;
        dialog.FileSelected += OnDialogFileSelected;

        for (int i = 0; i < 4; i++)
        {
            var a = i;
            loadButton[i].Pressed += () => OnLoadButton(a);
            clearButton[i].Pressed += () => OnClearButton(a);
            channelOption[i].ItemSelected += _ => UpdateImages();
            defaultValEd[i].ValueChanged += DefaultValEdChanged;
        }
    }

    public override void _Ready()
    {
        UpdateImages();
    }

    public override void _Process(double delta)
    {
        if (updateTimer > 0.0 && (updateTimer -= delta) <= 0.0)
        {
            UpdateImages();
        }
    }

    private void DefaultValEdChanged(double value)
    {
        if (updateTimer <= 0.0) updateTimer = 0.1;
    }

    // ----------------------------------------------------------------------------------------------------------------

    private void OnLoadButton(int channel)
    {
        dialogueTarget = channel;

        dialog.Filters = ["*.bmp,*.dds,*.exr,*.jpg,*.jpeg,*.ktx,*.png,*.svg,*.tga,*.webp;Image Files"];
        dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        dialog.DisplayMode = FileDialog.DisplayModeEnum.List;
        dialog.UseNativeDialog = true;
        dialog.ForceNative = true;
        dialog.Popup();
    }

    private void OnSaveButton()
    {
        dialogueTarget = -1;

        dialog.Filters = ["*.png;Image File"];
        dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        dialog.DisplayMode = FileDialog.DisplayModeEnum.List;
        dialog.UseNativeDialog = true;
        dialog.ForceNative = true;
        dialog.Popup();
    }

    private void OnDialogFileSelected(string path)
    {
        if (dialogueTarget < 0)
        {
            SaveImage(path);
        }
        else
        {
            LoadImage(path, dialogueTarget);
        }
    }

    // ----------------------------------------------------------------------------------------------------------------

    private void LoadImage(string path, int channel)
    {
        paths[channel] = path;
        UpdateImages();
    }

    private void OnClearButton(int channel)
    {
        subTR[channel].Texture = null;
        paths[channel] = null;

        UpdateImages();
    }

    private void UpdateImages()
    {
        var maxSize = Vector2I.Zero;
        var images = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            if (!FileAccess.FileExists(paths[i])) continue;

            images[i] = Image.LoadFromFile(paths[i]);
            if (images[i] == null) continue;

            var w = images[i].GetWidth();
            var h = images[i].GetHeight();
            if (w > maxSize.X) maxSize.X = w;
            if (h > maxSize.Y) maxSize.Y = h;
        }

        if (maxSize == Vector2I.Zero)
        {
            maxSize = new Vector2I(8, 8);
        }

        var buffer = new Color[maxSize.X * maxSize.Y];

        for (int i = 0; i < 4; i++)
        {
            if (images[i] == null)
            {
                images[i] = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
                var f = (float)defaultValEd[i].Value;
                var col = new Color(f, f, f, 1f);
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        images[i].SetPixel(x, y, col);
                    }
                }
            }

            var img = images[i];
            var w = img.GetWidth();
            var h = img.GetHeight();
            if (w < maxSize.X || h < maxSize.Y)
            {
                img.Resize(maxSize.X, maxSize.Y);
                subTR[i].Texture = ImageTexture.CreateFromImage(img);
            }

            subTR[i].Texture = ImageTexture.CreateFromImage(img);

            var readChannel = channelOption[i].Selected;
            for (int x = 0; x < maxSize.X; x++)
            {
                for (int y = 0; y < maxSize.Y; y++)
                {
                    buffer[y * maxSize.X + x][i] = img.GetPixel(x, y)[readChannel];
                }
            }
        }

        mainImage = Image.CreateEmpty(maxSize.X, maxSize.Y, false, Image.Format.Rgba8);
        for (int x = 0; x < maxSize.X; x++)
        {
            for (int y = 0; y < maxSize.Y; y++)
            {
                mainImage.SetPixel(x, y, buffer[y * maxSize.X + x]);
            }
        }

        mainTR.Texture = ImageTexture.CreateFromImage(mainImage);
    }

    // ----------------------------------------------------------------------------------------------------------------

    private void SaveImage(string path)
    {
        mainImage.SavePng(path);
    }

    // ----------------------------------------------------------------------------------------------------------------
}
