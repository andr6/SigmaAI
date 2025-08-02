using Sigma.Core.Domain.Interface;
using Sigma.Core.Domain.Model;
using Sigma.Core.Repositories;
using Microsoft.KernelMemory;
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Diagnostics;

namespace Sigma.Core.Domain.Service
{
    public class ImportKMSService(
        IKMService _kMService,
        IKmsDetails_Repositories _kmsDetails_Repositories,
        IKmss_Repositories _kmss_Repositories,
        ILogger<ImportKMSService> logger
        ) : IImportKMSService
    {
        private static readonly Counter ImportCounter = Metrics.CreateCounter(
            "kms_import_total", "Total KMS import tasks", new CounterConfiguration { LabelNames = ["status"] });

        private static readonly Histogram ImportDuration = Metrics.CreateHistogram(
            "kms_import_duration_seconds", "Duration of KMS import tasks", new HistogramConfiguration { LabelNames = ["status"] });

        public async Task ImportKMSTask(ImportKMSTaskReq req)
        {
            var status = "success";
            var sw = Stopwatch.StartNew();
            try
            {
                var km = _kmss_Repositories.GetFirst(p => p.Id == req.KmsId);

                var _memory = _kMService.GetMemoryByKMS(km.Id);
                string fileid = req.KmsDetail.Id;
                switch (req.ImportType)
                {
                    case ImportType.File:
                        // import file
                        {
                            var importResult = await _memory.ImportDocumentAsync(new Document(fileid.ToString()).AddFile(req.FilePath).AddTag("kmsid", req.KmsId.ToString()), index: "kms");
                            // query document count
                            var docTextList = await _kMService.GetDocumentByFileID(km.Id, fileid.ToString());
                            string fileGuidName = Path.GetFileName(req.FilePath);
                            req.KmsDetail.FileName = req.FileName;
                            req.KmsDetail.FileGuidName = fileGuidName;
                            req.KmsDetail.DataCount = docTextList.Count;
                        }
                        break;

                    case ImportType.Url:
                        {
                            // import URL
                            var importResult = await _memory.ImportWebPageAsync(req.Url, fileid.ToString(), new TagCollection() { { "kmsid", req.KmsId.ToString() } }, index: "kms");
                            // query document count
                            var docTextList = await _kMService.GetDocumentByFileID(km.Id, fileid.ToString());
                            req.KmsDetail.Url = req.Url;
                            req.KmsDetail.DataCount = docTextList.Count;
                        }
                        break;

                    case ImportType.Text:
                        // import text
                        {
                            var importResult = await _memory.ImportTextAsync(req.Text, fileid.ToString(), new TagCollection() { { "kmsid", req.KmsId.ToString() } }, index: "kms");
                            // query document count
                            var docTextList = await _kMService.GetDocumentByFileID(km.Id, fileid.ToString());
                            req.KmsDetail.Url = req.Url;
                            req.KmsDetail.DataCount = docTextList.Count;
                        }
                        break;
                }
                req.KmsDetail.Status = Model.Enum.ImportKmsStatus.Success;
                _kmsDetails_Repositories.Update(req.KmsDetail);
                //_kmsDetails_Repositories.GetList(p => p.KmsId == req.KmsId);
                Console.WriteLine("Background import succeeded:" + req.KmsDetail.DataCount);
            }
            catch (Exception ex)
            {
                status = "fail";
                req.KmsDetail.Status = Model.Enum.ImportKmsStatus.Fail;
                _kmsDetails_Repositories.Update(req.KmsDetail);
                logger.LogError(ex, "An exception waas thrown.");
                throw;
            }
            finally
            {
                sw.Stop();
                ImportCounter.WithLabels(status).Inc();
                ImportDuration.WithLabels(status).Observe(sw.Elapsed.TotalSeconds);
            }
        }
    }
}
