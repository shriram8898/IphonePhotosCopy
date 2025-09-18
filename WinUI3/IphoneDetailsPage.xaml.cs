using MediaDevices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using static Vanara.PInvoke.SetupAPI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI3;


public class ErrorDetails
{
    public string Message { get; set; }
    public string FileName { get; set; }
    // Add this property to your App class if it does not already exist:
    public Window _window { get; private set; }
    public ErrorDetails()
    {

    }
}
public sealed partial class IphoneDetailsPage : UserControl
{
    ObservableCollection<MediaFileItem> MediaFiles = new ObservableCollection<MediaFileItem>();
    ObservableCollection<ErrorDetails> ErrorDetailCollection = new ObservableCollection<ErrorDetails>();
    string _FolderPath = "";
    public IphoneDetailsPage()
    {
        InitializeComponent();
    }

    private async void CopyFolderPath_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();        
        if (folder != null)
        {
            _FolderPath = folder.Path;
        }
    }

    private async void SearchIcon_Click(object sender, RoutedEventArgs e)
    {
        if(string.IsNullOrWhiteSpace(_FolderPath))
        {
            ContentDialog noWifiDialog = new ContentDialog
            {
                Title = "Error",
                Content = "Please enter a valid folder path.",
                CloseButtonText = "Ok",
                XamlRoot=this.Content.XamlRoot
            };
            await noWifiDialog.ShowAsync();
            return;
        }
        var devices = MediaDevice.GetDevices();
        var iphoneDevice = devices.FirstOrDefault(x => x.FriendlyName == "Apple iPhone");
        if(iphoneDevice != null)
        {
            try
            {
                iphoneDevice.Connect();
                // Use the root path "/" for iOS devices
                var rootDirs = iphoneDevice.GetDirectories("Internal Storage");
                await Task.Run(async() =>
                {
                    if (rootDirs?.Length > 0)
                    {
                        for (int i = 0; i < rootDirs.Length; i++)
                        {
                            UpdateFolderProgress(i, rootDirs.Length -1);
                            var rootDir = rootDirs[i];
                            try
                            {
                                var filesPath = iphoneDevice.GetFiles(rootDir);
                                if (filesPath?.Length > 0)
                                {
                                    foreach (var filePath in filesPath)
                                    {
                                        UpdateFileProgress(filesPath.ToList().IndexOf(filePath), filesPath.Length - 1);
                                        try
                                        {
                                            MediaFileInfo fileInfo = iphoneDevice.GetFileInfo(filePath);
                                            if (fileInfo != null)
                                            {
                                                Directory.CreateDirectory(_FolderPath);

                                                // Build destination path using original filename
                                                string destPath = Path.Combine(_FolderPath, fileInfo.Name);
                                                if(File.Exists(destPath))
                                                {
                                                    continue;
                                                }
                                                using (var sourceStream = iphoneDevice.OpenReadFromPersistentUniqueId(fileInfo.PersistentUniqueId))
                                                using (var destStream = File.Create(destPath))
                                                {
                                                    await sourceStream.CopyToAsync(destStream);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                           AddErrorDetail(new ErrorDetails { FileName = filePath, Message = ex.Message });
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) 
                            {
                                AddErrorDetail(new ErrorDetails { FileName = rootDir, Message = ex.Message });
                            }

                        }
                    }
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                iphoneDevice.Disconnect();
            }
        }
        ContentDialog dialog = new ContentDialog
        {
            Title = "Action completed",
            Content = "Copying of files is completed",
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void AddErrorDetail(ErrorDetails errorDetails)
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
           ErrorDetailCollection.Add(errorDetails);    
        });
    }

    private void UpdateFolderProgress(int i, int length)
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            FoldersInfoTb.Text = $"{i}/{length} folders processing";
            FoldersProgressBar.Value = ((double)i / (double)length) * 100;
        });

    }
    private void UpdateFileProgress(int i, int length)
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            FilesInfoTb.Text = $"{i}/{length} files processing";
            FilesProgressBar.Value = ((double)i / (double)length) * 100;
        });

    }
    Window _MainWindowInstance;
    internal void SetMainWindow(Window mainWindow)
    {
        _MainWindowInstance = mainWindow;
    }
}
