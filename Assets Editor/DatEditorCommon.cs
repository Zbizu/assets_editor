// common actions shared across modern and legacy dat editor classes

using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tibia.Protobuf.Appearances;
using Xceed.Wpf.Toolkit;
using ButtonProgressAssist = MaterialDesignThemes.Wpf.ButtonProgressAssist;
using Snackbar = MaterialDesignThemes.Wpf.Snackbar;

namespace Assets_Editor;

public class LowercaseContractResolver : DefaultContractResolver {
    protected override string ResolvePropertyName(string propertyName) {
        return propertyName.ToLower();
    }
}

public partial class DatEditorCommon : Window {
    protected bool isPageLoaded = false;
    protected bool isUpdatingFrame = false;

    // direction of currently viewed outfit
    protected int CurrentSprDir = 2;

    protected static ObservableCollection<ShowList> ThingsOutfit { get; set; } = [];
    protected static ObservableCollection<ShowList> ThingsItem { get; set; } = [];
    protected static ObservableCollection<ShowList> ThingsEffect { get; set; } = [];
    protected static ObservableCollection<ShowList> ThingsMissile { get; set; } = [];

    // these fields get initialized from XAML
    // if the XAML doesn't have these fields, an exception will be thrown (this is intended)
    // "null!" silences a warning regarding that
    // to do: some safeguard in the future to ensure that the ui implements these (?)
    protected ComboBox ObjectMenu { get; set; } = null!;
    protected IntegerUpDown ObjListViewSelectedIndex = null!;
    protected IntegerUpDown SprDefaultPhase = null!;
    protected IntegerUpDown SprLoopCount = null!;
    protected ListView SprListView { get; set; } = null!;
    protected ListView ObjListView { get; set; } = null!;
    protected Slider SprFramesSlider { get; set; } = null!;
    protected Slider SprGroupSlider { get; set; } = null!;
    protected ToggleButton DarkModeToggle { get; set; } = null!;

    // color pickers for flags
    protected ComboBox A_FlagMarketProfession {  get; set; } = null!;
    protected ColorPicker A_FlagAutomapColorPicker { get; set; } = null!;
    protected IntegerUpDown A_FlagAutomapColor { get; set; } = null!;
    protected ColorPicker A_FlagLightColorPicker { get; set; } = null!;
    protected IntegerUpDown A_FlagLightColor { get; set; } = null!;

    // outfit preview color pickers
    protected ColorPicker SprLayerHeadPicker { get; set; } = null!;
    protected ColorPicker SprLayerBodyPicker { get; set; } = null!;
    protected ColorPicker SprLayerLegsPicker { get; set; } = null!;
    protected ColorPicker SprLayerFeetPicker { get; set; } = null!;

    // outfit preview direction buttons
    protected Button SprUpArrow { get; set; } = null!;
    protected Button SprDownArrow { get; set; } = null!;
    protected Button SprLeftArrow { get; set; } = null!;
    protected Button SprRightArrow { get; set; } = null!;

    // notification bar at the bottom
    protected Snackbar StatusBar { get; set; } = null!;

    // temporary data
    public Appearance CurrentObjectAppearance;
    public AppearanceFlags CurrentFlags = null;

    public static void SyncPickerToValue(int? value, ColorPicker picker) {
        Utils.SafeSetColor(value, picker);
    }

    public static void SyncValueToPicker(ColorPicker picker, IntegerUpDown upDown) {
        foreach (var color in picker.AvailableColors) {
            if (color.Color.Value.ToString() == picker.SelectedColor.ToString()) {
                upDown.Value = int.Parse(color.Name);
                return;
            }
        }
    }

    protected override void OnClosed(EventArgs e) {
        base.OnClosed(e);
        Application.Current.Shutdown();
    }
    private void Window_Loaded(object sender, RoutedEventArgs e) {
        isPageLoaded = true;
    }

