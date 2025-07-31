﻿using AntDesign;
using AntDesign.ProLayout;
using Sigma.Core.Domain.Model.Enum;
using Sigma.Core.Options;
using Sigma.Core.Repositories;
using Sigma.Core.Utils;
using Downloader;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace Sigma.Components.Pages.Setting.AIModel
{
    public partial class AddModel
    {
        [Parameter]
        public string ModelId { get; set; }
        [Parameter]
        public string ModelPath { get; set; }
        [Inject] protected IAIModels_Repositories _aimodels_Repositories { get; set; }
        [Inject] protected MessageService? Message { get; set; }
        [Inject] public HttpClient HttpClient { get; set; }
        [Inject] private IOptions<LLamaSharpOption> Options { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

        private AIModels _aiModel = new AIModels();

        private string _downloadUrl;
        private bool _downloadModalVisible;
        private double _downloadProgress;
        private bool _downloadStarted;
        IDownload _download;

        private Modal _modal;

        string[] _modelFiles = Array.Empty<string>();

        IEnumerable<string> _menuKeys = Array.Empty<string>();

        private List<MenuDataItem> menuList = new List<MenuDataItem>();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                if (!string.IsNullOrEmpty(ModelId))
                {
                    _aiModel = _aimodels_Repositories.GetFirst(p => p.Id == ModelId);
                }
                //目前只支持gguf的 所以筛选一下
                _modelFiles = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), Options.Value.FileDirectory)).Where(p => p.Contains(".gguf")).ToArray();
                if (!string.IsNullOrEmpty(ModelPath))
                {
                    //下载页跳入
                    _aiModel.AIType = AIType.LLamaSharp;
                    _downloadModalVisible = true;

                    _downloadUrl = $"https://hf-mirror.com{ModelPath.Replace("---", "/")}";
                }
            }
            catch
            {
                _ = Message.Error("LLamaSharp.FileDirectory目录配置不正确！", 2);
            }
        }

        private void HandleSubmit()
        {
            if (_aiModel.AIType.IsNull())
            {
                _ = Message.Error("AI类型必须选择", 2);
                return;
            }
            if (!_aiModel.IsChat && !_aiModel.IsEmbedding)
            {
                _ = Message.Error("模型类型必须选择", 2);
                return;
            }
            if (_aimodels_Repositories.IsAny(p => p.Id != _aiModel.Id.ConvertToString() && p.EndPoint == _aiModel.EndPoint.ConvertToString() && p.ModelKey == _aiModel.ModelKey && p.ModelName == _aiModel.ModelName))
            {
                _ = Message.Error("模型已存在！", 2);
                return;
            }
            if (string.IsNullOrEmpty(ModelId))
            {
                //新增
                _aiModel.Id = Guid.NewGuid().ToString();

                if (_aimodels_Repositories.IsAny(p => p.ModelDescription == _aiModel.ModelDescription))
                {
                    _ = Message.Error("模型描述已存在！", 2);
                    return;
                }
                _aimodels_Repositories.Insert(_aiModel);
            }
            else
            {
                _aimodels_Repositories.Update(_aiModel);
            }

            Back();
        }

        private void Back()
        {
            NavigationManager.NavigateTo("/setting/modellist");
        }

        private async Task StartDownload()
        {
            if (string.IsNullOrWhiteSpace(_downloadUrl))
            {
                return;
            }

            _download = DownloadBuilder.New()
            .WithUrl(_downloadUrl)
            .WithDirectory(Path.Combine(Directory.GetCurrentDirectory(), Options.Value.FileDirectory))
            .WithConfiguration(new DownloadConfiguration()
            {
                ParallelCount = 5,
            })
            .Build();

            _download.DownloadProgressChanged += DownloadProgressChanged;
            _download.DownloadFileCompleted += DownloadFileCompleted;
            _download.DownloadStarted += DownloadStarted;

            await _download.StartAsync();

            //download.Stop(); // cancel current download
        }

        private void DownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
        {
            _downloadProgress = Math.Round(e.ProgressPercentage, 2);
            InvokeAsync(StateHasChanged);
        }

        private void DownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            _aiModel.ModelName = _download.Package.FileName;
            _downloadModalVisible = false;
            _downloadStarted = false;
            _modelFiles = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), Options.Value.FileDirectory));
            InvokeAsync(StateHasChanged);
        }

        private void DownloadStarted(object? sender, DownloadStartedEventArgs e)
        {
            _downloadStarted = true;
            InvokeAsync(StateHasChanged);
        }

        private void OnCancel()
        {
            if (_downloadStarted)
            {
                return;
            }

            _downloadModalVisible = false;
        }

        private void Stop()
        {
            _downloadStarted = false;
            _download?.Stop();
            InvokeAsync(StateHasChanged);
        }
    }
}
