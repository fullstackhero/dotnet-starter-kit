using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Shared;
using MudBlazor.Services;

namespace FSH.Starter.Blazor.Client.Components.EntityTable;

public partial class ImportModal
{
    [Parameter]
    public string? ModelName { get; set; } = string.Empty;

    [Parameter]
    public Func<Task>? OnInitializedFunc { get; set; }

    [Parameter]
    public FileUploadCommand RequestModel { get; set; } = new();
    public bool IsUpdate { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<FileUploadCommand, bool, Task> ImportFunc { get; set; } = default!;

    public string? SuccessMessage { get; set; }
    private FshValidation? _customValidation;
    
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = default!;

    private IBrowserFile? _file;
    private bool _uploading;
    
    protected override Task OnInitializedAsync() =>
        OnInitializedFunc is not null
            ? OnInitializedFunc()
            : Task.CompletedTask;

    
    private async Task SaveAsync()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        
        _uploading = true;
        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => ImportFunc(RequestModel, IsUpdate), Toast, _customValidation, SuccessMessage))
        {
            _uploading = false;
            MudDialog.Close();
        }
 
        _uploading = false;

        stopwatch.Stop();
        TimeSpan ts = stopwatch.Elapsed;
        Toast.Add($"Processing time is about {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");

    }

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        _file = e.File;
        if (_file is not null)
        {
            if (_file.Size >= AppConstants.MaxExcelFileSize)
            {
                Toast.Add("File have size too big !", Severity.Error);
                _file = null;
                return;
            }

            string? extension = Path.GetExtension(_file.Name);
            if (!AppConstants.SupportedExcelFormats.Contains(extension.ToLower(CultureInfo.CurrentCulture)))
            {
                Toast.Add(@"File Format Not Supported.", Severity.Error);
                return;
            }

            byte[]? buffer = new byte[_file.Size];
            await _file.OpenReadStream(_file.Size).ReadAsync(buffer);
            string? base64String = $"data:{AppConstants.StandardExcelFormat};base64,{Convert.ToBase64String(buffer)}";

            RequestModel = new FileUploadCommand
            {
                Name = _file.Name,
                Extension = extension,
                Data = base64String,
            };
        }
    }
}