    protected void ForceSliderChange() {
        isUpdatingFrame = true;
        try {
            SprFramesSlider.Minimum = -1;
            SprFramesSlider.Value = -1;
        } finally {
            isUpdatingFrame = false;
        }

        SprFramesSlider.Minimum = 0;
    }

    protected void DarkModeToggle_Checked(object sender, RoutedEventArgs e) {
        MainWindow.SetCurrentTheme(DarkModeToggle.IsChecked ?? false);
    }

    protected void SprListView_ScrollChanged(object sender, ScrollChangedEventArgs e) {
        VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(SprListView);
        if (SprListView.Items.Count > 0 && panel != null) {
            int offset = (int)panel.VerticalOffset;
            //int maxOffset = (int)panel.ViewportHeight;
            for (int i = 0; i < SprListView.Items.Count; i++) {
                if (i >= offset && i < Math.Min(offset + 20, SprListView.Items.Count) && MainWindow.SprLists.ContainsKey(i))
                    try {
                        MainWindow.AllSprList[i].Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream(i));
                    } catch (Exception) {
                        MainWindow.AllSprList[i].Image = null;
                    }
                else
                    MainWindow.AllSprList[i].Image = null;
            }
        }
    }

    protected void SprListViewSelectedIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        if (SprListView.IsLoaded && e.NewValue != null) {
            int nIndex = (int)e.NewValue;
            SprListView.SelectedIndex = nIndex;
            ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(SprListView);
            VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(SprListView);
            int offset = (int)panel.VerticalOffset;
            int maxOffset = (int)panel.ViewportHeight;
            if (nIndex - maxOffset == offset)
                scrollViewer.ScrollToVerticalOffset(offset + 1);
            else if (nIndex + 1 == offset)
                scrollViewer.ScrollToVerticalOffset(offset - 1);
            else if (nIndex >= offset + maxOffset || nIndex < offset)
                scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
        }
    }

    public virtual void UpdateShowList(int selection, uint? preserveId = null) {}

    protected void ObjectMenuChanged(object sender, SelectionChangedEventArgs e) {
        UpdateShowList(ObjectMenu.SelectedIndex);
    }

    protected void ObjListViewSelectedIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        if (ObjListView.IsLoaded) {
            foreach (ShowList item in ObjListView.Items) {
                if (item.Id == ObjListViewSelectedIndex.Value) {
                    ObjListView.SelectedItem = item;
                    ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(ObjListView);
                    VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(ObjListView);
                    int offset = (int)panel.VerticalOffset;
                    int maxOffset = (int)panel.ViewportHeight;
                    if (ObjListView.SelectedIndex > offset + maxOffset || ObjListView.SelectedIndex < offset) {
                        scrollViewer.ScrollToVerticalOffset(ObjListView.SelectedIndex);
                    }
                    break;
                }
            }
        }
    }

    protected void LoadSelectedObjectAppearances(Appearance ObjectAppearance) {
        if (ObjectAppearance == null) {
            return;
        }
        CurrentObjectAppearance = ObjectAppearance.Clone();
        LoadCurrentObjectAppearances();

        isUpdatingFrame = true;
        try {
            SprGroupSlider.Value = 0;
            ChangeGroupType(0);
        } finally {
            isUpdatingFrame = false;
        }
    }

    protected virtual void LoadCurrentObjectAppearances() { }
    protected virtual void ChangeGroupType(int group) { }

    protected void A_FlagLightColorPickerChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
        foreach (var color in A_FlagLightColorPicker.AvailableColors) {
            if (color.Color.Value.ToString() == A_FlagLightColorPicker.SelectedColor.ToString())
                A_FlagLightColor.Value = int.Parse(color.Name);
        }

    }
    protected void A_FlagAutomapColorPickerChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
        foreach (var color in A_FlagAutomapColorPicker.AvailableColors) {
            if (color.Color.Value.ToString() == A_FlagAutomapColorPicker.SelectedColor.ToString())
                A_FlagAutomapColor.Value = int.Parse(color.Name);
        }
    }
    protected void A_FlagAutomapColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        Utils.SafeSetColor(A_FlagAutomapColor.Value, A_FlagAutomapColorPicker);
    }
    protected void A_FlagLightColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        Utils.SafeSetColor(A_FlagLightColor.Value, A_FlagLightColorPicker);
    }
    protected void A_FlagMarketProfession_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        A_FlagMarketProfession.SelectedIndex = -1;
    }
    protected void Randomize_Click(object sender, RoutedEventArgs e) {
        Random rnd = new();
        SprLayerHeadPicker.SelectedColor = SprLayerHeadPicker.AvailableColors[rnd.Next(0, SprLayerHeadPicker.AvailableColors.Count)].Color;
        SprLayerBodyPicker.SelectedColor = SprLayerBodyPicker.AvailableColors[rnd.Next(0, SprLayerBodyPicker.AvailableColors.Count)].Color;
        SprLayerLegsPicker.SelectedColor = SprLayerLegsPicker.AvailableColors[rnd.Next(0, SprLayerLegsPicker.AvailableColors.Count)].Color;
        SprLayerFeetPicker.SelectedColor = SprLayerFeetPicker.AvailableColors[rnd.Next(0, SprLayerFeetPicker.AvailableColors.Count)].Color;
    }
    protected void OutfitXml(object sender, RoutedEventArgs e) {
        int typeValue = (int)CurrentObjectAppearance.Id;
        int headValue = 0;
        int bodyValue = 0;
        int legsValue = 0;
        int feetValue = 0;
        int corpseValue = 0;

        foreach (var color in SprLayerHeadPicker.AvailableColors) {
            if (color.Color.Value.ToString() == SprLayerHeadPicker.SelectedColor.ToString())
                headValue = int.Parse(color.Name);
            if (color.Color.Value.ToString() == SprLayerBodyPicker.SelectedColor.ToString())
                bodyValue = int.Parse(color.Name);
            if (color.Color.Value.ToString() == SprLayerLegsPicker.SelectedColor.ToString())
                legsValue = int.Parse(color.Name);
            if (color.Color.Value.ToString() == SprLayerFeetPicker.SelectedColor.ToString())
                feetValue = int.Parse(color.Name);
        }

        string xml = $"<look type=\"{typeValue}\" head=\"{headValue}\" body=\"{bodyValue}\" legs=\"{legsValue}\" feet=\"{feetValue}\" corpse=\"{corpseValue}\"/>";
        Dispatcher.Invoke(() => {
            ClipboardManager.CopyText(xml, "xml", StatusBar);
        });
    }

    protected void Colorize(System.Drawing.Bitmap imageTemplate, System.Drawing.Bitmap imageOutfit, Color head, Color body, Color legs, Color feet) {
        for (int i = 0; i < imageTemplate.Height; i++) {
            for (int j = 0; j < imageTemplate.Width; j++) {
                System.Drawing.Color templatePixel = imageTemplate.GetPixel(j, i);
                System.Drawing.Color outfitPixel = imageOutfit.GetPixel(j, i);

                if (templatePixel == outfitPixel)
                    continue;

                int rt = templatePixel.R;
                int gt = templatePixel.G;
                int bt = templatePixel.B;
                int ro = outfitPixel.R;
                int go = outfitPixel.G;
                int bo = outfitPixel.B;

                if (rt > 0 && gt > 0 && bt == 0) // yellow == head
                {
                    ColorizePixel(ref ro, ref go, ref bo, head);
                } else if (rt > 0 && gt == 0 && bt == 0) // red == body
                  {
                    ColorizePixel(ref ro, ref go, ref bo, body);
                } else if (rt == 0 && gt > 0 && bt == 0) // green == legs
                  {
                    ColorizePixel(ref ro, ref go, ref bo, legs);
                } else if (rt == 0 && gt == 0 && bt > 0) // blue == feet
                  {
                    ColorizePixel(ref ro, ref go, ref bo, feet);
                } else {
                    continue; // if nothing changed, skip the change of pixel
                }

                imageOutfit.SetPixel(j, i, System.Drawing.Color.FromArgb(ro, go, bo));
            }
        }
    }

    protected void ColorizePixel(ref int r, ref int g, ref int b, Color colorPart) {
        r = (r + colorPart.R) / 2;
        g = (g + colorPart.G) / 2;
        b = (b + colorPart.B) / 2;
    }

    protected virtual void InternalUpdateThingPreview() { }

    protected void SprFramesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        if (isUpdatingFrame)
            return;

        InternalUpdateThingPreview();
    }

    protected void SprOutfitChanged(object sender, RoutedEventArgs e) {
        ForceSliderChange();
    }

    protected void SprLayerPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
        if (isPageLoaded)
            ForceSliderChange();
    }

    protected void Img_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        Image img = e.Source as Image;
        SprListView.SelectedIndex = int.Parse((string)img.ToolTip);
        ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(SprListView);
        scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
    }

    protected virtual void Spr_Drop(object sender, DragEventArgs e) { }

    // LegacyDatEditor values prior to merge:
    // existingImage.Width = 32;
    // existingImage.Height = 32;
    protected void SetImageInGrid(Grid grid, int gridWidth, int gridHeight, BitmapImage image, int id, int spriteId, int index) {
        // Get the row and column of the cell based on the ID number
        int row = (id - 1) / gridHeight;
        int col = (id - 1) % gridHeight;

        // Get the existing Image control in the cell, or create a new one if it doesn't exist
        Image existingImage = null;
        foreach (UIElement element in grid.Children) {
            if (Grid.GetRow(element) == row && Grid.GetColumn(element) == col && element is Image) {
                existingImage = element as Image;
                break;
            }
        }
        if (existingImage == null) {
            existingImage = new Image();
            existingImage.Width = image.Width;
            existingImage.Height = image.Height;
            AllowDrop = true;
            Grid.SetRow(existingImage, row);
            Grid.SetColumn(existingImage, col);
            grid.Children.Add(existingImage);
        }
        existingImage.MouseLeftButtonDown += Img_PreviewMouseLeftButtonDown;
        existingImage.Drop += Spr_Drop;
        existingImage.ToolTip = spriteId.ToString();
        existingImage.Tag = index;
        // Set the Source property of the Image control to the specified Image
        existingImage.Source = image;
        RenderOptions.SetBitmapScalingMode(existingImage, BitmapScalingMode.NearestNeighbor);
    }

    protected void SprGroupSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        ChangeGroupType((int)SprGroupSlider.Value);
    }

    protected void ChangeDirection(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        Button _dir = (Button)sender;

        CurrentSprDir = int.Parse(_dir.Uid);
        InternalUpdateThingPreview();
        ButtonProgressAssist.SetIsIndicatorVisible(SprUpArrow, false);
        ButtonProgressAssist.SetIsIndicatorVisible(SprRightArrow, false);
        ButtonProgressAssist.SetIsIndicatorVisible(SprDownArrow, false);
        ButtonProgressAssist.SetIsIndicatorVisible(SprLeftArrow, false);
        ButtonProgressAssist.SetIsIndicatorVisible(_dir, true);
    }

    protected void SprMount_Click(object sender, RoutedEventArgs e) {
        InternalUpdateThingPreview();
    }

    protected void SprBlendLayer_Click(object sender, RoutedEventArgs e) {
        InternalUpdateThingPreview();
    }

    protected void SprAddonSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        InternalUpdateThingPreview();
    }

    protected virtual void FixSpritesCount() {}

    // helper: get the correct appearance list based on selected index
    public IList<Appearance> GetAppearanceList() {
        return ObjectMenu.SelectedIndex switch {
            0 => MainWindow.appearances.Outfit,
            1 => MainWindow.appearances.Object,
            2 => MainWindow.appearances.Effect,
            3 => MainWindow.appearances.Missile,
            _ => null
        };
    }

    public IList<Appearance> GetAppearanceListByType(int type) {
        return type switch {
            0 => MainWindow.appearances.Outfit,
            1 => MainWindow.appearances.Object,
            2 => MainWindow.appearances.Effect,
            3 => MainWindow.appearances.Missile,
            _ => null
        };
    }

    // to do: binding instead of this
    public ObservableCollection<ShowList> GetThingsFromDropdown() {
        return ObjectMenu.SelectedIndex switch {
            0 => ThingsOutfit,
            1 => ThingsItem,
            2 => ThingsEffect,
            3 => ThingsMissile,
            _ => null
        };
    }

    public ObservableCollection<ShowList> GetThingsByType(APPEARANCE_TYPE thingType) {
        return thingType switch {
            APPEARANCE_TYPE.AppearanceOutfit => ThingsOutfit,
            APPEARANCE_TYPE.AppearanceObject => ThingsItem,
            APPEARANCE_TYPE.AppearanceEffect => ThingsEffect,
            APPEARANCE_TYPE.AppearanceMissile => ThingsMissile,
            _ => null
        };
    }

    // helper: set correct Things list
    protected void UpdateThingsList() {
        switch (ObjectMenu.SelectedIndex) {
            case 0:
                ThingsOutfit = new ObservableCollection<ShowList>(ThingsOutfit.OrderBy(i => i.Id));
                break;
            case 1:
                ThingsItem = new ObservableCollection<ShowList>(ThingsItem.OrderBy(i => i.Id));
                break;
            case 2:
                ThingsEffect = new ObservableCollection<ShowList>(ThingsEffect.OrderBy(i => i.Id));
                break;
            case 3:
                ThingsMissile = new ObservableCollection<ShowList>(ThingsMissile.OrderBy(i => i.Id));
                break;
        }
    }

    protected void CopyObjectFlags(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        CurrentFlags = CurrentObjectAppearance.Flags.Clone();
        StatusBar.MessageQueue?.Enqueue($"Copied Current Object Flags.", null, null, null, false, true, TimeSpan.FromSeconds(2));
    }
    protected void PasteObjectFlags(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (CurrentFlags != null) {
            CurrentObjectAppearance.Flags = CurrentFlags.Clone();
            LoadCurrentObjectAppearances();
            StatusBar.MessageQueue?.Enqueue($"Pasted Object Flags.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        } else
            StatusBar.MessageQueue?.Enqueue($"Copy Flags First.", null, null, null, false, true, TimeSpan.FromSeconds(2));
    }

    protected void Exit_Click(object sender, RoutedEventArgs e) {
        base.OnClosed(e);
        Application.Current.Shutdown();
    }

    protected void ObjectClone_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        List<ShowList> selectedItems = [.. ObjListView.SelectedItems.Cast<ShowList>()];
        if (selectedItems.Count != 0) {
            ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;

            var list = GetAppearanceList();
            var things = GetThingsFromDropdown();
            if (list == null || things == null) {
                StatusBar.MessageQueue?.Enqueue($"Failed to duplicate objects - invalid list type.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                return;
            }

            foreach (var item in selectedItems) {
                Appearance NewObject = new();
                NewObject = list.FirstOrDefault(o => o.Id == item.Id).Clone();
                NewObject.Id = (uint)list[^1].Id + 1;
                list.Add(NewObject);
                things.Add(new ShowList() { Id = NewObject.Id });
            }

            ObjListView.SelectedItem = ObjListView.Items[^1];

            // scroll to the duplicated item
            base.Dispatcher.BeginInvoke(new Action(() =>
            {
                ObjListView.ScrollIntoView(ObjListView.Items[^1]);
            }), System.Windows.Threading.DispatcherPriority.Background);

            StatusBar.MessageQueue?.Enqueue($"Successfully duplicated {selectedItems.Count} {(selectedItems.Count == 1 ? "object" : "objects")}.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
    }

    protected void SprDefaultPhase_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation?.DefaultStartPhase = (uint)SprDefaultPhase.Value;
    }

    protected void SprLoopCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation?.LoopCount = (uint)SprLoopCount.Value;
    }
}
