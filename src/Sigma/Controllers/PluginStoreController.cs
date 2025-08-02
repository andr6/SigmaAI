using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Sigma.Core.Repositories;

namespace Sigma.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PluginStoreController : ControllerBase
    {
        private readonly IPluginRepository _pluginRepository;

        public PluginStoreController(IPluginRepository pluginRepository)
        {
            _pluginRepository = pluginRepository;
        }

        /// <summary>
        /// Upload a plugin package to the server and register its metadata.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? description)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var pluginsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            if (!Directory.Exists(pluginsFolder))
            {
                Directory.CreateDirectory(pluginsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(pluginsFolder, fileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var metadata = new PluginMetadata
            {
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                Description = description,
                FilePath = filePath
            };
            _pluginRepository.RegisterPlugin(metadata);

            return Ok(metadata);
        }

        /// <summary>
        /// Download a previously uploaded plugin package by name.
        /// </summary>
        [HttpGet("{name}")]
        public IActionResult Download(string name)
        {
            var meta = _pluginRepository.GetPlugin(name);
            if (meta == null || !System.IO.File.Exists(meta.FilePath))
            {
                return NotFound();
            }

            var fileBytes = System.IO.File.ReadAllBytes(meta.FilePath);
            var downloadName = Path.GetFileName(meta.FilePath);
            return File(fileBytes, "application/octet-stream", downloadName);
        }

        /// <summary>
        /// Get list of registered plugins.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            var plugins = _pluginRepository.GetPlugins();
            return Ok(plugins);
        }
    }
}
